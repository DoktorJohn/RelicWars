using Application.Services.Buildings;
using Domain.Entities;
using Domain.Enums;
using Domain.User;

namespace Application.Tests;

public class WarehouseAndMarketplacePreviewTests
{
    [Fact]
    public async Task WarehousePreviewShowsFiveRowsNearMaxLevel()
    {
        var city = CreateCityWithBuilding(BuildingTypeEnum.Warehouse, 19);
        var service = new WarehouseService(
            new MemoryCityRepository(city),
            TestData.BuildingReader(),
            TestData.ModifierService(out _),
            new TestPlayerAccessService(cities: [city]));

        var result = await service.GetWarehouseProjectionAsync(city.Id);

        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { 16, 17, 18, 19, 20 }, result.Select(item => item.Level).ToArray());
        Assert.Contains(result, item => item.IsCurrentLevel && item.Level == 19);
    }

    [Fact]
    public async Task MarketPlacePreviewShowsFiveRowsNearMaxLevel()
    {
        var city = CreateCityWithBuilding(BuildingTypeEnum.MarketPlace, 19);
        var service = new MarketPlaceService(
            new MemoryCityRepository(city),
            TestData.BuildingReader(),
            TestData.ModifierService(out _),
            new TestPlayerAccessService(cities: [city]));

        var result = await service.GetMarketPlaceInfoAsync(city.Id);

        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { 16, 17, 18, 19, 20 }, result.Select(item => item.Level).ToArray());
        Assert.Contains(result, item => item.IsCurrentLevel && item.Level == 19);
    }

    private static City CreateCityWithBuilding(BuildingTypeEnum buildingType, int level)
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            CompletedResearches = new(),
            Cities = new(),
            Ideology = IdeologyTypeEnum.Feudalism
        };

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = $"{buildingType} City",
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = new List<Building>
            {
                new Building
                {
                    Id = Guid.NewGuid(),
                    Type = buildingType,
                    Level = level,
                    CityId = Guid.NewGuid()
                }
            },
            UnitStacks = new(),
            ActiveFocuses = new()
        };

        player.Cities.Add(city);
        return city;
    }
}
