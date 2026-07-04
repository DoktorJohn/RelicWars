using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.User;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class WorldServiceTests
{
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
        Assert.False(dto.CanAttack);
        Assert.False(dto.CanSupport);
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
            new DeploymentPermissionService(new TestAllianceRepository(relations)));
    }

    private sealed class SingleCityRepository(City city) : ICityRepository
    {
        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(ids.Contains(city.Id) ? new List<City> { city } : new List<City>());

        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult<City?>(cityId == city.Id ? city : null);
        public Task UpdateAsync(City city) => Task.CompletedTask;
        public Task<List<City>> GetAllAsync() => Task.FromResult(new List<City> { city });
        public Task UpdateRangeAsync(List<City> cities) => Task.CompletedTask;
        public Task AddAsync(City city) => Task.CompletedTask;
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetByCoordinatesAsync(int x, int y) => Task.FromResult<City?>(city.X == x && city.Y == y ? city : null);
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => Task.FromResult<Guid?>(cityId == city.Id ? city.WorldPlayerId : null);
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(city.WorldPlayerId == worldPlayerId ? new List<City> { city } : new List<City>());
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
