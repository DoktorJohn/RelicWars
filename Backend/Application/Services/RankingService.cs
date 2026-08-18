using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.StaticData.Data;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RankingService : IRankingService
    {
        private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromMinutes(15);
        private static readonly SemaphoreSlim SnapshotGenerationLock = new(1, 1);
        private static DateTime _lastWindowTriggeredGenerationUtc = DateTime.MinValue;

        private readonly RankingDataReader _reader;
        private readonly ICityRepository _cityRepository;
        private readonly BuildingDataReader _buildingDataReader;

        public RankingService(
            RankingDataReader reader,
            ICityRepository cityRepository,
            BuildingDataReader buildingDataReader)
        {
            _reader = reader;
            _cityRepository = cityRepository;
            _buildingDataReader = buildingDataReader;
        }

        // Internal consumers (profiles, alliances, etc.) read the existing snapshot.
        // Only opening RankingWindow is allowed to trigger regeneration.
        public Task<List<RankingEntryData>> GetRankings() => Task.FromResult(ReadRankings());

        public async Task<List<RankingEntryData>> GetRankingsForWindow()
        {
            if (SnapshotIsStale())
            {
                await SnapshotGenerationLock.WaitAsync();
                try
                {
                    // A concurrent request may already have regenerated the file.
                    if (SnapshotIsStale())
                    {
                        var allCities = await _cityRepository.GetAllAsync();
                        RankingGenerator.GenerateRankingSnapshot(
                            _reader.StoragePath,
                            allCities,
                            _buildingDataReader);
                        _reader.ReloadFromDisk();
                        _lastWindowTriggeredGenerationUtc = DateTime.UtcNow;
                    }
                }
                finally
                {
                    SnapshotGenerationLock.Release();
                }
            }

            return ReadRankings();
        }

        public Task<RankingEntryData?> GetRankingById(Guid worldPlayerId)
        {
            RankingEntryData? entry = _reader.GetGlobalRankings()
                .FirstOrDefault(item => item.WorldPlayerId == worldPlayerId);
            return Task.FromResult(entry);
        }

        private List<RankingEntryData> ReadRankings() => _reader.GetGlobalRankings()
            .OrderBy(item => item.Rank)
            .ToList();

        private bool SnapshotIsStale()
        {
            string path = _reader.StoragePath;
            return _lastWindowTriggeredGenerationUtc == DateTime.MinValue ||
                   DateTime.UtcNow - _lastWindowTriggeredGenerationUtc >= SnapshotLifetime ||
                   string.IsNullOrWhiteSpace(path) ||
                   !File.Exists(path) ||
                   DateTime.UtcNow - File.GetLastWriteTimeUtc(path) >= SnapshotLifetime;
        }
    }
}
