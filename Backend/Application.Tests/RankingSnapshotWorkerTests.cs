using Application.Interfaces.IRepositories;
using Application.Services;
using Application.Services.Workers;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using System.Text.Json;

namespace Application.Tests;

public class RankingSnapshotWorkerTests
{
    [Fact]
    public async Task RankingServiceReturnsLoadedSnapshotInRankOrder()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await WriteSnapshotAsync(tempPath,
                new RankingEntryData { WorldPlayerId = Guid.NewGuid(), Rank = 2 },
                new RankingEntryData { WorldPlayerId = Guid.NewGuid(), Rank = 1 });
            var reader = LoadReader(tempPath);

            var rankings = await new RankingService(reader).GetRankings();

            Assert.Equal([1, 2], rankings.Select(entry => entry.Rank));
        }
        finally
        {
            DeleteIfExists(tempPath);
        }
    }

    [Fact]
    public async Task RefreshSnapshotAsyncUsesTargetedReadAndReloadsSnapshot()
    {
        var previousPlayerId = Guid.NewGuid();
        var currentPlayerId = Guid.NewGuid();
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await WriteSnapshotAsync(tempPath,
                new RankingEntryData { WorldPlayerId = previousPlayerId, Rank = 1 });
            var reader = LoadReader(tempPath);
            var repository = new SnapshotCityRepository(_ => Task.FromResult(new List<City>
            {
                CreateCity(currentPlayerId)
            }));
            var worker = new RankingSnapshotWorker(repository, TestData.BuildingReader(), reader);

            await worker.RefreshSnapshotAsync();

            var entry = Assert.Single(reader.GetGlobalRankings());
            Assert.Equal(currentPlayerId, entry.WorldPlayerId);
            Assert.Equal(1, repository.SnapshotReadCount);
            Assert.Equal(0, repository.GeneralReadCount);
        }
        finally
        {
            DeleteIfExists(tempPath);
        }
    }

    [Fact]
    public async Task FailedRefreshLeavesExistingSnapshotReadable()
    {
        var previousPlayerId = Guid.NewGuid();
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await WriteSnapshotAsync(tempPath,
                new RankingEntryData { WorldPlayerId = previousPlayerId, Rank = 1 });
            string previousJson = await File.ReadAllTextAsync(tempPath);
            var reader = LoadReader(tempPath);
            var repository = new SnapshotCityRepository(_ =>
                Task.FromException<List<City>>(new InvalidOperationException("Database unavailable.")));
            var worker = new RankingSnapshotWorker(repository, TestData.BuildingReader(), reader);

            await Assert.ThrowsAsync<InvalidOperationException>(() => worker.RefreshSnapshotAsync());

            Assert.Equal(previousJson, await File.ReadAllTextAsync(tempPath));
            Assert.Equal(previousPlayerId, Assert.Single(reader.GetGlobalRankings()).WorldPlayerId);
        }
        finally
        {
            DeleteIfExists(tempPath);
        }
    }

    private static City CreateCity(Guid worldPlayerId)
    {
        var player = new WorldPlayer
        {
            Id = worldPlayerId,
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Current" }
        };
        return new City
        {
            Id = Guid.NewGuid(),
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = [new Building { Type = BuildingTypeEnum.TownHall, Level = 1 }]
        };
    }

    private static RankingDataReader LoadReader(string path)
    {
        var reader = new RankingDataReader();
        reader.Load(path);
        return reader;
    }

    private static Task WriteSnapshotAsync(string path, params RankingEntryData[] entries) =>
        File.WriteAllTextAsync(path, JsonSerializer.Serialize(entries));

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class SnapshotCityRepository(
        Func<CancellationToken, Task<List<City>>> loadSnapshot) : ICityRepository
    {
        public int SnapshotReadCount { get; private set; }
        public int GeneralReadCount { get; private set; }

        public Task<List<City>> GetForRankingSnapshotAsync(CancellationToken cancellationToken = default)
        {
            SnapshotReadCount++;
            return loadSnapshot(cancellationToken);
        }

        public Task<List<City>> GetAllAsync()
        {
            GeneralReadCount++;
            throw new InvalidOperationException("Ranking refresh must use the targeted repository read.");
        }

        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) => throw new NotSupportedException();
        public Task<City?> GetByIdAsync(Guid cityId) => throw new NotSupportedException();
        public Task UpdateAsync(City city) => throw new NotSupportedException();
        public Task UpdateRangeAsync(List<City> cities) => throw new NotSupportedException();
        public Task AddAsync(City city) => throw new NotSupportedException();
        public Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities) => throw new NotSupportedException();
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => throw new NotSupportedException();
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => throw new NotSupportedException();
        public Task<City?> GetByCoordinatesAsync(int x, int y) => throw new NotSupportedException();
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => throw new NotSupportedException();
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) => throw new NotSupportedException();
    }
}
