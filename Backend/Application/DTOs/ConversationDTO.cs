using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class ConversationParticipantDTO
    {
        public Guid WorldPlayerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime? LastReadAt { get; set; }
    }

    public class ConversationDTO
    {
        public Guid Id { get; set; }
        public Guid ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public List<ConversationParticipantDTO> Participants { get; set; } = new();
        public bool IsGroupConversation { get; set; }
        public string Subject { get; set; } = "No Subject";
        public string LastMessageContent { get; set; } = string.Empty;
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }
}
