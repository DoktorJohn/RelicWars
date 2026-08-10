using Domain.Abstraction;
using Domain.User;
using System;

namespace Domain.Entities
{
    public class Message : BaseEntity
    {
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public Guid SenderId { get; set; }
        public WorldPlayer Sender { get; set; } = null!;

        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public MessageReportAttachment? ReportAttachment { get; set; }
    }
}
