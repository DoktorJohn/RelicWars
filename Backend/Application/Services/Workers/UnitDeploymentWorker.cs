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
        private readonly IUnitDeploymentRepository _unitDeploymentRepo;
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
            IUnitDeploymentRepository unitDeploymentRepo,
            IWorldMapObjectRepository worldMapObjectRepo,
            ILogger<UnitDeploymentWorker> logger)
        {
            _deployRepo = deployRepo;
            _cityRepo = cityRepo;
            _combatService = combatService;
            _reportRepo = reportRepo;
            _resService = resService;
            _worldPlayerService = worldPlayerService;
            _unitDeploymentRepo = unitDeploymentRepo;
            _statService = statService;
            _modifierService = modifierService;
            _unitData = unitData;
            _worldMapObjectRepo = worldMapObjectRepo;
            _logger = logger;
        }

        public async Task ProcessMilitaryMovementsAsync()
        {
            var activeDeployments = await _unitDeploymentRepo.GetActiveDeploymentsAsync();
            var dueMovements = activeDeployments
                .Where(deployment => deployment.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Moving
                                     && deployment.NextStepTime <= DateTime.UtcNow)
                .ToList();

            if (!dueMovements.Any()) return;

            foreach (var deployment in dueMovements)
            {
                try
                {
                    // 1. Opdater nuværende position
                    deployment.CurrentX = deployment.NextX;
                    deployment.CurrentY = deployment.NextY;
                    deployment.LastStepTime = DateTime.UtcNow;

                    var worldMapObject = await _worldMapObjectRepo.GetWorldMapObjectByReferenceIdAsync(deployment.Id);
                    if (worldMapObject != null)
                    {
                        worldMapObject.X = (short)deployment.CurrentX;
                        worldMapObject.Y = (short)deployment.CurrentY;
                        await _worldMapObjectRepo.UpdateAsync(worldMapObject);
                    }

                    // 2. Tjek om vi er landet i en by
                    var cityInHex = await _cityRepo.GetByCoordinatesAsync(deployment.CurrentX, deployment.CurrentY);
                    if (cityInHex != null)
                    {
                        // Hvis vi er i OriginCity, skal enheden opløses og tropperne hjem
                        if (cityInHex.Id == deployment.OriginCityId)
                        {
                            await ResolveUnitReturnToOriginCityAsync(deployment, cityInHex);
                            continue;
                        }

                        // Interaktion med fremmede byer
                        bool isPlayerOwnedCity = cityInHex.WorldPlayerId == deployment.WorldPlayerId;
                        if (!isPlayerOwnedCity)
                        {
                            bool wasUnitDestroyed = await HandleInstantCombatInteractionAsync(deployment, cityInHex);
                            if (wasUnitDestroyed)
                            {
                                await CleanupUnitDeploymentFromWorldAsync(deployment);
                                continue;
                            }
                        }
                    }

                    // 3. Planlæg næste skridt
                    await UpdateMovementPathAndNextStepAsync(deployment);

                    await _unitDeploymentRepo.UpdateAsync(deployment);

                    _logger.LogInformation($"Unit {deployment.Id} processed step to {deployment.CurrentX},{deployment.CurrentY}");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Fejl ved processering af bevægelse for UnitDeployment: {deployment.Id}");
                }
            }
        }

        private async Task ResolveUnitReturnToOriginCityAsync(UnitDeployment unitDeployment, City targetCity)
        {
            var originCity = await _cityRepo.GetByIdAsync(targetCity.Id);
            if (originCity == null) return;

            _logger.LogInformation($"Unit {unitDeployment.Id} returneret til {originCity.Name}. Starter afrustning.");

            // 1. Overfør loot
            double warehouseCapacity = _statService.GetWarehouseCapacity(originCity);
            originCity.Wood = Math.Min(warehouseCapacity, originCity.Wood + unitDeployment.LootWood);
            originCity.Stone = Math.Min(warehouseCapacity, originCity.Stone + unitDeployment.LootStone);
            originCity.Metal = Math.Min(warehouseCapacity, originCity.Metal + unitDeployment.LootMetal);

            // 2. Overfør UnitStacks (Merge)
            foreach (var deploymentStack in unitDeployment.UnitStacks)
            {
                var existingCityStack = originCity.UnitStacks
                    .FirstOrDefault(stack => stack.Type == deploymentStack.Type);

                if (existingCityStack != null)
                {
                    existingCityStack.Quantity += deploymentStack.Quantity;
                }
                else
                {
                    originCity.UnitStacks.Add(new UnitStack
                    {
                        Id = Guid.NewGuid(),
                        Type = deploymentStack.Type,
                        Quantity = deploymentStack.Quantity,
                        WorldPlayerId = originCity.WorldPlayerId,
                        CityId = originCity.Id
                    });
                }
            }

            await _cityRepo.UpdateAsync(originCity);
            await CleanupUnitDeploymentFromWorldAsync(unitDeployment);

            _logger.LogInformation($"Unit {unitDeployment.Id} færdigbehandlet og fjernet fra kortet.");
        }

        private async Task UpdateMovementPathAndNextStepAsync(UnitDeployment unitDeployment)
        {
            if (unitDeployment.CurrentX == unitDeployment.FinalX && unitDeployment.CurrentY == unitDeployment.FinalY)
            {
                unitDeployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                unitDeployment.RemainingPathJson = null;
                unitDeployment.NextX = unitDeployment.CurrentX;
                unitDeployment.NextY = unitDeployment.CurrentY;
                return;
            }

            if (!string.IsNullOrEmpty(unitDeployment.RemainingPathJson))
            {
                var pathSteps = JsonSerializer.Deserialize<List<HexCoordinate>>(unitDeployment.RemainingPathJson);

                if (pathSteps != null && pathSteps.Any())
                {
                    var nextStep = pathSteps.First();
                    pathSteps.RemoveAt(0);

                    unitDeployment.NextX = nextStep.X;
                    unitDeployment.NextY = nextStep.Y;
                    unitDeployment.RemainingPathJson = JsonSerializer.Serialize(pathSteps);

                    double secondsPerHexagon = 7200.0 / Math.Max(1, unitDeployment.Mobility);
                    unitDeployment.NextStepTime = DateTime.UtcNow.AddSeconds(secondsPerHexagon);
                    unitDeployment.ArrivalTime = unitDeployment.NextStepTime.AddSeconds(pathSteps.Count * secondsPerHexagon);
                }
                else
                {
                    unitDeployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                }
            }
            else
            {
                unitDeployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
            }
        }

        private async Task CleanupUnitDeploymentFromWorldAsync(UnitDeployment unitDeployment)
        {
            await _worldMapObjectRepo.DeleteByReferenceIdAsync(unitDeployment.Id);
            await _unitDeploymentRepo.DeleteAsync(unitDeployment);
        }

        private async Task<bool> HandleInstantCombatInteractionAsync(UnitDeployment attacker, City targetCity)
        {
            _logger.LogWarning($"Kamp simuleret for {attacker.Id} mod {targetCity.Name}");
            return false;
        }
    }
}