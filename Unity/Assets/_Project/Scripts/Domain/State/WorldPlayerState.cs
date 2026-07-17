using Project.Network.Models;
using System.Collections.Generic;

namespace Assets.Scripts.Domain.State
{
    [System.Serializable]
    public class WorldPlayerState
    {
        public double CoinsAmount;
        public double CoinsProductionPerHour;

        public double ResearchPointsAmount;
        public double ResearchPointsProductionPerHour;

        public double IdeologyFocusPointsAmount;
        public double IdeologyFocusPointsProductionPerHour;

        public double TotalWoodAmount;
        public double TotalStoneAmount;
        public double TotalMetalAmount;
        public double TotalPopulationAmount;

        public List<CityDTO> PlayerCities = new List<CityDTO>();
    }
}
