using Application.DTOs;
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UnitDeploymentService : IUnitDeploymentService
    {
        private readonly ILogger<UnitDeploymentService> _logger;
        private readonly IUnitDeploymentRepository _unitDeploymentRepo;
        private readonly IWorldPlayerRepository _worldPlayerRepo;
        private readonly ICityRepository _cityRepo;
        private readonly IWorldMapObjectRepository _worldMapObjectRepo;
        private readonly UnitDataReader _unitDataReader;

        public UnitDeploymentService(
            ILogger<UnitDeploymentService> logger,
            IUnitDeploymentRepository unitDeploymentRepo,
            IWorldPlayerRepository worldPlayerRepo,
            ICityRepository cityRepo,
            IWorldMapObjectRepository worldMapObjectRepo,
            UnitDataReader unitDataReader)
        {
            _logger = logger;
            _unitDeploymentRepo = unitDeploymentRepo;
            _cityRepo = cityRepo;
            _worldMapObjectRepo = worldMapObjectRepo;
            _worldPlayerRepo = worldPlayerRepo;
            _unitDataReader = unitDataReader;
        }

        public async Task<UnitDeploymentDTO> DeployUnitsAsync(DeployUnitRequestDTO dto)
        {
            var sourceCity = await _cityRepo.GetCityWithBuildingsByCityIdentifierAsync(dto.OriginCityId);
            var worldPlayer = await _worldPlayerRepo.GetByIdAsync(dto.WorldPlayerId);

            if (sourceCity == null || sourceCity.WorldPlayerId != dto.WorldPlayerId)
            {
                throw new InvalidOperationException("Kildebyen findes ikke eller tilhører ikke spilleren.");
            }

            int slowestMobility = int.MaxValue;
            var unitsToMove = new List<UnitStack>();

            foreach (var selection in dto.UnitsToDeploy)
            {
                var staticUnitData = _unitDataReader.GetUnit(selection.Type);
                if (staticUnitData.Mobility < slowestMobility)
                {
                    slowestMobility = staticUnitData.Mobility;
                }

                var cityStack = sourceCity.UnitStacks.FirstOrDefault(s => s.Type == selection.Type);

                if (cityStack == null || cityStack.Quantity < selection.Quantity)
                    throw new InvalidOperationException($"Ikke nok enheder af typen {selection.Type} i byen.");

                cityStack.Quantity -= selection.Quantity;
                if (cityStack.Quantity <= 0) cityStack.IsDeleted = true;

                unitsToMove.Add(new UnitStack
                {
                    Type = selection.Type,
                    Quantity = selection.Quantity,
                    WorldPlayerId = dto.WorldPlayerId
                });
            }

            if (slowestMobility <= 0) slowestMobility = 1;

            double secondsPerHexagon = 7200.0 / slowestMobility;

            Guid? targetCityId = null;
            City? targetCity = null;

            var targetCityWorldObject = await _worldMapObjectRepo.GetCityOnCoordinatesAsync(worldPlayer!.WorldId, (short)dto.TargetX, (short)dto.TargetY);

            if (targetCityWorldObject != null)
            {
                targetCity = await _cityRepo.GetByIdAsync(targetCityWorldObject.ReferenceEntityId ?? Guid.Empty);

                if (targetCity != null)
                {
                    targetCityId = targetCity.Id;
                }
            }

            var deployment = new UnitDeployment
            {
                Id = Guid.NewGuid(),
                Name = $"Retinue of {sourceCity.Name}",
                WorldPlayerId = dto.WorldPlayerId,
                WorldId = worldPlayer!.WorldId,
                CurrentX = sourceCity.X,
                CurrentY = sourceCity.Y,
                FinalX = dto.TargetX,
                FinalY = dto.TargetY,
                Mobility = slowestMobility,
                UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving,
                DepartureTime = DateTime.UtcNow,
                LastStepTime = DateTime.UtcNow,
                UnitStacks = unitsToMove,
                OriginCityId = sourceCity.Id,
                OriginCity = sourceCity,
                TargetCity = targetCity,
                TargetCityId = targetCityId,
            };

            var pathfinder = new HexPathfinder();
            var fullPath = pathfinder.FindPath(deployment.CurrentX, deployment.CurrentY, deployment.FinalX, deployment.FinalY);

            if (fullPath != null && fullPath.Count > 0)
            {
                var firstStep = fullPath[0];
                deployment.NextX = firstStep.X;
                deployment.NextY = firstStep.Y;

                fullPath.RemoveAt(0); 
                deployment.RemainingPathJson = JsonSerializer.Serialize(fullPath);

                deployment.NextStepTime = deployment.LastStepTime.AddSeconds(secondsPerHexagon);

                int totalSteps = fullPath.Count + 1;
                deployment.ArrivalTime = deployment.DepartureTime.AddSeconds(totalSteps * secondsPerHexagon);
            }
            else
            {
                deployment.NextX = deployment.CurrentX;
                deployment.NextY = deployment.CurrentY;
                deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                deployment.NextStepTime = DateTime.UtcNow;
                deployment.ArrivalTime = DateTime.UtcNow;
            }

            var worldMapObject = new WorldMapObject
            {
                WorldId = sourceCity.WorldId,
                X = (short)deployment.CurrentX,
                Y = (short)deployment.CurrentY,
                Type = MapObjectTypeEnum.UnitDeployment,
                ReferenceEntityId = deployment.Id
            };

            await _unitDeploymentRepo.AddAsync(deployment);
            await _cityRepo.UpdateAsync(sourceCity);
            await _worldMapObjectRepo.AddAsync(worldMapObject);

            return MapToDTO(deployment);
        }

        public async Task<UnitDeploymentDTO> MoveUnits(MoveUnitRequestDTO dto)
        {
            var deployment = await _unitDeploymentRepo.GetByIdAsync(dto.UnitDeploymentId);
            if (deployment == null) throw new InvalidOperationException("Hæren blev ikke fundet.");

            // Logik: 2 mobility = 1 time (3600s). Formel: (7200 / Mobility)
            double secondsPerHexagon = 7200.0 / deployment.Mobility;

            bool wasAlreadyMoving = deployment.UnitDeploymentMovementStatus == UnitDeploymentMovementStatusEnum.Moving;

            int startX = wasAlreadyMoving ? deployment.NextX : deployment.CurrentX;
            int startY = wasAlreadyMoving ? deployment.NextY : deployment.CurrentY;

            var pathfinder = new HexPathfinder();
            var newPath = pathfinder.FindPath(startX, startY, dto.TargetX, dto.TargetY);

            if (newPath == null || newPath.Count == 0)
            {
                deployment.FinalX = startX;
                deployment.FinalY = startY;
                deployment.RemainingPathJson = JsonSerializer.Serialize(new List<HexCoordinate>());

                if (deployment.UnitDeploymentMovementStatus != UnitDeploymentMovementStatusEnum.Moving)
                {
                    deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
                    deployment.ArrivalTime = DateTime.UtcNow;
                }
            }
            else
            {
                deployment.FinalX = dto.TargetX;
                deployment.FinalY = dto.TargetY;
                deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving;

                // Hvis den stod stille, skal vi starte bevægelsen med det samme
                if (deployment.NextX == deployment.CurrentX && deployment.NextY == deployment.CurrentY)
                {
                    var firstStep = newPath[0];
                    deployment.NextX = firstStep.X;
                    deployment.NextY = firstStep.Y;
                    deployment.LastStepTime = DateTime.UtcNow;
                    deployment.NextStepTime = deployment.LastStepTime.AddSeconds(secondsPerHexagon);
                    newPath.RemoveAt(0);
                }
                // Hvis den allerede bevægede sig, fortsætter den mod NextX/Y, men ruten efter det ændres.

                deployment.RemainingPathJson = JsonSerializer.Serialize(newPath);

                int totalStepsRemaining = newPath.Count + 1;
                deployment.ArrivalTime = deployment.NextStepTime.AddSeconds(newPath.Count * secondsPerHexagon);
            }

            await _unitDeploymentRepo.UpdateAsync(deployment);
            _logger.LogInformation($"Unit {deployment.Id} march-ordre opdateret mod {dto.TargetX},{dto.TargetY}");

            return MapToDTO(deployment);
        }

        public async Task<UnitDeploymentDTO> AbortMovementAsync(Guid unitDeploymentId)
        {
            var deployment = await _unitDeploymentRepo.GetByIdAsync(unitDeploymentId);
            if (deployment == null) throw new InvalidOperationException("Hæren blev ikke fundet.");

            if (deployment.UnitDeploymentMovementStatus != UnitDeploymentMovementStatusEnum.Moving)
            {
                _logger.LogWarning($"Forsøg på at afbryde bevægelse for hær {unitDeploymentId}, men den står allerede stille.");
                return MapToDTO(deployment);
            }

            _logger.LogInformation($"Afbryder bevægelse for enhed {unitDeploymentId}. Stopper ved koordinat {deployment.CurrentX}, {deployment.CurrentY}");

            deployment.UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Stationed;
            deployment.NextX = deployment.CurrentX;
            deployment.NextY = deployment.CurrentY;
            deployment.FinalX = deployment.CurrentX;
            deployment.FinalY = deployment.CurrentY;

            deployment.RemainingPathJson = JsonSerializer.Serialize(new List<HexCoordinate>());
            deployment.ArrivalTime = DateTime.UtcNow;
            deployment.NextStepTime = DateTime.UtcNow;

            var mapObject = await _worldMapObjectRepo.GetWorldMapObjectByReferenceIdAsync(deployment.Id);
            if (mapObject != null)
            {
                mapObject.X = (short)deployment.CurrentX;
                mapObject.Y = (short)deployment.CurrentY;
                await _worldMapObjectRepo.UpdateAsync(mapObject);
            }

            await _unitDeploymentRepo.UpdateAsync(deployment);

            return MapToDTO(deployment);
        }

        private UnitDeploymentDTO MapToDTO(UnitDeployment deployment)
        {
            CityDTO originCity = new CityDTO(
                deployment.OriginCityId,
                deployment.OriginCity.Name,
                deployment.OriginCity.X,
                deployment.OriginCity.Y,
                deployment.OriginCity.Points);

            CityDTO? targetCity = null;

            if (deployment.TargetCity != null)
            {
                targetCity = new CityDTO(
                    deployment.TargetCityId ?? Guid.Empty,
                    deployment.TargetCity.Name,
                    deployment.TargetCity.X,
                    deployment.TargetCity.Y,
                    deployment.TargetCity.Points
                );
            }

            return new UnitDeploymentDTO(
                deployment.Id,
                deployment.Name,
                deployment.WorldPlayerId,
                deployment.OriginCityId,
                originCity,
                deployment.TargetCityId ?? Guid.Empty,
                targetCity,
                deployment.UnitDeploymentMovementStatus,
                deployment.ArrivalTime,
                deployment.NextStepTime,
                deployment.LastStepTime,
                deployment.CurrentX,
                deployment.CurrentY,
                deployment.NextX,
                deployment.NextY,
                deployment.FinalX,
                deployment.FinalY,
                deployment.Mobility,
                deployment.RemainingPathJson ?? "",
                deployment.UnitStacks.Select(s => new UnitStackDTO(s.Type, s.Quantity)).ToList(),
                deployment.OwnerWorldPlayer?.PlayerProfile?.UserName ?? "Ukendt Spiller"
            );
        }
    }
}