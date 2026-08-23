using Application.Interfaces;
using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Domain.StaticData.Data;
using System.Collections.Concurrent;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Xunit;

namespace Application.Tests;

public class DailyObjectiveServiceTests
{
    [Fact]
    public void Catalog_contains_valid_complete_definition_set()
    {
        var reader = CreateCatalogReader();

        Assert.Equal(51, reader.Catalog.Definitions.Count);
        Assert.Equal(51, reader.Catalog.Definitions.Select(x => x.Id).Distinct().Count());
        Assert.All(reader.Catalog.Definitions, definition =>
        {
            Assert.InRange(definition.Rewards.Count, 1, 3);
            Assert.Equal(definition.Rewards.Count, definition.Rewards.Select(reward => reward.Type).Distinct().Count());
            Assert.All(definition.Rewards, reward => Assert.True(reward.Amount > 0));
            int expectedTotal = definition.Tier switch
            {
                DailyObjectiveTierEnum.Fixed => 500,
                DailyObjectiveTierEnum.Uncommon => 1000,
                DailyObjectiveTierEnum.Rare => 1500,
                DailyObjectiveTierEnum.Unique => 2000,
                _ => throw new ArgumentOutOfRangeException()
            };
            Assert.Equal(expectedTotal, definition.Rewards.Sum(reward => reward.Amount));
        });
        Assert.Equal(new[] { 8, 9, 11, 12, 13, 18, 19, 21, 23, 24, 25, 26, 27, 28, 32, 39, 44, 45, 46, 47, 48, 50, 51 },
            reader.Catalog.Definitions.Where(x => !x.IsImplemented).Select(x => x.Id));
    }

    [Fact]
    public void Selection_draws_ten_fixed_and_ten_weighted_without_duplicates()
    {
        var service = CreateService(out _, out _, new CyclingRandom());

        var selected = service.SelectDefinitions();

        Assert.Equal(20, selected.Count);
        Assert.Equal(20, selected.Select(x => x.Id).Distinct().Count());
        Assert.Equal(10, selected.Take(10).Count(x => x.Tier == DailyObjectiveTierEnum.Fixed));
        Assert.Equal(10, selected.Skip(10).Count(x => x.Tier != DailyObjectiveTierEnum.Fixed));
    }

    [Fact]
    public void Exhausted_weighted_tier_is_rerolled_among_remaining_tiers()
    {
        var random = new AlwaysMaximumRandom();
        var service = CreateService(out _, out _, random);

        var weighted = service.SelectDefinitions().Skip(10).ToList();

        Assert.Equal(10, weighted.Count);
        Assert.True(weighted.Select(x => x.Id).Distinct().Count() == 10);
        Assert.Equal(5, weighted.Take(5).Count(x => x.Tier == DailyObjectiveTierEnum.Unique));
        Assert.Equal(DailyObjectiveTierEnum.Rare, weighted[5].Tier);
    }

    [Theory]
    [InlineData(64, DailyObjectiveTierEnum.Uncommon)]
    [InlineData(65, DailyObjectiveTierEnum.Rare)]
    [InlineData(94, DailyObjectiveTierEnum.Rare)]
    [InlineData(95, DailyObjectiveTierEnum.Unique)]
    public void Tier_roll_boundaries_follow_65_30_5_weights(int roll, DailyObjectiveTierEnum expected)
    {
        var service = CreateService(out _, out _, new FirstTierRollRandom(roll));

        Assert.Equal(expected, service.SelectDefinitions()[10].Tier);
    }

    [Fact]
    public async Task Set_is_stable_same_day_and_resets_at_exact_utc_midnight()
    {
        var service = CreateService(out var repository, out var time, new CyclingRandom());
        Guid playerId = Guid.NewGuid();

        var first = await service.GetAsync(playerId);
        var second = await service.GetAsync(playerId);
        time.UtcNow = new DateTimeOffset(2026, 7, 18, 0, 0, 0, TimeSpan.Zero);
        var reset = await service.GetAsync(playerId);

        Assert.Equal(first.Rows.Select(x => x.DefinitionId), second.Rows.Select(x => x.DefinitionId));
        Assert.Equal(new DateTime(2026, 7, 18), reset.DayStartUtc);
        Assert.Equal(20, repository.Set!.Assignments.Count);
    }

