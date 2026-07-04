using Domain.Abstraction;
using Domain.User;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Conversation : BaseEntity
    {
        // Legacy 1:1 fields are kept for compatibility with the existing schema,
        // but the conversation is now driven by the Participants collection.
        public Guid Participant1Id { get; set; }
        public WorldPlayer Participant1 { get; set; } = null!;

        public Guid Participant2Id { get; set; }
        public WorldPlayer Participant2 { get; set; } = null!;

        public DateTime LastMessageDate { get; set; } = DateTime.UtcNow;

        public string Subject { get; set; } = "No Subject";

        public List<ConversationParticipant> Participants { get; set; } = new();
        public List<Message> Messages { get; set; } = new();

        public bool IsGroupConversation => Participants.Count > 2;
    }
}
