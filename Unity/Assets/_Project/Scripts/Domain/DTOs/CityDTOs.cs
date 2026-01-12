using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;

namespace Project.Network.Models
{
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

        // Kapaciteter til bue-indikatorer
        public double MaxWoodCapacity; // Tilføjet
        public double MaxStoneCapacity; // Tilføjet
        public double MaxMetalCapacity; // Tilføjet

        public double WoodProductionPerHour;
        public double StoneProductionPerHour;
        public double MetalProductionPerHour;

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