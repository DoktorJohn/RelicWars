using Domain.Abstraction;
using Domain.User;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Conversation : BaseEntity
    {
        public DateTime LastMessageDate { get; set; } = DateTime.UtcNow;

        public string Subject { get; set; } = "No Subject";

        public List<ConversationParticipant> Participants { get; set; } = new();
        public List<Message> Messages { get; set; } = new();

        public bool IsGroupConversation => Participants.Count > 2;
    }
}
