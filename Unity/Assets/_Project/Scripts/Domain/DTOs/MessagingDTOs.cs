using System;

namespace Project.Scripts.Domain.DTOs
{
    public class ConversationDTO
    {
        public Guid Id { get; set; }
        public Guid ParticipantId { get; set; }
        public string ParticipantName { get; set; }
        public string Subject { get; set; }
        public string LastMessageContent { get; set; }
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }

    public class MessageDTO
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}
