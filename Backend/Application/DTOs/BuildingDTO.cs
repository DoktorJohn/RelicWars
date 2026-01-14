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

    public class AcademyInfoDTO
    {
        public int Level { get; set; }
        public bool IsCurrentLevel { get; set; }
    }

    public class BarracksInfoDTO
    {
        public int Level { get; set; }
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
