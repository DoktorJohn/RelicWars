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
using Application.Utility;

namespace Application.Services
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _cityRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IResourceService _resourceService;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly IModifierService _modifierService;
        private readonly ICityStatService _cityStatService;
        private readonly IExoticResourceService _exoticResourceService;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly UnitDataReader _unitDataReader;
        private readonly ILogger<CityService> _logger;
        private readonly ConstructionTimeCalculator _constructionTimeCalculator;
        private readonly IResistanceService _resistanceService;

        public CityService(
            ICityRepository cityRepository,
            IResourceService resourceService,
            IWorldPlayerService worldPlayerService,
            IPlayerAccessService playerAccessService,
            IModifierService modifierService,
            ICityStatService cityStatService,
            IExoticResourceService exoticResourceService,
            BuildingDataReader buildingDataReader,
            UnitDataReader unitDataReader,
            IJobRepository jobRepository,
            ILogger<CityService> logger,
            ConstructionTimeCalculator constructionTimeCalculator,
            IResistanceService resistanceService)
        {
            _cityRepository = cityRepository;
            _resourceService = resourceService;
            _worldPlayerService = worldPlayerService;
            _playerAccessService = playerAccessService;
            _cityStatService = cityStatService;
            _exoticResourceService = exoticResourceService;
            _buildingDataReader = buildingDataReader;
            _unitDataReader = unitDataReader;
            _jobRepository = jobRepository;
            _modifierService = modifierService;
            _logger = logger;
            _constructionTimeCalculator = constructionTimeCalculator;
            _resistanceService = resistanceService;

        }

        public async Task<CityOverviewHUD> GetCityOverviewHUD(Guid cityIdentifier)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityIdentifier);

            var playerEntity = cityEntity.WorldPlayer
                ?? throw new InvalidOperationException("Byens ejer blev ikke fundet.");
            var currentDateTime = DateTime.UtcNow;

            _worldPlayerService.SyncGlobalResources(playerEntity, currentDateTime);

            SyncCityResources(cityEntity, currentDateTime);
            var exoticResources = await _exoticResourceService.SyncCityExoticResourcesAsync(cityEntity, currentDateTime);
            var exoticResourceProductions = await _exoticResourceService.GetProductionBreakdownsForCityAsync(cityEntity);

            var activeBuildingJobs = await _jobRepository.GetBuildingJobsAsync(cityIdentifier);
            var activeRecruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityIdentifier);
            _resistanceService.UpdateResistance(cityEntity, currentDateTime);
            await _cityRepository.UpdateAsync(cityEntity);

            return new CityOverviewHUD(
                cityEntity.Id,
                cityEntity.Name,
                CreateResourceOverview(cityEntity, BuildingTypeEnum.TimberCamp, ModifierTagEnum.Wood, ModifierTagEnum.ResourceProduction),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.StoneQuarry, ModifierTagEnum.Stone, ModifierTagEnum.ResourceProduction),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.MetalMine, ModifierTagEnum.Metal, ModifierTagEnum.ResourceProduction),
                CreateCoinsProductionBreakdown(cityEntity),
                CreateProductionBreakdown(cityEntity, BuildingTypeEnum.University, ModifierTagEnum.Research),
                CreateIdeologyProductionBreakdown(playerEntity),
                CreatePopulationBreakdown(cityEntity, activeBuildingJobs),
                cityEntity.Resistance,
                cityEntity.ResistanceTarget,
                _resistanceService.CalculateRecoveryPerHour(cityEntity),
                CreateBuildingQueueOverview(activeBuildingJobs),
                CreateBarracksQueueOverview(activeRecruitmentJobs),
                exoticResources,
                exoticResourceProductions
            );
        }

        public async Task<CityControllerGetDetailedCityInformationDTO?> GetDetailedCityInformationByCityIdentifierAsync(Guid cityIdentifier)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityIdentifier);

            var currentDateTime = DateTime.UtcNow;

            // Opdater kun denne by
            var currentCitySnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);
            
            cityEntity.Wood = currentCitySnapshot.Wood;
            cityEntity.Stone = currentCitySnapshot.Stone;
            cityEntity.Metal = currentCitySnapshot.Metal;
            cityEntity.LastResourceUpdate = currentDateTime;
            var cityExoticResources = await _exoticResourceService.SyncCityExoticResourcesAsync(cityEntity, currentDateTime);
            var islandExoticResources = await _exoticResourceService.GetIslandResourcesForCityAsync(cityEntity);
            await _cityRepository.UpdateAsync(cityEntity);

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

                // Kapaciteter og produktion
                MaxWoodCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                MaxStoneCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                MaxMetalCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),

                WoodProductionPerHour = currentCitySnapshot.WoodProductionPerHour,
                StoneProductionPerHour = currentCitySnapshot.StoneProductionPerHour,
                MetalProductionPerHour = currentCitySnapshot.MetalProductionPerHour,

                CurrentPopulationUsage = _cityStatService.GetCurrentPopulationUsage(cityEntity, activeRecruitmentJobs),
                MaxPopulationCapacity = _cityStatService.GetMaxPopulation(cityEntity),
                Resistance = cityEntity.Resistance,
                ResistanceTarget = cityEntity.ResistanceTarget,
                ResistanceRecoveryPerHour = _resistanceService.CalculateRecoveryPerHour(cityEntity),
                ExoticResources = cityExoticResources,
                IslandExoticResources = islandExoticResources,

                BuildingList = cityEntity.Buildings.Select(b => new CityControllerGetDetailedCityInformationBuildingDTO
                {
                    BuildingType = b.Type,
                    CurrentLevel = b.Level
                }).ToList(),

                StationedUnits = stationedUnitsDto,
            };
        }

        public async Task<CityResourcesDTO?> GetCityResourcesAsync(Guid cityIdentifier)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityIdentifier);

            var currentDateTime = DateTime.UtcNow;

            // Opdater kun denne ene bys ressourcer for hurtig respons
            var citySnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);
            
            cityEntity.Wood = citySnapshot.Wood;
            cityEntity.Stone = citySnapshot.Stone;
            cityEntity.Metal = citySnapshot.Metal;
            cityEntity.LastResourceUpdate = currentDateTime;
            _resistanceService.UpdateResistance(cityEntity, currentDateTime);
            var exoticResources = await _exoticResourceService.SyncCityExoticResourcesAsync(cityEntity, currentDateTime);

            await _cityRepository.UpdateAsync(cityEntity);

            var activeRecruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityIdentifier);

            return new CityResourcesDTO
            {
                CityId = cityEntity.Id,
                CurrentWoodAmount = Math.Floor(cityEntity.Wood),
                CurrentStoneAmount = Math.Floor(cityEntity.Stone),
                CurrentMetalAmount = Math.Floor(cityEntity.Metal),
                
                WoodProductionPerHour = citySnapshot.WoodProductionPerHour,
                StoneProductionPerHour = citySnapshot.StoneProductionPerHour,
                MetalProductionPerHour = citySnapshot.MetalProductionPerHour,

                MaxWoodCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                MaxStoneCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),
                MaxMetalCapacity = _cityStatService.GetWarehouseCapacity(cityEntity),

                CurrentPopulationUsage = _cityStatService.GetCurrentPopulationUsage(cityEntity, activeRecruitmentJobs),
                MaxPopulationCapacity = _cityStatService.GetMaxPopulation(cityEntity)
                ,Resistance = cityEntity.Resistance
                ,ResistanceTarget = cityEntity.ResistanceTarget
                ,ResistanceRecoveryPerHour = _resistanceService.CalculateRecoveryPerHour(cityEntity)
                ,ExoticResources = exoticResources
            };
        }

        public async Task<List<AvailableBuildingDTO>> GetAvailableBuildingsForTownHallAsync(Guid cityIdentifier)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityForTownHallAsync(cityIdentifier);

            // Hent alle aktive bygge-jobs for at beregne det "reelle" niveau efter køen
            List<BuildingJob> activeBuildingConstructionJobs = await _jobRepository.GetBuildingJobsAsync(cityIdentifier);
            var currentCitySnapshot = _resourceService.CalculateCityResources(cityEntity, DateTime.UtcNow);
            var availableBuildingsResponse = new List<AvailableBuildingDTO>();

            foreach (BuildingTypeEnum buildingType in Enum.GetValues<BuildingTypeEnum>())
            {
                Building? existingBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                BuildingJob? pendingJobForThisBuilding = activeBuildingConstructionJobs
                    .Where(job => job.BuildingType == buildingType)
                    .OrderByDescending(job => job.TargetLevel)
                    .FirstOrDefault();

                int targetUpgradeLevel = pendingJobForThisBuilding?.TargetLevel + 1
                    ?? (existingBuilding is null ? 1 : existingBuilding.Level + 1);

                // En manglende bygning har intet level; dens første konstruktion bruger level 1-data.
                BuildingLevelData? nextLevelConfiguration = _buildingDataReader.GetConfig<BuildingLevelData>(buildingType, targetUpgradeLevel);

                if (nextLevelConfiguration == null) continue;

                // ==========================================
                // ANVEND MODIFIERS PÅ PRIS OG TID
                // ==========================================
                int modifiedWoodCost = (int)Math.Floor(_modifierService.CalculateCityValue(
                    cityEntity, nextLevelConfiguration.WoodCost, ModifierTagEnum.ConstructionCost).FinalValue);

                int modifiedStoneCost = (int)Math.Floor(_modifierService.CalculateCityValue(
                    cityEntity, nextLevelConfiguration.StoneCost, ModifierTagEnum.ConstructionCost).FinalValue);

                int modifiedMetalCost = (int)Math.Floor(_modifierService.CalculateCityValue(
                    cityEntity, nextLevelConfiguration.MetalCost, ModifierTagEnum.ConstructionCost).FinalValue);

                int modifiedConstructionTime = _constructionTimeCalculator.CalculateSeconds(
                    cityEntity, nextLevelConfiguration.BuildTime.TotalSeconds);
                // ==========================================

                // Tjek rådighed mod de MODIFICEREDE priser i stedet for standardpriserne
                bool canAffordUpgrade = currentCitySnapshot.Wood >= modifiedWoodCost &&
                                        currentCitySnapshot.Stone >= modifiedStoneCost &&
                                        currentCitySnapshot.Metal >= modifiedMetalCost;

                availableBuildingsResponse.Add(new AvailableBuildingDTO
                {
                    BuildingType = buildingType,
                    BuildingName = buildingType.ToString(),
                    CurrentLevel = existingBuilding?.Level,
                    IsConstructed = existingBuilding != null,

                    // Brug de modificerede værdier til DTO'en
                    WoodCost = modifiedWoodCost,
                    StoneCost = modifiedStoneCost,
                    MetalCost = modifiedMetalCost,
                    ConstructionTimeInSeconds = modifiedConstructionTime,

                    IsCurrentlyUpgrading = existingBuilding?.IsUpgrading == true || pendingJobForThisBuilding != null,
                    CanAfford = canAffordUpgrade,
                });
            }

            return availableBuildingsResponse;
        }
        private ResourceOverviewDTO CreateResourceOverview(City cityEntity, BuildingTypeEnum buildingType, params ModifierTagEnum[] targetTags)
        {
            return new ResourceOverviewDTO(
                _cityStatService.GetWarehouseCapacity(cityEntity),
                CreateProductionBreakdown(cityEntity, buildingType, targetTags)
            );
        }

        private void SyncCityResources(City city, DateTime synchronizedAt)
        {
            var snapshot = _resourceService.CalculateCityResources(city, synchronizedAt);
            city.Wood = snapshot.Wood;
            city.Stone = snapshot.Stone;
            city.Metal = snapshot.Metal;
            city.LastResourceUpdate = synchronizedAt;
        }

        private ProductionBreakdownDTO CreateIdeologyProductionBreakdown(WorldPlayer player)
        {
            double baseRate = player.Cities.Count * 1.0;

            // BRUGER NU DEN NYE MODIFIER SERVICE METODE TIL SPILLER
            var result = _modifierService.CalculatePlayerValue(player, baseRate, ModifierTagEnum.Ideology);

            return new ProductionBreakdownDTO(
                baseRate,
                result.FlatBonus,
                result.PercentageBonus,
                result.FinalValue
            );
        }

        private ProductionBreakdownDTO CreateProductionBreakdown(City cityEntity, BuildingTypeEnum buildingType, params ModifierTagEnum[] targetTags)
        {
            var building = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
            double baseProductionValue = ExtractBaseValueFromLevelData(cityEntity, buildingType, building?.Level ?? 0);

            var result = _modifierService.CalculateCityValue(cityEntity, baseProductionValue, targetTags);

            return new ProductionBreakdownDTO(
                baseProductionValue,
                result.FlatBonus,
                result.PercentageBonus,
                result.FinalValue
            );
        }

        private CoinsBreakdownDTO CreateCoinsProductionBreakdown(City cityEntity)
        {
            double baseIncome = _cityStatService.GetMaxPopulation(cityEntity) * 7.0;
            var incomeResult = _modifierService.CalculateCityValue(cityEntity, baseIncome, ModifierTagEnum.Coins, ModifierTagEnum.Market);

            // 2. Calculate coins EXPENDITURE
            int stationedPopulation = cityEntity.UnitStacks
                .Sum(stack => _unitDataReader.GetUnit(stack.Type).PopulationCost * stack.Quantity);

            int deployedPopulation = cityEntity.OriginUnitDeployments
                .SelectMany(deployment => deployment.UnitStacks)
                .Sum(stack => _unitDataReader.GetUnit(stack.Type).PopulationCost * stack.Quantity);

            int buildingUpkeepCost = cityEntity.Buildings
                .Sum(building => _buildingDataReader.GetConfig<BuildingLevelData>(building.Type, building.Level).UpkeepCost);

            double baseExpenditure = ((stationedPopulation + deployedPopulation) * 7) + buildingUpkeepCost;

            var expenditureResult = _modifierService.CalculateCityValue(cityEntity, baseExpenditure, ModifierTagEnum.Upkeep, ModifierTagEnum.BuildingUpkeep, ModifierTagEnum.UnitUpkeep);

            return new CoinsBreakdownDTO(
                incomeResult.BaseValue,
                incomeResult.FlatBonus,
                incomeResult.PercentageBonus,
                incomeResult.FinalValue,
                expenditureResult.FinalValue,
                expenditureResult.PercentageBonus
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

        public async Task<List<CityDTO>> GetPlayerCitiesByCityId(Guid cityId)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);
            var playerId = cityEntity.WorldPlayerId ?? throw new InvalidOperationException("Ejer af byen blev ikke fundet.");

            // 2. Fetch only the cities belonging to this player
            var cities = await _cityRepository.GetCitiesByWorldPlayerIdAsync(playerId);

            // 3. Map to simple DTOs
            return cities
                .Select(c => new CityDTO(c.Id, c.Name, c.X, c.Y, 0)) // Points 0 for now as requested
                .OrderBy(c => c.CityName)
                .ToList();
        }

        public async Task<ChangeCityNameResponseDTO> ChangeCityName(Guid cityId, string newCityName)
        {
            if (string.IsNullOrWhiteSpace(newCityName))
            {
                return new ChangeCityNameResponseDTO { Success = false, Message = "Bynavn må ikke være tomt." };
            }

            if (cityId == Guid.Empty)
            {
                return new ChangeCityNameResponseDTO { Success = false, Message = "Ugyldigt by-ID." };
            }

            string sanitizedCityName = newCityName.Trim();

            if (sanitizedCityName.Length < 3 || sanitizedCityName.Length > 30)
            {
                return new ChangeCityNameResponseDTO { Success = false, Message = "Bynavnet skal være mellem 3 og 30 tegn." };
            }

            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            cityEntity.Name = sanitizedCityName;
            await _cityRepository.UpdateAsync(cityEntity);

            return new ChangeCityNameResponseDTO
            {
                CityId = cityEntity.Id,
                CityName = cityEntity.Name,
                Message = "Bynavnet blev ændret succesfuldt.",
                Success = true,
            };
        }
    }
}
