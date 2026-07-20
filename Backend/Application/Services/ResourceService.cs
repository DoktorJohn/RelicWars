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

    public record CityProductionSnapshot(
        double CoinsProductionPerHour,
        double ResearchPointsPerHour,
        double IdeologyFocusPointsPerHour);

    public record GlobalResourceSnapshot(
        double CoinsAmount,
        double ResearchPoints,
        double IdeologyFocusPoints,
        double CoinsProductionPerHour,
        double ResearchPointsPerHour,
        double IdeologyFocusPointsPerHour,
        DateTime Timestamp)
    {
        public double TotalAvailablePopulation { get; init; }
    }

    public class ResourceService : IResourceService
    {
        private readonly BuildingDataReader _buildingData;
        private readonly ResearchDataReader _researchData;
        private readonly IdeologyDataReader _ideologyData;
        private readonly ICityStatService _statService;
        private readonly IModifierService _modifierService;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(
            BuildingDataReader buildingData,
            ResearchDataReader researchData,
            IdeologyDataReader ideologyData,
            ICityStatService statService,
            IModifierService modifierService,
            ILogger<ResourceService> logger)
        {
            _buildingData = buildingData;
            _researchData = researchData;
            _ideologyData = ideologyData;
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
            _logger.LogInformation("[ResourceService] Calculating global resources for Player {PlayerId}. Hours passed: {HoursPassed:F4}. Last Update: {LastUpdate}, Current: {Current}", 
                playerEntity.Id, hoursPassed, playerEntity.LastResourceUpdate, currentDateTime);

            double totalCoinsRate = 0;
            double totalResearchRate = 0;
            int totalAvailablePopulation = 0;

            double totalIdeologyRate = CalculateTotalIdeologyProduction(playerEntity);
            for (int cityIndex = 0; cityIndex < playerEntity.Cities.Count; cityIndex++)
            {
                var city = playerEntity.Cities[cityIndex];
                var production = CalculateCityProduction(playerEntity, city, totalIdeologyRate, cityIndex);

                totalCoinsRate += production.CoinsProductionPerHour;
                totalResearchRate += production.ResearchPointsPerHour;
                totalAvailablePopulation += Math.Max(0, _statService.GetAvailablePopulation(city, Array.Empty<Domain.Workers.Abstraction.BaseJob>()));
            }

            // Globale ressourcer
            double newCoinsAmount = playerEntity.Coins + (totalCoinsRate * hoursPassed);
            double newResearchAmount = playerEntity.ResearchPoints + (totalResearchRate * hoursPassed);
            double newIdeologyAmount = playerEntity.IdeologyFocusPoints + (totalIdeologyRate * hoursPassed);

            _logger.LogInformation("[ResourceService] Global Calc Result: Total Coins Rate: {TotalCoinsRate}, Old Coins: {OldCoins}, New Coins: {NewCoins}", 
                totalCoinsRate, playerEntity.Coins, newCoinsAmount);

            return new GlobalResourceSnapshot(
                newCoinsAmount,
                newResearchAmount,
                newIdeologyAmount,
                totalCoinsRate,
                totalResearchRate,
                totalIdeologyRate,
                currentDateTime
            )
            {
                TotalAvailablePopulation = totalAvailablePopulation
            };
        }

        public CityProductionSnapshot CalculateCityProduction(WorldPlayer playerEntity, City cityEntity)
        {
            int cityIndex = playerEntity.Cities.FindIndex(city => city.Id == cityEntity.Id);
            if (cityIndex < 0)
            {
                throw new ArgumentException("City must belong to the supplied world player.", nameof(cityEntity));
            }

            return CalculateCityProduction(playerEntity, cityEntity, CalculateTotalIdeologyProduction(playerEntity), cityIndex);
        }

        private CityProductionSnapshot CalculateCityProduction(
            WorldPlayer playerEntity,
            City cityEntity,
            double totalIdeologyRate,
            int cityIndex)
        {
            double ideologyContribution = 0d;
            int cityCount = playerEntity.Cities.Count;
            if (cityCount > 0)
            {
                double equalShare = totalIdeologyRate / cityCount;
                ideologyContribution = cityIndex == cityCount - 1
                    ? totalIdeologyRate - (equalShare * (cityCount - 1))
                    : equalShare;
            }

            return new CityProductionSnapshot(
                GetCoinsProductionResult(cityEntity),
                GetProductionResult(cityEntity, BuildingTypeEnum.University, new[] { ModifierTagEnum.Research }).FinalValue,
                ideologyContribution);
        }

        private double CalculateTotalIdeologyProduction(WorldPlayer playerEntity)
        {
            var result = _modifierService.CalculateEntityValueWithModifiers(
                playerEntity.Cities.Count,
                new[] { ModifierTagEnum.Ideology },
                new List<IModifierProvider> { playerEntity });
            return result.FinalValue;
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
            return _modifierService.CalculateCityValue(cityEntity, baseValue, targetTags.ToArray());
        }

        private double GetCoinsProductionResult(City cityEntity)
        {
            //Calculate coins INCOME
            double baseProductionValue = _statService.GetMaxPopulation(cityEntity) * 7.0;
            var coinsProduction = _modifierService.CalculateCityValue(cityEntity, baseProductionValue,
                ModifierTagEnum.Coins, ModifierTagEnum.Market);

            //Calculate coins EXPENDITURE
            int unitPopulation = _statService.GetCurrentPopulationUsage(
                cityEntity,
                Array.Empty<Domain.Workers.Abstraction.BaseJob>());

            int buildingUpkeepCost = cityEntity.Buildings.Sum(building => _buildingData.GetConfig<BuildingLevelData>(building.Type, building.Level).UpkeepCost);

            double flatUnitCoinsExpenditure = unitPopulation * 7;
            var unitExpenditure = _modifierService.CalculateCityValue(cityEntity, flatUnitCoinsExpenditure,
                ModifierTagEnum.Upkeep, ModifierTagEnum.UnitUpkeep);
            var buildingExpenditure = _modifierService.CalculateCityValue(cityEntity, buildingUpkeepCost,
                ModifierTagEnum.Upkeep, ModifierTagEnum.BuildingUpkeep);
            double finalExpenditure = unitExpenditure.FinalValue + buildingExpenditure.FinalValue;
            double netCoins = coinsProduction.FinalValue - finalExpenditure;

            _logger.LogInformation("[ResourceService] City {CityName} Coins Breakdown: Income Base={IncomeBase} -> Final={IncomeFinal}. Expenditure Base={ExpBase} (Units={UnitExp}, Buildings={BuildExp}) -> Final={ExpFinal}. Net={Net}",
                cityEntity.Name, baseProductionValue, coinsProduction.FinalValue, flatUnitCoinsExpenditure + buildingUpkeepCost, flatUnitCoinsExpenditure, buildingUpkeepCost, finalExpenditure, netCoins);

            return netCoins;
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
