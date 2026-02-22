using Project.Network.Models;
using System.Collections.Generic;

namespace Assets.Scripts.Domain.State
{
    [System.Serializable]
    public class WorldPlayerState
    {
        public double SilverAmount;
        public double SilverProductionPerHour;

        public double ResearchPointsAmount;
        public double ResearchPointsProductionPerHour;

        public double IdeologyFocusPointsAmount;
        public double IdeologyFocusPointsProductionPerHour;

        public List<CityDTO> PlayerCities = new List<CityDTO>();
    }
}
