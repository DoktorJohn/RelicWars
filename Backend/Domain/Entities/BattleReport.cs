using Domain.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BattleReport : BaseEntity
    {
        public Guid UserId { get; set; } // Hvem skal se rapporten?
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty; // Detaljer om tab og loot
        public DateTime OccurredAt { get; set; }
        public bool IsRead { get; set; }
    }
}
