using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class WorkshopInfoDTO
    {
        public int Level;
        public bool IsCurrentLevel;
    }

    [Serializable]
    public class AcademyInfoDTO
    {
        public int Level;
        public bool IsCurrentLevel;
    }

    [Serializable]
    public class StableInfoDTO
    {
        public int Level;
        public bool IsCurrentLevel;
    }

    [Serializable]
    public class WallInfoDTO
    {
        public int Level;
        public ModifierDTO DefensiveModifier; // Den nestede DTO
        public bool IsCurrentLevel;
    }

    [Serializable]
    public class ModifierDTO
    {
        public double Value;
        public ModifierTypeEnum ModifierType;
        public ModifierTagEnum ModifierTag;
    }

    [Serializable]
    public class HousingProjectionDTO
    {
        public int Level;
        public int Population; // Antal borgere huset giver plads til
        public bool IsCurrentLevel;
    }

    [Serializable]
    public class AvailableBuildingDTO
    {
        // Identifikation
        public BuildingTypeEnum BuildingType;
        public string BuildingName;
        public int CurrentLevel;

        // Ressourcekrav for næste niveau
        public double WoodCost;
        public double StoneCost;
        public double MetalCost;
        public int PopulationCost;
        public int ConstructionTimeInSeconds;

        // Status-indikatorer (Beregnet i Backend)
        public bool IsCurrentlyUpgrading;
        public bool CanAfford;
        public bool HasPopulationRoom;

        /// <summary>
        /// Samlet vurdering af om spilleren teknisk set kan starte opgraderingen nu.
        /// </summary>
        public bool MeetsRequirements => CanAfford && HasPopulationRoom && !IsCurrentlyUpgrading;
    }

    [Serializable]
    public class WarehouseProjectionDTO
    {
        // Navnene SKAL starte med lille bogstav hvis backend sender camelCase
        public int level;
        public int capacity;
        public bool isCurrentLevel;

        // Properties til at gøre resten af din kode glad (så du ikke skal omdøbe alt i Controlleren)
        public int Level => level;
        public int Capacity => capacity;
        public bool IsCurrentLevel => isCurrentLevel;
    }

    [Serializable]
    public class ResourceBuildingInfoDTO
    {
        // Husk: Med Newtonsoft.Json er casing mindre vigtig, men det er god stil at matche.
        // Hvis backend sender camelCase (level), mapper Newtonsoft det automatisk til PascalCase (Level).
        public int Level;
        public int ProductionPrHour;
        public bool IsCurrentLevel;
    }
}
