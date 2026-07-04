using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Domain.Workers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class CityServiceTownHallTests
{
    [Fact]
    public async Task GetAvailableBuildingsForTownHallAsync_IncludesMissingBuildingsAsLevelOneOptions()
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            Cities = new List<City>()
        };

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Capital",
            WorldId = player.WorldId,
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Wood = 10_000,
            Stone = 10_000,
            Metal = 10_000,
            Buildings = new List<Building>
            {
                new() { Type = BuildingTypeEnum.TownHall, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.Warehouse, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.Housing, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.TimberCamp, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.StoneQuarry, Level = 1, CityId = Guid.NewGuid() },
                new() { Type = BuildingTypeEnum.MetalMine, Level = 1, CityId = Guid.NewGuid() }
            }
        };
        player.Cities.Add(city);

        var service = new CityService(
            new MemoryCityRepository(city),
            new NoOpResourceService(),
            new NoOpWorldPlayerService(),
            new TestPlayerAccessService(cities: [city]),
            new NoOpModifierService(),
            new FixedCityStatService(),
            new NoOpExoticResourceService(),
            TestData.BuildingReader(),
            TestData.UnitReader(),
            new EmptyJobRepository(),
            NullLogger<CityService>.Instance,
            new ConstructionTimeCalculator(new NoOpModifierService()),
            new NoOpResistanceService());

        var result = await service.GetAvailableBuildingsForTownHallAsync(city.Id);

        Assert.Equal(12, result.Count);
        Assert.Contains(result, item => item.BuildingType == BuildingTypeEnum.TownHall && item.IsConstructed && item.CurrentLevel == 1);
        Assert.Contains(result, item => item.BuildingType == BuildingTypeEnum.Barracks && !item.IsConstructed && item.CurrentLevel == null);
        Assert.All(result, item => Assert.True(item.WoodCost >= 0 && item.StoneCost >= 0 && item.MetalCost >= 0));
    }

    private sealed class NoOpWorldPlayerService : IWorldPlayerService
    {
        public Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid worldId) => throw new NotSupportedException();
        public Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId) => throw new NotSupportedException();
        public Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description) => throw new NotSupportedException();
        public Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId) => throw new NotSupportedException();
        public Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query) => throw new NotSupportedException();
        public void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime) { }
        public Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request) => throw new NotSupportedException();
    }

    private sealed class NoOpResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime) =>
            new(cityEntity.Wood, cityEntity.Stone, cityEntity.Metal, 0, 0, 0, currentDateTime);

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime) =>
            new(0, 0, 0, 0, 0, 0, currentDateTime);
    }

    private sealed class NoOpModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<Domain.Abstraction.IModifierProvider> providers) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityUnitValue(City city, Domain.StaticData.Data.UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }

    private sealed class NoOpExoticResourceService : IExoticResourceService
    {
        public Task<List<CityExoticResourceDTO>> SyncCityExoticResourcesAsync(City city, DateTime currentDateTime) => throw new NotSupportedException();
        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesAsync(Guid islandId) => throw new NotSupportedException();
        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesForCityAsync(City city) => throw new NotSupportedException();
        public Task<List<CityExoticResourceProductionDTO>> GetProductionBreakdownsForCityAsync(City city) => Task.FromResult(new List<CityExoticResourceProductionDTO>());
        public Task<ExoticResourceInvestmentResponseDTO> InvestAsync(Guid cityId, ExoticResourceInvestmentRequestDTO request) => throw new NotSupportedException();
    }

    private sealed class NoOpResistanceService : IResistanceService
    {
        public double CalculateRecoveryPerHour(City city) => 0;
        public void UpdateResistance(City city, DateTime now) { }
    }
}
