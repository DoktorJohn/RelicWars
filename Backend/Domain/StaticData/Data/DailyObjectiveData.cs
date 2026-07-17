using Domain.Enums;

namespace Domain.StaticData.Data
{
    public sealed class DailyObjectiveCatalogData
    {
        public DailyObjectiveSelectionData Selection { get; set; } = new();
        public List<DailyObjectiveDefinitionData> Definitions { get; set; } = new();
    }

    public sealed class DailyObjectiveSelectionData
    {
        public int FixedSlots { get; set; }
        public int WeightedSlots { get; set; }
        public Dictionary<DailyObjectiveTierEnum, int> Weights { get; set; } = new();
    }

    public sealed class DailyObjectiveDefinitionData
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CompletionInfo { get; set; } = string.Empty;
        public DailyObjectiveTierEnum Tier { get; set; }
        public long Target { get; set; }
        public DailyObjectiveProgressTypeEnum ProgressType { get; set; }
        public UnitTypeEnum? UnitType { get; set; }
        public UnitCategoryEnum? UnitCategory { get; set; }
        public bool RequiresElite { get; set; }
        public bool IsImplemented { get; set; }
    }
}
