using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class BattleReportDTO
    {
        public Guid Id { get; set; }
        public ReportTypeEnum ReportType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public bool IsRead { get; set; }
        public List<UnitStackDTO> AttackerLosses { get; set; } = new();
        public List<UnitStackDTO> DefenderLosses { get; set; } = new();
        public List<UnitStackDTO> RevivedUnits { get; set; } = new();
        public List<string> AppliedModifiers { get; set; } = new();
    }

    public class BattleReportUnreadStatusDTO
    {
        public int UnreadCount { get; set; }
    }
}
