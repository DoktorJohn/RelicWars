using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class WorldPlayerRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_LoadsUniversityAcrossPlayerCitiesForResearchEligibility()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var profile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Researcher" };
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            PlayerProfile = profile,
            PlayerProfileId = profile.Id,
            Cities = []
        };
        var cityWithoutUniversity = City(player, "Capital");
        var universityCity = City(player, "Academy City");
        universityCity.Buildings.Add(new Building
        {
            Id = Guid.NewGuid(),
            Type = BuildingTypeEnum.University,
            Level = 1,
            City = universityCity,
            CityId = universityCity.Id
        });
        player.Cities.AddRange([cityWithoutUniversity, universityCity]);

        await using (var writeContext = new GameContext(options))
        {
            writeContext.WorldPlayers.Add(player);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new GameContext(options);
        var result = await new WorldPlayerRepository(readContext).GetByIdAsync(player.Id);

        Assert.NotNull(result);
        var loadedUniversityCity = Assert.Single(result.Cities, city => city.Id == universityCity.Id);
        var university = Assert.Single(loadedUniversityCity.Buildings);
        Assert.Equal(BuildingTypeEnum.University, university.Type);
        Assert.Equal(1, university.Level);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotRemarkTrackedDailyObjectiveGraphAsModified()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var profile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Economist" };
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            PlayerProfile = profile,
            PlayerProfileId = profile.Id
        };
        var objectiveSet = new DailyObjectiveSet
        {
            Id = Guid.NewGuid(),
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            DayStartUtc = new DateTime(2026, 7, 19),
            Assignments =
            [
                new DailyObjectiveAssignment
                {
                    Id = Guid.NewGuid(),
                    DefinitionId = 1,
                    Slot = 1,
                    Target = 100
                }
            ]
        };
        player.DailyObjectiveSet = objectiveSet;

        await using (var writeContext = new GameContext(options))
        {
            writeContext.WorldPlayers.Add(player);
            await writeContext.SaveChangesAsync();
        }

        await using var updateContext = new RecordingGameContext(options);
        var trackedPlayer = await updateContext.WorldPlayers.SingleAsync(candidate => candidate.Id == player.Id);
        await updateContext.DailyObjectiveSets
            .Include(set => set.Assignments)
            .SingleAsync(set => set.WorldPlayerId == player.Id);
        trackedPlayer.Coins++;

        await new WorldPlayerRepository(updateContext).UpdateAsync(trackedPlayer);

        Assert.Equal([typeof(WorldPlayer)], updateContext.ModifiedTypesAtSave);
    }

    [Fact]
    public async Task UpdateAsync_PreservesNewerDailyProgressWhenTrackedGraphHasStaleRowVersions()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string databaseName = $"RelicWarsWorldPlayerRepository_{Guid.NewGuid():N}";
        string connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            (Guid playerId, Guid assignmentId) = await SeedDailyObjectiveGraphAsync(options);

            await using var economyContext = new GameContext(options);
            var trackedPlayer = await economyContext.WorldPlayers.SingleAsync(player => player.Id == playerId);
            var trackedSet = await economyContext.DailyObjectiveSets
                .Include(set => set.Assignments)
                .SingleAsync(set => set.WorldPlayerId == playerId);
            byte[] staleRowVersion = trackedSet.Assignments.Single().RowVersion.ToArray();

            await using (var progressContext = new GameContext(options))
            {
                var assignment = await progressContext.DailyObjectiveAssignments
                    .SingleAsync(candidate => candidate.Id == assignmentId);
                assignment.Progress = 7;
                await progressContext.SaveChangesAsync();
            }

            trackedPlayer.Coins = 125;
            await new WorldPlayerRepository(economyContext).UpdateAsync(trackedPlayer);

            await using var verificationContext = new GameContext(options);
            var savedPlayer = await verificationContext.WorldPlayers
                .AsNoTracking()
                .SingleAsync(player => player.Id == playerId);
            var savedAssignment = await verificationContext.DailyObjectiveAssignments
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == assignmentId);

            Assert.Equal(125, savedPlayer.Coins);
            Assert.Equal(7, savedAssignment.Progress);
            Assert.NotEqual(staleRowVersion, savedAssignment.RowVersion);
        }
        finally
        {
            await using var cleanupContext = new GameContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<(Guid PlayerId, Guid AssignmentId)> SeedDailyObjectiveGraphAsync(
        DbContextOptions<GameContext> options)
    {
        await using var context = new GameContext(options);
        await context.Database.MigrateAsync();

        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Repository regression world",
            Abbrevation = "RR",
            Width = 100,
            Height = 100,
            MapSeed = 1234
        };
        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            UserName = "Economist",
            NormalizedUserName = "ECONOMIST"
        };
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            World = world,
            WorldId = world.Id,
            PlayerProfile = profile,
            PlayerProfileId = profile.Id,
            Coins = 100
        };
        var assignment = new DailyObjectiveAssignment
        {
            Id = Guid.NewGuid(),
            DefinitionId = 1,
            Slot = 1,
            Target = 10
        };
        player.DailyObjectiveSet = new DailyObjectiveSet
        {
            Id = Guid.NewGuid(),
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            DayStartUtc = new DateTime(2026, 7, 20),
            Assignments = [assignment]
        };

        context.WorldPlayers.Add(player);
        await context.SaveChangesAsync();
        return (player.Id, assignment.Id);
    }

    private static City City(WorldPlayer player, string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        WorldId = player.WorldId,
        WorldPlayer = player,
        WorldPlayerId = player.Id,
        Buildings = []
    };

    private sealed class RecordingGameContext(DbContextOptions<GameContext> options) : GameContext(options)
    {
        public IReadOnlyList<Type> ModifiedTypesAtSave { get; private set; } = [];

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.DetectChanges();
            ModifiedTypesAtSave = ChangeTracker.Entries()
                .Where(entry => entry.State == EntityState.Modified)
                .Select(entry => entry.Entity.GetType())
                .ToList();
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