    [Fact]
    public async Task Gameplay_event_before_window_open_creates_set_and_applies_progress()
    {
        var service = CreateService(out var repository, out _, new CyclingRandom());
        Guid playerId = Guid.NewGuid();

        await service.ApplyProgressAsync(playerId,
            new(DailyObjectiveProgressTypeEnum.BuildingsCompleted, 1, new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc)));

        Assert.NotNull(repository.Set);
        Assert.Equal(20, repository.Set!.Assignments.Count);
        Assert.All(repository.Set.Assignments.Where(x => x.DefinitionId == 2), x => Assert.Equal(1, x.Progress));
    }

    [Fact]
    public async Task Different_world_players_receive_independently_drawn_sets()
    {
        var service = CreateService(out _, out _, new CyclingRandom());

        var first = await service.GetAsync(Guid.NewGuid());
        var second = await service.GetAsync(Guid.NewGuid());

        Assert.False(first.Rows.Select(x => x.DefinitionId).SequenceEqual(second.Rows.Select(x => x.DefinitionId)));
    }

    [Fact]
    public async Task Read_requires_owned_world_player()
    {
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        var service = new DailyObjectiveService(
            new MemoryDailyObjectiveRepository(),
            new DenyPlayerAccessService(),
            CreateCatalogReader(),
            new CyclingRandom(),
            new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)),
            new MemoryTransactionManager(),
            unitReader,
            new NoOpResourceService(),
            new FixedCityStatService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Coming_soon_assignment_never_receives_progress()
    {
        var service = CreateService(out var repository, out _, new CyclingRandom());
        Guid playerId = Guid.NewGuid();
        await service.GetAsync(playerId);
        var lockedDefinition = CreateCatalogReader().Catalog.Definitions.First(x => !x.IsImplemented);
        var assignment = repository.Set!.Assignments[0];
        assignment.DefinitionId = lockedDefinition.Id;
        assignment.Target = lockedDefinition.Target;

        await service.ApplyProgressAsync(playerId,
            new(lockedDefinition.ProgressType, lockedDefinition.Target, new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc)));
        var response = await service.GetAsync(playerId);

        Assert.Equal(0, assignment.Progress);
        Assert.Equal(DailyObjectiveStateEnum.ComingSoon, response.Rows[0].State);
    }

    [Fact]
    public async Task Concurrent_progress_is_accumulated_and_clamped()
    {
        var service = CreateService(out var repository, out _, new CyclingRandom());
        Guid playerId = Guid.NewGuid();
        await service.GetAsync(playerId);
        var assignment = repository.Set!.Assignments[0];
        assignment.DefinitionId = 2;
        assignment.Target = 3;

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => service.ApplyProgressAsync(playerId,
            new(DailyObjectiveProgressTypeEnum.BuildingsCompleted, 1, new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc)))));

        Assert.Equal(3, assignment.Progress);
        Assert.True(assignment.IsComplete);
    }

    [Fact]
    public async Task Completed_objective_awards_resources_and_is_idempotent()
    {
        var repository = new MemoryDailyObjectiveRepository();
        var access = new AllowPlayerAccessService();
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero));
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        var service = new DailyObjectiveService(repository, access, CreateCatalogReader(), new CyclingRandom(), time,
            new MemoryTransactionManager(), unitReader, new NoOpResourceService(), new FixedCityStatService());
        Guid playerId = Guid.NewGuid();
        await service.GetAsync(playerId);
        var assignment = repository.Set!.Assignments[0];
        assignment.DefinitionId = 1;
        assignment.IsComplete = true;
        assignment.Progress = assignment.Target;

        var response = await service.CollectAsync(playerId, assignment.DefinitionId, playerId);
        var repeated = await service.CollectAsync(playerId, assignment.DefinitionId, playerId);

        Assert.True(assignment.IsCollected);
        Assert.True(response.Rows.Single(x => x.DefinitionId == assignment.DefinitionId).IsCollected);
        Assert.True(repeated.Rows.Single(x => x.DefinitionId == assignment.DefinitionId).IsCollected);
        Assert.Equal(500, access.GetPlayer(playerId).Coins);
    }

    [Fact]
    public async Task Local_daily_reward_is_capped_at_warehouse_capacity()
    {
        var repository = new MemoryDailyObjectiveRepository();
        var access = new AllowPlayerAccessService();
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        var service = new DailyObjectiveService(repository, access, CreateCatalogReader(), new CyclingRandom(),
            new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)),
            new MemoryTransactionManager(), unitReader, new NoOpResourceService(), new FixedCityStatService());
        Guid playerId = Guid.NewGuid();
        await service.GetAsync(playerId);
        var assignment = repository.Set!.Assignments[0];
        foreach (var duplicate in repository.Set.Assignments.Skip(1).Where(candidate => candidate.DefinitionId == 2))
            duplicate.DefinitionId = 51;
        assignment.DefinitionId = 2;
        assignment.IsComplete = true;
        access.GetPlayer(playerId).Cities[0].Wood = 9_800;

        await service.CollectAsync(playerId, assignment.DefinitionId, playerId);

        Assert.Equal(10_000, access.GetPlayer(playerId).Cities[0].Wood);
    }

    [Fact]
    public async Task Incomplete_daily_and_foreign_city_are_rejected_without_rewards()
    {
        var repository = new MemoryDailyObjectiveRepository();
        var access = new AllowPlayerAccessService();
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        var service = new DailyObjectiveService(repository, access, CreateCatalogReader(), new CyclingRandom(),
            new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)),
            new MemoryTransactionManager(), unitReader, new NoOpResourceService(), new FixedCityStatService());
        Guid playerId = Guid.NewGuid();
        await service.GetAsync(playerId);
        var assignment = repository.Set!.Assignments[0];

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CollectAsync(playerId, assignment.DefinitionId, playerId));
        assignment.IsComplete = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CollectAsync(playerId, assignment.DefinitionId, Guid.NewGuid()));

        Assert.False(assignment.IsCollected);
        Assert.Equal(0, access.GetPlayer(playerId).Coins);
    }

    [Fact]
    public async Task Same_player_is_serialized_until_the_first_transaction_commits()
    {
        var database = new TransactionAwareDailyObjectiveDatabase();
        var firstTransaction = new TransactionAwareTransactionManager { PauseBeforeCompletion = true };
        var secondTransaction = new TransactionAwareTransactionManager();
        Guid playerId = Guid.NewGuid();
        var firstService = CreateTransactionAwareService(database, firstTransaction);
        var secondService = CreateTransactionAwareService(database, secondTransaction);

        Task<DailyObjectivesDTO> first = firstService.GetAsync(playerId);
        await firstTransaction.CompletionReached.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task<DailyObjectivesDTO> second = secondService.GetAsync(playerId);
        await database.WaitForLockAttemptsAsync(2);

        Assert.False(second.IsCompleted);
        firstTransaction.AllowCompletion.TrySetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(20, database.Sets[playerId].Assignments.Count);
        Assert.Equal(2, database.LockResources.Count(resource => resource == $"RelicWars:DailyObjective:{playerId}"));
    }

    [Fact]
    public async Task Concurrent_rollover_and_progress_preserve_and_clamp_progress()
    {
        var database = new TransactionAwareDailyObjectiveDatabase();
        Guid playerId = Guid.NewGuid();
        var initialTime = new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero));
        await CreateTransactionAwareService(database, new TransactionAwareTransactionManager(), initialTime).GetAsync(playerId);
        var nextDay = new MutableTimeProvider(new DateTimeOffset(2026, 7, 18, 0, 0, 0, TimeSpan.Zero));
        var rolloverTransaction = new TransactionAwareTransactionManager { PauseBeforeCompletion = true };
        var rollover = CreateTransactionAwareService(database, rolloverTransaction, nextDay);
        var progress = CreateTransactionAwareService(database, new TransactionAwareTransactionManager(), nextDay);

        Task<DailyObjectivesDTO> read = rollover.GetAsync(playerId);
        await rolloverTransaction.CompletionReached.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task update = progress.ApplyProgressAsync(playerId,
            new(DailyObjectiveProgressTypeEnum.BuildingsCompleted, 100, nextDay.UtcNow.UtcDateTime));
        await database.WaitForLockAttemptsAsync(3);
        rolloverTransaction.AllowCompletion.TrySetResult();
        await Task.WhenAll(read, update);

        DailyObjectiveSet set = database.Sets[playerId];
        Assert.Equal(new DateTime(2026, 7, 18), set.DayStartUtc);
        Assert.Equal(20, set.Assignments.Count);
        var assignment = Assert.Single(set.Assignments, assignment => assignment.DefinitionId == 2);
        Assert.Equal(assignment.Target, assignment.Progress);
        Assert.True(assignment.IsComplete);
    }

    [Fact]
    public async Task Rollback_releases_player_lock_for_a_retry()
    {
        var database = new TransactionAwareDailyObjectiveDatabase { ThrowOnNextRead = true };
        Guid playerId = Guid.NewGuid();
        var first = CreateTransactionAwareService(database, new TransactionAwareTransactionManager());
        var retry = CreateTransactionAwareService(database, new TransactionAwareTransactionManager());

        await Assert.ThrowsAsync<InvalidOperationException>(() => first.GetAsync(playerId));
        DailyObjectivesDTO response = await retry.GetAsync(playerId).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(20, response.Rows.Count);
    }

    [Fact]
    public async Task Different_players_use_different_lock_resources_and_do_not_block_each_other()
    {
        var database = new TransactionAwareDailyObjectiveDatabase();
        var heldTransaction = new TransactionAwareTransactionManager { PauseBeforeCompletion = true };
        Guid firstPlayerId = Guid.NewGuid();
        Guid secondPlayerId = Guid.NewGuid();
        Task<DailyObjectivesDTO> held = CreateTransactionAwareService(database, heldTransaction).GetAsync(firstPlayerId);
        await heldTransaction.CompletionReached.Task.WaitAsync(TimeSpan.FromSeconds(2));

        DailyObjectivesDTO other = await CreateTransactionAwareService(database, new TransactionAwareTransactionManager())
            .GetAsync(secondPlayerId).WaitAsync(TimeSpan.FromSeconds(2));
        heldTransaction.AllowCompletion.TrySetResult();
        await held;

        Assert.Equal(20, other.Rows.Count);
        Assert.Contains($"RelicWars:DailyObjective:{firstPlayerId}", database.LockResources);
        Assert.Contains($"RelicWars:DailyObjective:{secondPlayerId}", database.LockResources);
    }

    [Fact]
    public async Task Standalone_progress_reloads_daily_state_once_after_concurrency_conflict()
    {
        var repository = new MemoryDailyObjectiveRepository();
        using var conflictContext = CreateConflictContext(new DailyObjectiveAssignment());
        var transactionManager = new RetryOnceTransactionManager(DailyConflict(conflictContext));
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        var service = new DailyObjectiveService(
            repository,
            new AllowPlayerAccessService(),
            CreateCatalogReader(),
            new CyclingRandom(),
            new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)),
            transactionManager,
            unitReader,
            new NoOpResourceService(),
            new FixedCityStatService());

        await service.ApplyProgressAsync(Guid.NewGuid(),
            new(DailyObjectiveProgressTypeEnum.BuildingsCompleted, 1,
                new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(2, transactionManager.SaveAttempts);
        Assert.Equal(2, repository.ResetCount);
    }

    [Fact]
    public async Task Standalone_progress_does_not_retry_a_concurrency_conflict_without_daily_entries()
    {
        var repository = new MemoryDailyObjectiveRepository();
        using var conflictContext = CreateConflictContext(new WorldPlayer());
        var transactionManager = new RetryOnceTransactionManager(NonDailyConflict(conflictContext));
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        var service = new DailyObjectiveService(
            repository,
            new AllowPlayerAccessService(),
            CreateCatalogReader(),
            new CyclingRandom(),
            new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)),
            transactionManager,
            unitReader,
            new NoOpResourceService(),
            new FixedCityStatService());

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.ApplyProgressAsync(Guid.NewGuid(),
            new(DailyObjectiveProgressTypeEnum.BuildingsCompleted, 1,
                new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc))));

        Assert.Equal(1, transactionManager.SaveAttempts);
        Assert.Equal(1, repository.ResetCount);
    }

    [Fact]
    public async Task Standalone_read_reloads_daily_state_once_after_concurrency_conflict()
    {
        var repository = new MemoryDailyObjectiveRepository();
        using var conflictContext = CreateConflictContext(new DailyObjectiveSet());
        var transactionManager = new RetryOnceTransactionManager(DailySetConflict(conflictContext));
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        var service = new DailyObjectiveService(
            repository,
            new AllowPlayerAccessService(),
            CreateCatalogReader(),
            new CyclingRandom(),
            new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)),
            transactionManager,
            unitReader,
            new NoOpResourceService(),
            new FixedCityStatService());

        DailyObjectivesDTO response = await service.GetAsync(Guid.NewGuid());

        Assert.Equal(20, response.Rows.Count);
        Assert.Equal(2, transactionManager.SaveAttempts);
        Assert.Equal(2, repository.ResetCount);
    }

    [Fact]
    public async Task Daily_state_is_reset_after_lock_and_before_load()
    {
        var service = CreateService(out var repository, out _, new CyclingRandom());

        await service.GetAsync(Guid.NewGuid());

        Assert.Equal(["Lock", "Reset", "Load"], repository.Calls.Take(3));
    }

    [Fact]
    public async Task Production_is_clipped_to_current_utc_day_and_negative_coins_are_ignored()
    {
        var service = CreateService(out var repository, out _, new CyclingRandom());
        Guid playerId = Guid.NewGuid();
        await service.GetAsync(playerId);
        repository.Set!.Assignments[0].DefinitionId = 30;
        repository.Set.Assignments[0].Target = 1000;
        repository.Set.Assignments[1].DefinitionId = 1;
        repository.Set.Assignments[1].Target = 100;

        await service.ApplyProductionAsync(
            playerId,
            new DateTime(2026, 7, 16, 23, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 17, 1, 0, 0, DateTimeKind.Utc),
            coinsPerHour: -50,
            exoticResourcesPerHour: 20);

        Assert.Equal(0, repository.Set.Assignments[0].Progress);
        Assert.Equal(20, repository.Set.Assignments[1].Progress, 5);
    }

    private static DailyObjectiveService CreateService(
        out MemoryDailyObjectiveRepository repository,
        out MutableTimeProvider time,
        IRandomService random)
    {
        repository = new MemoryDailyObjectiveRepository();
        time = new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero));
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        return new DailyObjectiveService(
            repository,
            new AllowPlayerAccessService(),
            CreateCatalogReader(),
            random,
            time,
            new MemoryTransactionManager(),
            unitReader,
            new NoOpResourceService(),
            new FixedCityStatService());
    }

    private static DailyObjectiveService CreateTransactionAwareService(
        TransactionAwareDailyObjectiveDatabase database,
        TransactionAwareTransactionManager transactionManager,
        MutableTimeProvider? time = null)
    {
        var unitReader = new UnitDataReader();
        unitReader.Load(RepositoryFile("Backend", "Game", "units.json"));
        return new DailyObjectiveService(
            new TransactionAwareDailyObjectiveRepository(database, transactionManager),
            new AllowPlayerAccessService(),
            CreateCatalogReader(),
            new CyclingRandom(),
            time ?? new MutableTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)),
            transactionManager,
            unitReader,
            new NoOpResourceService(),
            new FixedCityStatService());
    }

    private static DailyObjectiveDataReader CreateCatalogReader()
    {
        var reader = new DailyObjectiveDataReader();
        reader.Load(RepositoryFile("Backend", "Game", "daily-objectives-complete.json"));
        return reader;
    }

    private static string RepositoryFile(params string[] parts)
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !Directory.Exists(Path.Combine(directory, "Backend")))
            directory = Directory.GetParent(directory)?.FullName;
        return Path.Combine(new[] { directory ?? throw new InvalidOperationException("Repository root not found.") }.Concat(parts).ToArray());
    }

    private sealed class MemoryDailyObjectiveRepository : IDailyObjectiveRepository
    {
        private readonly Dictionary<Guid, DailyObjectiveSet> _sets = new();
        public DailyObjectiveSet? Set { get; private set; }
        public int ResetCount { get; private set; }
        public List<string> Calls { get; } = [];
        public Task AcquirePlayerLockAsync(Guid worldPlayerId)
        {
            Calls.Add("Lock");
            return Task.CompletedTask;
        }
        public Task<DailyObjectiveSet?> GetByWorldPlayerIdAsync(Guid worldPlayerId)
        {
            Calls.Add("Load");
            return Task.FromResult(_sets.GetValueOrDefault(worldPlayerId));
        }
        public Task<DailyObjectiveSet> ReplaceAsync(DailyObjectiveSet? existingSet, DailyObjectiveSet replacement)
        {
            Set = replacement;
            _sets[replacement.WorldPlayerId] = replacement;
            return Task.FromResult(replacement);
        }
        public void ResetTrackedState(Guid worldPlayerId)
        {
            ResetCount++;
            Calls.Add("Reset");
        }
    }

    private sealed class MemoryTransactionManager : ITransactionManager
    {
        public async Task ExecuteAsync(Func<Task> operation) => await operation();
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => await operation();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class TransactionAwareDailyObjectiveDatabase
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private int _lockAttempts;
        public ConcurrentDictionary<Guid, DailyObjectiveSet> Sets { get; } = new();
        public ConcurrentQueue<string> LockResources { get; } = new();
        public bool ThrowOnNextRead { get; set; }

        public async Task AcquireAsync(Guid worldPlayerId, TransactionAwareTransactionManager transactionManager)
        {
            string resource = $"RelicWars:DailyObjective:{worldPlayerId}";
            LockResources.Enqueue(resource);
            Interlocked.Increment(ref _lockAttempts);
            var gate = _locks.GetOrAdd(resource, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            transactionManager.Enlist(gate);
        }

        public async Task WaitForLockAttemptsAsync(int expected)
        {
            var timeout = DateTime.UtcNow.AddSeconds(2);
            while (Volatile.Read(ref _lockAttempts) < expected && DateTime.UtcNow < timeout)
                await Task.Delay(10);
            Assert.True(Volatile.Read(ref _lockAttempts) >= expected);
        }
    }

    private sealed class TransactionAwareDailyObjectiveRepository : IDailyObjectiveRepository
    {
        private readonly TransactionAwareDailyObjectiveDatabase _database;
        private readonly TransactionAwareTransactionManager _transactionManager;

        public TransactionAwareDailyObjectiveRepository(
            TransactionAwareDailyObjectiveDatabase database,
            TransactionAwareTransactionManager transactionManager)
        {
            _database = database;
            _transactionManager = transactionManager;
        }

        public Task AcquirePlayerLockAsync(Guid worldPlayerId) => _database.AcquireAsync(worldPlayerId, _transactionManager);

        public Task<DailyObjectiveSet?> GetByWorldPlayerIdAsync(Guid worldPlayerId)
        {
            if (_database.ThrowOnNextRead)
            {
                _database.ThrowOnNextRead = false;
                throw new InvalidOperationException("Simulated transaction failure.");
            }
            return Task.FromResult(_database.Sets.GetValueOrDefault(worldPlayerId));
        }

        public Task<DailyObjectiveSet> ReplaceAsync(DailyObjectiveSet? existingSet, DailyObjectiveSet replacement)
        {
            if (existingSet != null)
            {
                existingSet.DayStartUtc = replacement.DayStartUtc;
                existingSet.Assignments = replacement.Assignments;
                foreach (var assignment in existingSet.Assignments)
                    assignment.DailyObjectiveSetId = existingSet.Id;
                return Task.FromResult(existingSet);
            }
            _database.Sets[replacement.WorldPlayerId] = replacement;
            return Task.FromResult(replacement);
        }

        public void ResetTrackedState(Guid worldPlayerId) { }
    }

    private static GameContext CreateConflictContext(object entity)
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new GameContext(options);
        context.Attach(entity);
        return context;
    }

    private static DbUpdateConcurrencyException DailyConflict(GameContext context) =>
        Conflict(context.Entry(context.ChangeTracker.Entries<DailyObjectiveAssignment>().Single().Entity));

    private static DbUpdateConcurrencyException DailySetConflict(GameContext context) =>
        Conflict(context.Entry(context.ChangeTracker.Entries<DailyObjectiveSet>().Single().Entity));

    private static DbUpdateConcurrencyException NonDailyConflict(GameContext context) =>
        Conflict(context.Entry(context.ChangeTracker.Entries<WorldPlayer>().Single().Entity));

    #pragma warning disable EF1001
    private static DbUpdateConcurrencyException Conflict(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        new("Simulated concurrency conflict.", [(IUpdateEntry)entry.GetInfrastructure()]);
    #pragma warning restore EF1001

    private sealed class RetryOnceTransactionManager(DbUpdateConcurrencyException conflict) : ITransactionManager
    {
        public int SaveAttempts { get; private set; }
        public Task ExecuteAsync(Func<Task> operation) => operation();
        public Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => operation();

        public Task SaveChangesAsync()
        {
            SaveAttempts++;
            return SaveAttempts == 1
                ? Task.FromException(conflict)
                : Task.CompletedTask;
        }
    }

    private sealed class TransactionAwareTransactionManager : ITransactionManager
    {
        private readonly AsyncLocal<List<SemaphoreSlim>?> _heldLocks = new();
        public bool HasActiveTransaction => _heldLocks.Value != null;
        public bool PauseBeforeCompletion { get; init; }
        public TaskCompletionSource CompletionReached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ExecuteAsync(Func<Task> operation) => ExecuteAsync(async () =>
        {
            await operation();
            return true;
        });

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (HasActiveTransaction) return await operation();
            _heldLocks.Value = new List<SemaphoreSlim>();
            try
            {
                T result = await operation();
                if (PauseBeforeCompletion)
                {
                    CompletionReached.TrySetResult();
                    await AllowCompletion.Task;
                }
                return result;
            }
            finally
            {
                foreach (var gate in _heldLocks.Value!) gate.Release();
                _heldLocks.Value = null;
            }
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
        public void Enlist(SemaphoreSlim gate) => _heldLocks.Value!.Add(gate);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public MutableTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; set; }
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class CyclingRandom : IRandomService
    {
        private int _value;
        public int Next(int maxValue) => _value++ % maxValue;
        public double NextDouble() => 0.5;
    }

    private sealed class AlwaysMaximumRandom : IRandomService
    {
        public int Next(int maxValue) => maxValue - 1;
        public double NextDouble() => 1;
    }

    private sealed class FirstTierRollRandom : IRandomService
    {
        private readonly int _tierRoll;
        private int _calls;
        public FirstTierRollRandom(int tierRoll) => _tierRoll = tierRoll;
        public int Next(int maxValue)
        {
            _calls++;
            return _calls == 11 ? Math.Min(_tierRoll, maxValue - 1) : 0;
        }
        public double NextDouble() => 0;
    }

    private sealed class AllowPlayerAccessService : IPlayerAccessService
    {
        private readonly Dictionary<Guid, WorldPlayer> _players = new();
        public Guid GetAuthenticatedProfileId() => Guid.NewGuid();
        public WorldPlayer GetPlayer(Guid worldPlayerId) => _players.GetValueOrDefault(worldPlayerId) ??
            throw new KeyNotFoundException();
        public Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId)
        {
            if (!_players.TryGetValue(worldPlayerId, out var player))
            {
                player = new WorldPlayer
                {
                    Id = worldPlayerId,
                    Cities = new() { new City { Id = worldPlayerId, WorldPlayerId = worldPlayerId } }
                };
                _players[worldPlayerId] = player;
            }
            return Task.FromResult(player);
        }
        public Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId) => throw new NotImplementedException();
        public Task<City> RequireOwnedCityAsync(Guid cityId) => throw new NotImplementedException();
        public Task<City> RequireOwnedCityForTownHallAsync(Guid cityId) => throw new NotImplementedException();
        public Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId) => throw new NotImplementedException();
    }

    private sealed class NoOpResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City city, DateTime now) =>
            new(city.Wood, city.Stone, city.Metal, 0, 0, 0, now);
        public CityProductionSnapshot CalculateCityProduction(WorldPlayer player, City city) => new(0, 0);
        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer player, DateTime now) =>
            new(player.Coins, player.IdeologyFocusPoints, 0, 0, now);
    }

    private sealed class FixedCityStatService : ICityStatService
    {
        public double GetWarehouseCapacity(City city) => 10_000;
        public int GetMaxPopulation(City city) => 0;
        public int GetCurrentPopulationUsage(City city, IEnumerable<Domain.Workers.Abstraction.BaseJob> activeJobs) => 0;
        public int GetAvailablePopulation(City city, IEnumerable<Domain.Workers.Abstraction.BaseJob> activeJobs) => 0;
    }

    private sealed class DenyPlayerAccessService : IPlayerAccessService
    {
        public Guid GetAuthenticatedProfileId() => Guid.Empty;
        public Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId) => throw new UnauthorizedAccessException();
        public Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId) => throw new NotImplementedException();
        public Task<City> RequireOwnedCityAsync(Guid cityId) => throw new NotImplementedException();
        public Task<City> RequireOwnedCityForTownHallAsync(Guid cityId) => throw new NotImplementedException();
        public Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId) => throw new NotImplementedException();
    }
}
