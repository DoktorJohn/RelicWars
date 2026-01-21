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
    public record BuildingRequirement(BuildingTypeEnum Type, int RequiredLevel);

    public class BuildingLevelData : IModifierProvider
    {
        public int Level { get; set; }
        public int Points { get; set; }
        public int WoodCost { get; set; }
        public int StoneCost { get; set; }
        public int MetalCost { get; set; }
        public int PopulationCost { get; set; }
        public TimeSpan BuildTime { get; set; }

        public List<BuildingRequirement> Prerequisites { get; set; } = new();
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }

    public class TownHallLevelData : BuildingLevelData
    {
    }

    public class TimberCampLevelData : BuildingLevelData
    {
        public int ProductionPerHour { get; set; }
    }

    public class StoneQuarryLevelData : BuildingLevelData
    {
        public int ProductionPerHour { get; set; }
    }

    public class MetalMineLevelData : BuildingLevelData
    {
        public int ProductionPerHour { get; set; }
    }

    public class HousingLevelData : BuildingLevelData
    {
        public int Population { get; set; }
    }

    public class BarracksLevelData : BuildingLevelData
    {
    }

    public class StableLevelData : BuildingLevelData
    {
    }

    public class WorkshopLevelData : BuildingLevelData
    {
    }

    public class UniversityLevelData : BuildingLevelData
    {
        public int ProductionPerHour { get; set; }
    }

    public class WarehouseLevelData : BuildingLevelData
    {
        public int Capacity { get; set; }
    }

    public class WallLevelData : BuildingLevelData
    {
    }

    public class MarketPlaceLevelData : BuildingLevelData
    {

    }
}
