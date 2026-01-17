using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.StaticData.Data
{
    public class RankingEntryData
    {
        public Guid WorldPlayerId { get; set; }
        public int Rank { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string AllianceName { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int CityCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
