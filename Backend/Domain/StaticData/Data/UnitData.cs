using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.StaticData.Data
{
    public record UnitRequirement(BuildingTypeEnum Type, int RequiredLevel);
    public class UnitData
    {
        public UnitTypeEnum Type { get; set; }
        public UnitCategoryEnum Category { get; set; }

        public int Power { get; set; }
        public int Armor { get; set; }
        public int Reach { get; set; }
        public int Discipline { get; set; }
        public int Mobility { get; set; }
        public int LootCapacity { get; set; }

        public int WoodCost { get; set; }
        public int StoneCost { get; set; }
        public int MetalCost { get; set; }
        public int PopulationCost { get; set; }

        public int RecruitmentTimeInSeconds { get; set; }
        public List<UnitRequirement> Prerequisites { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();

    }
}
