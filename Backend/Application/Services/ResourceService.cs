using Application.DTOs;
using Application.Interfaces.IServices;
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

namespace Application.Services
{
    public record CityResourceSnapshot(
        double Wood,
        double Stone,
        double Metal,
        double WoodProductionPerHour,
        double StoneProductionPerHour,
        double MetalProductionPerHour,
        DateTime Timestamp);

    public record GlobalResourceSnapshot(
        double SilverAmount,
        double ResearchPoints,
        double IdeologyFocusPoints,
        double SilverProductionPerHour,
        double ResearchPointsPerHour,
        double IdeologyFocusPointsPerHour,
        DateTime Timestamp);

    public class ResourceService : IResourceService
    {
        private readonly BuildingDataReader _buildingData;
        private readonly ResearchDataReader _researchData;
        private readonly IdeologyDataReader _ideologyData;
        private readonly UnitDataReader _unitData;
        private readonly ICityStatService _statService;
        private readonly IModifierService _modifierService;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(
            BuildingDataReader buildingData,
            ResearchDataReader researchData,
            IdeologyDataReader ideologyData,
            UnitDataReader unitData,
            ICityStatService statService,
            IModifierService modifierService,
            ILogger<ResourceService> logger)
        {
            _buildingData = buildingData;
            _researchData = researchData;
            _ideologyData = ideologyData;
            _unitData = unitData;
            _statService = statService;
            _modifierService = modifierService;
            _logger = logger;
        }

        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime)
        {
            double hoursPassed = CalculateHoursPassed(cityEntity.LastResourceUpdate, currentDateTime);

            var woodResult = GetProductionResult(cityEntity, BuildingTypeEnum.TimberCamp, new[] { ModifierTagEnum.Wood, ModifierTagEnum.ResourceProduction });
            var stoneResult = GetProductionResult(cityEntity, BuildingTypeEnum.StoneQuarry, new[] { ModifierTagEnum.Stone, ModifierTagEnum.ResourceProduction });
            var metalResult = GetProductionResult(cityEntity, BuildingTypeEnum.MetalMine, new[] { ModifierTagEnum.Metal, ModifierTagEnum.ResourceProduction });

            double capacityLimit = _statService.GetWarehouseCapacity(cityEntity);

            return new CityResourceSnapshot(
                CalculateNewAmountWithCapacity(cityEntity.Wood, woodResult.FinalValue, hoursPassed, capacityLimit),
                CalculateNewAmountWithCapacity(cityEntity.Stone, stoneResult.FinalValue, hoursPassed, capacityLimit),
                CalculateNewAmountWithCapacity(cityEntity.Metal, metalResult.FinalValue, hoursPassed, capacityLimit),
                woodResult.FinalValue,
                stoneResult.FinalValue,
                metalResult.FinalValue,
                currentDateTime
            );
        }

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime)
        {
            double hoursPassed = CalculateHoursPassed(playerEntity.LastResourceUpdate, currentDateTime);

            double totalSilverRate = 0;
            double totalResearchRate = 0;

            foreach (var city in playerEntity.Cities)
            {
                var silverResult = GetSilverProductionResult(city);
                var researchResult = GetProductionResult(city, BuildingTypeEnum.University, new[] { ModifierTagEnum.Research });

                totalSilverRate += silverResult;
                totalResearchRate += researchResult.FinalValue;
            }

            double baseIdeologyRate = playerEntity.Cities.Count * 1.0;
            var ideologyCalculation = _modifierService.CalculateEntityValueWithModifiers(
                baseIdeologyRate,
                new[] { ModifierTagEnum.Ideology },
                new List<IModifierProvider> { playerEntity }
            );

            // Globale ressourcer
            double newSilverAmount = playerEntity.Silver + (totalSilverRate * hoursPassed);
            double newResearchAmount = playerEntity.ResearchPoints + (totalResearchRate * hoursPassed);
            double newIdeologyAmount = playerEntity.IdeologyFocusPoints + (ideologyCalculation.FinalValue * hoursPassed);

            return new GlobalResourceSnapshot(
                newSilverAmount,
                newResearchAmount,
                newIdeologyAmount,
                totalSilverRate,
                totalResearchRate,
                ideologyCalculation.FinalValue,
                currentDateTime
            );
        }

        private double CalculateHoursPassed(DateTime lastUpdate, DateTime currentDateTime)
        {
            DateTime startTime = DateTime.SpecifyKind(lastUpdate, DateTimeKind.Utc);
            DateTime endTime = DateTime.SpecifyKind(currentDateTime, DateTimeKind.Utc);
            double hours = (endTime - startTime).TotalHours;
            return hours < 0 ? 0 : hours;
        }

        private double CalculateNewAmountWithCapacity(double current, double rate, double hours, double capacity)
        {
            return Math.Min(capacity, current + (rate * hours));
        }

        private ModifierCalculationResult GetProductionResult(City cityEntity, BuildingTypeEnum buildingType, IEnumerable<ModifierTagEnum> targetTags)
        {
            double baseValue = GetBaseProductionValue(cityEntity, buildingType);
            var providers = new List<IModifierProvider> { cityEntity };

            if (cityEntity.WorldPlayer != null)
            {
                providers.Add(cityEntity.WorldPlayer);
                if (cityEntity.WorldPlayer.Alliance != null) providers.Add(cityEntity.WorldPlayer.Alliance);
            }

            var targetBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
            if (targetBuilding != null && targetBuilding.Level > 0)
            {
                providers.Add(_buildingData.GetConfig<BuildingLevelData>(buildingType, targetBuilding.Level));
            }

            return _modifierService.CalculateEntityValueWithModifiers(baseValue, targetTags, providers);
        }

        private double GetSilverProductionResult(City cityEntity)
        {
            IEnumerable<ModifierTagEnum> silverIncomeTags = new[] { ModifierTagEnum.Silver };
            IEnumerable<ModifierTagEnum> silverExpenditureTags = new[] { ModifierTagEnum.Upkeep };

            //Calculate silver INCOME
            double baseProductionValue = _statService.GetMaxPopulation(cityEntity) * 7.0;

            var modifierProviders = new List<IModifierProvider> { cityEntity, cityEntity.WorldPlayer };
            if (cityEntity.WorldPlayer?.Alliance != null) modifierProviders.Add(cityEntity.WorldPlayer.Alliance);

            foreach (var cityBuilding in cityEntity.Buildings.Where(b => b.Level > 0))
            {
                var levelConfig = _buildingData.GetConfig<BuildingLevelData>(cityBuilding.Type, cityBuilding.Level);
                if (levelConfig != null) modifierProviders.Add(levelConfig);
            }

            foreach (var research in cityEntity.WorldPlayer.CompletedResearches)
            {
                var researchToGetModifiers = _researchData.GetNode(research.ResearchId);
                modifierProviders.Add(researchToGetModifiers);
            }

            var ideology = _ideologyData.GetIdeology(cityEntity.WorldPlayer.Ideology);
            if (ideology != null) modifierProviders.Add(ideology);

            var silverProduction = _modifierService.CalculateEntityValueWithModifiers(baseProductionValue, silverIncomeTags, modifierProviders);

            //Calculate silver EXPENDITURE
            int stationedPopulation = cityEntity.UnitStacks
                .Sum(stack => _unitData.GetUnit(stack.Type).PopulationCost * stack.Quantity);

            int deployedPopulation = cityEntity.OriginUnitDeployments
                .SelectMany(deployment => deployment.UnitStacks)
                .Sum(stack => _unitData.GetUnit(stack.Type).PopulationCost * stack.Quantity);

            int totalPopulation = stationedPopulation + deployedPopulation;

            int buildingUpkeepCost = cityEntity.Buildings.Sum(building => _buildingData.GetConfig<BuildingLevelData>(building.Type, building.Level).UpkeepCost);

            double flatUnitSilverExpenditure = (stationedPopulation + deployedPopulation) * 7;
            double flatTotalSilverExpenditure = flatUnitSilverExpenditure + buildingUpkeepCost;


            var silverExpenditure = _modifierService.CalculateEntityValueWithModifiers(flatTotalSilverExpenditure, silverExpenditureTags, modifierProviders);

            return silverProduction.FinalValue - silverExpenditure.FinalValue;
        }

        private double GetBaseProductionValue(City cityEntity, BuildingTypeEnum buildingType)
        {
            var building = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
            if (building == null || building.Level == 0) return 0.0;

            return buildingType switch
            {
                BuildingTypeEnum.TimberCamp => _buildingData.GetConfig<TimberCampLevelData>(buildingType, building.Level).ProductionPerHour,
                BuildingTypeEnum.StoneQuarry => _buildingData.GetConfig<StoneQuarryLevelData>(buildingType, building.Level).ProductionPerHour,
                BuildingTypeEnum.MetalMine => _buildingData.GetConfig<MetalMineLevelData>(buildingType, building.Level).ProductionPerHour,
                BuildingTypeEnum.University => _buildingData.GetConfig<UniversityLevelData>(buildingType, building.Level).ProductionPerHour,
                _ => 0.0
            };
        }
    }
}