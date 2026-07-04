using Domain.Abstraction;
using Domain.User;
using System;

namespace Domain.Entities
{
    public class ConversationParticipant : BaseEntity
    {
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public Guid WorldPlayerId { get; set; }
        public WorldPlayer WorldPlayer { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
