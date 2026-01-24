using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Workers
{
    /// <summary>
    /// Worker-service der håndterer alle militære bevægelser, kamp-opløsning og ressource-synkronisering ved ankomst.
    /// </summary>
    public class UnitDeploymentWorker
    {
        private readonly IUnitDeploymentRepository _deployRepo;
        private readonly ICityRepository _cityRepo;
        private readonly CombatService _combatService;
        private readonly IBattleReportRepository _reportRepo;
        private readonly IResourceService _resService;
        private readonly IWorldPlayerService _worldPlayerService; // Tilføjet for SoC
        private readonly ICityStatService _statService;
        private readonly IModifierService _modifierService;
        private readonly UnitDataReader _unitData;
        private readonly ILogger<UnitDeploymentWorker> _logger;

        public UnitDeploymentWorker(
            IUnitDeploymentRepository deployRepo,
            ICityRepository cityRepo,
            CombatService combatService,
            IBattleReportRepository reportRepo,
            IResourceService resService,
            IWorldPlayerService worldPlayerService,
            ICityStatService statService,
            IModifierService modifierService,
            UnitDataReader unitData,
            ILogger<UnitDeploymentWorker> logger)
        {
            _deployRepo = deployRepo;
            _cityRepo = cityRepo;
            _combatService = combatService;
            _reportRepo = reportRepo;
            _resService = resService;
            _worldPlayerService = worldPlayerService;
            _statService = statService;
            _modifierService = modifierService;
            _unitData = unitData;
            _logger = logger;
        }

        public async Task ProcessMilitaryMovementsAsync()
        {
            var activeDeployments = await _deployRepo.GetActiveDeploymentsAsync();
            var due = activeDeployments.Where(d => d.ArrivalTime <= DateTime.UtcNow).ToList();

            if (!due.Any()) return;

            var arrivals = due.Where(d => d.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Arriving)
                              .GroupBy(d => d.TargetCityId);

            foreach (var group in arrivals)
            {
                await ResolveArrivalGroup(group.Key ?? Guid.Empty, group.ToList());
            }

            var returnees = due.Where(d => d.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Returning);
            foreach (var dep in returnees)
            {
                await ResolveReturnee(dep);
            }
        }

        private async Task ResolveArrivalGroup(Guid targetCityId, List<UnitDeployment> incomingGroup)
        {
            var targetCity = await _cityRepo.GetByIdAsync(targetCityId);
            if (targetCity == null) return;

            // SoC: Synkroniser byens tilstand inden kampen
            SynkroniserByOgSpillerRessourcerTilNuværendeTidspunkt(targetCity);

            var supportMissions = incomingGroup.Where(d => d.UnitDeploymentType == UnitDeploymentTypeEnum.Support).ToList();
            var combatMissions = incomingGroup.Where(d => d.UnitDeploymentType == UnitDeploymentTypeEnum.Attack ||
                                                         d.UnitDeploymentType == UnitDeploymentTypeEnum.Conquest).ToList();

            foreach (var sup in supportMissions)
            {
                sup.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                sup.ArrivalTime = DateTime.MaxValue;
                await _deployRepo.UpdateAsync(sup);
            }

            if (combatMissions.Any())
            {
                var allDeployments = await _deployRepo.GetActiveDeploymentsAsync();
                var stationedDefenders = allDeployments
                    .Where(d => d.TargetCityId == targetCityId && d.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Stationed)
                    .ToList();

                var defensePool = targetCity.UnitStacks.Select(s => new UnitStack { Type = s.Type, Quantity = s.Quantity }).ToList();
                foreach (var sd in stationedDefenders)
                {
                    defensePool.Add(new UnitStack { Type = sd.UnitType, Quantity = sd.Quantity });
                }

                var attackerStacks = combatMissions.Select(d => new UnitStack { Type = d.UnitType, Quantity = d.Quantity }).ToList();

                var result = _combatService.ResolveBattle(attackerStacks, defensePool);
                bool attackerWon = result.RemainingAttackers.Any(s => s.Quantity > 0);

                double stolenWood = 0, stolenStone = 0, stolenMetal = 0;

                if (attackerWon)
                {
                    var originCityId = combatMissions.First().OriginCityId;
                    var originCity = await _cityRepo.GetByIdAsync(originCityId);

                    var attackerProviders = new List<IModifierProvider>();
                    if (originCity != null)
                    {
                        attackerProviders.Add(originCity);
                        if (originCity.WorldPlayer != null)
                        {
                            attackerProviders.Add(originCity.WorldPlayer);
                            if (originCity.WorldPlayer.Alliance != null)
                                attackerProviders.Add(originCity.WorldPlayer.Alliance);
                        }
                    }

                    var lootCapModifierResult = _modifierService.CalculateEntityValueWithModifiers(
                        1.0,
                        new[] { ModifierTagEnum.LootCapacity },
                        attackerProviders
                    );

                    double totalCarryCap = result.RemainingAttackers.Sum(s =>
                        (_unitData.GetUnit(s.Type).LootCapacity * lootCapModifierResult.FinalValue) * s.Quantity
                    );

                    double availableTotal = targetCity.Wood + targetCity.Stone + targetCity.Metal;
                    double takeRatio = availableTotal > 0 ? Math.Min(1.0, totalCarryCap / availableTotal) : 0;

                    stolenWood = targetCity.Wood * takeRatio;
                    stolenStone = targetCity.Stone * takeRatio;
                    stolenMetal = targetCity.Metal * takeRatio;

                    targetCity.Wood -= stolenWood;
                    targetCity.Stone -= stolenStone;
                    targetCity.Metal -= stolenMetal;
                }

                await GenerateReport(targetCity, combatMissions, result, attackerWon, stolenWood, stolenStone, stolenMetal);

                foreach (var sd in stationedDefenders)
                {
                    var survivor = result.RemainingDefenders.FirstOrDefault(r => r.Type == sd.UnitType);
                    if (survivor == null || survivor.Quantity <= 0)
                        await _deployRepo.DeleteAsync(sd);
                    else
                    {
                        sd.Quantity = survivor.Quantity;
                        await _deployRepo.UpdateAsync(sd);
                    }
                }

                targetCity.UnitStacks = result.RemainingDefenders
                    .Where(r => !stationedDefenders.Any(sd => sd.UnitType == r.Type))
                    .ToList();

                await _cityRepo.UpdateAsync(targetCity);

                foreach (var dep in combatMissions)
                {
                    var survivor = result.RemainingAttackers.FirstOrDefault(s => s.Type == dep.UnitType);
                    if (survivor != null && survivor.Quantity > 0)
                    {
                        double share = (double)survivor.Quantity / result.RemainingAttackers.Sum(a => a.Quantity);
                        dep.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Returning;
                        dep.Quantity = survivor.Quantity;
                        dep.LootWood = stolenWood * share;
                        dep.LootStone = stolenStone * share;
                        dep.LootMetal = stolenMetal * share;
                        dep.ArrivalTime = DateTime.UtcNow.AddSeconds(30);
                        await _deployRepo.UpdateAsync(dep);
                    }
                    else
                    {
                        await _deployRepo.DeleteAsync(dep);
                    }
                }
            }
        }

        private async Task ResolveReturnee(UnitDeployment dep)
        {
            var homeCity = await _cityRepo.GetByIdAsync(dep.OriginCityId);
            if (homeCity != null)
            {
                // SoC: Opdater byens egne ressourcer og spillerens globale økonomi inden vi aflæsser loot
                SynkroniserByOgSpillerRessourcerTilNuværendeTidspunkt(homeCity);

                double cap = _statService.GetWarehouseCapacity(homeCity);

                homeCity.Wood = Math.Min(cap, homeCity.Wood + dep.LootWood);
                homeCity.Stone = Math.Min(cap, homeCity.Stone + dep.LootStone);
                homeCity.Metal = Math.Min(cap, homeCity.Metal + dep.LootMetal);

                var stack = homeCity.UnitStacks.FirstOrDefault(s => s.Type == dep.UnitType);
                if (stack != null) stack.Quantity += dep.Quantity;
                else homeCity.UnitStacks.Add(new UnitStack { Type = dep.UnitType, Quantity = dep.Quantity, CityId = homeCity.Id });

                await _cityRepo.UpdateAsync(homeCity);
            }
            await _deployRepo.DeleteAsync(dep);
        }

        /// <summary>
        /// Udfører den dobbelte synkronisering: Globale spiller-ressourcer via WorldPlayerService 
        /// og lokale by-ressourcer via ResourceService.
        /// </summary>
        private void SynkroniserByOgSpillerRessourcerTilNuværendeTidspunkt(City city)
        {
            var nuværendeTidspunkt = DateTime.UtcNow;

            // 1. GLOBAL OPPDATERING (Silver, Research Points)
            if (city.WorldPlayer != null)
            {
                _worldPlayerService.UpdateGlobalResourceState(city.WorldPlayer, nuværendeTidspunkt);
            }

            // 2. LOKAL OPPDATERING (Wood, Stone, Metal)
            var citySnapshot = _resService.CalculateCityResources(city, nuværendeTidspunkt);

            city.Wood = citySnapshot.Wood;
            city.Stone = citySnapshot.Stone;
            city.Metal = citySnapshot.Metal;
            city.LastResourceUpdate = nuværendeTidspunkt;

            _logger.LogInformation("[UnitDeploymentWorker] Ressourcer synkroniseret for by: {CityName}", city.Name);
        }

        private async Task GenerateReport(City target, List<UnitDeployment> attackers, CombatResult res, bool win, double w, double s, double m)
        {
            string lootTxt = win ? $"\nLOOT: Wood: {Math.Floor(w)}, Stone: {Math.Floor(s)}, Metal: {Math.Floor(m)}" : "";
            string body = $"--- BATTLE REPORT: {target.Name} ---\n" +
                          $"Result: {(win ? "VICTORY" : "DEFEAT")}" +
                          lootTxt +
                          $"\nLosses: Attackers: {res.AttackerLosses.Sum(l => l.Quantity)} | Defenders: {res.DefenderLosses.Sum(l => l.Quantity)}";

            var origin = await _cityRepo.GetByIdAsync(attackers.First().OriginCityId);
            if (origin?.WorldPlayerId != null)
            {
                await _reportRepo.AddAsync(new BattleReport
                {
                    UserId = origin.WorldPlayerId.Value,
                    Title = $"Attack on {target.Name}",
                    Body = body,
                    OccurredAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
        }
    }
}