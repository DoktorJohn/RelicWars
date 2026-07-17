using Application.Generators;
using Application.Interfaces.IRepositories;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;

namespace Application.Tests;

public class NPCSpawnerServiceTests
{
    [Fact]
    public async Task EnsureNPCVillagesAsync_CreatesDeterministicTargetsAndIsIdempotent()
    {
        var first = CreateSetup();

        int firstCreated = await first.Service.EnsureNPCVillagesAsync();
        int secondCreated = await first.Service.EnsureNPCVillagesAsync();

        Assert.True(firstCreated > 0);
        Assert.Equal(0, secondCreated);
        Assert.Equal(firstCreated, first.CityRepository.MapObjects.Count);
        Assert.Equal(1, first.CityRepository.BatchPersistenceCalls);

        var villages = first.CityRepository.Cities.Where(city => city.IsNPC).ToList();
        Assert.Equal(firstCreated, villages.Count);
        Assert.Equal(villages.Count, villages.Select(city => (city.X, city.Y)).Distinct().Count());
        Assert.All(villages, AssertLegacyVillageBalance);

        foreach (var group in villages.GroupBy(city => GetIsland(city, first.World.MapSeed)))
        {
            var island = WorldGenerationService.GetIslandDefinition(group.Key.X, group.Key.Y, first.World.MapSeed);
            var sites = GetCanonicalSites(first.World, island);
            Assert.Equal(
                NPCSpawnerService.CalculateTargetCount(first.World.MapSeed, group.Key.X, group.Key.Y, sites.Count),
                group.Count());
            Assert.All(group, village => Assert.Contains((village.X, village.Y), sites));
        }

        var second = CreateSetup();
        await second.Service.EnsureNPCVillagesAsync();
        var firstAttributes = villages
            .OrderBy(city => city.X)
            .ThenBy(city => city.Y)
            .Select(VillageAttributes)
            .ToList();
        var secondAttributes = second.CityRepository.Cities
            .Where(city => city.IsNPC)
            .OrderBy(city => city.X)
            .ThenBy(city => city.Y)
            .Select(VillageAttributes)
            .ToList();

        Assert.Equal(firstAttributes, secondAttributes);
    }

    [Fact]
    public async Task EnsureNPCVillagesAsync_PreservesOccupiedSitesAndPersistsCityMapPairsInOneBatch()
    {
        var setup = CreateSetup();
        var island = FindFirstIslandWithSites(setup.World);
        var occupiedSite = GetCanonicalSites(setup.World, island).First();
        var playerCity = new City
        {
            Id = Guid.NewGuid(),
            Name = "Existing capital",
            WorldId = setup.World.Id,
            WorldPlayerId = Guid.NewGuid(),
            X = occupiedSite.X,
            Y = occupiedSite.Y
        };
        setup.CityRepository.Cities.Add(playerCity);

        await setup.Service.EnsureNPCVillagesAsync();

        Assert.Contains(playerCity, setup.CityRepository.Cities);
        Assert.DoesNotContain(
            setup.CityRepository.Cities,
            city => city.IsNPC && city.X == playerCity.X && city.Y == playerCity.Y);
        Assert.Equal(1, setup.CityRepository.BatchPersistenceCalls);
        Assert.All(setup.CityRepository.MapObjects, mapObject =>
        {
            Assert.Contains(setup.CityRepository.Cities, city => city.Id == mapObject.ReferenceEntityId);
        });
    }

    [Theory]
    [InlineData(42069, -3, 4)]
    [InlineData(12345, 7, -2)]
    [InlineData(-99, 0, 0)]
    public void TargetPercentage_IsStableAndWithinConfiguredRange(int seed, int cellX, int cellY)
    {
        int percentage = NPCSpawnerService.CalculateTargetPercentage(seed, cellX, cellY);

        Assert.InRange(percentage, 15, 25);
        Assert.Equal(percentage, NPCSpawnerService.CalculateTargetPercentage(seed, cellX, cellY));
    }

    private static void AssertLegacyVillageBalance(City village)
    {
        Assert.Null(village.WorldPlayerId);
        Assert.Equal(500, village.Wood);
        Assert.Equal(500, village.Stone);
        Assert.Equal(500, village.Metal);
        Assert.Equal(Enum.GetValues<ExoticResourceTypeEnum>().Length, village.ExoticResources.Count);
        Assert.All(village.ExoticResources, resource => Assert.Equal(0, resource.Amount));
        Assert.Equal(
            [BuildingTypeEnum.TimberCamp, BuildingTypeEnum.StoneQuarry],
            village.Buildings.Select(building => building.Type));
        Assert.All(village.Buildings, building => Assert.InRange(building.Level, 1, 2));
        Assert.InRange(Assert.Single(village.UnitStacks).Quantity, 5, 24);
        Assert.True(village.Points > 0);
    }

