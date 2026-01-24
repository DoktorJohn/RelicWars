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
using static System.Formats.Asn1.AsnWriter;
using System.Runtime.ConstrainedExecution;
using Domain.User;

namespace Application.Services
{

    namespace Application.Services
    {
        public class CityService : ICityService
        {
            private readonly ICityRepository _cityRepository;
            private readonly IJobRepository _jobRepository;
            private readonly IResourceService _resourceService;
            private readonly IWorldPlayerService _worldPlayerService;
            private readonly IModifierService _modifierService;
            private readonly ICityStatService _cityStatService;
            private readonly BuildingDataReader _buildingDataReader;
            private readonly UnitDataReader _unitDataReader;
            private readonly ILogger<CityService> _logger;

            public CityService(
                ICityRepository cityRepository,
                IResourceService resourceService,
                IWorldPlayerService worldPlayerService,
                IModifierService modifierService,
                ICityStatService cityStatService,
                BuildingDataReader buildingDataReader,
                UnitDataReader unitDataReader,
                IJobRepository jobRepository,
                ILogger<CityService> logger)
            {
                _cityRepository = cityRepository;
                _resourceService = resourceService;
                _worldPlayerService = worldPlayerService;
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
                    throw new KeyNotFoundException($"Byen med ID {cityIdentifier} blev ikke fundet.");
                }

                var playerEntity = cityEntity.WorldPlayer;
                var currentDateTime = DateTime.UtcNow;

                // 1. Deleger opdatering af globale ressourcer til WorldPlayerService
                _worldPlayerService.UpdateGlobalResourceState(cityEntity.WorldPlayer, currentDateTime);

                // 2. Opdater lokale ressourcer for alle byer
                foreach (var city in cityEntity.WorldPlayer.Cities)
                {
                    var citySnapshot = _resourceService.CalculateCityResources(city, currentDateTime);
                    city.Wood = citySnapshot.Wood;
                    city.Stone = citySnapshot.Stone;
                    city.Metal = citySnapshot.Metal;
                    city.LastResourceUpdate = currentDateTime;
                }

                await _cityRepository.UpdateRangeAsync(cityEntity.WorldPlayer.Cities.ToList());

                var activeBuildingJobs = await _jobRepository.GetBuildingJobsAsync(cityIdentifier);
                var activeRecruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityIdentifier);

