using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    // Request: Bruges når klienten vil starte en træning
    public class RecruitUnitRequestDTO
    {
        public UnitTypeEnum UnitType { get; set; }
        public int Amount { get; set; }
    }

    public class StableFullViewDTO
    {
        public int BuildingLevel { get; set; }
        public List<StableUnitInfoDTO> AvailableUnits { get; set; } = new();
        public List<RecruitmentQueueItemDTO> RecruitmentQueue { get; set; } = new();
    }

    // Detaljer om de enkelte kavaleri-enheder (f.eks. Rider, Knight)
    public class StableUnitInfoDTO
    {
        public UnitTypeEnum UnitType { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int CurrentInventoryCount { get; set; }
        public int CostWood { get; set; }
        public int CostStone { get; set; }
        public int CostMetal { get; set; }
        public int RecruitmentTimeInSeconds { get; set; }
        public bool IsUnlocked { get; set; }
    }

    // Response: Det fulde overblik over barracks
    public class BarracksFullViewDTO
    {
        public int BuildingLevel { get; set; }
        public List<BarracksUnitInfoDTO> AvailableUnits { get; set; } = new List<BarracksUnitInfoDTO>();
        public List<RecruitmentQueueItemDTO> RecruitmentQueue { get; set; } = new List<RecruitmentQueueItemDTO>();
    }

    // Detaljer om en enhed (pris, tid, inventory)
    public class BarracksUnitInfoDTO
    {
        public UnitTypeEnum UnitType { get; set; }
        public string UnitName { get; set; }
        public int CurrentInventoryCount { get; set; }
        public int CostWood { get; set; }
        public int CostStone { get; set; }
        public int CostMetal { get; set; }
        public int RecruitmentTimeInSeconds { get; set; }
        public bool IsUnlocked { get; set; }
    }

    // Detaljer om et job i køen
    public class RecruitmentQueueItemDTO
    {
        public Guid QueueId { get; set; }
        public UnitTypeEnum UnitType { get; set; }
        public int Amount { get; set; }
        public double TimeRemainingSeconds { get; set; }
        public int TotalDurationSeconds { get; set; }
    }
}
