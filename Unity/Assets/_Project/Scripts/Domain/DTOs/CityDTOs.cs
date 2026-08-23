using System;
using System.Collections.Generic;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;

namespace Project.Network.Models
{
    [Serializable]
    public class ChangeCityNameResponseDTO
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }

    }

    [Serializable]
    public class CityResourcesDTO
    {
        public Guid CityId;
        public double CurrentWoodAmount;
        public double CurrentStoneAmount;
        public double CurrentMetalAmount;
        public double WoodProductionPerHour;
        public double StoneProductionPerHour;
        public double MetalProductionPerHour;
        public double MaxWoodCapacity;
        public double MaxStoneCapacity;
        public double MaxMetalCapacity;
        public int CurrentPopulationUsage;
        public int MaxPopulationCapacity;
        public double Resistance;
        public double ResistanceTarget;
        public double ResistanceRecoveryPerHour;
        public List<CityExoticResourceDTO> ExoticResources = new();
    }

    [Serializable]
    public class CityDTO
    {
        public Guid Id { get; set; }
        public string CityName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Points { get; set; }
        public bool IsNPC { get; set; }
    }

    [Serializable]
    public class CityInspectionDTO
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Points { get; set; }
        public int? PlayerTotalPoints { get; set; }
        public Guid? WorldPlayerId { get; set; }
        public string WorldPlayerName { get; set; } = string.Empty;
        public Guid? AllianceId { get; set; }
        public string AllianceName { get; set; } = string.Empty;
        public bool CanAttack { get; set; }
        public bool CanSupport { get; set; }
        public bool IsNPC { get; set; }
    }

    [Serializable]
    public class CityOverviewHUDDTO
    {
        public Guid CityId;
        public string CityName;

        public ResourceOverviewDTO Wood;
        public ResourceOverviewDTO Stone;
        public ResourceOverviewDTO Metal;
        public CoinsBreakdownDTO CoinsProduction;
        public ResearchPowerBreakdownDTO ResearchPower;
        public ProductionBreakdownDTO IdeologyProduction;
        public PopulationBreakdownDTO Population;
        public double Resistance;
        public double ResistanceTarget;
        public double ResistanceRecoveryPerHour;
        public BuildingQueueOverviewDTO TownHallStatus;
        public BarracksQueueOverviewDTO BarracksStatus;
        public List<CityExoticResourceDTO> ExoticResources = new();
        public List<CityExoticResourceProductionDTO> ExoticResourceProductions = new();
    }

    [Serializable]
    public class CityExoticResourceProductionDTO
    {
        public int SlotIndex;
        public ExoticResourceTypeEnum ResourceType;
        public ProductionBreakdownDTO Production;
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
    public class ResearchPowerBreakdownDTO
    {
        public double BaseResearchPower;
        public double FlatBonus;
        public double PercentageBonus;
        public double EffectiveResearchPower;
    }

    [Serializable]
    public class CoinsBreakdownDTO
    {
        public double BaseValue;
        public double BuildingBonus;
        public double GlobalModifierMultiplier;
        public double FinalValuePerHour;
        public double Expenditure;
        public double GlobalUpkeepMultiplier;
        public double UnitUpkeepPerHour;
        public double BuildingUpkeepPerHour;
    }

    [Serializable]
    public class PopulationBreakdownDTO
    {
        public int HousingCapacity;
        public double ModifierBonus;
        public int TotalCapacity;
        public int InUse;
        public int Remaining;
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
        public double CurrentCoinsAmount;
        public double CurrentIdeologyFocusPoints;

        public double MaxWoodCapacity; 
        public double MaxStoneCapacity;
        public double MaxMetalCapacity; 

        public double WoodProductionPerHour;
        public double StoneProductionPerHour;
        public double MetalProductionPerHour;
        public double CoinsProductionPerHour;
        public double UnitUpkeepPerHour;
        public double BuildingUpkeepPerHour;
        public double ResearchPower;
        public double IdeologyFocusPointsPerHour;

        public int CurrentPopulationUsage;
        public int MaxPopulationCapacity;
        public PopulationBreakdownDTO Population = new();
        public double Resistance;
        public double ResistanceTarget;
        public double ResistanceRecoveryPerHour;
        public List<CityExoticResourceDTO> ExoticResources = new();
        public List<WorldIslandResourceDTO> IslandExoticResources = new();

        public List<CityControllerGetDetailedCityInformationBuildingDTO> BuildingList = new();
        public List<UnitStackDTO> StationedUnits { get; set; } = new();
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

    [Serializable]
    public class CityExoticResourceDTO
    {
        public ExoticResourceTypeEnum ResourceType;
        public double Amount;
    }

    [Serializable]
    public class ExoticResourceInvestmentRequestDTO
    {
        public int SlotIndex;
        public double WoodAmount;
        public double StoneAmount;
        public double MetalAmount;
        public double CoinAmount;
    }

    [Serializable]
    public class ExoticResourceInvestmentResponseDTO
    {
        public Guid CityId;
        public Guid IslandId;
        public int SlotIndex;
        public int NewTier;
        public List<WorldIslandResourceDTO> IslandExoticResources = new();
        public List<CityExoticResourceDTO> CityExoticResources = new();
    }
}
