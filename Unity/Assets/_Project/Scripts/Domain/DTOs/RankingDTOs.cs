using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class RankingEntryDataDTO
    {
        public string WorldPlayerId { get; set; }
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public string AllianceName { get; set; }
        public int TotalPoints { get; set; }
        public int CityCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
