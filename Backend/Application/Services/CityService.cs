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
            private readonly ResearchDataReader _researchDataReader;
            private readonly IdeologyDataReader _ideologyDataReader;
            private readonly UnitDataReader _unitDataReader;
            private readonly ILogger<CityService> _logger;

            public CityService(
                ICityRepository cityRepository,
                IResourceService resourceService,
                IWorldPlayerService worldPlayerService,
                IModifierService modifierService,
                ICityStatService cityStatService,
                BuildingDataReader buildingDataReader,
                ResearchDataReader researchDataReader,
                IdeologyDataReader ideologyDataReader,
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
                _researchDataReader = researchDataReader;
                _ideologyDataReader = ideologyDataReader;
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
                CreateResourceOverview(cityEntity, BuildingTypeEnum.TimberCamp, new[] { ModifierTagEnum.Wood, ModifierTagEnum.ResourceProduction }),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.StoneQuarry, new[] { ModifierTagEnum.Stone, ModifierTagEnum.ResourceProduction }),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.MetalMine, new[] { ModifierTagEnum.Metal, ModifierTagEnum.ResourceProduction }),
                CreateSilverProductionBreakdown(cityEntity),
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

                var stationedUnitsDto = cityEntity.UnitStacks
                    .Select(u => new UnitStackDTO(u.Type, u.Quantity))
                    .ToList();

                var activeRecruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityIdentifier);

                return new CityControllerGetDetailedCityInformationDTO
                {
                    CityId = cityEntity.Id,
                    CityName = cityEntity.Name,
                    X = cityEntity.X,
                    Y = cityEntity.Y,

                    // Ressource-afrunding og mapping
                    CurrentWoodAmount = Math.Floor(cityEntity.Wood),
                    CurrentStoneAmount = Math.Floor(cityEntity.Stone),
                    CurrentMetalAmount = Math.Floor(cityEntity.Metal),
                    CurrentSilverAmount = Math.Floor(cityEntity.WorldPlayer.Silver),
                    CurrentResearchPoints = Math.Floor(cityEntity.WorldPlayer.ResearchPoints),
                    CurrentIdeologyFocusPoints = Math.Floor(cityEntity.WorldPlayer.IdeologyFocusPoints),

                    // Kapaciteter og produktion
                    MaxWoodCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                    MaxStoneCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                    MaxMetalCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),

                    WoodProductionPerHour = currentCitySnapshot.WoodProductionPerHour,
                    StoneProductionPerHour = currentCitySnapshot.StoneProductionPerHour,
                    MetalProductionPerHour = currentCitySnapshot.MetalProductionPerHour,

                    SilverProductionPerHour = globalSnapshot.SilverProductionPerHour,
                    ResearchPointsPerHour = globalSnapshot.ResearchPointsPerHour,
                    IdeologyFocusPointsPerHour = globalSnapshot.IdeologyFocusPointsPerHour,

                    CurrentPopulationUsage = _cityStatService.GetCurrentPopulationUsage(cityEntity, activeRecruitmentJobs),
                    MaxPopulationCapacity = _cityStatService.GetMaxPopulation(cityEntity),

                    BuildingList = cityEntity.Buildings.Select(b => new CityControllerGetDetailedCityInformationBuildingDTO
                    {
                        BuildingType = b.Type,
                        CurrentLevel = b.Level
                    }).ToList(),

                    StationedUnits = stationedUnitsDto,
                };
            }

            public async Task<List<AvailableBuildingDTO>> GetAvailableBuildingsForTownHallAsync(Guid cityIdentifier)
            {
                var cityEntity = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
                if (cityEntity == null)
                {
                    _logger.LogWarning("GetAvailableBuildingsForTownHallAsync: City med identifier {CityId} blev ikke fundet.", cityIdentifier);
                    return new List<AvailableBuildingDTO>();
                }

                // Hent alle aktive bygge-jobs for at beregne det "reelle" niveau efter køen
                List<BuildingJob> activeBuildingConstructionJobs = await _jobRepository.GetBuildingJobsAsync(cityIdentifier);

                var currentCitySnapshot = _resourceService.CalculateCityResources(cityEntity, DateTime.UtcNow);

                // Vi sender en tom liste af jobs med her, da vi i denne DTO fokuserer på de statiske krav
                int availablePopulation = _cityStatService.GetAvailablePopulation(cityEntity, new List<BaseJob>());

                var availableBuildingsResponse = new List<AvailableBuildingDTO>();

                foreach (BuildingTypeEnum buildingType in Enum.GetValues<BuildingTypeEnum>())
                {
                    // Find den nuværende bygning i databasen
                    Building? existingBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                    int databaseLevel = existingBuilding?.Level ?? 0;

                    // Tjek om der ligger opgraderinger i køen for denne bygningstype
                    // Vi tager det højeste niveau fra køen, hvis der er flere (hvis din arkitektur tillader det)
                    BuildingJob? pendingJobForThisBuilding = activeBuildingConstructionJobs
                        .Where(job => job.BuildingType == buildingType)
                        .OrderByDescending(job => job.TargetLevel)
                        .FirstOrDefault();

                    // Det "effektive" niveau er det niveau bygningen har, når køen er tom.
                    // Hvis der er et job i køen til Lvl 2, er det effektive niveau 2.
                    int effectiveCurrentLevel = pendingJobForThisBuilding != null
                        ? pendingJobForThisBuilding.TargetLevel
                        : databaseLevel;

                    // Næste opgradering vi skal vise data for (f.eks. Lvl 3, hvis Lvl 2 er i kø)
                    int targetUpgradeLevel = effectiveCurrentLevel + 1;

                    BuildingLevelData? nextLevelConfiguration = _buildingDataReader.GetConfig<BuildingLevelData>(buildingType, targetUpgradeLevel);

                    // Hvis der ikke er konfiguration for næste niveau, er bygningen fuldt udbygget
                    if (nextLevelConfiguration == null) continue;

                    bool canAffordUpgrade = currentCitySnapshot.Wood >= nextLevelConfiguration.WoodCost &&
                                           currentCitySnapshot.Stone >= nextLevelConfiguration.StoneCost &&
                                           currentCitySnapshot.Metal >= nextLevelConfiguration.MetalCost;

                    availableBuildingsResponse.Add(new AvailableBuildingDTO
                    {
                        BuildingType = buildingType,
                        BuildingName = buildingType.ToString(),
                        CurrentLevel = databaseLevel, // Vi viser stadig det faktiske niveau i UI-teksten
                        WoodCost = nextLevelConfiguration.WoodCost,
                        StoneCost = nextLevelConfiguration.StoneCost,
                        MetalCost = nextLevelConfiguration.MetalCost,
                        ConstructionTimeInSeconds = (int)nextLevelConfiguration.BuildTime.TotalSeconds,
                        // Bygningen er "IsCurrentlyUpgrading" hvis enten entiteten siger det, eller der ligger et job
                        IsCurrentlyUpgrading = existingBuilding?.IsUpgrading ?? (pendingJobForThisBuilding != null),
                        CanAfford = canAffordUpgrade,
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

            private ResourceOverviewDTO CreateResourceOverview(City cityEntity, BuildingTypeEnum buildingType, IEnumerable<ModifierTagEnum> targetTags)
            {
                return new ResourceOverviewDTO(
                    _cityStatService.GetWarehouseCapacity(cityEntity),
                    CreateProductionBreakdown(cityEntity, buildingType, targetTags)
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

                foreach (var research in cityEntity.WorldPlayer.CompletedResearches)
                {
                    var researchToGetModifiers = _researchDataReader.GetNode(research.ResearchId);
                    modifierProviders.Add(researchToGetModifiers);
                }

                var worldPlayerIdeologyType = cityEntity.WorldPlayer.Ideology;
                var ideology = _ideologyDataReader.GetIdeology(worldPlayerIdeologyType);
                modifierProviders.Add(ideology);

                var result = _modifierService.CalculateEntityValueWithModifiers(baseProductionValue, targetTags, modifierProviders);

                return new ProductionBreakdownDTO(baseProductionValue, result.FlatBonus, result.PercentageBonus, result.FinalValue);
            }

            private SilverBreakdownDTO CreateSilverProductionBreakdown(City cityEntity)
            {
                IEnumerable<ModifierTagEnum> silverIncomeTags = new[] { ModifierTagEnum.Silver };
                IEnumerable<ModifierTagEnum> silverExpenditureTags = new[] { ModifierTagEnum.Upkeep };

                //Calculate silver INCOME
                double baseProductionValue = _cityStatService.GetMaxPopulation(cityEntity) * 7.0;

                var modifierProviders = new List<IModifierProvider> { cityEntity, cityEntity.WorldPlayer };
                if (cityEntity.WorldPlayer?.Alliance != null) modifierProviders.Add(cityEntity.WorldPlayer.Alliance);

                foreach (var cityBuilding in cityEntity.Buildings.Where(b => b.Level > 0))
                {
                    var levelConfig = _buildingDataReader.GetConfig<BuildingLevelData>(cityBuilding.Type, cityBuilding.Level);
                    if (levelConfig != null) modifierProviders.Add(levelConfig);
                }

                foreach (var research in cityEntity.WorldPlayer.CompletedResearches)
                {
                    var researchToGetModifiers = _researchDataReader.GetNode(research.ResearchId);
                    modifierProviders.Add(researchToGetModifiers);
                }

                var ideology = _ideologyDataReader.GetIdeology(cityEntity.WorldPlayer.Ideology);
                if (ideology != null) modifierProviders.Add(ideology);

                var silverProduction = _modifierService.CalculateEntityValueWithModifiers(baseProductionValue, silverIncomeTags, modifierProviders);

                //Calculate silver EXPENDITURE
                int stationedPopulation = cityEntity.UnitStacks
                    .Sum(stack => _unitDataReader.GetUnit(stack.Type).PopulationCost * stack.Quantity);

                int deployedPopulation = cityEntity.OriginUnitDeployments
                    .SelectMany(deployment => deployment.UnitStacks)
                    .Sum(stack => _unitDataReader.GetUnit(stack.Type).PopulationCost * stack.Quantity);

                int totalPopulation = stationedPopulation + deployedPopulation;

                int buildingUpkeepCost = cityEntity.Buildings.Sum(building => _buildingDataReader.GetConfig<BuildingLevelData>(building.Type, building.Level).UpkeepCost);

                double flatUnitSilverExpenditure = (stationedPopulation + deployedPopulation) * 7;
                double flatTotalSilverExpenditure = flatUnitSilverExpenditure + buildingUpkeepCost;


                var silverExpenditure = _modifierService.CalculateEntityValueWithModifiers(flatTotalSilverExpenditure, silverExpenditureTags, modifierProviders);

                return new SilverBreakdownDTO(
                    silverProduction.FinalValue,
                    silverProduction.FlatBonus,
                    silverProduction.PercentageBonus,
                    silverProduction.FinalValue,
                    silverExpenditure.FinalValue,
                    silverExpenditure.PercentageBonus
                );

            }

            private double ExtractBaseValueFromLevelData(City cityEntity, BuildingTypeEnum buildingType, int level)
            {
                if (level <= 0) return 0;

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
                int unitUsage = cityEntity.UnitStacks.Sum(s => s.Quantity * (_unitDataReader.GetUnit(s.Type)?.PopulationCost ?? 0));

                return new PopulationBreakdownDTO(
                    _cityStatService.GetMaxPopulation(cityEntity),
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