                return new CityOverviewHUD(
                cityEntity.Id,
                cityEntity.Name,
                playerEntity.Silver,
                playerEntity.ResearchPoints,
                playerEntity.IdeologyFocusPoints,
                CreateResourceOverview(cityEntity, BuildingTypeEnum.TimberCamp, ModifierTagEnum.Wood, cityEntity.Wood),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.StoneQuarry, ModifierTagEnum.Stone, cityEntity.Stone),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.MetalMine, ModifierTagEnum.Metal, cityEntity.Metal),
                CreateProductionBreakdown(cityEntity, BuildingTypeEnum.MarketPlace, new[] { ModifierTagEnum.Silver }),
                CreateProductionBreakdown(cityEntity, BuildingTypeEnum.University, new[] { ModifierTagEnum.Research }),
                CreateIdeologyProductionBreakdown(playerEntity),
                CreatePopulationBreakdown(cityEntity, activeBuildingJobs),
                CreateBuildingQueueOverview(activeBuildingJobs),
                CreateBarracksQueueOverview(activeRecruitmentJobs)
            );
            }

            public async Task<CityControllerGetDetailedCityInformationDTO?> GetDetailedCityInformationByCityIdentifierAsync(Guid cityIdentifier)
            {
                var cityEntity = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
                if (cityEntity == null || cityEntity.WorldPlayer == null) return null;

                var currentDateTime = DateTime.UtcNow;

                // 1. Global synkronisering
                _worldPlayerService.UpdateGlobalResourceState(cityEntity.WorldPlayer, currentDateTime);

                // 2. Lokal by-synkronisering
                foreach (var city in cityEntity.WorldPlayer.Cities)
                {
                    var citySnapshot = _resourceService.CalculateCityResources(city, currentDateTime);
                    city.Wood = citySnapshot.Wood;
                    city.Stone = citySnapshot.Stone;
                    city.Metal = citySnapshot.Metal;
                    city.LastResourceUpdate = currentDateTime;
                }

                await _cityRepository.UpdateRangeAsync(cityEntity.WorldPlayer.Cities.ToList());

                var globalSnapshot = _resourceService.CalculateGlobalResources(cityEntity.WorldPlayer, currentDateTime);
                var currentCitySnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);

                return new CityControllerGetDetailedCityInformationDTO
                {
                    CityId = cityEntity.Id,
                    CityName = cityEntity.Name,
                    CurrentWoodAmount = Math.Floor(cityEntity.Wood),
                    CurrentStoneAmount = Math.Floor(cityEntity.Stone),
                    CurrentMetalAmount = Math.Floor(cityEntity.Metal),
                    CurrentSilverAmount = Math.Floor(cityEntity.WorldPlayer.Silver),
                    CurrentResearchPoints = Math.Floor(cityEntity.WorldPlayer.ResearchPoints),
                    CurrentIdeologyFocusPoints = Math.Floor(cityEntity.WorldPlayer.IdeologyFocusPoints),
                    MaxWoodCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                    MaxStoneCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                    MaxMetalCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                    WoodProductionPerHour = currentCitySnapshot.WoodProductionPerHour,
                    StoneProductionPerHour = currentCitySnapshot.StoneProductionPerHour,
                    MetalProductionPerHour = currentCitySnapshot.MetalProductionPerHour,
                    SilverProductionPerHour = globalSnapshot.SilverProductionPerHour,
                    ResearchPointsPerHour = globalSnapshot.ResearchPointsPerHour,
                    IdeologyFocusPointsPerHour = globalSnapshot.IdeologyFocusPointsPerHour,
                    CurrentPopulationUsage = _cityStatService.GetCurrentPopulationUsage(cityEntity),
                    MaxPopulationCapacity = _cityStatService.GetMaxPopulation(cityEntity),
                    BuildingList = cityEntity.Buildings.Select(b => new CityControllerGetDetailedCityInformationBuildingDTO
                    {
                        BuildingType = b.Type,
                        CurrentLevel = b.Level
                    }).ToList(),
                    StationedUnits = cityEntity.UnitStacks.Select(u => new UnitStackDTO(u.Type, u.Quantity)).ToList(),
                    DeployedUnits = cityEntity.OriginUnitDeployments.Select(d => new UnitDeploymentDTO(d.Id, d.UnitType, d.Quantity, d.UnitDeploymentMovementStatus, d.ArrivalTime, d.OriginCityId, d.TargetCityId, d.TargetCity?.Name)).ToList()
                };
            }

            public async Task<List<AvailableBuildingDTO>> GetAvailableBuildingsForTownHallAsync(Guid cityIdentifier)
            {
                var cityEntity = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
                if (cityEntity == null) return new List<AvailableBuildingDTO>();

                var currentCitySnapshot = _resourceService.CalculateCityResources(cityEntity, DateTime.UtcNow);
                int availablePopulation = _cityStatService.GetAvailablePopulation(cityEntity, new List<BaseJob>());

                var availableBuildingsResponse = new List<AvailableBuildingDTO>();

                foreach (var buildingType in Enum.GetValues<BuildingTypeEnum>())
                {
                    var existingBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                    int nextLevel = (existingBuilding?.Level ?? 0) + 1;

                    var nextConfig = _buildingDataReader.GetConfig<BuildingLevelData>(buildingType, nextLevel);
                    if (nextConfig == null) continue;

                    bool canAffordUpgrade = currentCitySnapshot.Wood >= nextConfig.WoodCost &&
                                            currentCitySnapshot.Stone >= nextConfig.StoneCost &&
                                            currentCitySnapshot.Metal >= nextConfig.MetalCost;

                    availableBuildingsResponse.Add(new AvailableBuildingDTO
                    {
                        BuildingType = buildingType,
                        BuildingName = buildingType.ToString(),
                        CurrentLevel = existingBuilding?.Level ?? 0,
                        WoodCost = nextConfig.WoodCost,
                        StoneCost = nextConfig.StoneCost,
                        MetalCost = nextConfig.MetalCost,
                        PopulationCost = nextConfig.PopulationCost,
                        ConstructionTimeInSeconds = (int)nextConfig.BuildTime.TotalSeconds,
                        IsCurrentlyUpgrading = existingBuilding?.IsUpgrading ?? false,
                        CanAfford = canAffordUpgrade,
                        HasPopulationRoom = availablePopulation >= nextConfig.PopulationCost
                    });
                }

                return availableBuildingsResponse;
            }

            public async Task UpdateCityPointsAsync(Guid cityIdentifier)
            {
                var cityEntity = await _cityRepository.GetByIdAsync(cityIdentifier);
                if (cityEntity == null) return;

                int calculatedPoints = cityEntity.Buildings.Sum(b => b.Level * 10);
                if (cityEntity.Points != calculatedPoints)
                {
                    cityEntity.Points = calculatedPoints;
                    await _cityRepository.UpdateAsync(cityEntity);
                }
            }

            private ResourceOverviewDTO CreateResourceOverview(City cityEntity, BuildingTypeEnum buildingType, ModifierTagEnum resourceTag, double currentStoredAmount)
            {
                return new ResourceOverviewDTO(
                    currentStoredAmount,
                    _cityStatService.GetWarehouseCapacity(cityEntity),
                    CreateProductionBreakdown(cityEntity, buildingType, new[] { resourceTag, ModifierTagEnum.ResourceProduction })
                );
            }

            private ProductionBreakdownDTO CreateIdeologyProductionBreakdown(WorldPlayer player)
            {
                double baseRate = player.Cities.Count * 1.0;
                var result = _modifierService.CalculateEntityValueWithModifiers(
                    baseRate,
                    new[] { ModifierTagEnum.Ideology },
                    new List<IModifierProvider> { player }
                );

                return new ProductionBreakdownDTO(
                    baseRate,
                    result.FlatBonus,
                    result.PercentageBonus,
                    result.FinalValue
                );
            }
            private ProductionBreakdownDTO CreateProductionBreakdown(City cityEntity, BuildingTypeEnum buildingType, IEnumerable<ModifierTagEnum> targetTags)
            {
                var building = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                double baseProductionValue = ExtractBaseValueFromLevelData(cityEntity, buildingType, building?.Level ?? 0);

                var modifierProviders = new List<IModifierProvider> { cityEntity, cityEntity.WorldPlayer };
                if (cityEntity.WorldPlayer?.Alliance != null) modifierProviders.Add(cityEntity.WorldPlayer.Alliance);

                foreach (var cityBuilding in cityEntity.Buildings.Where(b => b.Level > 0))
                {
                    var levelConfig = _buildingDataReader.GetConfig<BuildingLevelData>(cityBuilding.Type, cityBuilding.Level);
                    if (levelConfig != null) modifierProviders.Add(levelConfig);
                }

                var result = _modifierService.CalculateEntityValueWithModifiers(baseProductionValue, targetTags, modifierProviders);

                return new ProductionBreakdownDTO(baseProductionValue, result.FlatBonus, result.PercentageBonus, result.FinalValue);
            }

            private double ExtractBaseValueFromLevelData(City cityEntity, BuildingTypeEnum buildingType, int level)
            {
                if (level <= 0) return 0;
                if (buildingType == BuildingTypeEnum.MarketPlace) return (double)_cityStatService.GetMaxPopulation(cityEntity) * 7.0;

                return buildingType switch
                {
                    BuildingTypeEnum.TimberCamp => _buildingDataReader.GetConfig<TimberCampLevelData>(buildingType, level)?.ProductionPerHour ?? 0,
                    BuildingTypeEnum.StoneQuarry => _buildingDataReader.GetConfig<StoneQuarryLevelData>(buildingType, level)?.ProductionPerHour ?? 0,
                    BuildingTypeEnum.MetalMine => _buildingDataReader.GetConfig<MetalMineLevelData>(buildingType, level)?.ProductionPerHour ?? 0,
                    BuildingTypeEnum.University => _buildingDataReader.GetConfig<UniversityLevelData>(buildingType, level)?.ProductionPerHour ?? 0,
                    _ => 0
                };
            }

            private PopulationBreakdownDTO CreatePopulationBreakdown(City cityEntity, IEnumerable<BaseJob> activeJobs)
            {
                int buildingUsage = cityEntity.Buildings.Sum(b => _buildingDataReader.GetConfig<BuildingLevelData>(b.Type, b.Level)?.PopulationCost ?? 0);
                int unitUsage = cityEntity.UnitStacks.Sum(s => s.Quantity * (_unitDataReader.GetUnit(s.Type)?.PopulationCost ?? 0));

                return new PopulationBreakdownDTO(
                    _cityStatService.GetMaxPopulation(cityEntity),
                    buildingUsage,
                    unitUsage,
                    _cityStatService.GetAvailablePopulation(cityEntity, activeJobs),
                    0
                );
            }

            private BuildingQueueOverviewDTO CreateBuildingQueueOverview(List<BuildingJob> buildingJobs)
            {
                var firstJob = buildingJobs.FirstOrDefault();
                return new BuildingQueueOverviewDTO(buildingJobs.Any(), buildingJobs.Count, firstJob?.BuildingType.ToString() ?? "None", firstJob?.ExecutionTime);
            }

            private BarracksQueueOverviewDTO CreateBarracksQueueOverview(List<RecruitmentJob> recruitmentJobs)
            {
                var firstJob = recruitmentJobs.FirstOrDefault();
                return new BarracksQueueOverviewDTO(recruitmentJobs.Any(), recruitmentJobs.Sum(j => j.TotalQuantity - j.CompletedQuantity), firstJob?.UnitType.ToString() ?? "None", recruitmentJobs.LastOrDefault()?.ExecutionTime);
            }
        }
    }
}