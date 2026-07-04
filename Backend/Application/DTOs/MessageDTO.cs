using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class MessageDTO
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid SenderId { get; set; }
        public Guid? SenderAllianceId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAllianceName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class StartConversationRequestDTO
    {
        public Guid ReceiverWorldPlayerId { get; set; }
        public List<Guid> ParticipantWorldPlayerIds { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ReplyMessageRequestDTO
    {
        public string Content { get; set; } = string.Empty;
    }
}
