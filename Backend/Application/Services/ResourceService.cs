using Application.DTOs;
using Application.Interfaces.IServices;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services
{
    public record ResourceSnapshot(
        double Wood,
        double Stone,
        double Metal,
        double SilverGeneratedByThisCity,
        double ResearchGeneratedByThisCity,
        double WoodProductionPerHour,
        double StoneProductionPerHour,
        double MetalProductionPerHour,
        double SilverProductionPerHourByThisCity,
        double ResearchProductionPerHourByThisCity,
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

        public ResourceSnapshot CalculateCurrent(City cityEntity, DateTime currentDateTime)
        {
            DateTime startTime = DateTime.SpecifyKind(cityEntity.LastResourceUpdate, DateTimeKind.Utc);
            DateTime endTime = DateTime.SpecifyKind(currentDateTime, DateTimeKind.Utc);

            double totalHoursPassedSinceLastUpdate = (endTime - startTime).TotalHours;
            if (totalHoursPassedSinceLastUpdate < 0) totalHoursPassedSinceLastUpdate = 0;

            var woodTags = new[] { ModifierTagEnum.Wood, ModifierTagEnum.ResourceProduction };
            var stoneTags = new[] { ModifierTagEnum.Stone, ModifierTagEnum.ResourceProduction };
            var metalTags = new[] { ModifierTagEnum.Metal, ModifierTagEnum.ResourceProduction };
            var silverTags = new[] { ModifierTagEnum.Silver };
            var researchTags = new[] { ModifierTagEnum.Research };

            // 2. Beregn rater pr. time
            var woodResult = GetProductionResult(cityEntity, BuildingTypeEnum.TimberCamp, woodTags);
            var stoneResult = GetProductionResult(cityEntity, BuildingTypeEnum.StoneQuarry, stoneTags);
            var metalResult = GetProductionResult(cityEntity, BuildingTypeEnum.MetalMine, metalTags);
            var silverResult = GetProductionResult(cityEntity, BuildingTypeEnum.MarketPlace, silverTags);
            var researchResult = GetProductionResult(cityEntity, BuildingTypeEnum.University, researchTags);

            double capacityLimit = _statService.GetWarehouseCapacity(cityEntity);

            return new ResourceSnapshot(
                CalculateNewAmount(cityEntity.Wood, woodResult.FinalValue, totalHoursPassedSinceLastUpdate, capacityLimit),
                CalculateNewAmount(cityEntity.Stone, stoneResult.FinalValue, totalHoursPassedSinceLastUpdate, capacityLimit),
                CalculateNewAmount(cityEntity.Metal, metalResult.FinalValue, totalHoursPassedSinceLastUpdate, capacityLimit),
                silverResult.FinalValue * totalHoursPassedSinceLastUpdate,
                researchResult.FinalValue * totalHoursPassedSinceLastUpdate,
                woodResult.FinalValue,
                stoneResult.FinalValue,
                metalResult.FinalValue,
                silverResult.FinalValue,
                researchResult.FinalValue,
                endTime);
        }

        private double CalculateNewAmount(double currentAmount, double productionRate, double hours, double capacity)
        {
            return Math.Min(capacity, currentAmount + (productionRate * hours));
        }

        private ModifierCalculationResult GetProductionResult(City cityEntity, BuildingTypeEnum buildingType, IEnumerable<ModifierTagEnum> targetTags)
        {
            double baseValue = GetBaseValue(cityEntity, buildingType);
            var providers = new List<IModifierProvider>();

            var targetBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
            if (targetBuilding != null && targetBuilding.Level > 0)
            {
                providers.Add(_buildingData.GetConfig<BuildingLevelData>(buildingType, targetBuilding.Level));
            }

            providers.Add(cityEntity);
            if (cityEntity.WorldPlayer != null)
            {
                providers.Add(cityEntity.WorldPlayer);
                if (cityEntity.WorldPlayer.Alliance != null) providers.Add(cityEntity.WorldPlayer.Alliance);
            }

            return _modifierService.CalculateEntityValueWithModifiers(baseValue, targetTags, providers);
        }

        private double GetBaseValue(City cityEntity, BuildingTypeEnum buildingType)
        {
            if (buildingType == BuildingTypeEnum.MarketPlace)
            {
                // OBJEKTIV RETTELSE: Vi henter nu population direkte fra CityStatService, 
                // som beregner det ud fra Housing-bygninger.
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