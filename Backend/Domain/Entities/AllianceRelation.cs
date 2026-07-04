using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities
{
    public class AllianceRelation : BaseEntity
    {
        public Guid WorldId { get; set; }
        public World World { get; set; } = null!;
        public Guid AllianceIdA { get; set; }
        public Alliance AllianceA { get; set; } = null!;
        public Guid AllianceIdB { get; set; }
        public Alliance AllianceB { get; set; } = null!;
        public AllianceRelationTypeEnum RelationType { get; set; }
        public AllianceRelationStatusEnum Status { get; set; }
        public Guid InitiatorAllianceId { get; set; }
        public Guid RespondingAllianceId { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
