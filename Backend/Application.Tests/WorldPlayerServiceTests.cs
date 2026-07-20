using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Context;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using System.IO;
using System.Text.Json;

namespace Application.Tests;

public class WorldPlayerServiceTests
{
    [Fact]
    public async Task GetWorldPlayerEconomyAsync_ReturnsResourceTotalsAcrossOwnedCities()
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = Guid.NewGuid(),
            WorldId = Guid.NewGuid()
        };
        var firstCity = new City { Id = Guid.NewGuid(), WorldPlayerId = player.Id, Wood = 125, Stone = 80, Metal = 30 };
        var secondCity = new City { Id = Guid.NewGuid(), WorldPlayerId = player.Id, Wood = 75, Stone = 20, Metal = 45 };
        player.Cities = [firstCity, secondCity];
        var world = new World { Id = player.WorldId };
        var worldRepository = new FixedWorldRepository(world);
        var service = new WorldPlayerService(
            new MemoryWorldPlayerRepository(player),
            new NoOpPlayerProfileRepository(),
            new FixedCitiesRepository(firstCity, secondCity),
            worldRepository,
            new StoredCityResourceService(),
            worldRepository,
            new NoOpWorldMapObjectRepository(),
            new CapturingWorldMapObjectService(),
            new TestPlayerAccessService([player]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new ImmediateTransactionManager());

        var result = await service.GetWorldPlayerEconomyAsync(player.Id);

        Assert.Equal(200, result.TotalWoodAmount);
        Assert.Equal(100, result.TotalStoneAmount);
        Assert.Equal(75, result.TotalMetalAmount);
        Assert.Equal(240, result.TotalPopulationAmount);
    }

    [Fact]
    public async Task AssignPlayerToGameWorldAsync_PlacesCapitalOnCoast()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Width = 200,
            Height = 200,
            MapSeed = 42069
        };
        var profileId = Guid.NewGuid();
        var authenticatedPlayer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = profileId,
            WorldId = world.Id
        };
        var mapService = new CapturingWorldMapObjectService();

        var service = new WorldPlayerService(
            new MemoryWorldPlayerRepository(),
            new FixedPlayerProfileRepository(profileId, "CoastalPlayer"),
            new NoOpCityRepository(),
            new FixedWorldRepository(world),
            new NoOpResourceService(),
            new FixedWorldRepository(world),
            new NoOpWorldMapObjectRepository(),
            mapService,
            new TestPlayerAccessService([authenticatedPlayer]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new ImmediateTransactionManager());

        var response = await service.AssignPlayerToGameWorldAsync(world.Id);

        Assert.True(response.ConnectionSuccessful);
        var city = Assert.IsType<City>(mapService.AddedEntity);
        Assert.True(WorldGenerationService.IsCoastal(city.X, city.Y, world.MapSeed));
    }

    [Fact]
    public async Task AssignPlayerToGameWorldAsync_CreatesOnlyLevelOneStarterBuildings()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Width = 200,
            Height = 200,
            MapSeed = 42069
        };
        var profileId = Guid.NewGuid();
        var authenticatedPlayer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = profileId,
            WorldId = world.Id
        };
        var mapService = new CapturingWorldMapObjectService();

        var service = new WorldPlayerService(
            new MemoryWorldPlayerRepository(),
            new FixedPlayerProfileRepository(profileId, "StarterPlayer"),
            new NoOpCityRepository(),
            new FixedWorldRepository(world),
            new NoOpResourceService(),
            new FixedWorldRepository(world),
            new NoOpWorldMapObjectRepository(),
            mapService,
            new TestPlayerAccessService([authenticatedPlayer]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new ImmediateTransactionManager());

        var response = await service.AssignPlayerToGameWorldAsync(world.Id);

        Assert.True(response.ConnectionSuccessful);

        var city = Assert.IsType<City>(mapService.AddedEntity);
        Assert.Equal(
            [
                BuildingTypeEnum.TownHall,
                BuildingTypeEnum.Warehouse,
                BuildingTypeEnum.Housing,
                BuildingTypeEnum.TimberCamp,
                BuildingTypeEnum.StoneQuarry,
                BuildingTypeEnum.MetalMine
            ],
            city.Buildings.Select(building => building.Type));
        Assert.All(city.Buildings, building => Assert.Equal(1, building.Level));
        Assert.Equal(6, city.Buildings.Count);
    }

    [Fact]
    public async Task AssignPlayerToGameWorldAsync_ConcurrentRequestsReuseSameParticipationAndCapital()
    {
        var world = new World { Id = Guid.NewGuid(), Width = 200, Height = 200, MapSeed = 42069 };
        var profileId = Guid.NewGuid();
        var authenticatedPlayer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = profileId,
            WorldId = world.Id
        };
        var repository = new MemoryWorldPlayerRepository();
        var mapService = new CapturingWorldMapObjectService();
        var service = new WorldPlayerService(
            repository,
            new FixedPlayerProfileRepository(profileId, "ConcurrentPlayer"),
            new NoOpCityRepository(),
            new NoOpRankingService(),
            new NoOpResourceService(),
            new FixedWorldRepository(world),
            new NoOpWorldMapObjectRepository(),
            mapService,
            new TestPlayerAccessService([authenticatedPlayer]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new SerializingTransactionManager());

        var responses = await Task.WhenAll(
            service.AssignPlayerToGameWorldAsync(world.Id),
            service.AssignPlayerToGameWorldAsync(world.Id));

        Assert.All(responses, response => Assert.True(response.ConnectionSuccessful));
        Assert.Equal(responses[0].WorldPlayerId, responses[1].WorldPlayerId);
        Assert.Equal(responses[0].ActiveCityId, responses[1].ActiveCityId);
        Assert.Equal(1, repository.Count);
        Assert.Single(repository.SinglePlayer.Cities);
    }

    [Fact]
    public async Task AssignPlayerToGameWorldAsync_RollsBackParticipationAndCityWhenMapObjectCreationFails()
    {
        var world = new World { Id = Guid.NewGuid(), Width = 200, Height = 200, MapSeed = 42069 };
        var profileId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new GameContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        context.World.Add(world);
        context.PlayerProfiles.Add(new PlayerProfile
        {
            Id = profileId,
            UserName = "RollbackPlayer",
            NormalizedUserName = "ROLLBACKPLAYER",
            Email = "rollback@example.test",
            NormalizedEmail = "ROLLBACK@EXAMPLE.TEST"
        });
        await context.SaveChangesAsync();

        var service = new WorldPlayerService(
            new WorldPlayerRepository(context),
            new FixedPlayerProfileRepository(profileId, "RollbackPlayer"),
            new NoOpCityRepository(),
            new NoOpRankingService(),
            new NoOpResourceService(),
            new FixedWorldRepository(world),
            new WorldMapObjectRepository(context),
            new ThrowingWorldMapObjectService(),
            new TestPlayerAccessService([new WorldPlayer { PlayerProfileId = profileId, WorldId = world.Id }]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new TransactionManager(context));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AssignPlayerToGameWorldAsync(world.Id));

        Assert.Equal(0, await context.WorldPlayers.AsNoTracking().CountAsync());
        Assert.Equal(0, await context.Cities.AsNoTracking().CountAsync());
        Assert.Equal(0, await context.WorldMapObjects.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task AssignPlayerToGameWorldAsync_DbUpdateConflictReturnsWinningParticipation()
    {
        var world = new World { Id = Guid.NewGuid(), Width = 200, Height = 200, MapSeed = 42069 };
        var profileId = Guid.NewGuid();
        var winnerCity = new City { Id = Guid.NewGuid(), WorldId = world.Id };
        var winner = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = profileId,
            WorldId = world.Id,
            Cities = [winnerCity]
        };
        var service = new WorldPlayerService(
            new WinnerAfterConflictRepository(winner),
            new FixedPlayerProfileRepository(profileId, "RaceWinner"),
            new NoOpCityRepository(),
            new NoOpRankingService(),
            new NoOpResourceService(),
            new FixedWorldRepository(world),
            new NoOpWorldMapObjectRepository(),
            new NoOpWorldMapObjectService(),
            new TestPlayerAccessService([winner]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new ThrowingDbUpdateTransactionManager());

        var response = await service.AssignPlayerToGameWorldAsync(world.Id);

        Assert.True(response.ConnectionSuccessful);
        Assert.Equal(winner.Id, response.WorldPlayerId);
        Assert.Equal(winnerCity.Id, response.ActiveCityId);
        Assert.Equal("Welcome back.", response.Message);
    }

    [Fact]
    public async Task UniqueWorldJoinIndexesRejectDuplicateParticipationCityAndTypedMapObject()
    {
        await using (var context = await CreateSqliteContextAsync())
        {
            var world = new World { Id = Guid.NewGuid(), Name = "Participation world" };
            var profile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "UniquePlayer" };
            context.AddRange(world, profile);
            context.WorldPlayers.Add(new WorldPlayer { PlayerProfileId = profile.Id, WorldId = world.Id });
            await context.SaveChangesAsync();
            context.WorldPlayers.Add(new WorldPlayer { PlayerProfileId = profile.Id, WorldId = world.Id });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = await CreateSqliteContextAsync())
        {
            var world = new World { Id = Guid.NewGuid(), Name = "City world" };
            context.Add(world);
            context.Cities.AddRange(
                new City { WorldId = world.Id, X = 27, Y = 9, Name = "First" },
                new City { WorldId = world.Id, X = 27, Y = 9, Name = "Second" });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = await CreateSqliteContextAsync())
        {
            var world = new World { Id = Guid.NewGuid(), Name = "Map object world" };
            context.Add(world);
            context.WorldMapObjects.AddRange(
                new WorldMapObject { WorldId = world.Id, X = 27, Y = 9, Type = MapObjectTypeEnum.City },
                new WorldMapObject { WorldId = world.Id, X = 27, Y = 9, Type = MapObjectTypeEnum.City });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    private static async Task<GameContext> CreateSqliteContextAsync()
    {
        var context = new GameContext(new DbContextOptionsBuilder<GameContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task AssignPlayerToGameWorldAsync_FillsOneIslandWithThreeTileSpacing()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Width = 200,
            Height = 200,
            MapSeed = 42069
        };
        var players = new MemoryWorldPlayerRepository();
        var mapObjects = new TrackingWorldMapObjectRepository();
        var mapService = new TrackingWorldMapObjectService(mapObjects);

        for (int index = 0; index < 4; index++)
        {
            var profileId = Guid.NewGuid();
            var authenticatedPlayer = new WorldPlayer
            {
                Id = Guid.NewGuid(),
                PlayerProfileId = profileId,
                WorldId = world.Id
            };
            var service = new WorldPlayerService(
                players,
                new FixedPlayerProfileRepository(profileId, $"Player{index}"),
                new NoOpCityRepository(),
                new FixedWorldRepository(world),
                new NoOpResourceService(),
                new FixedWorldRepository(world),
                mapObjects,
                mapService,
                new TestPlayerAccessService([authenticatedPlayer]),
                NullLogger<WorldPlayerService>.Instance,
                new CityPointCalculator(TestData.BuildingReader()),
                new ImmediateTransactionManager());

            var response = await service.AssignPlayerToGameWorldAsync(world.Id);

            Assert.True(response.ConnectionSuccessful);
        }

        var cities = mapObjects.Objects;
        Assert.Equal(4, cities.Count);
        var islands = cities.Select(city =>
        {
            Assert.True(WorldGenerationService.IsCoastal(city.X, city.Y, world.MapSeed));
            Assert.True(WorldGenerationService.TryGetIslandCoordinates(
                city.X, city.Y, world.MapSeed, out int islandX, out int islandY));
            return (islandX, islandY);
        }).Distinct().ToList();
        Assert.Single(islands);

        for (int first = 0; first < cities.Count; first++)
        for (int second = first + 1; second < cities.Count; second++)
        {
            int firstCubeX = cities[first].X - (cities[first].Y - (cities[first].Y & 1)) / 2;
            int firstCubeZ = cities[first].Y;
            int secondCubeX = cities[second].X - (cities[second].Y - (cities[second].Y & 1)) / 2;
            int secondCubeZ = cities[second].Y;
            int distance = Math.Max(
                Math.Abs(firstCubeX - secondCubeX),
                Math.Max(
                    Math.Abs((-firstCubeX - firstCubeZ) - (-secondCubeX - secondCubeZ)),
                    Math.Abs(firstCubeZ - secondCubeZ)));
            Assert.True(distance >= 3, $"Cities were only {distance} tiles apart.");
        }
    }

    [Fact]
    public async Task AssignPlayerToGameWorldAsync_DoesNotPrioritizeAnIslandContainingOnlyNPCs()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Width = 200,
            Height = 200,
            MapSeed = 42069
        };
        var islandCandidates = (
            from cellX in Enumerable.Range(-4, 9)
            from cellY in Enumerable.Range(-4, 9)
            where WorldGenerationService.IsIslandCellActive(cellX, cellY, world.MapSeed)
            let island = WorldGenerationService.GetIslandDefinition(cellX, cellY, world.MapSeed)
            let sites = PlayerCitySiteGenerator.GenerateCanonicalSites(island, world.MapSeed, -100, 99, -100, 99)
            where sites.Count > 1
            orderby Math.Abs(cellX - 1) + Math.Abs(cellY - 1) descending
            select (Island: island, Sites: sites)).ToList();
        var npcIsland = islandCandidates.First();
        var npcCity = new City
        {
            Id = Guid.NewGuid(),
            Name = "NPC village",
            WorldId = world.Id,
            IsNPC = true,
            X = npcIsland.Sites[0].X,
            Y = npcIsland.Sites[0].Y
        };
        var mapObjects = new TrackingWorldMapObjectRepository();
        mapObjects.Objects.Add(new WorldMapObject
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            X = (short)npcCity.X,
            Y = (short)npcCity.Y,
            Type = MapObjectTypeEnum.City,
            ReferenceEntityId = npcCity.Id
        });
        var profileId = Guid.NewGuid();
        var authenticatedPlayer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = profileId,
            WorldId = world.Id
        };
        var service = new WorldPlayerService(
            new MemoryWorldPlayerRepository(),
            new FixedPlayerProfileRepository(profileId, "Player"),
            new FixedCitiesRepository(npcCity),
            new FixedWorldRepository(world),
            new NoOpResourceService(),
            new FixedWorldRepository(world),
            mapObjects,
            new TrackingWorldMapObjectService(mapObjects),
            new TestPlayerAccessService([authenticatedPlayer]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new ImmediateTransactionManager());

        var response = await service.AssignPlayerToGameWorldAsync(world.Id);

        Assert.True(response.ConnectionSuccessful);
        var spawnedMapObject = mapObjects.Objects.Single(mapObject => mapObject.ReferenceEntityId == response.ActiveCityId);
        Assert.True(WorldGenerationService.TryGetIslandCoordinates(
            spawnedMapObject.X,
            spawnedMapObject.Y,
            world.MapSeed,
            out int spawnedIslandX,
            out int spawnedIslandY));
        Assert.NotEqual((npcIsland.Island.CellX, npcIsland.Island.CellY), (spawnedIslandX, spawnedIslandY));
    }

    [Fact]
    public async Task GetWorldPlayerProfileAsync_AllowsForeignProfileInSameWorld()
    {
        var worldId = Guid.NewGuid();
        var alliance = new Alliance
        {
            Id = Guid.NewGuid(),
            Name = "Legion",
            Tag = "LEG"
        };

        var viewer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Viewer" },
            Cities = new List<City>()
        };

        var target = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            AllianceId = alliance.Id,
            Alliance = alliance,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Target", Description = "Target description" },
            Cities = new List<City>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Capital",
                    Points = 123,
                    WorldPlayerId = Guid.NewGuid()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Harbor",
                    Points = 321,
                    WorldPlayerId = Guid.NewGuid()
                }
            }
        };
        target.Cities[0].WorldPlayer = target;
        target.Cities[1].WorldPlayer = target;
        alliance.Members = new List<WorldPlayer> { target };

        var service = new WorldPlayerService(
            new MemoryWorldPlayerRepository(viewer, target),
            new NoOpPlayerProfileRepository(),
            new NoOpCityRepository(),
            new FixedRankingService(target.Id, 7, 789, 2),
            new NoOpResourceService(),
            new NoOpWorldRepository(),
            new NoOpWorldMapObjectRepository(),
            new NoOpWorldMapObjectService(),
            new TestPlayerAccessService([viewer]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new ImmediateTransactionManager());

        var profile = await service.GetWorldPlayerProfileAsync(target.Id);

        Assert.Equal("Target", profile.UserName);
        Assert.Equal(7, profile.Ranking);
        Assert.Equal(789, profile.TotalPoints);
        Assert.Equal(2, profile.CityCount);
        Assert.Equal("Target description", profile.Description);
        Assert.Equal(alliance.Id, profile.AllianceId);
        Assert.Equal(worldId, profile.WorldId);
        Assert.Equal(2, profile.Cities.Count);
        Assert.Equal("Harbor", profile.Cities[0].CityName);
    }

    [Fact]
    public async Task GetWorldPlayerProfileAsync_AllowsForeignProfileWithRankingSnapshot()
    {
        var worldId = Guid.NewGuid();
        var viewer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Viewer" },
            Cities = new List<City>()
        };

        var target = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Target", Description = "Old description" },
            Cities = new List<City>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Capital",
                    Points = 123,
                    WorldPlayerId = Guid.NewGuid()
                }
            }
        };
        target.Cities[0].WorldPlayer = target;

        var tempRankingFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            var rankingData = new List<RankingEntryData>
            {
                new()
                {
                    WorldPlayerId = target.Id,
                    Rank = 7,
                    TotalPoints = 789,
                    CityCount = 2
                }
            };

            await File.WriteAllTextAsync(tempRankingFile, JsonSerializer.Serialize(rankingData));

            var rankingReader = new RankingDataReader();
            rankingReader.Load(tempRankingFile);

            var service = new WorldPlayerService(
                new MemoryWorldPlayerRepository(viewer, target),
                new NoOpPlayerProfileRepository(),
                new NoOpCityRepository(),
                new RankingService(rankingReader),
                new NoOpResourceService(),
                new NoOpWorldRepository(),
                new NoOpWorldMapObjectRepository(),
                new NoOpWorldMapObjectService(),
                new TestPlayerAccessService([viewer]),
                NullLogger<WorldPlayerService>.Instance,
                new CityPointCalculator(TestData.BuildingReader()),
                new ImmediateTransactionManager());

            var profile = await service.GetWorldPlayerProfileAsync(target.Id);

            Assert.Equal("Target", profile.UserName);
            Assert.Equal(7, profile.Ranking);
            Assert.Equal(789, profile.TotalPoints);
            Assert.Equal(2, profile.CityCount);
            Assert.Equal(worldId, profile.WorldId);
        }
        finally
        {
            if (File.Exists(tempRankingFile))
            {
                File.Delete(tempRankingFile);
            }
        }
    }

    [Fact]
    public async Task UpdateWorldPlayerDescriptionAsync_UpdatesOwnProfileDescription()
    {
        var worldId = Guid.NewGuid();
        var viewer = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Viewer" },
            Cities = new List<City>()
        };

        var target = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfileId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Target", Description = string.Empty },
            Cities = new List<City>()
        };

        var repository = new MemoryWorldPlayerRepository(viewer, target);
        var playerProfileRepository = new TrackingPlayerProfileRepository(target.PlayerProfile);
        var service = new WorldPlayerService(
            repository,
            playerProfileRepository,
            new NoOpCityRepository(),
            new NoOpRankingService(),
            new NoOpResourceService(),
            new NoOpWorldRepository(),
            new NoOpWorldMapObjectRepository(),
            new NoOpWorldMapObjectService(),
            new TestPlayerAccessService([target]),
            NullLogger<WorldPlayerService>.Instance,
            new CityPointCalculator(TestData.BuildingReader()),
            new ImmediateTransactionManager());

        var result = await service.UpdateWorldPlayerDescriptionAsync(target.Id, "A new description");

        Assert.Equal("A new description", result.Description);
        Assert.Equal("A new description", target.PlayerProfile.Description);
        Assert.Equal(1, playerProfileRepository.UpdateCalls);
    }

    private sealed class FixedRankingService(Guid targetId, int rank, int totalPoints, int cityCount) : IRankingService
    {
        public Task<List<RankingEntryData>> GetRankings() =>
            Task.FromResult(new List<RankingEntryData>
            {
                new()
                {
                    WorldPlayerId = targetId,
                    Rank = rank,
                    TotalPoints = totalPoints,
                    CityCount = cityCount
                }
            });

        public Task<RankingEntryData?> GetRankingById(Guid worldPlayerId) =>
            Task.FromResult<RankingEntryData?>(worldPlayerId == targetId
                ? new RankingEntryData
                {
                    WorldPlayerId = targetId,
                    Rank = rank,
                    TotalPoints = totalPoints,
                    CityCount = cityCount
                }
                : null);
    }

    private sealed class MemoryWorldPlayerRepository : IWorldPlayerRepository
    {
        private readonly List<WorldPlayer> _players;

        public MemoryWorldPlayerRepository(params WorldPlayer[] players)
        {
            _players = players.ToList();
        }

        public int Count => _players.Count;
        public WorldPlayer SinglePlayer => _players.Single();

        public Task<WorldPlayer?> GetByIdAsync(Guid id) =>
            Task.FromResult(_players.SingleOrDefault(player => player.Id == id));

        public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => GetByIdAsync(id);
        public Task AddAsync(WorldPlayer user)
        {
            _players.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult(_players.ToList());
        public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) =>
            Task.FromResult(_players.SingleOrDefault(player => player.PlayerProfileId == profileId && player.WorldId == worldId));
        public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) =>
            Task.FromResult(_players.Where(player => player.AllianceId == allianceId).ToList());
        public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) =>
            Task.FromResult(new List<WorldPlayer>());
    }

    private sealed class SerializingTransactionManager : ITransactionManager
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public async Task ExecuteAsync(Func<Task> operation) =>
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            });

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            await _gate.WaitAsync();
            try
            {
                return await operation();
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private sealed class ThrowingWorldMapObjectService : IWorldMapObjectService
    {
        public Task AddEntityToWorldMapAsync(Domain.Abstraction.IMapEntity entity) =>
            throw new InvalidOperationException("Map object creation failed.");
        public Task UpdateEntityPositionOnWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
        public Task RemoveEntityFromWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
    }

    private sealed class ThrowingDbUpdateTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(Func<Task> operation) => throw new DbUpdateException("Unique constraint conflict.");
        public Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => throw new DbUpdateException("Unique constraint conflict.");
    }

    private sealed class WinnerAfterConflictRepository(WorldPlayer winner) : IWorldPlayerRepository
    {
        private int _profileWorldReads;

        public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) =>
            Task.FromResult<WorldPlayer?>(Interlocked.Increment(ref _profileWorldReads) == 1 ? null : winner);
        public Task<WorldPlayer?> GetByIdAsync(Guid id) => Task.FromResult<WorldPlayer?>(winner.Id == id ? winner : null);
        public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => GetByIdAsync(id);
        public Task AddAsync(WorldPlayer user) => Task.CompletedTask;
        public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult(new List<WorldPlayer> { winner });
        public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) => Task.FromResult(new List<WorldPlayer>());
        public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) => Task.FromResult(new List<WorldPlayer>());
    }

    private sealed class NoOpPlayerProfileRepository : IPlayerProfileRepository
    {
        public Task<PlayerProfile?> GetByEmailAsync(string email) => Task.FromResult<PlayerProfile?>(null);
        public Task<PlayerProfile?> GetByIdAsync(Guid id) => Task.FromResult<PlayerProfile?>(null);
        public Task AddAsync(PlayerProfile playerProfile) => Task.CompletedTask;
        public Task UpdateAsync(PlayerProfile playerProfile) => Task.CompletedTask;
        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(false);
        public Task<string?> GetUserNameByIdAsync(Guid id) => Task.FromResult<string?>(null);
    }

    private sealed class FixedPlayerProfileRepository(Guid profileId, string userName) : IPlayerProfileRepository
    {
        public Task<PlayerProfile?> GetByEmailAsync(string email) => Task.FromResult<PlayerProfile?>(null);
        public Task<PlayerProfile?> GetByIdAsync(Guid id) => Task.FromResult<PlayerProfile?>(null);
        public Task AddAsync(PlayerProfile playerProfile) => Task.CompletedTask;
        public Task UpdateAsync(PlayerProfile playerProfile) => Task.CompletedTask;
        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(false);
        public Task<string?> GetUserNameByIdAsync(Guid id) =>
            Task.FromResult<string?>(id == profileId ? userName : null);
    }

    private sealed class TrackingPlayerProfileRepository(params PlayerProfile[] profiles) : IPlayerProfileRepository
    {
        private readonly Dictionary<Guid, PlayerProfile> _profiles = profiles.ToDictionary(profile => profile.Id);

        public int UpdateCalls { get; private set; }

        public Task<PlayerProfile?> GetByEmailAsync(string email) => Task.FromResult<PlayerProfile?>(null);
        public Task<PlayerProfile?> GetByIdAsync(Guid id) => Task.FromResult(_profiles.TryGetValue(id, out var profile) ? profile : null);
        public Task AddAsync(PlayerProfile playerProfile)
        {
            _profiles[playerProfile.Id] = playerProfile;
            return Task.CompletedTask;
        }
        public Task UpdateAsync(PlayerProfile playerProfile)
        {
            _profiles[playerProfile.Id] = playerProfile;
            UpdateCalls++;
            return Task.CompletedTask;
        }
        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(false);
        public Task<string?> GetUserNameByIdAsync(Guid id) => Task.FromResult<string?>(null);
    }

    private sealed class NoOpRankingService : IRankingService
    {
        public Task<List<RankingEntryData>> GetRankings() => Task.FromResult(new List<RankingEntryData>());
        public Task<RankingEntryData?> GetRankingById(Guid worldPlayerId) => Task.FromResult<RankingEntryData?>(null);
    }

    private sealed class NoOpCityRepository : ICityRepository
    {
        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) => Task.FromResult(new List<City>());
        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult<City?>(null);
        public Task UpdateAsync(City city) => Task.CompletedTask;
        public Task<List<City>> GetAllAsync() => Task.FromResult(new List<City>());
        public Task UpdateRangeAsync(List<City> cities) => Task.CompletedTask;
        public Task AddAsync(City city) => Task.CompletedTask;
        public Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities) => Task.CompletedTask;
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => Task.FromResult<City?>(null);
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => Task.FromResult<City?>(null);
        public Task<City?> GetByCoordinatesAsync(int x, int y) => Task.FromResult<City?>(null);
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => Task.FromResult<Guid?>(null);
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) => Task.FromResult(new List<City>());
    }

    private sealed class FixedCitiesRepository(params City[] cities) : ICityRepository
    {
        private readonly List<City> _cities = cities.ToList();

        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) =>
            Task.FromResult(_cities.Where(city => ids.Contains(city.Id)).ToList());
        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult(_cities.SingleOrDefault(city => city.Id == cityId));
        public Task UpdateAsync(City city) => Task.CompletedTask;
        public Task<List<City>> GetAllAsync() => Task.FromResult(_cities.ToList());
        public Task UpdateRangeAsync(List<City> updatedCities) => Task.CompletedTask;
        public Task AddAsync(City city) { _cities.Add(city); return Task.CompletedTask; }
        public Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities) => Task.CompletedTask;
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => GetByIdAsync(cityId);
        public Task<City?> GetByCoordinatesAsync(int x, int y) =>
            Task.FromResult(_cities.SingleOrDefault(city => city.X == x && city.Y == y));
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) =>
            Task.FromResult(_cities.SingleOrDefault(city => city.Id == cityId)?.WorldPlayerId);
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(_cities.Where(city => city.WorldPlayerId == worldPlayerId).ToList());
    }

    private sealed class NoOpResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime) =>
            new(0, 0, 0, 0, 0, 0, currentDateTime);

        public CityProductionSnapshot CalculateCityProduction(WorldPlayer playerEntity, City cityEntity) => new(0, 0, 0);

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime) =>
            new(0, 0, 0, 0, 0, 0, currentDateTime);
    }

    private sealed class StoredCityResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime) =>
            new(cityEntity.Wood, cityEntity.Stone, cityEntity.Metal, 0, 0, 0, currentDateTime);

        public CityProductionSnapshot CalculateCityProduction(WorldPlayer playerEntity, City cityEntity) => new(0, 0, 0);

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime) =>
            new(playerEntity.Coins, playerEntity.ResearchPoints, playerEntity.IdeologyFocusPoints, 0, 0, 0, currentDateTime)
            {
                TotalAvailablePopulation = 240
            };
    }

    private sealed class NoOpWorldRepository : IWorldRepository
    {
        public Task<List<World>> GetAllAsync() => Task.FromResult(new List<World>());
        public Task<Dictionary<Guid, int>> GetPlayerCountsByWorldAsync() => Task.FromResult(new Dictionary<Guid, int>());
        public Task<World?> GetByIdAsync(Guid id) => Task.FromResult<World?>(null);
        public Task<int?> GetWorldSeedAsync(Guid worldId) => Task.FromResult<int?>(null);
    }

    private sealed class FixedWorldRepository(World world) : IRankingService, IWorldRepository
    {
        public Task<List<World>> GetAllAsync() => Task.FromResult(new List<World> { world });
        public Task<Dictionary<Guid, int>> GetPlayerCountsByWorldAsync() =>
            Task.FromResult(new Dictionary<Guid, int> { [world.Id] = world.PlayerCount });
        public Task<World?> GetByIdAsync(Guid id) => Task.FromResult<World?>(id == world.Id ? world : null);
        public Task<int?> GetWorldSeedAsync(Guid worldId) => Task.FromResult<int?>(worldId == world.Id ? world.MapSeed : null);
        public Task<List<RankingEntryData>> GetRankings() => Task.FromResult(new List<RankingEntryData>());
        public Task<RankingEntryData?> GetRankingById(Guid worldPlayerId) => Task.FromResult<RankingEntryData?>(null);
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

    private sealed class TrackingWorldMapObjectRepository : IWorldMapObjectRepository
    {
        public List<WorldMapObject> Objects { get; } = new();

        public Task AddAsync(WorldMapObject worldMapObject)
        {
            Objects.Add(worldMapObject);
            return Task.CompletedTask;
        }

        public Task<WorldMapObject?> GetWorldMapObjectByReferenceIdAsync(Guid referenceId) =>
            Task.FromResult(Objects.SingleOrDefault(mapObject => mapObject.ReferenceEntityId == referenceId));
        public Task<List<WorldMapObject>> GetObjectsInAreaAsync(Guid worldId, short startX, short startY, byte width, byte height) =>
            Task.FromResult(Objects.Where(mapObject => mapObject.WorldId == worldId).ToList());
        public Task DeleteAtCoordinatesAsync(Guid worldId, short x, short y) => Task.CompletedTask;
        public Task DeleteByReferenceIdAsync(Guid referenceEntityId) => Task.CompletedTask;
        public Task UpdateAsync(WorldMapObject worldMapObject) => Task.CompletedTask;
        public Task<WorldMapObject?> GetCityOnCoordinatesAsync(Guid worldId, short X, short Y) =>
            Task.FromResult(Objects.SingleOrDefault(mapObject => mapObject.WorldId == worldId && mapObject.X == X && mapObject.Y == Y));
        public Task<List<WorldMapObject>> GetObjectsByTypeAsync(Guid id, MapObjectTypeEnum type) =>
            Task.FromResult(Objects.Where(mapObject => mapObject.WorldId == id && mapObject.Type == type).ToList());
    }

    private sealed class NoOpWorldMapObjectService : IWorldMapObjectService
    {
        public Task AddEntityToWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
        public Task UpdateEntityPositionOnWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
        public Task RemoveEntityFromWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
    }

    private sealed class CapturingWorldMapObjectService : IWorldMapObjectService
    {
        public Domain.Abstraction.IMapEntity? AddedEntity { get; private set; }

        public Task AddEntityToWorldMapAsync(Domain.Abstraction.IMapEntity entity)
        {
            AddedEntity = entity;
            return Task.CompletedTask;
        }

        public Task UpdateEntityPositionOnWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
        public Task RemoveEntityFromWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
    }

    private sealed class TrackingWorldMapObjectService(TrackingWorldMapObjectRepository repository) : IWorldMapObjectService
    {
        public Task AddEntityToWorldMapAsync(Domain.Abstraction.IMapEntity entity) =>
            repository.AddAsync(new WorldMapObject
            {
                Id = Guid.NewGuid(),
                WorldId = entity.WorldId,
                X = (short)entity.X,
                Y = (short)entity.Y,
                Type = MapObjectTypeEnum.City,
                ReferenceEntityId = entity.Id
            });

        public Task UpdateEntityPositionOnWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
        public Task RemoveEntityFromWorldMapAsync(Domain.Abstraction.IMapEntity entity) => Task.CompletedTask;
    }
}
