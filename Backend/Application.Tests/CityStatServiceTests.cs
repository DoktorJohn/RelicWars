using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Utility;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;

namespace Application.Tests;

public class CityStatServiceTests
{
    [Fact]
    public void PopulationUsage_DeployedMilitiaRemainsChargedToOriginCity()
    {
        var city = CreateCity();
        city.Buildings.Add(new Building { Type = BuildingTypeEnum.Housing, Level = 1, CityId = city.Id });
        city.OriginUnitDeployments.Add(Deployment(city, UnitDeploymentPhaseEnum.Outbound, 1));
        var service = CreateService();

        int usage = service.GetCurrentPopulationUsage(city, Array.Empty<BaseJob>());
        int available = service.GetAvailablePopulation(city, Array.Empty<BaseJob>());

        Assert.Equal(80, service.GetMaxPopulation(city));
        Assert.Equal(3, usage);
        Assert.Equal(77, available);
    }

    [Fact]
    public void PopulationUsage_CountsEveryOriginPhaseButNotTargetDeployments()
    {
        var city = CreateCity();
        city.OriginUnitDeployments.Add(Deployment(city, UnitDeploymentPhaseEnum.Outbound, 1));
        city.OriginUnitDeployments.Add(Deployment(city, UnitDeploymentPhaseEnum.Stationed, 1));
        city.OriginUnitDeployments.Add(Deployment(city, UnitDeploymentPhaseEnum.Returning, 1));

        var otherOrigin = CreateCity();
        var incoming = Deployment(otherOrigin, UnitDeploymentPhaseEnum.Stationed, 20);
        incoming.TargetCity = city;
        incoming.TargetCityId = city.Id;
        city.TargetUnitDeployments.Add(incoming);

        Assert.Equal(9, CreateService().GetCurrentPopulationUsage(city, Array.Empty<BaseJob>()));
    }

    [Fact]
    public void PopulationUsage_AddsGarrisonDeploymentAndRemainingRecruitment()
    {
        var city = CreateCity();
        city.UnitStacks.Add(new UnitStack { Type = UnitTypeEnum.Militia, Quantity = 2, CityId = city.Id });
        city.OriginUnitDeployments.Add(Deployment(city, UnitDeploymentPhaseEnum.Returning, 1));
        var job = new RecruitmentJob
        {
            CityId = city.Id,
            UnitType = UnitTypeEnum.Militia,
            TotalQuantity = 7,
            CompletedQuantity = 2
        };

        Assert.Equal(24, CreateService().GetCurrentPopulationUsage(city, new BaseJob[] { job }));
    }

    [Fact]
    public void PopulationUsage_LossesReleaseCapacityAndReturnDoesNotChangeNetUsage()
    {
        var city = CreateCity();
        var deployment = Deployment(city, UnitDeploymentPhaseEnum.Returning, 2);
        city.OriginUnitDeployments.Add(deployment);
        var service = CreateService();

        Assert.Equal(6, service.GetCurrentPopulationUsage(city, Array.Empty<BaseJob>()));

        deployment.UnitStacks.Single().Quantity = 1;
        Assert.Equal(3, service.GetCurrentPopulationUsage(city, Array.Empty<BaseJob>()));

        city.OriginUnitDeployments.Remove(deployment);
        city.UnitStacks.Add(deployment.UnitStacks.Single());
        Assert.Equal(3, service.GetCurrentPopulationUsage(city, Array.Empty<BaseJob>()));
    }

    private static CityStatService CreateService() =>
        new(TestData.BuildingReader(), TestData.UnitReader(), new NoOpModifierService());

    private static City CreateCity()
    {
        var player = new WorldPlayer { Id = Guid.NewGuid(), Cities = [] };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Population City",
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = [],
            UnitStacks = [],
            OriginUnitDeployments = [],
            TargetUnitDeployments = []
        };
        player.Cities.Add(city);
        return city;
    }

    private static UnitDeployment Deployment(City origin, UnitDeploymentPhaseEnum phase, int quantity) => new()
    {
        OriginCity = origin,
        OriginCityId = origin.Id,
        WorldPlayerId = origin.WorldPlayerId!.Value,
        WorldId = origin.WorldId,
        Phase = phase,
        UnitDeploymentMovementStatus = phase == UnitDeploymentPhaseEnum.Stationed
            ? UnitDeploymentMovementStatusEnum.Stationed
            : UnitDeploymentMovementStatusEnum.Moving,
        UnitStacks = [new UnitStack { Type = UnitTypeEnum.Militia, Quantity = quantity }]
    };

    private sealed class NoOpModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(
            double baseValue,
            IEnumerable<ModifierTagEnum> targetTags,
            IEnumerable<IModifierProvider> providers) => Result(baseValue);

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags) => Result(baseValue);
        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) => Result(baseValue);
        public ModifierCalculationResult CalculateCityUnitValue(City city, UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) => Result(baseValue);

        private static ModifierCalculationResult Result(double value) => new() { BaseValue = value, FinalValue = value };
    }
}
