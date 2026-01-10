using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Building : BaseEntity
    {
        public int Level { get; set; }
        public BuildingTypeEnum Type { get; set; }
        public DateTime? TimeOfUpgradeStarted { get; set; }
        public DateTime? TimeOfUpgradeFinished { get; set; }
        public bool IsUpgrading => TimeOfUpgradeFinished.HasValue && TimeOfUpgradeFinished > DateTime.UtcNow;
    }
}
