using System;
using System.Collections.Generic;
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
        public ResourceOverviewDTO Wood;
        public ResourceOverviewDTO Stone;
        public ResourceOverviewDTO Metal;
        public ProductionBreakdownDTO SilverProduction;
        public ProductionBreakdownDTO ResearchProduction;
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
        public double CurrentSilverAmount; // Tilføjet
        public double CurrentResearchPoints;

        // Kapaciteter til bue-indikatorer
        public double MaxWoodCapacity; // Tilføjet
        public double MaxStoneCapacity; // Tilføjet
        public double MaxMetalCapacity; // Tilføjet

        public double WoodProductionPerHour;
        public double StoneProductionPerHour;
        public double MetalProductionPerHour;
        public double SilverProductionPerHour;
        public double ResearchPointsPerHour;

        public int CurrentPopulationUsage;
        public int MaxPopulationCapacity;

        public List<CityControllerGetDetailedCityInformationBuildingDTO> BuildingList = new();
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