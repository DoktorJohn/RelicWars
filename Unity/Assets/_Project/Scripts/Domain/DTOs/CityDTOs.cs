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
        public double CurrentWoodAmount;
        public double CurrentStoneAmount;
        public double CurrentMetalAmount;
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