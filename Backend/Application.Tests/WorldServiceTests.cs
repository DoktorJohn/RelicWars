using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Utility;
using Domain.Entities;
using Domain.StaticData.Generators;
using Domain.User;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class WorldServiceTests
{
    [Fact]
    public async Task GetWorldMapChunk_GeneratesSitesWithOneMapObjectAreaQuery()
    {
        const int seed = 42069;
        var activeCell = (
            from cellX in Enumerable.Range(-10, 21)
            from cellY in Enumerable.Range(-10, 21)
            where WorldGenerationService.IsIslandCellActive(cellX, cellY, seed)
            select (cellX, cellY)).First();
        var definition = WorldGenerationService.GetIslandDefinition(activeCell.cellX, activeCell.cellY, seed);
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Performance test world",
            Width = 2_000,
            Height = 2_000,
            MapSeed = seed
        };
        var viewer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            PlayerProfileId = Guid.NewGuid()
        };
        var island = new WorldIsland
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            CellX = activeCell.cellX,
            CellY = activeCell.cellY,
            CenterX = definition.CenterX,
            CenterY = definition.CenterY
        };
        var mapObjects = new RecordingWorldMapObjectRepository();
        var service = new WorldService(
            new CountingWorldRepository(world, 1),
            mapObjects,
            new SingleCityRepository(new City { Id = Guid.NewGuid(), WorldId = world.Id }),
            new TestPlayerAccessService([viewer]),
            new FixedWorldIslandRepository(island),
            new NoOpExoticResourceService(),
            new DeploymentPermissionService(new TestAllianceRepository()),
            new CityPointCalculator(TestData.BuildingReader()),
            NullLogger<WorldService>.Instance);
        var request = new GetWorldMapChunkDTO
        {
            worldId = world.Id,
            startX = checked((short)(definition.CenterX - 25)),
            startY = checked((short)(definition.CenterY - 25)),
            width = 50,
            height = 50
        };

        var response = await service.GetWorldMapChunk(request);

        Assert.NotNull(response);
        Assert.Equal(1, mapObjects.AreaQueryCount);
        Assert.NotEmpty(response!.FutureCitySites);
        Assert.All(response.FutureCitySites, site =>
        {
            Assert.InRange(site.X, request.startX, request.startX + request.width - 1);
            Assert.InRange(site.Y, request.startY, request.startY + request.height - 1);
        });
    }

    [Fact]
    public async Task GetWorldMapChunk_DuplicateCityCoordinatesSelectLowestId()
    {
        var world = new World { Id = Guid.NewGuid(), Width = 200, Height = 200, MapSeed = 42069 };
        var viewer = new WorldPlayer { Id = Guid.NewGuid(), WorldId = world.Id, PlayerProfileId = Guid.NewGuid() };
        var selectedCity = new City
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            WorldId = world.Id,
            Name = "Selected",
            X = 27,
            Y = 9
        };
        var discardedCity = new City
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            WorldId = world.Id,
            Name = "Discarded",
            X = 27,
            Y = 9
        };
        var mapObjects = new RecordingWorldMapObjectRepository(
            new WorldMapObject
            {
                Id = Guid.NewGuid(), WorldId = world.Id, X = 27, Y = 9,
                Type = Domain.Enums.MapObjectTypeEnum.City, ReferenceEntityId = discardedCity.Id
            },
            new WorldMapObject
            {
                Id = Guid.NewGuid(), WorldId = world.Id, X = 27, Y = 9,
                Type = Domain.Enums.MapObjectTypeEnum.City, ReferenceEntityId = selectedCity.Id
            });
        var service = new WorldService(
            new CountingWorldRepository(world, 1),
            mapObjects,
            new SingleCityRepository(discardedCity, selectedCity),
            new TestPlayerAccessService([viewer]),
            new NoOpWorldIslandRepository(),
            new NoOpExoticResourceService(),
            new DeploymentPermissionService(new TestAllianceRepository()),
            new CityPointCalculator(TestData.BuildingReader()),
            NullLogger<WorldService>.Instance);

        var response = await service.GetWorldMapChunk(new GetWorldMapChunkDTO
        {
            worldId = world.Id,
            startX = 25,
            startY = 5,
            width = 10,
            height = 10
        });

        Assert.Equal(selectedCity.Id, Assert.Single(response!.Cities).Id);
    }

    [Fact]
    public async Task ObtainAllActiveGameWorldsAsync_UsesActualWorldPlayerCount()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Alpha",
            PlayerCount = 12
        };
        var service = CreateService(
            new CountingWorldRepository(world, 2),
            new City { Id = Guid.NewGuid(), WorldId = world.Id },
            new WorldPlayer { Id = Guid.NewGuid(), WorldId = world.Id });

        var worlds = await service.ObtainAllActiveGameWorldsAsync();

        var result = Assert.Single(worlds);
        Assert.Equal(2, result.CurrentPlayerCount);
    }

    [Fact]
    public async Task WorldMapAndIslandDTOs_ExposeNPCVillageState()
    {
        const int seed = 42069;
        var activeCell = (
            from cellX in Enumerable.Range(-10, 21)
            from cellY in Enumerable.Range(-10, 21)
            where WorldGenerationService.IsIslandCellActive(cellX, cellY, seed)
            select (cellX, cellY)).First();
        var definition = WorldGenerationService.GetIslandDefinition(activeCell.cellX, activeCell.cellY, seed);
        var world = new World
        {
            Id = Guid.NewGuid(),
            Width = 2_000,
            Height = 2_000,
            MapSeed = seed
        };
        var viewer = new WorldPlayer { Id = Guid.NewGuid(), WorldId = world.Id, PlayerProfileId = Guid.NewGuid() };
        var island = new WorldIsland
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            CellX = activeCell.cellX,
            CellY = activeCell.cellY,
            CenterX = definition.CenterX,
            CenterY = definition.CenterY
        };
        var site = PlayerCitySiteGenerator.GenerateCanonicalSites(
            definition,
            seed,
            -1_000,
            999,
            -1_000,
            999).First();
        var village = new City
        {
            Id = Guid.NewGuid(),
            Name = "Old Watch",
            WorldId = world.Id,
            X = site.X,
            Y = site.Y,
            IsNPC = true
        };
        var mapObject = new WorldMapObject
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            X = (short)village.X,
            Y = (short)village.Y,
            Type = Domain.Enums.MapObjectTypeEnum.City,
            ReferenceEntityId = village.Id
        };
        var service = new WorldService(
            new CountingWorldRepository(world, 1),
            new RecordingWorldMapObjectRepository(mapObject),
            new SingleCityRepository(village),
            new TestPlayerAccessService([viewer]),
            new FixedWorldIslandRepository(island),
            new NoOpExoticResourceService(),
            new DeploymentPermissionService(new TestAllianceRepository()),
            new CityPointCalculator(TestData.BuildingReader()),
            NullLogger<WorldService>.Instance);

        var chunk = await service.GetWorldMapChunk(new GetWorldMapChunkDTO
        {
            worldId = world.Id,
            startX = (short)(village.X - 5),
            startY = (short)(village.Y - 5),
            width = 10,
            height = 10
        });
        var details = await service.GetIslandDetailsAsync(island.Id);

        Assert.True(Assert.Single(chunk!.Cities).IsNPC);
        Assert.True(Assert.Single(details!.Cities).IsNPC);
    }

    [Fact]
    public async Task GetCityInspectionAsync_ReturnsOwnerAndAllianceDetails()
    {
        var worldId = Guid.NewGuid();
        var alliance = new Alliance
        {
            Id = Guid.NewGuid(),
            Name = "Legion"
        };
        var owner = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "CityOwner" },
            AllianceId = alliance.Id,
            Alliance = alliance
        };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Stonewatch",
            X = 12,
            Y = -4,
            Points = 345,
            WorldId = worldId,
            WorldPlayerId = owner.Id,
            WorldPlayer = owner
        };
        owner.Cities.Add(city);

        var service = CreateService(city, owner);

        var dto = await service.GetCityInspectionAsync(city.Id);

        Assert.NotNull(dto);
        Assert.Equal(city.Id, dto!.CityId);
        Assert.Equal("Stonewatch", dto.CityName);
        Assert.Equal(12, dto.X);
        Assert.Equal(-4, dto.Y);
        Assert.Equal(345, dto.Points);
        Assert.Equal(owner.Id, dto.WorldPlayerId);
        Assert.Equal("CityOwner", dto.WorldPlayerName);
        Assert.Equal(alliance.Id, dto.AllianceId);
        Assert.Equal("Legion", dto.AllianceName);
        Assert.False(dto.CanAttack);
        Assert.True(dto.CanSupport);
    }

    [Fact]
    public async Task GetCityInspectionAsync_ReturnsNullLinksWhenCityHasNoOwner()
    {
        var worldId = Guid.NewGuid();
        var viewer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Viewer" }
        };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Ruins",
            X = 4,
            Y = 9,
            Points = 17,
            WorldId = worldId,
            IsNPC = true
        };

        var service = CreateService(city, viewer);

        var dto = await service.GetCityInspectionAsync(city.Id);

        Assert.NotNull(dto);
        Assert.Null(dto!.WorldPlayerId);
        Assert.Null(dto.WorldPlayerName);
        Assert.Null(dto.AllianceId);
        Assert.Null(dto.AllianceName);
        Assert.True(dto.IsNPC);
        Assert.True(dto.CanAttack);
        Assert.True(dto.CanSupport);
    }

    [Fact]
    public async Task GetCityInspectionAsync_AllowsSupportForNeutralPlayerCity()
    {
        var worldId = Guid.NewGuid();
        var viewer = new WorldPlayer { Id = Guid.NewGuid(), WorldId = worldId, PlayerProfileId = Guid.NewGuid() };
        var owner = new WorldPlayer { Id = Guid.NewGuid(), WorldId = worldId, PlayerProfileId = Guid.NewGuid() };
        var city = new City
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            WorldPlayerId = owner.Id,
            WorldPlayer = owner
        };

        var dto = await CreateService(city, viewer).GetCityInspectionAsync(city.Id);

        Assert.NotNull(dto);
        Assert.True(dto!.CanSupport);
    }

    [Fact]
    public async Task GetCityInspectionAsync_AllowsOnlyWorldMembers()
    {
        var cityWorldId = Guid.NewGuid();
        var viewerWorldId = Guid.NewGuid();
        var viewer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = viewerWorldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Viewer" }
        };
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Borderkeep",
            X = 1,
            Y = 2,
            Points = 88,
            WorldId = cityWorldId
        };

        var service = CreateService(city, viewer);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetCityInspectionAsync(city.Id));
    }

    private static WorldService CreateService(
        City city,
        WorldPlayer viewer,
        IEnumerable<AllianceRelation>? relations = null)
    {
        return CreateService(new NoOpWorldRepository(), city, viewer, relations);
    }

    private static WorldService CreateService(
        IWorldRepository worldRepository,
        City city,
        WorldPlayer viewer,
        IEnumerable<AllianceRelation>? relations = null)
    {
        return new WorldService(
            worldRepository,
            new NoOpWorldMapObjectRepository(),
            new SingleCityRepository(city),
            new TestPlayerAccessService([viewer]),
            new NoOpWorldIslandRepository(),
            new NoOpExoticResourceService(),
            new DeploymentPermissionService(new TestAllianceRepository(relations)),
            new CityPointCalculator(TestData.BuildingReader()),
            NullLogger<WorldService>.Instance);
    }

    private sealed class SingleCityRepository(params City[] cities) : ICityRepository
    {
        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(cities.Where(city => ids.Contains(city.Id)).ToList());

        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult(cities.SingleOrDefault(city => cityId == city.Id));
        public Task UpdateAsync(City city) => Task.CompletedTask;
        public Task<List<City>> GetAllAsync() => Task.FromResult(cities.ToList());
        public Task UpdateRangeAsync(List<City> cities) => Task.CompletedTask;
        public Task AddAsync(City city) => Task.CompletedTask;
        public Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities) => Task.CompletedTask;
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetByCoordinatesAsync(int x, int y) => Task.FromResult(cities.FirstOrDefault(city => city.X == x && city.Y == y));
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => Task.FromResult(cities.SingleOrDefault(city => cityId == city.Id)?.WorldPlayerId);
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(cities.Where(city => city.WorldPlayerId == worldPlayerId).ToList());
    }

    private sealed class NoOpWorldRepository : IWorldRepository
    {
        public Task<List<World>> GetAllAsync() => Task.FromResult(new List<World>());
        public Task<Dictionary<Guid, int>> GetPlayerCountsByWorldAsync() => Task.FromResult(new Dictionary<Guid, int>());
        public Task<World?> GetByIdAsync(Guid id) => Task.FromResult<World?>(null);
        public Task<int?> GetWorldSeedAsync(Guid worldId) => Task.FromResult<int?>(null);
    }

    private sealed class CountingWorldRepository(World world, int playerCount) : IWorldRepository
    {
        public Task<List<World>> GetAllAsync() => Task.FromResult(new List<World> { world });
        public Task<Dictionary<Guid, int>> GetPlayerCountsByWorldAsync() =>
            Task.FromResult(new Dictionary<Guid, int> { [world.Id] = playerCount });
        public Task<World?> GetByIdAsync(Guid id) => Task.FromResult<World?>(id == world.Id ? world : null);
        public Task<int?> GetWorldSeedAsync(Guid worldId) => Task.FromResult<int?>(worldId == world.Id ? world.MapSeed : null);
    }

    private sealed class NoOpWorldMapObjectRepository : IWorldMapObjectRepository
    {
        public Task AddAsync(WorldMapObject worldMapObject) => Task.CompletedTask;
        public Task<WorldMapObject?> GetWorldMapObjectByReferenceIdAsync(Guid referenceId) => Task.FromResult<WorldMapObject?>(null);
        public Task<List<WorldMapObject>> GetObjectsInAreaAsync(Guid worldId, short startX, short startY, byte width, byte height) => Task.FromResult(new List<WorldMapObject>());
        public Task DeleteAtCoordinatesAsync(Guid worldId, short x, short y) => Task.CompletedTask;
        public Task DeleteByReferenceIdAsync(Guid referenceEntityId) => Task.CompletedTask;
        public Task UpdateAsync(WorldMapObject worldMapObject) => Task.CompletedTask;
        public Task<WorldMapObject?> GetCityOnCoordinatesAsync(Guid worldId, short X, short Y) => Task.FromResult<WorldMapObject?>(null);
        public Task<List<WorldMapObject>> GetObjectsByTypeAsync(Guid id, Domain.Enums.MapObjectTypeEnum type) => Task.FromResult(new List<WorldMapObject>());
    }

    private sealed class NoOpWorldIslandRepository : IWorldIslandRepository
    {
        public Task<WorldIsland?> GetByCellAsync(Guid worldId, int cellX, int cellY) => Task.FromResult<WorldIsland?>(null);
        public Task<WorldIsland?> GetByIdAsync(Guid islandId) => Task.FromResult<WorldIsland?>(null);
        public Task<List<WorldIsland>> GetInAreaAsync(Guid worldId, int startX, int startY, int width, int height) => Task.FromResult(new List<WorldIsland>());
        public Task UpdateAsync(WorldIsland island) => Task.CompletedTask;
    }

    private sealed class FixedWorldIslandRepository(WorldIsland island) : IWorldIslandRepository
    {
        public Task<WorldIsland?> GetByCellAsync(Guid worldId, int cellX, int cellY) =>
            Task.FromResult<WorldIsland?>(cellX == island.CellX && cellY == island.CellY ? island : null);
        public Task<WorldIsland?> GetByIdAsync(Guid islandId) =>
            Task.FromResult<WorldIsland?>(islandId == island.Id ? island : null);
        public Task<List<WorldIsland>> GetInAreaAsync(Guid worldId, int startX, int startY, int width, int height) =>
            Task.FromResult(new List<WorldIsland> { island });
        public Task UpdateAsync(WorldIsland updatedIsland) => Task.CompletedTask;
    }

    private sealed class RecordingWorldMapObjectRepository(params WorldMapObject[] objects) : IWorldMapObjectRepository
    {
        private readonly List<WorldMapObject> _objects = objects.ToList();
        public int AreaQueryCount { get; private set; }

        public Task AddAsync(WorldMapObject worldMapObject) => Task.CompletedTask;
        public Task<WorldMapObject?> GetWorldMapObjectByReferenceIdAsync(Guid referenceId) => Task.FromResult<WorldMapObject?>(null);
        public Task<List<WorldMapObject>> GetObjectsInAreaAsync(Guid worldId, short startX, short startY, byte width, byte height)
        {
            AreaQueryCount++;
            return Task.FromResult(_objects.Where(mapObject => mapObject.WorldId == worldId
                && mapObject.X >= startX && mapObject.X < startX + width
                && mapObject.Y >= startY && mapObject.Y < startY + height).ToList());
        }
        public Task DeleteAtCoordinatesAsync(Guid worldId, short x, short y) => Task.CompletedTask;
        public Task DeleteByReferenceIdAsync(Guid referenceEntityId) => Task.CompletedTask;
        public Task UpdateAsync(WorldMapObject worldMapObject) => Task.CompletedTask;
        public Task<WorldMapObject?> GetCityOnCoordinatesAsync(Guid worldId, short X, short Y) => Task.FromResult<WorldMapObject?>(null);
        public Task<List<WorldMapObject>> GetObjectsByTypeAsync(Guid id, Domain.Enums.MapObjectTypeEnum type) =>
            Task.FromResult(_objects.Where(mapObject => mapObject.WorldId == id && mapObject.Type == type).ToList());
    }

    private sealed class NoOpExoticResourceService : IExoticResourceService
    {
        public Task<List<CityExoticResourceDTO>> SyncCityExoticResourcesAsync(City city, DateTime currentDateTime) => Task.FromResult(new List<CityExoticResourceDTO>());
        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesAsync(Guid islandId) => Task.FromResult(new List<WorldIslandExoticResourceDTO>());
        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesForCityAsync(City city) => Task.FromResult(new List<WorldIslandExoticResourceDTO>());
        public Task<List<CityExoticResourceProductionDTO>> GetProductionBreakdownsForCityAsync(City city) => Task.FromResult(new List<CityExoticResourceProductionDTO>());
        public Task<ExoticResourceInvestmentResponseDTO> InvestAsync(Guid cityId, ExoticResourceInvestmentRequestDTO request) =>
            Task.FromResult(new ExoticResourceInvestmentResponseDTO(
                Guid.Empty,
                Guid.Empty,
                0,
                0,
                new List<WorldIslandExoticResourceDTO>(),
                new List<CityExoticResourceDTO>()));
    }
}
