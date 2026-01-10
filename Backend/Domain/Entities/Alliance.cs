using Domain.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Alliance : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BannerImageUrl { get; set; } = string.Empty;
        public int MaxPlayers { get; set; } = 50;
        public long TotalPoints { get; set; }

        //Nav prop
        public List<Guid> MemberIds { get; set; } = new();
        public List<Guid> AlliancesAtWar { get; set; } = new();
        public List<Guid> AlliancesPacted { get; set; } = new();
    }
}
