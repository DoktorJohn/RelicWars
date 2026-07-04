using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities
{
    public class CityExoticResource : BaseEntity
    {
        public Guid CityId { get; set; }
        public City City { get; set; } = null!;
        public ExoticResourceTypeEnum ResourceType { get; set; }
        public double Amount { get; set; }
    }
}
