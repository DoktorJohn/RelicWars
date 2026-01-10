using Domain.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Research : BaseEntity
    {
        public Guid UserId { get; set; }
        public string ResearchId { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}
