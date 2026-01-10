using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Workers
{
    public class UnitDeploymentWorker
    {
        private readonly IUnitDeploymentRepository _deployRepo;
        private readonly ICityRepository _cityRepo;
        private readonly CombatService _combatService;
        private readonly IBattleReportRepository _reportRepo;
        private readonly IResourceService _resService;
        private readonly ICityStatService _statService;
        private readonly UnitDataReader _unitData;
        private readonly ILogger<UnitDeploymentWorker> _logger;

        public UnitDeploymentWorker(
            IUnitDeploymentRepository deployRepo,
            ICityRepository cityRepo,
            CombatService combatService,
            IBattleReportRepository reportRepo,
            IResourceService resService,
            ICityStatService statService,
            UnitDataReader unitData,
            ILogger<UnitDeploymentWorker> logger)
        {
            _deployRepo = deployRepo;
            _cityRepo = cityRepo;
            _combatService = combatService;
            _reportRepo = reportRepo;
            _resService = resService;
            _statService = statService;
            _unitData = unitData;
            _logger = logger;
        }

        public async Task ProcessMilitaryMovementsAsync()
        {
            var activeDeployments = await _deployRepo.GetActiveDeploymentsAsync();
            var due = activeDeployments.Where(d => d.ArrivalTime <= DateTime.UtcNow).ToList();

            if (!due.Any()) return;

            // 1. Håndtér ankomster (Tropper der lander i en mållokalitet)
            var arrivals = due.Where(d => d.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Arriving)
                              .GroupBy(d => d.TargetCityId);

            foreach (var group in arrivals)
            {
                await ResolveArrivalGroup(group.Key, group.ToList());
            }

            // 2. Håndtér hjemkomster (Tropper der lander i deres oprindelsesby)
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

            // Split gruppen op i Support (venner) og Combat (fjender)
            var supportMissions = incomingGroup.Where(d => d.UnitDeploymentType == UnitDeploymentTypeEnum.Support).ToList();
            var combatMissions = incomingGroup.Where(d => d.UnitDeploymentType == UnitDeploymentTypeEnum.Attack ||
                                                         d.UnitDeploymentType == UnitDeploymentTypeEnum.Conquest).ToList();

            // --- 1. HÅNDTÉR SUPPORT ANKOMST ---
            foreach (var sup in supportMissions)
            {
                sup.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                sup.ArrivalTime = DateTime.MaxValue; // De bliver stående indtil de kaldes hjem
                await _deployRepo.UpdateAsync(sup);
                _logger.LogInformation($"{sup.Quantity}x {sup.UnitType} er nu stationeret som support i {targetCity.Name}");
            }

            // --- 2. HÅNDTÉR KAMP (ATTACK / CONQUEST) ---
            if (combatMissions.Any())
            {
                // Find alle forsvarere: Lokale tropper + Stationed tropper (Support)
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

                // Afvikl slag
                var result = _combatService.ResolveBattle(attackerStacks, defensePool);
                bool attackerWon = result.RemainingAttackers.Any(s => s.Quantity > 0);

                // --- LOOT BEREGNING ---
                double stolenWood = 0, stolenStone = 0, stolenMetal = 0;
                if (attackerWon)
                {
                    int totalCarryCap = result.RemainingAttackers.Sum(s => _unitData.GetUnit(s.Type).LootCapacity * s.Quantity);
                    var defenderRes = _resService.CalculateCurrent(targetCity, DateTime.UtcNow);
                    double availableTotal = defenderRes.Wood + defenderRes.Stone + defenderRes.Metal;
                    double takeRatio = availableTotal > 0 ? Math.Min(1.0, (double)totalCarryCap / availableTotal) : 0;

                    stolenWood = defenderRes.Wood * takeRatio;
                    stolenStone = defenderRes.Stone * takeRatio;
                    stolenMetal = defenderRes.Metal * takeRatio;

                    targetCity.Wood = defenderRes.Wood - stolenWood;
                    targetCity.Stone = defenderRes.Stone - stolenStone;
                    targetCity.Metal = defenderRes.Metal - stolenMetal;
                    targetCity.LastResourceUpdate = DateTime.UtcNow;
                }

                await GenerateReport(targetCity, combatMissions, result, attackerWon, stolenWood, stolenStone, stolenMetal);

                // --- OPDATER FORSVARERES TILSTAND ---
                // Opdater stationerede support-enheder (hvis de døde eller tog skade)
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

                // Opdater byens egne lokale enheder (fjern de typer der var supportet)
                targetCity.UnitStacks = result.RemainingDefenders
                    .Where(r => !stationedDefenders.Any(sd => sd.UnitType == r.Type))
                    .ToList();

                await _cityRepo.UpdateAsync(targetCity);

                // --- OPDATER ANGRIBERES TILSTAND ---
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
                        dep.ArrivalTime = DateTime.UtcNow.AddSeconds(30); // Hjemrejse
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
                var currentRes = _resService.CalculateCurrent(homeCity, DateTime.UtcNow);
                double cap = _statService.GetWarehouseCapacity(homeCity);

                homeCity.Wood = Math.Min(cap, currentRes.Wood + dep.LootWood);
                homeCity.Stone = Math.Min(cap, currentRes.Stone + dep.LootStone);
                homeCity.Metal = Math.Min(cap, currentRes.Metal + dep.LootMetal);
                homeCity.LastResourceUpdate = DateTime.UtcNow;

                var stack = homeCity.UnitStacks.FirstOrDefault(s => s.Type == dep.UnitType);
                if (stack != null) stack.Quantity += dep.Quantity;
                else homeCity.UnitStacks.Add(new UnitStack { Type = dep.UnitType, Quantity = dep.Quantity, CityId = homeCity.Id });

                await _cityRepo.UpdateAsync(homeCity);
                _logger.LogInformation($"{dep.Quantity}x {dep.UnitType} er vendt hjem til {homeCity.Name} med bytte.");
            }
            await _deployRepo.DeleteAsync(dep);
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