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
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Workers
{
    public record HexCoordinate(int X, int Y);
    public class UnitDeploymentWorker
    {
        private readonly IUnitDeploymentRepository _deployRepo;
        private readonly ICityRepository _cityRepo;
        private readonly CombatService _combatService;
        private readonly IBattleReportRepository _reportRepo;
        private readonly IResourceService _resService;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly ICityStatService _statService;
        private readonly IModifierService _modifierService;
        private readonly UnitDataReader _unitData;
        private readonly ILogger<UnitDeploymentWorker> _logger;
        private readonly IWorldMapObjectRepository _worldMapObjectRepo;

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
            IWorldMapObjectRepository worldMapObjectRepo,
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
            _worldMapObjectRepo = worldMapObjectRepo;
            _logger = logger;
        }

        public async Task ProcessMilitaryMovementsAsync()
        {
            var activeDeployments = await _deployRepo.GetActiveDeploymentsAsync();
            var dueMovements = activeDeployments
                .Where(d => d.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Moving
                         && d.ArrivalTime <= DateTime.UtcNow)
                .ToList();

            if (!dueMovements.Any()) return;



            foreach (var movement in dueMovements)
            {
                var worldGameObject = await _worldMapObjectRepo.GetWorldMapObjectByReferenceIdAsync(movement.Id);

                try
                {
                    movement.CurrentX = movement.NextX;
                    movement.CurrentY = movement.NextY;
                    movement.LastStepTime = DateTime.UtcNow;

                    if (worldGameObject != null)
                    {
                        worldGameObject.X = (short)movement.NextX;
                        worldGameObject.Y = (short)movement.NextY;

                        await _worldMapObjectRepo.UpdateAsync(worldGameObject);
                    }

                    // 2. Tjek for interaktion på den nye hex (Byer/Fjender)
                    var cityInHex = await _cityRepo.GetByCoordinatesAsync(movement.CurrentX, movement.CurrentY);

                    if (cityInHex != null)
                    {
                        bool isOwnCity = cityInHex.WorldPlayerId == movement.WorldPlayerId;

                        if (isOwnCity)
                        {
                            await ResolveStationing(movement, cityInHex);
                            continue;
                        }

                        else
                        {
                            bool unitDestroyed = await HandleInstantCombat(movement, cityInHex);
                            if (unitDestroyed)
                            {
                                await _worldMapObjectRepo.DeleteByReferenceIdAsync(movement.Id);
                                continue;
                            }
                        }
                    }

                    // 3. Planlæg næste skridt eller stop marchen
                    await UpdateMovementPath(movement);

                    await _deployRepo.UpdateAsync(movement);

                    _logger.LogInformation($"Unit {movement.Id} moved to {movement.CurrentX},{movement.CurrentY}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Fejl ved processering af UnitDeployment: {movement.Id}");
                }
            }
        }

        private async Task<bool> HandleInstantCombat(UnitDeployment attacker, City targetCity)
        {
            _logger.LogWarning($"Instant kamp simuleret mod {targetCity.Name}");
            return false;
        }

        private async Task UpdateMovementPath(UnitDeployment deployment)
        {
            // Er vi nået til den endelige destination?
            if (deployment.CurrentX == deployment.FinalX && deployment.CurrentY == deployment.FinalY)
            {
                deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                deployment.RemainingPathJson = null;
                return;
            }

            // Ellers skal vi finde næste hop i ruten
            if (!string.IsNullOrEmpty(deployment.RemainingPathJson))
            {
                var path = JsonSerializer.Deserialize<List<HexCoordinate>>(deployment.RemainingPathJson);
                
                if (path != null && path.Any())
                {
                    var nextStep = path.First();
                    path.RemoveAt(0);

                    deployment.NextX = nextStep.X;
                    deployment.NextY = nextStep.Y;
                    deployment.RemainingPathJson = JsonSerializer.Serialize(path);
                    
                    // Beregn ankomsttid for næste hex (f.eks. 10 sekunder)
                    // Her kan du gange med modifiers for terræn/hastighed
                    deployment.ArrivalTime = DateTime.UtcNow.AddSeconds(10); 
                }
                else
                {
                    // Stien var tom, men vi er ikke ved FinalX/Y - vi må stoppe her
                    deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                }
            }

            await _deployRepo.UpdateAsync(deployment);
        }

        private async Task ResolveStationing(UnitDeployment dep, City city)
        {
            var stationedCity = await _cityRepo.GetByIdAsync(city.Id);

            if (stationedCity != null)
            {
                double capacity = _statService.GetWarehouseCapacity(stationedCity);

                stationedCity.Wood = Math.Min(capacity, stationedCity.Wood + dep.LootWood);
                stationedCity.Stone = Math.Min(capacity, stationedCity.Stone + dep.LootStone);
                stationedCity.Metal = Math.Min(capacity, stationedCity.Metal + dep.LootMetal);

                // Her skal du også merge UnitStacks ind i byens stacks hvis nødvendigt
                // (Logik udeladt for korthed)

                await _cityRepo.UpdateAsync(stationedCity);

                // VIGTIGT: Fjern hæren fra kortet og slet deployment-entiteten
                await _worldMapObjectRepo.DeleteByReferenceIdAsync(dep.Id);
                await _deployRepo.DeleteAsync(dep);

                _logger.LogInformation($"Unit {dep.Id} stationed in city {city.Name} and removed from map.");
            }
        }


        private async Task GenerateReport(City target, List<UnitDeployment> attackers, CombatResult res, bool win, double w, double s, double m)
        {
            string lootTxt = win ? $"\nLOOT: Wood: {Math.Floor(w)}, Stone: {Math.Floor(s)}, Metal: {Math.Floor(m)}" : "";
            string body = $"--- BATTLE REPORT: {target.Name} ---\n" +
                          $"Result: {(win ? "VICTORY" : "DEFEAT")}" +
                          lootTxt +
                          $"\nLosses: Attackers: {res.AttackerLosses.Sum(l => l.Quantity)} | Defenders: {res.DefenderLosses.Sum(l => l.Quantity)}";

            var origin = await _cityRepo.GetByIdAsync(attackers.First().WorldPlayerId);
            if (origin?.WorldPlayerId != null)
            {
                await _reportRepo.AddAsync(new BattleReport
                {
                    WorldPlayerId = origin.WorldPlayerId.Value,
                    Title = $"Attack on {target.Name}",
                    Body = body,
                    OccurredAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
        }
    }
}