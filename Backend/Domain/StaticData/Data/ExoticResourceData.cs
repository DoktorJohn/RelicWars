using Domain.Enums;

namespace Domain.StaticData.Data
{
    public class ExoticResourceTierData
    {
        public int Tier { get; set; }
        public int WoodCost { get; set; }
        public int StoneCost { get; set; }
        public int MetalCost { get; set; }
        public int CoinsCost { get; set; }
        public double OutputPerHour { get; set; }
    }

    public class ExoticResourceDefinitionData
    {
        public ExoticResourceTypeEnum ResourceType { get; set; }
        public string IconKey { get; set; } = string.Empty;
        public List<ExoticResourceTierData> Tiers { get; set; } = new();
    }

    public class ExoticResourceDefinitionEntry
    {
        public ExoticResourceTypeEnum ResourceType { get; set; }
        public string IconKey { get; set; } = string.Empty;
    }

    public class ExoticResourceBalanceFile
    {
        public List<ExoticResourceTierData> Tiers { get; set; } = new();
        public List<ExoticResourceDefinitionEntry> Resources { get; set; } = new();
    }
}
