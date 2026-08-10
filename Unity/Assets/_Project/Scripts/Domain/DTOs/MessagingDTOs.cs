using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Project.Network.Models;

namespace Project.Scripts.Domain.DTOs
{
    public class ConversationParticipantDTO
    {
        public Guid WorldPlayerId { get; set; }
        public string Username { get; set; }
        public DateTime? LastReadAt { get; set; }
    }

    public class ConversationDTO
    {
        public Guid Id { get; set; }
        public Guid ParticipantId { get; set; }
        public string ParticipantName { get; set; }
        public List<ConversationParticipantDTO> Participants { get; set; }
        public bool IsGroupConversation { get; set; }
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
        public Guid? SenderAllianceId { get; set; }
        public string SenderName { get; set; }
        public string SenderAllianceName { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public ReportAttachmentDTO ReportAttachment { get; set; }
    }

    public class ReportAttachmentDTO
    {
        public bool IsAvailable { get; set; }
        public SharedBattleReportDTO Report { get; set; }
    }

    public class SharedBattleReportDTO
    {
        public Guid Id { get; set; }
        public ReportTypeEnum ReportType { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime OccurredAt { get; set; }
        public List<UnitStackDTO> AttackerLosses { get; set; }
        public List<UnitStackDTO> DefenderLosses { get; set; }
        public List<UnitStackDTO> RevivedUnits { get; set; }
        public List<string> AppliedModifiers { get; set; }
    }
}
