using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class WorldPlayerProfileDTO
    {
        public Guid WorldPlayerId { get; set; }
        public string UserName { get; set; }
        public int TotalPoints { get; set; }
        public int Ranking { get; set; }
        public int CityCount { get; set; }
        public string AllianceName { get; set; }
        public Guid AllianceId { get; set; }
    }
}
