using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services.Buildings;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;

namespace Application.Tests;

public class ResourceBuildingServiceTests
{
    [Theory]
    [InlineData(18, new[] { 18, 19, 20 })]
    [InlineData(20, new[] { 20 })]
    public async Task GetResourceBuildingInfoAsync_ProjectsThroughConfiguredMaximum(int currentLevel, int[] expectedLevels)
    {
        var player = new WorldPlayer { Id = Guid.NewGuid(), CompletedResearches = new() };
        var city = new City
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Buildings = [new Building { Type = BuildingTypeEnum.TimberCamp, Level = currentLevel }]
        };
        var service = new ResourceBuildingService(
            new MemoryCityRepository(city),
            TestData.BuildingReader(),
            new IdentityModifierService(),
            new TestPlayerAccessService([player], [city]));

        var result = await service.GetResourceBuildingInfoAsync(city.Id, BuildingTypeEnum.TimberCamp);

        Assert.Equal(expectedLevels, result.Select(level => level.Level));
        Assert.True(result[0].IsCurrentLevel);
    }

    private sealed class IdentityModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<IModifierProvider> providers) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
        public ModifierCalculationResult CalculateCityUnitValue(City city, UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }
}
