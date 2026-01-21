using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{

    public class CityControllerGetDetailedCityInformationDTO
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;

        // Ressourcebeholdning
        public double CurrentWoodAmount { get; set; }
        public double CurrentStoneAmount { get; set; }
        public double CurrentMetalAmount { get; set; }
        public double CurrentSilverAmount { get; set; }
        public double CurrentResearchPoints { get; set; }

        public double MaxWoodCapacity { get; set; }
        public double MaxStoneCapacity { get; set; }
        public double MaxMetalCapacity { get; set; }

        public double WoodProductionPerHour { get; set; }
        public double StoneProductionPerHour { get; set; }
        public double MetalProductionPerHour { get; set; }
        public double SilverProductionPerHour { get; set; }
        public double ResearchPointsPerHour { get; set; }

        public int CurrentPopulationUsage { get; set; }
        public int MaxPopulationCapacity { get; set; }

        // Liste over bygninger med dedikeret DTO til denne specifikke forespørgsel
        public List<CityControllerGetDetailedCityInformationBuildingDTO> BuildingList { get; set; } = new();
    }

    public record CityOverviewHUD(
        Guid CityId,
        string CityName,

        // 1. Globale Bebeholdninger (Wallet)
        double GlobalSilverAmount,
        double GlobalResearchPointsAmount,

        // 2. Ressource Oversigt (Lager-ressourcer)
        ResourceOverviewDTO Wood,
        ResourceOverviewDTO Stone,
        ResourceOverviewDTO Metal,

        // 3. Produktions-detaljer (Hvor kommer tallene fra?)
        ProductionBreakdownDTO SilverProduction,
        ProductionBreakdownDTO ResearchProduction,

        // 4. Befolknings-detaljer
        PopulationBreakdownDTO Population,

        // 5. By-status (Hvor travlt er der?)
        BuildingQueueOverviewDTO TownHallStatus,
        BarracksQueueOverviewDTO BarracksStatus
    );

    public record ResourceOverviewDTO(
        double CurrentAmount,
        double MaxCapacity,
        ProductionBreakdownDTO Production
    );

    public record ProductionBreakdownDTO(
        double BaseValue,              // Grundproduktion (fx fra bygningens level)
        double BuildingBonus,          // Flade bonusser fra andre bygninger
        double GlobalModifierMultiplier, // Procentvise bonusser fra Alliance/Research (fx 1.10 for +10%)
        double FinalValuePerHour       // Det endelige tal efter alle beregninger
    );

    public record PopulationBreakdownDTO(
        int MaxCapacity,
        int UsedByBuildings,
        int UsedByUnits,
        int FreePopulation,
        double ModifierBonus           // Ekstra plads fra f.eks. research eller buffs
    );
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
}
