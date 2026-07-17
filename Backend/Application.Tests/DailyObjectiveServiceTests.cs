using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Domain.StaticData.Data;
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
            unitReader);

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
            unitReader);
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
        public Task<DailyObjectiveSet?> GetByWorldPlayerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(_sets.GetValueOrDefault(worldPlayerId));
        public Task<DailyObjectiveSet> ReplaceAsync(DailyObjectiveSet? existingSet, DailyObjectiveSet replacement)
        {
            Set = replacement;
            _sets[replacement.WorldPlayerId] = replacement;
            return Task.FromResult(replacement);
        }
    }

    private sealed class MemoryTransactionManager : ITransactionManager
    {
        public async Task ExecuteAsync(Func<Task> operation) => await operation();
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => await operation();
        public Task SaveChangesAsync() => Task.CompletedTask;
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
        public Guid GetAuthenticatedProfileId() => Guid.NewGuid();
        public Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId) => Task.FromResult(new WorldPlayer { Id = worldPlayerId });
        public Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId) => throw new NotImplementedException();
        public Task<City> RequireOwnedCityAsync(Guid cityId) => throw new NotImplementedException();
        public Task<City> RequireOwnedCityForTownHallAsync(Guid cityId) => throw new NotImplementedException();
        public Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId) => throw new NotImplementedException();
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
