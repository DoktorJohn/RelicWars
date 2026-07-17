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

    public class WorkshopFullViewDTO
    {
        public int BuildingLevel { get; set; }
        public List<WorkshopUnitInfoDTO> AvailableUnits { get; set; } = new();
    }

    public class WorkshopUnitInfoDTO
    {
        public UnitTypeEnum UnitType { get; set; }
        public string UnitName { get; set; }
        public int AlreadyOwnedCount { get; set; }
        public int CostWood { get; set; }
        public int CostStone { get; set; }
        public int CostMetal { get; set; }
        public int Power { get; set; }
        public int Armor { get; set; }
        public int Discipline { get; set; }
        public int Mobility { get; set; }
        public int Reach { get; set; }
        public int LootCapacity { get; set; }
        public int PopulationCost { get; set; }
        public int UnitCapacity { get; set; }
        public int RecruitmentTimeInSeconds { get; set; }
        public bool IsUnlocked { get; set; }
        public List<string> UnmetRequirements { get; set; } = new();
    }

    public class StableFullViewDTO
    {
        public int BuildingLevel { get; set; }
        public List<StableUnitInfoDTO> AvailableUnits { get; set; } = new();
    }

    // Detaljer om de enkelte kavaleri-enheder (f.eks. Rider, Knight)
    public class StableUnitInfoDTO
    {
        public UnitTypeEnum UnitType { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int AlreadyOwnedCount { get; set; }
        public int CostWood { get; set; }
        public int CostStone { get; set; }
        public int CostMetal { get; set; }
        public int Power { get; set; }
        public int Armor { get; set; }
        public int Discipline { get; set; }
        public int Mobility { get; set; }
        public int Reach { get; set; }
        public int LootCapacity { get; set; }
        public int PopulationCost { get; set; }
        public int UnitCapacity { get; set; }
        public int RecruitmentTimeInSeconds { get; set; }
        public bool IsUnlocked { get; set; }
        public List<string> UnmetRequirements { get; set; } = new();
    }

    // Response: Det fulde overblik over barracks
    public class BarracksFullViewDTO
    {
        public int BuildingLevel { get; set; }
        public List<BarracksUnitInfoDTO> AvailableUnits { get; set; } = new List<BarracksUnitInfoDTO>();
    }

    // Detaljer om en enhed (pris, tid, inventory)
    public class BarracksUnitInfoDTO
    {
        public UnitTypeEnum UnitType { get; set; }
        public string UnitName { get; set; }
        public int AlreadyOwnedCount { get; set; }
        public int CostWood { get; set; }
        public int CostStone { get; set; }
        public int CostMetal { get; set; }
        public int Power { get; set; }
        public int Armor { get; set; }
        public int Discipline { get; set; }
        public int Mobility { get; set; }
        public int Reach { get; set; }
        public int LootCapacity { get; set; }
        public int PopulationCost { get; set; }
        public int UnitCapacity { get; set; }
        public int RecruitmentTimeInSeconds { get; set; }
        public bool IsUnlocked { get; set; }
        public List<string> UnmetRequirements { get; set; } = new();
    }

    public class HarborFullViewDTO
    {
        public int BuildingLevel { get; set; }
        public List<HarborUnitInfoDTO> AvailableUnits { get; set; } = new();
    }

    public class HarborUnitInfoDTO
    {
        public UnitTypeEnum UnitType { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int AlreadyOwnedCount { get; set; }
        public int CostWood { get; set; }
        public int CostStone { get; set; }
        public int CostMetal { get; set; }
        public int Power { get; set; }
        public int Armor { get; set; }
        public int Discipline { get; set; }
        public int Mobility { get; set; }
        public int Reach { get; set; }
        public int LootCapacity { get; set; }
        public int PopulationCost { get; set; }
        public int UnitCapacity { get; set; }
        public int RecruitmentTimeInSeconds { get; set; }
        public bool IsUnlocked { get; set; }
        public List<string> UnmetRequirements { get; set; } = new();
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

    public class GetRecruitmentQueueItemsDTO
    {
        public Guid CityId { get; set; }
        public List<UnitCategoryEnum> UnitCategories { get; set; } = new();
    }
}
