using Domain.Abstraction;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Research : BaseEntity
    {
        public string ResearchId { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }

        //Nav props
        public Guid WorldPlayerId { get; set; }
        public WorldPlayer WorldPlayer { get; set; } = null!;
    }
}
