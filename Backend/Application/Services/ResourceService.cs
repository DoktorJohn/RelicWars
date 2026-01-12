using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Services.ResourceService;

namespace Application.Services
{
    public record ResourceSnapshot(
    double Wood,
    double Stone,
    double Metal,
    double WoodProductionPerHour,
    double StoneProductionPerHour,
    double MetalProductionPerHour,
    DateTime Timestamp);

    public class ResourceService : IResourceService
    {
        private readonly BuildingDataReader _buildingData;
        private readonly ICityStatService _statService;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(BuildingDataReader buildingData, ICityStatService statService, ILogger<ResourceService> logger)
        {
            _buildingData = buildingData;
            _statService = statService;
            _logger = logger;
        }

        // Vi tilføjer List<Modifier> her, så vi kan regne research med
        public ResourceSnapshot CalculateCurrent(City cityEntity, DateTime currentDateTime, List<Modifier>? externalAccountModifiers = null)
        {
            externalAccountModifiers ??= cityEntity.WorldPlayer?.ModifiersAppliedToWorldPlayer;

            DateTime startTime = DateTime.SpecifyKind(cityEntity.LastResourceUpdate, DateTimeKind.Utc);
            DateTime endTime = DateTime.SpecifyKind(currentDateTime, DateTimeKind.Utc);

            double totalHoursPassedSinceLastUpdate = (endTime - startTime).TotalHours;
            if (totalHoursPassedSinceLastUpdate < 0) totalHoursPassedSinceLastUpdate = 0;

            // LOGGING: Tjekker om bygninger er indlæst
            if (cityEntity.Buildings == null || !cityEntity.Buildings.Any())
            {
                _logger.LogWarning($"[ResourceService] ADVARSEL: Ingen bygninger fundet for by {cityEntity.Id}. Produktion bliver 0!");
            }

            double hourlyWoodProduction = GetProductionPerHour(cityEntity, BuildingTypeEnum.TimberCamp, externalAccountModifiers);
            double hourlyStoneProduction = GetProductionPerHour(cityEntity, BuildingTypeEnum.StoneQuarry, externalAccountModifiers);
            double hourlyMetalProduction = GetProductionPerHour(cityEntity, BuildingTypeEnum.MetalMine, externalAccountModifiers);

            // LOGGING: Tjekker de beregnede rater
            _logger.LogInformation($"[ResourceService] By {cityEntity.Name}: Delta={totalHoursPassedSinceLastUpdate:F4}t, WoodRate={hourlyWoodProduction}/t, CurrentWood={cityEntity.Wood}");

            double capacityLimit = _statService.GetWarehouseCapacity(cityEntity);

            double newWoodAmount = Math.Min(capacityLimit, cityEntity.Wood + (hourlyWoodProduction * totalHoursPassedSinceLastUpdate));
            double newStoneAmount = Math.Min(capacityLimit, cityEntity.Stone + (hourlyStoneProduction * totalHoursPassedSinceLastUpdate));
            double newMetalAmount = Math.Min(capacityLimit, cityEntity.Metal + (hourlyMetalProduction * totalHoursPassedSinceLastUpdate));

            return new ResourceSnapshot(newWoodAmount, newStoneAmount, newMetalAmount, hourlyWoodProduction, hourlyStoneProduction, hourlyMetalProduction, endTime);
        }

        private double GetProductionPerHour(City cityEntity, BuildingTypeEnum resourceBuildingType, List<Modifier>? activeAccountModifiers = null)
        {
            var targetBuilding = cityEntity.Buildings.FirstOrDefault(building => building.Type == resourceBuildingType);

            // Hvis bygningen ikke findes eller er i niveau 0, produceres der intet.
            if (targetBuilding == null || targetBuilding.Level == 0)
            {
                return 0.0;
            }

            double baseProductionValueFromStaticData = 0.0;

            // OBJEKTIV FIX: Vi kalder GetConfig med den SPECIFIKKE type-parameter i stedet for base-klassen.
            // Dette sikrer, at deserializeren/læseren returnerer et objekt med ProductionPerHour feltet udfyldt.
            switch (resourceBuildingType)
            {
                case BuildingTypeEnum.TimberCamp:
                    var timberConfig = _buildingData.GetConfig<TimberCampLevelData>(resourceBuildingType, targetBuilding.Level);
                    baseProductionValueFromStaticData = timberConfig?.ProductionPerHour ?? 0.0;
                    break;

                case BuildingTypeEnum.StoneQuarry:
                    var stoneConfig = _buildingData.GetConfig<StoneQuarryLevelData>(resourceBuildingType, targetBuilding.Level);
                    baseProductionValueFromStaticData = stoneConfig?.ProductionPerHour ?? 0.0;
                    break;

                case BuildingTypeEnum.MetalMine:
                    var metalConfig = _buildingData.GetConfig<MetalMineLevelData>(resourceBuildingType, targetBuilding.Level);
                    baseProductionValueFromStaticData = metalConfig?.ProductionPerHour ?? 0.0;
                    break;

                default:
                    baseProductionValueFromStaticData = 0.0;
                    break;
            }

            // Definer hvilke tags denne produktion reagerer på i StatCalculator
            var relevantModifierTags = new List<ModifierTagEnum> { ModifierTagEnum.ResourceProduction };

            if (resourceBuildingType == BuildingTypeEnum.TimberCamp)
                relevantModifierTags.Add(ModifierTagEnum.Wood);
            else if (resourceBuildingType == BuildingTypeEnum.StoneQuarry)
                relevantModifierTags.Add(ModifierTagEnum.Stone);
            else if (resourceBuildingType == BuildingTypeEnum.MetalMine)
                relevantModifierTags.Add(ModifierTagEnum.Metal);

            // Beregn slutværdien via StatCalculator
            return StatCalculator.ApplyModifiers(baseProductionValueFromStaticData, relevantModifierTags, activeAccountModifiers);
        }
    }
}
