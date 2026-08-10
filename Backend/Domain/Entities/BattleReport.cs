using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BattleReport : BaseEntity
    {
        public ReportTypeEnum ReportType { get; set; } = ReportTypeEnum.Battle;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsPublic { get; set; }
        public string AttackerLossesJson { get; set; } = "[]";
        public string DefenderLossesJson { get; set; } = "[]";
        public string RevivedUnitsJson { get; set; } = "[]";
        public string AppliedModifiersJson { get; set; } = "[]";

        //FK
        public Guid WorldPlayerId { get; set; }
    }
}
