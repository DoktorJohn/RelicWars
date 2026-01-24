using System;
using System.Collections.Generic;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;

namespace Project.Network.Models
{
    [Serializable]
    public class CityOverviewHUDDTO
    {
        public Guid CityId;
        public string CityName;
        public double GlobalSilverAmount;
        public double GlobalResearchPointsAmount;
        public double GlobalIdeologyFocusPointsAmount;

        public ResourceOverviewDTO Wood;
        public ResourceOverviewDTO Stone;
        public ResourceOverviewDTO Metal;
        public ProductionBreakdownDTO SilverProduction;
        public ProductionBreakdownDTO ResearchProduction;
        public ProductionBreakdownDTO IdeologyProduction;
        public PopulationBreakdownDTO Population;
        public BuildingQueueOverviewDTO TownHallStatus;
        public BarracksQueueOverviewDTO BarracksStatus;
    }

    [Serializable]
    public class ResourceOverviewDTO
    {
        public double CurrentAmount;
        public double MaxCapacity;
        public ProductionBreakdownDTO Production;
    }

    [Serializable]
    public class ProductionBreakdownDTO
    {
        public double BaseValue;
        public double BuildingBonus;
        public double GlobalModifierMultiplier;
        public double FinalValuePerHour;
    }

    [Serializable]
    public class PopulationBreakdownDTO
    {
        public int MaxCapacity;
        public int UsedByBuildings;
        public int UsedByUnits;
        public int FreePopulation;
        public double ModifierBonus;
    }

    [Serializable]
    public class BuildingQueueOverviewDTO
    {
        public bool IsBusy;
        public int JobsInQueue;
        public string CurrentBuildingName;
        public DateTime? NextFinishedAt;
    }

    [Serializable]
    public class BarracksQueueOverviewDTO
    {
        public bool IsBusy;
        public int TotalUnitsInQueue;
        public string CurrentUnitType;
        public DateTime? QueueFinishedAt;
    }

    [Serializable]
    public class CityControllerGetDetailedCityInformationDTO
    {
        public Guid CityId;
        public string CityName;

        // Ressourcer
        public double CurrentWoodAmount;
        public double CurrentStoneAmount;
        public double CurrentMetalAmount;
        public double CurrentSilverAmount;
        public double CurrentResearchPoints;
        public double CurrentIdeologyFocusPoints;

        public double MaxWoodCapacity; 
        public double MaxStoneCapacity;
        public double MaxMetalCapacity; 

        public double WoodProductionPerHour;
        public double StoneProductionPerHour;
        public double MetalProductionPerHour;
        public double SilverProductionPerHour;
        public double ResearchPointsPerHour;
        public double IdeologyFocusPointsPerHour;

        public int CurrentPopulationUsage;
        public int MaxPopulationCapacity;

        public List<CityControllerGetDetailedCityInformationBuildingDTO> BuildingList = new();
        public List<UnitStackDTO> StationedUnits { get; set; } = new();
        public List<UnitDeploymentDTO> DeployedUnits { get; set; } = new();
    }

    [Serializable]
    public class UnitStackDTO
    {
        public UnitTypeEnum Type { get; set; }
        public int Quantity { get; set; }
    }

    [Serializable]
    public class UnitDeploymentDTO
    {
        public Guid Id { get; set; }
        public UnitTypeEnum Type { get; set; }
        public int Quantity { get; set; }
        public UnitDeploymentMovementStatusEnum Status { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public Guid OriginCityId { get; set; }
        public Guid? TargetCityId { get; set; }
        public string? TargetCityName { get; set; }
    }

    [Serializable]
    public class CityControllerGetDetailedCityInformationBuildingDTO
    {
        public Guid BuildingId;
        public BuildingTypeEnum BuildingType;
        public int CurrentLevel;
        public DateTime? UpgradeStartedAt;
        public DateTime? UpgradeFinishedAt;
        public bool IsCurrentlyUpgrading;
    }
}