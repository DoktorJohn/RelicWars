using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities
{
    public class WorldIslandExoticResource : BaseEntity
    {
        public Guid WorldIslandId { get; set; }
        public WorldIsland WorldIsland { get; set; } = null!;
        public int SlotIndex { get; set; }
        public ExoticResourceTypeEnum ResourceType { get; set; }
        public int Tier { get; set; } = 1;
        public double WoodInvestment { get; set; }
        public double StoneInvestment { get; set; }
        public double MetalInvestment { get; set; }
        public double CoinInvestment { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }
}
