using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class ExoticResourceServiceTests
{
    [Fact]
    public async Task SyncCityExoticResourcesAsync_AccumulatesHourlyOutput()
    {
        var fixture = await CreateFixtureAsync();
        var currentTime = TestData.Now;

        fixture.City.LastExoticResourceUpdate = currentTime.AddHours(-1);
        await fixture.Service.SyncCityExoticResourcesAsync(fixture.City, currentTime);

        var cloth = fixture.City.ExoticResources.Single(resource => resource.ResourceType == ExoticResourceTypeEnum.Cloth);

        Assert.Equal(2.64, cloth.Amount, precision: 2);
    }

    [Fact]
    public async Task InvestAsync_ConsumesNativeResources_AndAdvancesTier()
    {
        var fixture = await CreateFixtureAsync();

        fixture.City.Wood = 200_000;
        fixture.City.Stone = 200_000;
        fixture.City.Metal = 200_000;
        fixture.Player.Coins = 200_000;

        var result = await fixture.Service.InvestAsync(
            fixture.City.Id,
            new ExoticResourceInvestmentRequestDTO(
                SlotIndex: 0,
                WoodAmount: 200_000,
                StoneAmount: 200_000,
                MetalAmount: 200_000,
                CoinAmount: 200_000));

        var slot = fixture.Island.ExoticResources.Single(resource => resource.SlotIndex == 0);

        Assert.Equal(2, slot.Tier);
        Assert.Equal(0, fixture.City.Wood);
        Assert.Equal(0, fixture.City.Stone);
        Assert.Equal(0, fixture.City.Metal);
        Assert.Equal(0, fixture.Player.Coins);
        Assert.Equal(33_190, slot.WoodInvestment);
        Assert.Equal(33_190, slot.StoneInvestment);
        Assert.Equal(33_190, slot.MetalInvestment);
        Assert.Equal(33_190, slot.CoinInvestment);
        Assert.Equal(2, result.NewTier);
    }

    [Fact]
    public async Task SyncCityExoticResourcesAsync_DoesNotCreateMissingInventoryDuringRead()
    {
        var fixture = await CreateFixtureAsync();
        fixture.City.ExoticResources.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.SyncCityExoticResourcesAsync(fixture.City, TestData.Now));

        Assert.Contains("ufuldstændig exotic resource-beholdning", exception.Message);
        Assert.Empty(fixture.City.ExoticResources);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task InvestAsync_RejectsNonFiniteAmounts(double amount)
    {
        var fixture = await CreateFixtureAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.InvestAsync(
            fixture.City.Id,
            new ExoticResourceInvestmentRequestDTO(0, amount, 0, 0, 0)));

        Assert.Equal(200_000, fixture.City.Wood);
        Assert.Equal(0, fixture.Island.ExoticResources.Single().WoodInvestment);
    }

    [Fact]
    public async Task InvestAsync_UsesTransactionBoundary()
    {
        var transactionManager = new RecordingTransactionManager();
        var fixture = await CreateFixtureAsync(transactionManager);

        await fixture.Service.InvestAsync(
            fixture.City.Id,
            new ExoticResourceInvestmentRequestDTO(0, 100_000, 100_000, 100_000, 100_000));

        Assert.Equal(1, transactionManager.ExecutionCount);
    }

    [Fact]
    public async Task InvestAsync_WithInsufficientResources_DoesNotMutateInvestmentPool()
    {
        var fixture = await CreateFixtureAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.InvestAsync(
            fixture.City.Id,
            new ExoticResourceInvestmentRequestDTO(0, 200_001, 0, 0, 0)));

        Assert.Equal(200_000, fixture.City.Wood);
        Assert.Equal(0, fixture.Island.ExoticResources.Single().WoodInvestment);
    }

    [Fact]
    public async Task InvestAsync_AtTierTen_DoesNotDeductResources()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Island.ExoticResources.Single().Tier = 10;

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.InvestAsync(
            fixture.City.Id,
            new ExoticResourceInvestmentRequestDTO(0, 1, 1, 1, 1)));

        Assert.Equal(200_000, fixture.City.Wood);
        Assert.Equal(200_000, fixture.Player.Coins);
    }

    [Fact]
    public async Task InvestAsync_SynchronizesEveryCityOnIslandBeforeChangingTier()
    {
        var fixture = await CreateFixtureAsync();
        var lastUpdate = DateTime.UtcNow.AddHours(-1);
        fixture.City.LastExoticResourceUpdate = lastUpdate;
        var secondCity = CreateCityOnIsland(fixture.Player, fixture.Island, lastUpdate);
        fixture.Context.Cities.Add(secondCity);
        fixture.Context.WorldMapObjects.Add(new WorldMapObject
        {
            Id = Guid.NewGuid(),
            WorldId = secondCity.WorldId,
            X = (short)secondCity.X,
            Y = (short)secondCity.Y,
            Type = MapObjectTypeEnum.City,
            ReferenceEntityId = secondCity.Id
        });
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.InvestAsync(
            fixture.City.Id,
            new ExoticResourceInvestmentRequestDTO(0, 200_000, 200_000, 200_000, 200_000));

        Assert.InRange(fixture.City.ExoticResources.Single(resource => resource.ResourceType == ExoticResourceTypeEnum.Cloth).Amount, 2.64, 2.65);
        Assert.InRange(secondCity.ExoticResources.Single(resource => resource.ResourceType == ExoticResourceTypeEnum.Cloth).Amount, 2.64, 2.65);
        Assert.Equal(2, fixture.Island.ExoticResources.Single().Tier);
    }

    [Fact]
    public async Task WorldIslandExoticResource_RowVersionIsAConcurrencyToken()
    {
        var fixture = await CreateFixtureAsync();
        var property = fixture.Context.Model
            .FindEntityType(typeof(WorldIslandExoticResource))!
            .FindProperty(nameof(WorldIslandExoticResource.RowVersion))!;

        Assert.True(property.IsConcurrencyToken);
    }

    private static async Task<Fixture> CreateFixtureAsync(ITransactionManager? transactionManager = null)
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GameContext(options);

        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Test World",
            MapSeed = 42069,
            Width = 1000,
            Height = 1000
        };

        var islandDefinition = WorldGenerationService.GetIslandDefinition(0, 0, world.MapSeed);
        var island = new WorldIsland
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            CellX = 0,
            CellY = 0,
            CenterX = islandDefinition.CenterX,
            CenterY = islandDefinition.CenterY,
            Shape = (IslandShapeEnum)islandDefinition.Shape,
            MajorRadius = islandDefinition.MajorRadius,
            MinorRadius = islandDefinition.MinorRadius,
            RotationDegrees = islandDefinition.RotationDegrees,
            EdgeRoughness = islandDefinition.EdgeRoughness,
            ExoticResources =
            [
                new WorldIslandExoticResource
                {
                    Id = Guid.NewGuid(),
                    SlotIndex = 0,
                    ResourceType = ExoticResourceTypeEnum.Cloth,
                    Tier = 1
                }
            ]
        };

        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            Coins = 200_000,
            ResearchPoints = 0,
            IdeologyFocusPoints = 0,
            Ideology = IdeologyTypeEnum.Feudalism,
            Cities = new List<City>(),
            CompletedResearches = new List<Research>()
        };

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Test City",
            WorldId = world.Id,
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            X = island.CenterX,
            Y = island.CenterY,
            Wood = 200_000,
            Stone = 200_000,
            Metal = 200_000,
            LastResourceUpdate = TestData.Now,
            LastExoticResourceUpdate = TestData.Now.AddHours(-1),
            ExoticResources = Enum.GetValues<ExoticResourceTypeEnum>()
                .Select(resourceType => new CityExoticResource
                {
                    Id = Guid.NewGuid(),
                    ResourceType = resourceType,
                    Amount = 0
                })
                .ToList(),
            Buildings = new List<Building>(),
            UnitStacks = new List<UnitStack>(),
            ActiveFocuses = new List<IdeologyFocus>()
        };

        player.Cities.Add(city);

        context.AddRange(world, island, player, city);
        context.WorldMapObjects.Add(new WorldMapObject
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            X = (short)city.X,
            Y = (short)city.Y,
            Type = MapObjectTypeEnum.City,
            ReferenceEntityId = city.Id
        });
        await context.SaveChangesAsync();

        var cityRepository = new CityRepository(context);
        var islandRepository = new WorldIslandRepository(context);
        var worldRepository = new WorldRepository(context);
        var mapObjectRepository = new WorldMapObjectRepository(context);

        var modifierService = TestData.ModifierService(out _);
        var resourceService = new ResourceService(
            TestData.BuildingReader(),
            TestData.ResearchReader(),
            TestData.IdeologyReader(),
            TestData.UnitReader(),
            new LargeCapacityCityStatService(),
            modifierService,
            NullLogger<ResourceService>.Instance);

        var exoticReader = new ExoticResourceDataReader();
        exoticReader.Load(TestData.GameFile("exotic-resources.json"));

        var service = new ExoticResourceService(
            cityRepository,
            islandRepository,
            worldRepository,
            mapObjectRepository,
            new FakePlayerAccessService(city, player),
            resourceService,
            new FakeWorldPlayerService(),
            exoticReader,
            NullLogger<ExoticResourceService>.Instance,
            transactionManager ?? new ImmediateTransactionManager());

        return new Fixture(context, service, city, island, player);
    }

    private static City CreateCityOnIsland(WorldPlayer player, WorldIsland island, DateTime lastUpdate)
    {
        return new City
        {
            Id = Guid.NewGuid(),
            Name = "Second City",
            WorldId = island.WorldId,
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            X = island.CenterX,
            Y = island.CenterY,
            LastResourceUpdate = lastUpdate,
            LastExoticResourceUpdate = lastUpdate,
            ExoticResources = Enum.GetValues<ExoticResourceTypeEnum>()
                .Select(resourceType => new CityExoticResource
                {
                    Id = Guid.NewGuid(),
                    ResourceType = resourceType
                })
                .ToList()
        };
    }

    private sealed record Fixture(GameContext Context, ExoticResourceService Service, City City, WorldIsland Island, WorldPlayer Player) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }

    private sealed class FakePlayerAccessService : IPlayerAccessService
    {
        private readonly City _city;
        private readonly WorldPlayer _player;

        public FakePlayerAccessService(City city, WorldPlayer player)
        {
            _city = city;
            _player = player;
        }

        public Guid GetAuthenticatedProfileId() => Guid.NewGuid();
        public Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId) => Task.FromResult(_player);
        public Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId) => Task.FromResult(_player);
        public Task<City> RequireOwnedCityAsync(Guid cityId) => Task.FromResult(_city);
        public Task<City> RequireOwnedCityForTownHallAsync(Guid cityId) => Task.FromResult(_city);
        public Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId) => throw new NotImplementedException();
    }

    private sealed class FakeWorldPlayerService : IWorldPlayerService
    {
        public void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime) { }
        public Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid worldId) => throw new NotImplementedException();
        public Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId) => throw new NotImplementedException();
        public Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description) => throw new NotImplementedException();
        public Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId) => throw new NotImplementedException();
        public Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query) => throw new NotImplementedException();
        public Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request) => throw new NotImplementedException();
    }

    private sealed class LargeCapacityCityStatService : ICityStatService
    {
        public int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs) => 0;
        public int GetCurrentPopulationUsage(City city, IEnumerable<BaseJob> activeJobs) => 0;
        public int GetMaxPopulation(City city) => 0;
        public double GetWarehouseCapacity(City city) => 1_000_000;
    }

    private sealed class RecordingTransactionManager : ITransactionManager
    {
        public int ExecutionCount { get; private set; }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            ExecutionCount++;
            await operation();
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            ExecutionCount++;
            return await operation();
        }
    }
}
