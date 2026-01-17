using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IRankingService
    {
        Task<List<RankingEntryData>> GetRankings();
        Task<RankingEntryData> GetRankingById(Guid worldPlayerId);
    }
}
