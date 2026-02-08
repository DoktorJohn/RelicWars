using System;
using System.Collections.Generic;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;

namespace Project.Network.Models
{
    [Serializable]
    public class CityDTO
    {
        public Guid Id { get; set; }
        public string CityName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Points { get; set; }
    }

    [Serializable]
    public class CityOverviewHUDDTO
    {
        public Guid CityId;
        public string CityName;

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
        public int X;
        public int Y;

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