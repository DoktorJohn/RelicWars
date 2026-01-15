using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class RecruitmentQueueItemDTO
    {
        public Guid QueueId;
        public UnitTypeEnum UnitType;
        public int Amount;
        public double TimeRemainingSeconds;
        public int TotalDurationSeconds;
    }

    [Serializable]
    public class RecruitUnitRequestDTO
    {
        public UnitTypeEnum UnitType;
        public int Amount;
    }

    // Ny hjælpe-DTO til at læse { "Message": "..." } fra backenden
    [Serializable]
    public class BackendMessageDTO
    {
        public string Message;
    }
}
