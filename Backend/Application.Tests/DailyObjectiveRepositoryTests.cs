using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Infrastructure.Context;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class DailyObjectiveRepositoryTests
{
    [Fact]
    public async Task Rollover_inserts_replacement_assignments_and_is_idempotent()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string databaseName = $"RelicWarsDailyObjectiveRollover_{Guid.NewGuid():N}";
        string connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            (Guid playerId, HashSet<Guid> previousAssignmentIds) = await SeedPreviousDayAsync(options);
            var today = new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

            await using (var rolloverContext = new RecordingGameContext(options))
            {
                var service = CreateService(rolloverContext, playerId, today);

                var first = await service.GetAsync(playerId);

                Assert.Equal(new DateTime(2026, 7, 20), first.DayStartUtc);
                Assert.Equal(20, first.Rows.Count);
                var rolloverStates = Assert.Single(rolloverContext.DailyStatesAtSave);
                Assert.Equal(20, rolloverStates.Count(state =>
                    state.EntityType == typeof(DailyObjectiveAssignment) && state.State == EntityState.Deleted));
                Assert.Equal(20, rolloverStates.Count(state =>
                    state.EntityType == typeof(DailyObjectiveAssignment) && state.State == EntityState.Added));
                Assert.DoesNotContain(rolloverStates, state =>
                    state.EntityType == typeof(DailyObjectiveAssignment) && state.State == EntityState.Modified);

                var second = await service.GetAsync(playerId);

                Assert.Equal(first.DayStartUtc, second.DayStartUtc);
                Assert.Equal(first.Rows.Select(row => row.DefinitionId), second.Rows.Select(row => row.DefinitionId));
                Assert.Equal(2, rolloverContext.DailyStatesAtSave.Count);
                Assert.Empty(rolloverContext.DailyStatesAtSave[1]);
            }

            await using var verificationContext = new GameContext(options);
            var savedSet = await verificationContext.DailyObjectiveSets
                .AsNoTracking()
                .Include(set => set.Assignments)
                .SingleAsync(set => set.WorldPlayerId == playerId);

            Assert.Equal(new DateTime(2026, 7, 20), savedSet.DayStartUtc);
            Assert.Equal(20, savedSet.Assignments.Count);
            Assert.DoesNotContain(savedSet.Assignments, assignment => previousAssignmentIds.Contains(assignment.Id));
        }
        finally
        {
            await using var cleanupContext = new GameContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static DailyObjectiveService CreateService(
        GameContext context,
        Guid playerId,
        DateTimeOffset now)
    {
        var objectiveReader = new DailyObjectiveDataReader();
        objectiveReader.Load(TestData.GameFile("daily-objectives-complete.json"));
        var unitReader = new UnitDataReader();
        unitReader.Load(TestData.GameFile("units.json"));
        var transactionManager = new TransactionManager(context);

        return new DailyObjectiveService(
            new DailyObjectiveRepository(context),
            new AllowWorldPlayerAccessService(playerId),
            objectiveReader,
            new FixedRandomService(),
            new FixedTimeProvider(now),
            transactionManager,
            unitReader,
            null!,
            null!);
    }

    private static async Task<(Guid PlayerId, HashSet<Guid> AssignmentIds)> SeedPreviousDayAsync(
        DbContextOptions<GameContext> options)
    {
        await using var context = new GameContext(options);
        await context.Database.MigrateAsync();

        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Daily rollover regression world",
            Abbrevation = "DR",
            Width = 100,
            Height = 100,
            MapSeed = 20260720
        };
        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            UserName = "DailyPlayer",
            NormalizedUserName = "DAILYPLAYER"
        };
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            World = world,
            WorldId = world.Id,
            PlayerProfile = profile,
            PlayerProfileId = profile.Id
        };
        var assignments = Enumerable.Range(1, 20)
            .Select(slot => new DailyObjectiveAssignment
            {
                Id = Guid.NewGuid(),
                DefinitionId = slot,
                Slot = slot,
                Target = 100
            })
            .ToList();
        player.DailyObjectiveSet = new DailyObjectiveSet
        {
            Id = Guid.NewGuid(),
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            DayStartUtc = new DateTime(2026, 7, 19),
            Assignments = assignments
        };

        context.WorldPlayers.Add(player);
        await context.SaveChangesAsync();
        return (player.Id, assignments.Select(assignment => assignment.Id).ToHashSet());
    }

    private sealed class RecordingGameContext(DbContextOptions<GameContext> options) : GameContext(options)
    {
        public List<IReadOnlyList<DailyEntryState>> DailyStatesAtSave { get; } = [];

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.DetectChanges();
            DailyStatesAtSave.Add(ChangeTracker.Entries()
                .Where(entry => entry.Entity is DailyObjectiveSet or DailyObjectiveAssignment &&
                                entry.State != EntityState.Unchanged)
                .Select(entry => new DailyEntryState(entry.Entity.GetType(), entry.State))
                .ToList());
            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record DailyEntryState(Type EntityType, EntityState State);

    private sealed class AllowWorldPlayerAccessService(Guid playerId) : IPlayerAccessService
    {
        public Guid GetAuthenticatedProfileId() => Guid.Empty;

        public Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId)
        {
            Assert.Equal(playerId, worldPlayerId);
            return Task.FromResult(new WorldPlayer { Id = worldPlayerId });
        }

        public Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId) => throw new NotSupportedException();
        public Task<City> RequireOwnedCityAsync(Guid cityId) => throw new NotSupportedException();
        public Task<City> RequireOwnedCityForTownHallAsync(Guid cityId) => throw new NotSupportedException();
        public Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId) => throw new NotSupportedException();
    }
}
