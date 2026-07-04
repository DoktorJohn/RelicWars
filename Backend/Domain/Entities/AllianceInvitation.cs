using Domain.Abstraction;
using Domain.User;

namespace Domain.Entities
{
    public class AllianceInvitation : BaseEntity
    {
        public Guid AllianceId { get; set; }
        public Alliance Alliance { get; set; } = null!;
        public Guid InvitedWorldPlayerId { get; set; }
        public WorldPlayer InvitedWorldPlayer { get; set; } = null!;
        public Guid InvitedByWorldPlayerId { get; set; }
        public WorldPlayer InvitedByWorldPlayer { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
