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
        private readonly ICityStatService _statService;
        private readonly IModifierService _modifierService;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(
            BuildingDataReader buildingData,
            ICityStatService statService,
            IModifierService modifierService,
            ILogger<ResourceService> logger)
        {
            _buildingData = buildingData;
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
                var silverResult = GetProductionResult(city, BuildingTypeEnum.MarketPlace, new[] { ModifierTagEnum.Silver });
                var researchResult = GetProductionResult(city, BuildingTypeEnum.University, new[] { ModifierTagEnum.Research });

                totalSilverRate += silverResult.FinalValue;
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

        private double GetBaseProductionValue(City cityEntity, BuildingTypeEnum buildingType)
        {
            if (buildingType == BuildingTypeEnum.MarketPlace)
            {
                return (double)_statService.GetMaxPopulation(cityEntity) * 7.0;
            }

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