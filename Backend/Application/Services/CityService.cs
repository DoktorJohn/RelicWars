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

            _worldPlayerService.UpdateGlobalResourceState(cityEntity.WorldPlayer, currentDateTime);

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
                CreateResourceOverview(cityEntity, BuildingTypeEnum.TimberCamp, ModifierTagEnum.Wood, ModifierTagEnum.ResourceProduction),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.StoneQuarry, ModifierTagEnum.Stone, ModifierTagEnum.ResourceProduction),
                CreateResourceOverview(cityEntity, BuildingTypeEnum.MetalMine, ModifierTagEnum.Metal, ModifierTagEnum.ResourceProduction),
                CreateSilverProductionBreakdown(cityEntity),
                CreateProductionBreakdown(cityEntity, BuildingTypeEnum.University, ModifierTagEnum.Research),
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

            // Opdater kun denne by
            var currentCitySnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);
            
            cityEntity.Wood = currentCitySnapshot.Wood;
            cityEntity.Stone = currentCitySnapshot.Stone;
            cityEntity.Metal = currentCitySnapshot.Metal;
            cityEntity.LastResourceUpdate = currentDateTime;

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
            var cityEntity = await _cityRepository.GetByIdAsync(cityIdentifier);
            if (cityEntity == null) return null;

            var currentDateTime = DateTime.UtcNow;

            // Opdater kun denne ene bys ressourcer for hurtig respons
            var citySnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);
            
            cityEntity.Wood = citySnapshot.Wood;
            cityEntity.Stone = citySnapshot.Stone;
            cityEntity.Metal = citySnapshot.Metal;
            cityEntity.LastResourceUpdate = currentDateTime;

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
            var availableBuildingsResponse = new List<AvailableBuildingDTO>();

            foreach (BuildingTypeEnum buildingType in Enum.GetValues<BuildingTypeEnum>())
            {
                Building? existingBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                int databaseLevel = existingBuilding?.Level ?? 0;

                BuildingJob? pendingJobForThisBuilding = activeBuildingConstructionJobs
                    .Where(job => job.BuildingType == buildingType)
                    .OrderByDescending(job => job.TargetLevel)
                    .FirstOrDefault();

                int effectiveCurrentLevel = pendingJobForThisBuilding != null
                    ? pendingJobForThisBuilding.TargetLevel
                    : databaseLevel;

                int targetUpgradeLevel = effectiveCurrentLevel + 1;

                BuildingLevelData? nextLevelConfiguration = _buildingDataReader.GetConfig<BuildingLevelData>(buildingType, targetUpgradeLevel - 1);

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

                int modifiedConstructionTime = (int)Math.Floor(_modifierService.CalculateCityValue(
                    cityEntity, nextLevelConfiguration.BuildTime.TotalSeconds, ModifierTagEnum.Construction).FinalValue);
                // ==========================================

                // Tjek rådighed mod de MODIFICEREDE priser i stedet for standardpriserne
                bool canAffordUpgrade = currentCitySnapshot.Wood >= modifiedWoodCost &&
                                        currentCitySnapshot.Stone >= modifiedStoneCost &&
                                        currentCitySnapshot.Metal >= modifiedMetalCost;

                availableBuildingsResponse.Add(new AvailableBuildingDTO
                {
                    BuildingType = buildingType,
                    BuildingName = buildingType.ToString(),
                    CurrentLevel = databaseLevel,

                    // Brug de modificerede værdier til DTO'en
                    WoodCost = modifiedWoodCost,
                    StoneCost = modifiedStoneCost,
                    MetalCost = modifiedMetalCost,
                    ConstructionTimeInSeconds = modifiedConstructionTime,

                    IsCurrentlyUpgrading = existingBuilding?.IsUpgrading ?? (pendingJobForThisBuilding != null),
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

        private SilverBreakdownDTO CreateSilverProductionBreakdown(City cityEntity)
        {
            double baseIncome = _cityStatService.GetMaxPopulation(cityEntity) * 7.0;
            var incomeResult = _modifierService.CalculateCityValue(cityEntity, baseIncome, ModifierTagEnum.Silver, ModifierTagEnum.Market);

            // 2. Calculate silver EXPENDITURE
            int stationedPopulation = cityEntity.UnitStacks
                .Sum(stack => _unitDataReader.GetUnit(stack.Type).PopulationCost * stack.Quantity);

            int deployedPopulation = cityEntity.OriginUnitDeployments
                .SelectMany(deployment => deployment.UnitStacks)
                .Sum(stack => _unitDataReader.GetUnit(stack.Type).PopulationCost * stack.Quantity);

            int buildingUpkeepCost = cityEntity.Buildings
                .Sum(building => _buildingDataReader.GetConfig<BuildingLevelData>(building.Type, building.Level).UpkeepCost);

            double baseExpenditure = ((stationedPopulation + deployedPopulation) * 7) + buildingUpkeepCost;

            var expenditureResult = _modifierService.CalculateCityValue(cityEntity, baseExpenditure, ModifierTagEnum.Upkeep, ModifierTagEnum.BuildingUpkeep, ModifierTagEnum.UnitUpkeep);

            return new SilverBreakdownDTO(
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
            // 1. Find the player ID efficiently (without loading the entire City object graph)
            var playerId = await _cityRepository.GetWorldPlayerIdByCityIdAsync(cityId);

            if (playerId == null)
            {
                return new List<CityDTO>();
            }

            // 2. Fetch only the cities belonging to this player
            var cities = await _cityRepository.GetCitiesByWorldPlayerIdAsync(playerId.Value);

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

            var cityEntity = await _cityRepository.GetByIdAsync(cityId);

            if (cityEntity == null)
            {
                return new ChangeCityNameResponseDTO { Success = false, Message = $"Ingen by fundet med ID {cityId}." };
            }

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