using System;

namespace Application.DTOs
{
    public class ConversationDTO
    {
        public Guid Id { get; set; }
        public Guid ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string Subject { get; set; } = "No Subject";
        public string LastMessageContent { get; set; } = string.Empty;
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }
}
