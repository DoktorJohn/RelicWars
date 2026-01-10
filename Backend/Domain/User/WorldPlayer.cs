using Domain.Abstraction;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.User
{
    public class WorldPlayer : BaseEntity
    {
        public int Silver { get; set; }

        //Navprops
        public List<City> Cities { get; set; } = new();
        public List<Research> CompletedResearches { get; set; } = new();

        //Foreign keys
        public Guid? AllianceId { get; set; }
        public Alliance? Alliance { get; set; }
        public Guid PlayerProfileId { get; set; }
        public PlayerProfile PlayerProfile { get; set; } = null!;
        public Guid WorldId { get; set; }
        public World World { get; set; } = null!;

    }
}
