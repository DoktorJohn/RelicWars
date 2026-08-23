using Application.Interfaces.IServices;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RankingService : IRankingService
    {
        private readonly RankingDataReader _reader;

        public RankingService(RankingDataReader reader)
        {
            _reader = reader;
        }

        public Task<List<RankingEntryData>> GetRankings() => Task.FromResult(ReadRankings());

        public Task<RankingEntryData?> GetRankingById(Guid worldPlayerId)
        {
            RankingEntryData? entry = _reader.GetGlobalRankings()
                .FirstOrDefault(item => item.WorldPlayerId == worldPlayerId);
            return Task.FromResult(entry);
        }

        private List<RankingEntryData> ReadRankings() => _reader.GetGlobalRankings()
            .OrderBy(item => item.Rank)
            .ToList();
    }
}
