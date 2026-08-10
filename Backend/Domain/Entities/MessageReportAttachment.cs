using Domain.Abstraction;
using System;

namespace Domain.Entities
{
    public class MessageReportAttachment : BaseEntity
    {
        public Guid MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public Guid? BattleReportId { get; set; }
        public BattleReport? BattleReport { get; set; }
    }
}
