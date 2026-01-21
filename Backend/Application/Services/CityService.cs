using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Abstraction;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;

namespace Application.Services
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _cityRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IResourceService _resourceService;
        private readonly IModifierService _modifierService;
        private readonly ICityStatService _cityStatService;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly UnitDataReader _unitDataReader;
        private readonly ILogger<CityService> _logger;

        public CityService(
            ICityRepository cityRepository,
            IResourceService resourceService,
            IModifierService modifierService,
            ICityStatService cityStatService,
            BuildingDataReader buildingDataReader,
            UnitDataReader unitDataReader,
            IJobRepository jobRepository,
            ILogger<CityService> logger)
        {
            _cityRepository = cityRepository;
            _resourceService = resourceService;
            _cityStatService = cityStatService;
            _buildingDataReader = buildingDataReader;
            _unitDataReader = unitDataReader;
            _jobRepository = jobRepository;
            _modifierService = modifierService;
            _logger = logger;
        }

        public async Task<CityOverviewHUD> GetCityOverviewHUD(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
            if (cityEntity == null || cityEntity.WorldPlayer == null)
            {
                _logger.LogError("[CityService] HUD calculation failed. City or WorldPlayer not found for ID: {CityId}", cityIdentifier);
                throw new KeyNotFoundException($"Byen med ID {cityIdentifier} eller dens ejer blev ikke fundet.");
            }

            var playerEntity = cityEntity.WorldPlayer;
            var currentDateTime = DateTime.UtcNow;

            _logger.LogInformation("[CityService] Generating City Overview HUD for {CityName}", cityEntity.Name);

            foreach (var city in playerEntity.Cities)
            {
                var resourceSnapshot = _resourceService.CalculateCurrent(city, currentDateTime);
                playerEntity.Silver += resourceSnapshot.SilverGeneratedByThisCity;
                playerEntity.ResearchPoints += resourceSnapshot.ResearchGeneratedByThisCity;

                if (city.Id == cityIdentifier)
                {
                    city.Wood = resourceSnapshot.Wood;
                    city.Stone = resourceSnapshot.Stone;
                    city.Metal = resourceSnapshot.Metal;
                }
                city.LastResourceUpdate = currentDateTime;
            }
            await _cityRepository.UpdateRangeAsync(playerEntity.Cities.ToList());

            // 2. Hent aktive arbejdsprocesser (Køer)
            var activeBuildingJobs = await _jobRepository.GetBuildingJobsAsync(cityIdentifier);
            var activeRecruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityIdentifier);

            // 3. Sammensæt den detaljerede HUD DTO
            return new CityOverviewHUD(
                cityEntity.Id,
                cityEntity.Name,
                playerEntity.Silver,
                playerEntity.ResearchPoints,
                CreateResourceOverview(cityEntity, BuildingTypeEnum.TimberCamp, ModifierTagEnum.Wood, cityEntity.Wood),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.StoneQuarry, ModifierTagEnum.Stone, cityEntity.Stone),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.MetalMine, ModifierTagEnum.Metal, cityEntity.Metal),
                CreateProductionBreakdown(cityEntity, BuildingTypeEnum.MarketPlace, new[] { ModifierTagEnum.Silver }),
                CreateProductionBreakdown(cityEntity, BuildingTypeEnum.University, new[] { ModifierTagEnum.Research }),
                CreatePopulationBreakdown(cityEntity, activeBuildingJobs),
                CreateBuildingQueueOverview(activeBuildingJobs),
                CreateBarracksQueueOverview(activeRecruitmentJobs)
            );
        }

        private ResourceOverviewDTO CreateResourceOverview(City cityEntity, BuildingTypeEnum buildingType, ModifierTagEnum resourceTag, double currentStoredAmount)
        {
            return new ResourceOverviewDTO(
                currentStoredAmount,
                _cityStatService.GetWarehouseCapacity(cityEntity),
                CreateProductionBreakdown(cityEntity, buildingType, new[] { resourceTag, ModifierTagEnum.ResourceProduction })
            );
        }

        private ProductionBreakdownDTO CreateProductionBreakdown(City cityEntity, BuildingTypeEnum buildingType, IEnumerable<ModifierTagEnum> targetTags)
        {
            var building = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
            int currentLevel = building?.Level ?? 0;

            // 1. Beregn fundamentet (Base Value) ved at trække data fra de specifikke konfigurations-klasser
            double baseProductionValue = ExtractBaseValueFromLevelData(cityEntity, buildingType, currentLevel);

            // 2. Identificer alle Modifier Providers
            var modifierProviders = new List<IModifierProvider> { cityEntity, cityEntity.WorldPlayer };

            if (cityEntity.WorldPlayer?.Alliance != null)
            {
                modifierProviders.Add(cityEntity.WorldPlayer.Alliance);
            }

            // VIGTIG FIX: Tilføj alle bygningernes konfigurationsdata som providers.
            // Dette sikrer at Marketplace Level 1's "Flat 56 Silver" bliver fundet af ModifierService.
            foreach (var cityBuilding in cityEntity.Buildings.Where(b => b.Level > 0))
            {
                var levelConfig = _buildingDataReader.GetConfig<BuildingLevelData>(cityBuilding.Type, cityBuilding.Level);
                if (levelConfig != null)
                {
                    modifierProviders.Add(levelConfig);
                }
            }

            // 3. Beregn den endelige værdi via ModifierService
            var calculationResult = _modifierService.CalculateEntityValueWithModifiers(baseProductionValue, targetTags, modifierProviders);

            return new ProductionBreakdownDTO(
                baseProductionValue,
                calculationResult.FlatBonus,
                calculationResult.PercentageBonus,
                calculationResult.FinalValue
            );
        }

        private double ExtractBaseValueFromLevelData(City cityEntity, BuildingTypeEnum buildingType, int level)
        {
            if (level <= 0) return 0;

            if (buildingType == BuildingTypeEnum.MarketPlace)
            {
                return (double)_cityStatService.GetMaxPopulation(cityEntity) * 7.0;
            }

            switch (buildingType)
            {
                case BuildingTypeEnum.TimberCamp:
                    return _buildingDataReader.GetConfig<TimberCampLevelData>(buildingType, level)?.ProductionPerHour ?? 0;

                case BuildingTypeEnum.StoneQuarry:
                    return _buildingDataReader.GetConfig<StoneQuarryLevelData>(buildingType, level)?.ProductionPerHour ?? 0;

                case BuildingTypeEnum.MetalMine:
                    return _buildingDataReader.GetConfig<MetalMineLevelData>(buildingType, level)?.ProductionPerHour ?? 0;

                case BuildingTypeEnum.University:
                    return _buildingDataReader.GetConfig<UniversityLevelData>(buildingType, level)?.ProductionPerHour ?? 0;

                default:
                    return 0;
            }
        }

        private PopulationBreakdownDTO CreatePopulationBreakdown(City cityEntity, IEnumerable<BaseJob> activeJobs)
        {
            int maxPopulation = _cityStatService.GetMaxPopulation(cityEntity);

            int buildingPopulationUsage = cityEntity.Buildings.Sum(b =>
                _buildingDataReader.GetConfig<BuildingLevelData>(b.Type, b.Level)?.PopulationCost ?? 0);

            int unitPopulationUsage = cityEntity.UnitStacks.Sum(stack =>
                stack.Quantity * (_unitDataReader.GetUnit(stack.Type)?.PopulationCost ?? 0));

            return new PopulationBreakdownDTO(
                maxPopulation,
                buildingPopulationUsage,
                unitPopulationUsage,
                _cityStatService.GetAvailablePopulation(cityEntity, activeJobs),
                0
            );
        }

        private BuildingQueueOverviewDTO CreateBuildingQueueOverview(List<BuildingJob> buildingJobs)
        {
            var firstJob = buildingJobs.FirstOrDefault();
            return new BuildingQueueOverviewDTO(
                buildingJobs.Any(),
                buildingJobs.Count,
                firstJob?.BuildingType.ToString() ?? "None",
                firstJob?.ExecutionTime
            );
        }

        private BarracksQueueOverviewDTO CreateBarracksQueueOverview(List<RecruitmentJob> recruitmentJobs)
        {
            var firstJob = recruitmentJobs.FirstOrDefault();
            return new BarracksQueueOverviewDTO(
                recruitmentJobs.Any(),
                recruitmentJobs.Sum(j => j.TotalQuantity - j.CompletedQuantity),
                firstJob?.UnitType.ToString() ?? "None",
                recruitmentJobs.LastOrDefault()?.ExecutionTime
            );
        }

        public async Task<CityControllerGetDetailedCityInformationDTO?> GetDetailedCityInformationByCityIdentifierAsync(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
            if (cityEntity == null || cityEntity.WorldPlayer == null) return null;

            var playerEntity = cityEntity.WorldPlayer;
            var currentDateTime = DateTime.UtcNow;

            double totalSilverProductionPerHour = 0;
            double totalResearchProductionPerHour = 0;

            foreach (var city in playerEntity.Cities)
            {
                var snapshot = _resourceService.CalculateCurrent(city, currentDateTime);
                playerEntity.Silver += snapshot.SilverGeneratedByThisCity;
                playerEntity.ResearchPoints += snapshot.ResearchGeneratedByThisCity;
                totalSilverProductionPerHour += snapshot.SilverProductionPerHourByThisCity;
                totalResearchProductionPerHour += snapshot.ResearchProductionPerHourByThisCity;

                if (city.Id == cityEntity.Id)
                {
                    city.Wood = snapshot.Wood;
                    city.Stone = snapshot.Stone;
                    city.Metal = snapshot.Metal;
                }
                city.LastResourceUpdate = currentDateTime;
            }

            await _cityRepository.UpdateRangeAsync(playerEntity.Cities.ToList());

            return new CityControllerGetDetailedCityInformationDTO
            {
                CityId = cityEntity.Id,
                CityName = cityEntity.Name,
                CurrentWoodAmount = Math.Floor(cityEntity.Wood),
                CurrentStoneAmount = Math.Floor(cityEntity.Stone),
                CurrentMetalAmount = Math.Floor(cityEntity.Metal),
                CurrentSilverAmount = Math.Floor(playerEntity.Silver),
                CurrentResearchPoints = Math.Floor(playerEntity.ResearchPoints),
                MaxWoodCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                MaxStoneCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                MaxMetalCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                WoodProductionPerHour = _resourceService.CalculateCurrent(cityEntity, currentDateTime).WoodProductionPerHour,
                StoneProductionPerHour = _resourceService.CalculateCurrent(cityEntity, currentDateTime).StoneProductionPerHour,
                MetalProductionPerHour = _resourceService.CalculateCurrent(cityEntity, currentDateTime).MetalProductionPerHour,
                SilverProductionPerHour = totalSilverProductionPerHour,
                ResearchPointsPerHour = totalResearchProductionPerHour,
                CurrentPopulationUsage = _cityStatService.GetCurrentPopulationUsage(cityEntity),
                MaxPopulationCapacity = _cityStatService.GetMaxPopulation(cityEntity),
                BuildingList = cityEntity.Buildings.Select(b => new CityControllerGetDetailedCityInformationBuildingDTO
                {
                    BuildingType = b.Type,
                    CurrentLevel = b.Level
                }).ToList()
            };
        }

        public async Task<List<AvailableBuildingDTO>> GetAvailableBuildingsForTownHallAsync(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
            if (cityEntity == null) return new List<AvailableBuildingDTO>();

            var currentResourceSnapshot = _resourceService.CalculateCurrent(cityEntity, DateTime.UtcNow);
            int availablePopulation = _cityStatService.GetAvailablePopulation(cityEntity, new List<BaseJob>());

            var availableBuildingsResponse = new List<AvailableBuildingDTO>();
            var allBuildingTypes = Enum.GetValues<BuildingTypeEnum>();

            foreach (var buildingType in allBuildingTypes)
            {
                var existingBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                int currentLevel = existingBuilding?.Level ?? 0;
                int nextLevel = currentLevel + 1;

                var nextLevelConfiguration = _buildingDataReader.GetConfig<BuildingLevelData>(buildingType, nextLevel);
                if (nextLevelConfiguration == null) continue;

                bool canAffordUpgrade = currentResourceSnapshot.Wood >= nextLevelConfiguration.WoodCost &&
                                        currentResourceSnapshot.Stone >= nextLevelConfiguration.StoneCost &&
                                        currentResourceSnapshot.Metal >= nextLevelConfiguration.MetalCost;

                availableBuildingsResponse.Add(new AvailableBuildingDTO
                {
                    BuildingType = buildingType,
                    BuildingName = buildingType.ToString(),
                    CurrentLevel = currentLevel,
                    WoodCost = nextLevelConfiguration.WoodCost,
                    StoneCost = nextLevelConfiguration.StoneCost,
                    MetalCost = nextLevelConfiguration.MetalCost,
                    PopulationCost = nextLevelConfiguration.PopulationCost,
                    ConstructionTimeInSeconds = (int)nextLevelConfiguration.BuildTime.TotalSeconds,
                    IsCurrentlyUpgrading = existingBuilding?.IsUpgrading ?? false,
                    CanAfford = canAffordUpgrade,
                    HasPopulationRoom = availablePopulation >= nextLevelConfiguration.PopulationCost
                });
            }

            return availableBuildingsResponse;
        }

        public async Task UpdateCityPointsAsync(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepository.GetByIdAsync(cityIdentifier);
            if (cityEntity == null) return;

            int calculatedPointsFromBuildings = cityEntity.Buildings.Sum(building => building.Level * 10);
            if (cityEntity.Points != calculatedPointsFromBuildings)
            {
                cityEntity.Points = calculatedPointsFromBuildings;
                await _cityRepository.UpdateAsync(cityEntity);
            }
        }
    }
}