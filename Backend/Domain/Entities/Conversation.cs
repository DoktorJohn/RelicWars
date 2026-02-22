using Domain.Abstraction;
using Domain.User;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Conversation : BaseEntity
    {
        public Guid Participant1Id { get; set; }
        public WorldPlayer Participant1 { get; set; } = null!;

        public Guid Participant2Id { get; set; }
        public WorldPlayer Participant2 { get; set; } = null!;

        public DateTime LastMessageDate { get; set; } = DateTime.UtcNow;

        public string Subject { get; set; } = "No Subject";

        public List<Message> Messages { get; set; } = new();
    }
}
