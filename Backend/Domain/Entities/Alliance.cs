using Domain.Abstraction;
using Domain.User;
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

        //Nav prop
        public List<WorldPlayer> Members { get; set; } = new();
        public List<Alliance> AlliancesAtWar { get; set; } = new();
        public List<Alliance> AlliancesPacted { get; set; } = new();
    }
}
