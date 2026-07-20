using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class ResourceServiceProductionTests
{
    [Fact]
    public void CalculateCityProduction_GoldIsNetAfterSeparateUnitAndBuildingUpkeep()
    {
        var city = CreateCity();
        city.Buildings.Add(new Building { Type = BuildingTypeEnum.TimberCamp, Level = 1, CityId = city.Id });
        city.UnitStacks.Add(new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 2, CityId = city.Id });
        city.OriginUnitDeployments.Add(new UnitDeployment
        {
            OriginCity = city,
            OriginCityId = city.Id,
            WorldPlayerId = city.WorldPlayerId!.Value,
            WorldId = city.WorldPlayer!.WorldId,
            UnitStacks = [new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 1 }]
        });
        var service = CreateService(new SelectiveModifierService());

        var production = service.CalculateCityProduction(city.WorldPlayer!, city);

        int unitPopulation = TestData.UnitReader().GetUnit(UnitTypeEnum.Militia).PopulationCost * 3;
        int buildingUpkeep = TestData.BuildingReader()
            .GetConfig<BuildingLevelData>(BuildingTypeEnum.TimberCamp, 1).UpkeepCost;
        double expected = 700d - ((unitPopulation * 7d * 2d) + (buildingUpkeep * 0.5d));
        Assert.Equal(expected, production.CoinsProductionPerHour);
    }

    [Fact]
    public void CityProductionContributions_SumToGlobalProductionForAllGlobalResources()
    {
        var player = new WorldPlayer { Id = Guid.NewGuid(), Cities = new List<City>(), LastResourceUpdate = TestData.Now };
        for (int index = 0; index < 3; index++)
        {
            var city = CreateCity(player);
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.University, Level = 1, CityId = city.Id });
        }

        var service = CreateService(new SelectiveModifierService());
        var contributions = player.Cities.Select(city => service.CalculateCityProduction(player, city)).ToList();
        var global = service.CalculateGlobalResources(player, TestData.Now);

        Assert.Equal(global.CoinsProductionPerHour, contributions.Sum(item => item.CoinsProductionPerHour));
        Assert.Equal(global.ResearchPointsPerHour, contributions.Sum(item => item.ResearchPointsPerHour));
        Assert.Equal(global.IdeologyFocusPointsPerHour, contributions.Sum(item => item.IdeologyFocusPointsPerHour));
    }

    private static ResourceService CreateService(IModifierService modifierService) => new(
        TestData.BuildingReader(),
        TestData.ResearchReader(),
        TestData.IdeologyReader(),
        new FixedProductionCityStatService(),
        modifierService,
        NullLogger<ResourceService>.Instance);

    private static City CreateCity(WorldPlayer? player = null)
    {
        player ??= new WorldPlayer { Id = Guid.NewGuid(), Cities = new List<City>(), LastResourceUpdate = TestData.Now };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Production City",
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = new List<Building>(),
            UnitStacks = new List<UnitStack>(),
            OriginUnitDeployments = new List<UnitDeployment>(),
            LastResourceUpdate = TestData.Now
        };
        player.Cities.Add(city);
        return city;
    }

    private sealed class FixedProductionCityStatService : ICityStatService
    {
        public double GetWarehouseCapacity(City city) => 1_000;
        public int GetMaxPopulation(City city) => 100;
        public int GetCurrentPopulationUsage(City city, IEnumerable<BaseJob> activeJobs) =>
            city.UnitStacks
                .Concat(city.OriginUnitDeployments.SelectMany(deployment => deployment.UnitStacks))
                .Sum(stack => TestData.UnitReader().GetUnit(stack.Type).PopulationCost * stack.Quantity);
        public int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs) => 100;
    }

    private sealed class SelectiveModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(
            double baseValue,
            IEnumerable<ModifierTagEnum> targetTags,
            IEnumerable<IModifierProvider> providers)
        {
            double multiplier = targetTags.Contains(ModifierTagEnum.Ideology) ? 1.1d : 1d;
            return Result(baseValue, multiplier);
        }

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags)
        {
            double multiplier = targetTags.Contains(ModifierTagEnum.UnitUpkeep)
                ? 2d
                : targetTags.Contains(ModifierTagEnum.BuildingUpkeep) ? 0.5d : 1d;
            return Result(baseValue, multiplier);
        }

        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) => Result(baseValue, 1d);
        public ModifierCalculationResult CalculateCityUnitValue(City city, UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) => Result(baseValue, 1d);

        private static ModifierCalculationResult Result(double baseValue, double multiplier) => new()
        {
            BaseValue = baseValue,
            PercentageBonus = multiplier,
            FinalValue = baseValue * multiplier
        };
    }
}
