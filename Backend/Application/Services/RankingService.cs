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

        public async Task<List<RankingEntryData>> GetRankings()
        {
            // Vi wrapper i Task.Run hvis du vil beholde det asynkrone interface,
            // selvom operationen nu er synkron (cached memory læsning).
            return await Task.Run(() =>
            {
                return _reader.GetGlobalRankings()
                    .OrderBy(x => x.Rank)
                    .ToList();
            });
        }

        public async Task<RankingEntryData?> GetRankingById(Guid worldPlayerId)
        {
            return await Task.Run(() =>
            {
                var allRankings = _reader.GetGlobalRankings();

                var rankingEntry = allRankings.FirstOrDefault(x => x.WorldPlayerId == worldPlayerId);

                return rankingEntry;
            });
        }
    }
}