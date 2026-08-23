using Application.Interfaces.IRepositories;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;

namespace Application.Services.Workers
{
    public sealed class RankingSnapshotWorker
    {
        private readonly ICityRepository _cityRepository;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly RankingDataReader _rankingDataReader;

        public RankingSnapshotWorker(
            ICityRepository cityRepository,
            BuildingDataReader buildingDataReader,
            RankingDataReader rankingDataReader)
        {
            _cityRepository = cityRepository;
            _buildingDataReader = buildingDataReader;
            _rankingDataReader = rankingDataReader;
        }

        public async Task RefreshSnapshotAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_rankingDataReader.StoragePath))
            {
                throw new InvalidOperationException("Ranking snapshot storage path is not configured.");
            }

            var cities = await _cityRepository.GetForRankingSnapshotAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            RankingGenerator.GenerateRankingSnapshot(
                _rankingDataReader.StoragePath,
                cities,
                _buildingDataReader);
            _rankingDataReader.ReloadFromDisk();
        }
    }
}