    private static VillageSnapshot VillageAttributes(City city) => new(
        city.X,
        city.Y,
        city.Name,
        string.Join(",", city.Buildings.Select(building => $"{building.Type}:{building.Level}")),
        city.UnitStacks.Single().Quantity,
        city.Points);

    private static (int X, int Y) GetIsland(City city, int mapSeed)
    {
        Assert.True(WorldGenerationService.TryGetIslandCoordinates(
            city.X,
            city.Y,
            mapSeed,
            out int islandX,
            out int islandY));
        return (islandX, islandY);
    }

    private static WorldGenerationService.IslandDefinition FindFirstIslandWithSites(World world)
    {
        for (int cellX = -10; cellX <= 10; cellX++)
        for (int cellY = -10; cellY <= 10; cellY++)
        {
            if (!WorldGenerationService.IsIslandCellActive(cellX, cellY, world.MapSeed))
            {
                continue;
            }

            var island = WorldGenerationService.GetIslandDefinition(cellX, cellY, world.MapSeed);
            if (GetCanonicalSites(world, island).Count > 0)
            {
                return island;
            }
        }

        throw new InvalidOperationException("No island with canonical sites was found.");
    }

    private static List<(int X, int Y)> GetCanonicalSites(
        World world,
        WorldGenerationService.IslandDefinition island)
    {
        return PlayerCitySiteGenerator.GenerateCanonicalSites(
            island,
            world.MapSeed,
            -world.Width / 2,
            -world.Width / 2 + world.Width - 1,
            -world.Height / 2,
            -world.Height / 2 + world.Height - 1);
    }

    private static Setup CreateSetup()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "NPC test world",
            Width = 100,
            Height = 100,
            MapSeed = 42069
        };
        var cityRepository = new TrackingCityRepository();
        var service = new NPCSpawnerService(
            cityRepository,
            new FixedWorldRepository(world),
            new CityPointCalculator(TestData.BuildingReader()));

        return new Setup(world, service, cityRepository);
    }

    private sealed record Setup(
        World World,
        NPCSpawnerService Service,
        TrackingCityRepository CityRepository);

    private sealed record VillageSnapshot(
        int X,
        int Y,
        string Name,
        string Buildings,
        int Militia,
        int Points);

    private sealed class TrackingCityRepository : ICityRepository
    {
        public List<City> Cities { get; } = [];
        public List<WorldMapObject> MapObjects { get; } = [];
        public int BatchPersistenceCalls { get; private set; }

        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(Cities.Where(city => ids.Contains(city.Id)).ToList());
        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult(Cities.SingleOrDefault(city => city.Id == cityId));
        public Task UpdateAsync(City city) => Task.CompletedTask;
        public Task<List<City>> GetAllAsync() => Task.FromResult(Cities.ToList());
        public Task UpdateRangeAsync(List<City> cities) => Task.CompletedTask;
        public Task AddAsync(City city) { Cities.Add(city); return Task.CompletedTask; }
        public Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities)
        {
            if (cities.Count == 0)
            {
                return Task.CompletedTask;
            }

            BatchPersistenceCalls++;
            Cities.AddRange(cities);
            MapObjects.AddRange(cities.Select(city => new WorldMapObject
            {
                Id = Guid.NewGuid(),
                WorldId = city.WorldId,
                X = (short)city.X,
                Y = (short)city.Y,
                Type = MapObjectTypeEnum.City,
                ReferenceEntityId = city.Id
            }));
            return Task.CompletedTask;
        }
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetByCoordinatesAsync(int x, int y) =>
            Task.FromResult(Cities.SingleOrDefault(city => city.X == x && city.Y == y));
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) =>
            Task.FromResult(Cities.SingleOrDefault(city => city.Id == cityId)?.WorldPlayerId);
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(Cities.Where(city => city.WorldPlayerId == worldPlayerId).ToList());
    }

    private sealed class FixedWorldRepository(World world) : IWorldRepository
    {
        public Task<List<World>> GetAllAsync() => Task.FromResult(new List<World> { world });
        public Task<Dictionary<Guid, int>> GetPlayerCountsByWorldAsync() =>
            Task.FromResult(new Dictionary<Guid, int>());
        public Task<World?> GetByIdAsync(Guid id) => Task.FromResult<World?>(id == world.Id ? world : null);
        public Task<int?> GetWorldSeedAsync(Guid worldId) =>
            Task.FromResult<int?>(worldId == world.Id ? world.MapSeed : null);
    }

}
