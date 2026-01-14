using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _cityRepo;
        private readonly IResourceService _resService;
        private readonly ICityStatService _statService;
        private readonly BuildingDataReader _buildingData;
        private readonly ILogger<CityService> _logger;

        public CityService(
            ICityRepository cityRepo,
            IResourceService resService,
            ICityStatService statService,
            BuildingDataReader buildingData,
            ILogger<CityService> logger)
        {
            _cityRepo = cityRepo;
            _resService = resService;
            _statService = statService;
            _buildingData = buildingData;
            _logger = logger;
        }

        public async Task<CityControllerGetDetailedCityInformationDTO?> GetDetailedCityInformationByCityIdentifierAsync(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepo.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
            if (cityEntity == null) return null;

            // Recalculate resources to get live values
            var currentResources = _resService.CalculateCurrent(cityEntity, DateTime.UtcNow);

            int maxPop = _statService.GetMaxPopulation(cityEntity);
            int currentPopUsage = _statService.GetCurrentPopulationUsage(cityEntity);
            double warehouseCap = _statService.GetWarehouseCapacity(cityEntity);

            return new CityControllerGetDetailedCityInformationDTO
            {
                CityId = cityEntity.Id,
                CityName = cityEntity.Name,

                CurrentWoodAmount = currentResources.Wood,
                CurrentStoneAmount = currentResources.Stone,
                CurrentMetalAmount = currentResources.Metal,
                CurrentSilverAmount = cityEntity.WorldPlayer?.Silver ?? 0,

                MaxWoodCapacity = warehouseCap,
                MaxStoneCapacity = warehouseCap,
                MaxMetalCapacity = warehouseCap,

                WoodProductionPerHour = currentResources.WoodProductionPerHour,
                StoneProductionPerHour = currentResources.StoneProductionPerHour,
                MetalProductionPerHour = currentResources.MetalProductionPerHour,

                CurrentPopulationUsage = currentPopUsage,
                MaxPopulationCapacity = maxPop,

                BuildingList = cityEntity.Buildings.Select(b => new CityControllerGetDetailedCityInformationBuildingDTO
                {
                    BuildingId = b.Id,
                    BuildingType = b.Type,
                    CurrentLevel = b.Level,
                    IsCurrentlyUpgrading = b.IsUpgrading,
                    UpgradeFinishedAt = b.TimeOfUpgradeFinished,
                    UpgradeStartedAt = b.TimeOfUpgradeStarted
                }).ToList()
            };
        }

        public async Task<CityDetailsDTO?> GetCityOverviewAsync(Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null) return null;

            var currentResources = _resService.CalculateCurrent(cityEntity, DateTime.UtcNow);

            int maxPop = _statService.GetMaxPopulation(cityEntity);
            int currentUsage = _statService.GetCurrentPopulationUsage(cityEntity);
            int availablePop = maxPop - currentUsage;

            var buildingsDto = cityEntity.Buildings.Select(b => new BuildingDTO(
                b.Id, b.Type.ToString(), b.Level, b.TimeOfUpgradeFinished, b.IsUpgrading
            )).ToList();

            var unitsDto = cityEntity.UnitStacks
                .Where(s => s.Quantity > 0)
                .Select(s => new UnitStackDTO(s.Type.ToString(), s.Quantity))
                .ToList();

            return new CityDetailsDTO(
                cityEntity.Id,
                cityEntity.Name,
                cityEntity.Points,
                Math.Floor(currentResources.Wood),
                Math.Floor(currentResources.Stone),
                Math.Floor(currentResources.Metal),
                cityEntity.X,
                cityEntity.Y,
                new PopulationDTO(maxPop, currentUsage, availablePop),
                buildingsDto,
                unitsDto,
                new List<UnitDeploymentDTO>()
            );
        }

        public async Task<List<AvailableBuildingDTO>> GetAvailableBuildingsForSenateAsync(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepo.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
            if (cityEntity == null) return new List<AvailableBuildingDTO>();

            int availablePop = _statService.GetAvailablePopulation(cityEntity, new List<BaseJob>());
            var currentResources = _resService.CalculateCurrent(cityEntity, DateTime.UtcNow);

            var responseList = new List<AvailableBuildingDTO>();
            var allBuildingTypes = Enum.GetValues<BuildingTypeEnum>();

            foreach (var buildingType in allBuildingTypes)
            {
                var existingBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                int currentLevel = existingBuilding?.Level ?? 0;
                int nextLevel = currentLevel + 1;

                var nextLevelConfig = _buildingData.GetConfig<BuildingLevelData>(buildingType, nextLevel);
                if (nextLevelConfig == null) continue;

                bool canAfford = currentResources.Wood >= nextLevelConfig.WoodCost &&
                                 currentResources.Stone >= nextLevelConfig.StoneCost &&
                                 currentResources.Metal >= nextLevelConfig.MetalCost;

                responseList.Add(new AvailableBuildingDTO
                {
                    BuildingType = buildingType,
                    BuildingName = buildingType.ToString(),
                    CurrentLevel = currentLevel,
                    WoodCost = nextLevelConfig.WoodCost,
                    StoneCost = nextLevelConfig.StoneCost,
                    MetalCost = nextLevelConfig.MetalCost,
                    PopulationCost = nextLevelConfig.PopulationCost,
                    ConstructionTimeInSeconds = (int)nextLevelConfig.BuildTime.TotalSeconds,
                    IsCurrentlyUpgrading = existingBuilding?.IsUpgrading ?? false,
                    CanAfford = canAfford,
                    HasPopulationRoom = availablePop >= nextLevelConfig.PopulationCost
                });
            }

            return responseList;
        }

        /// <summary>
        /// Opdaterer byens samlede pointtal baseret på bygningsmassen.
        /// </summary>
        public async Task UpdateCityPointsAsync(Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null) return;

            int calculatedPoints = cityEntity.Buildings.Sum(b => b.Level * 10);

            if (cityEntity.Points != calculatedPoints)
            {
                cityEntity.Points = calculatedPoints;
                await _cityRepo.UpdateAsync(cityEntity);
            }
        }
    }
}