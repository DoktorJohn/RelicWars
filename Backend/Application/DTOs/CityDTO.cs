using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{

    public record CityDTO(
        Guid Id,
        string CityName,
        int X,
        int Y,
        int Points,
        bool IsNPC = false
        );

    public record CityInspectionDTO(
        Guid CityId,
        string CityName,
        int X,
        int Y,
        int Points,
        Guid? WorldPlayerId,
        string? WorldPlayerName,
        Guid? AllianceId,
        string? AllianceName,
        bool CanAttack,
        bool CanSupport,
        bool IsNPC = false);


    public class CityControllerGetDetailedCityInformationDTO
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }

        // Ressourcebeholdning
        public double CurrentWoodAmount { get; set; }
        public double CurrentStoneAmount { get; set; }
        public double CurrentMetalAmount { get; set; }

        public double MaxWoodCapacity { get; set; }
        public double MaxStoneCapacity { get; set; }
        public double MaxMetalCapacity { get; set; }

        public double WoodProductionPerHour { get; set; }
        public double StoneProductionPerHour { get; set; }
        public double MetalProductionPerHour { get; set; }
        public double CoinsProductionPerHour { get; set; }
        public double ResearchPointsPerHour { get; set; }
        public double IdeologyFocusPointsPerHour { get; set; }

        public int CurrentPopulationUsage { get; set; }
        public int MaxPopulationCapacity { get; set; }
        public PopulationBreakdownDTO Population { get; set; } = new(0, 0, 0, 0, 0);
        public double Resistance { get; set; }
        public double ResistanceTarget { get; set; }
        public double ResistanceRecoveryPerHour { get; set; }
        public List<CityExoticResourceDTO> ExoticResources { get; set; } = new();
        public List<WorldIslandExoticResourceDTO> IslandExoticResources { get; set; } = new();

        // Liste over bygninger med dedikeret DTO til denne specifikke forespørgsel
        public List<CityControllerGetDetailedCityInformationBuildingDTO> BuildingList { get; set; } = new();
        public List<UnitStackDTO> StationedUnits { get; set; } = new();
    }

    public class CityResourcesDTO
    {
        public Guid CityId { get; set; }
        public double CurrentWoodAmount { get; set; }
        public double CurrentStoneAmount { get; set; }
        public double CurrentMetalAmount { get; set; }
        public double WoodProductionPerHour { get; set; }
        public double StoneProductionPerHour { get; set; }
        public double MetalProductionPerHour { get; set; }
        public double MaxWoodCapacity { get; set; }
        public double MaxStoneCapacity { get; set; }
        public double MaxMetalCapacity { get; set; }
        public int CurrentPopulationUsage { get; set; }
        public int MaxPopulationCapacity { get; set; }
        public double Resistance { get; set; }
        public double ResistanceTarget { get; set; }
        public double ResistanceRecoveryPerHour { get; set; }
        public List<CityExoticResourceDTO> ExoticResources { get; set; } = new();
    }

    public record CityOverviewHUD(
        Guid CityId,
        string CityName,

        // 2. Ressource Oversigt (Lager-ressourcer)
        ResourceOverviewDTO Wood,
        ResourceOverviewDTO Stone,
        ResourceOverviewDTO Metal,

        // 3. Produktions-detaljer (Hvor kommer tallene fra?)
        CoinsBreakdownDTO CoinsProduction,
        ProductionBreakdownDTO ResearchProduction,
        ProductionBreakdownDTO IdeologyProduction,

        // 4. Befolknings-detaljer
        PopulationBreakdownDTO Population,

        double Resistance,
        double ResistanceTarget,
        double ResistanceRecoveryPerHour,

        // 5. By-status (Hvor travlt er der?)
        BuildingQueueOverviewDTO TownHallStatus,
        BarracksQueueOverviewDTO BarracksStatus,
        List<CityExoticResourceDTO> ExoticResources,
        List<CityExoticResourceProductionDTO> ExoticResourceProductions
    );

    public record CityExoticResourceProductionDTO(
        int SlotIndex,
        ExoticResourceTypeEnum ResourceType,
        ProductionBreakdownDTO Production);

    public record ResourceOverviewDTO(
        double MaxCapacity,
        ProductionBreakdownDTO Production
    );

    public record ProductionBreakdownDTO(
        double BaseValue,              // Grundproduktion (fx fra bygningens level)
        double BuildingBonus,          // Flade bonusser fra andre bygninger
        double GlobalModifierMultiplier, // Procentvise bonusser fra Alliance/Research (fx 1.10 for +10%)
        double FinalValuePerHour       // Det endelige tal efter alle beregninger
    );

    public record CoinsBreakdownDTO(
    double BaseValue,              // Grundproduktion (fx fra bygningens level)
    double BuildingBonus,          // Flade bonusser fra andre bygninger
    double GlobalModifierMultiplier, // Procentvise bonusser fra Alliance/Research (fx 1.10 for +10%)
    double FinalValuePerHour,
    double Expenditure,
    double GlobalUpkeepMultiplier
);

    public record PopulationBreakdownDTO(
        int HousingCapacity,
        double ModifierBonus,
        int TotalCapacity,
        int InUse,
        int Remaining);
    public record BarracksQueueOverviewDTO(
        bool IsBusy,
        int TotalUnitsInQueue,
        string CurrentUnitType,
        DateTime? QueueFinishedAt
    );

    public record BuildingQueueOverviewDTO(
        bool IsBusy,
        int JobsInQueue,               // Antal bygninger i kø
        string CurrentBuildingName,    // Hvad bygges lige nu?
        DateTime? NextFinishedAt       // Hvornår er den næste færdig?
    );

    /// <summary>
    /// Bygnings-data specifikt knyttet til CityControllerGetDetailedCityInformation forespørgslen.
    /// </summary>
    public class CityControllerGetDetailedCityInformationBuildingDTO
    {
        public Guid BuildingId { get; set; }
        public BuildingTypeEnum BuildingType { get; set; }
        public int CurrentLevel { get; set; }
        public DateTime? UpgradeStartedAt { get; set; }
        public DateTime? UpgradeFinishedAt { get; set; }
        public bool IsCurrentlyUpgrading { get; set; }
    }
    
    public class ChangeCityNameResponseDTO
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }

    }

    public record CityExoticResourceDTO(
        ExoticResourceTypeEnum ResourceType,
        double Amount);
}
