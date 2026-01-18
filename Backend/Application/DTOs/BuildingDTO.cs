using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record BuildingDTO(
    Guid Id,
    string Type,
    int Level,
    DateTime? UpgradeFinished,
    bool IsUpgrading
);

    public class WorkshopFullViewDTO
    {
        public int BuildingLevel { get; set; }
        public List<WorkshopUnitInfoDTO> AvailableUnits { get; set; } = new();
        public List<RecruitmentQueueItemDTO> RecruitmentQueue { get; set; } = new();
    }

    public class WorkshopUnitInfoDTO
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
    public class WarehouseProjectionDTO
    {
        public int Level { get; set; }
        public int Capacity { get; set; }
        public bool IsCurrentLevel { get; set; }
    }

    public class StableInfoDTO
    {
        public int Level { get; set; }
        public bool IsCurrentLevel { get; set; }
    }

    public class UniversityInfoDTO
    {
        public int Level { get; set; }
        public int ProductionPerHour { get; set; }
        public bool IsCurrentLevel { get; set; }
    }

    public class WorkshopInfoDTO
    {
        public int Level { get; set; }
        public bool IsCurrentLevel { get; set; }
    }

    public class ResourceBuildingInfoDTO
    {
        public int Level { get; set; }
        public int ProductionPrHour { get; set; }
        public bool IsCurrentLevel { get; set; }
    }

    public class HousingInfoDTO
    {
        public int Level { get; set; }
        public int Population { get; set; }
        public bool IsCurrentLevel { get; set; }
    }

    public class WallInfoDTO
    {
        public int Level { get; set; }
        public ModifierDTO DefensiveModifier { get; set; } = new();
        public bool IsCurrentLevel { get; set; }
    }

    public class MarketPlaceInfoDTO
    {
        public int Level { get; set; }
        public ModifierDTO Modifier { get; set; } = new();
    }

    public class TownHallInfoDTO
    {
        public int Level { get; set; }
        public ModifierDTO BuildingSpeedModifier { get; set; } = new();
    }

    public record BuildingResult(bool Success, string Message);

    public class AvailableBuildingDTO
    {
        public BuildingTypeEnum BuildingType { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }

        // Krav for næste niveau (Level + 1)
        public double WoodCost { get; set; }
        public double StoneCost { get; set; }
        public double MetalCost { get; set; }
        public int PopulationCost { get; set; }
        public int ConstructionTimeInSeconds { get; set; }

        // Status checks
        public bool IsCurrentlyUpgrading { get; set; }
        public bool CanAfford { get; set; }
        public bool HasPopulationRoom { get; set; }
        public bool MeetsRequirements => CanAfford && HasPopulationRoom && !IsCurrentlyUpgrading;
    }
}
