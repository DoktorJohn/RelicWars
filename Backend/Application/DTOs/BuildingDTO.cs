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
