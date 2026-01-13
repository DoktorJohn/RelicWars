using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets._Project.Scripts.Domain.DTOs
{
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
}
