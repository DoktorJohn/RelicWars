using Project.Network.Models;
using Project.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class WorldPlayerEconomyDTO
    {
        public Guid WorldPlayerId;
        public double CurrentCoinsAmount;
        public double CurrentResearchPoints;
        public double CurrentIdeologyFocusPoints;
        public double CoinsProductionPerHour;
        public double ResearchPointsPerHour;
        public double IdeologyFocusPointsPerHour;
        public double TotalWoodAmount;
        public double TotalStoneAmount;
        public double TotalMetalAmount;
        public double TotalPopulationAmount;
        public List<CityDTO> PlayerCities;
        public DateTime LastUpdated;
    }

    [Serializable]
    public class WorldPlayerJoinResponse
    {
        public bool ConnectionSuccessful;
        public string Message;
        public Guid? ActiveCityId;
        public Guid? WorldPlayerId;
        public IdeologyTypeEnum SelectedIdeology;
    }

    [Serializable]
    public class WorldPlayerProfileDTO
    {
        public Guid WorldPlayerId { get; set; }
        public string UserName { get; set; }
        public int TotalPoints { get; set; }
        public int Ranking { get; set; }
        public int CityCount { get; set; }
        public string Description { get; set; }
        public string AllianceName { get; set; }
        public IdeologyTypeEnum Ideology { get; set; }
        public Guid AllianceId { get; set; }
        public Guid WorldId { get; set; }
        public List<CityDTO> Cities { get; set; } = new();
    }

    [Serializable]
    public class UpdateWorldPlayerDescriptionRequestDTO
    {
        public string Description { get; set; }
    }

    [Serializable]
    public class WorldPlayerSelectIdeologyResponse
    {
        public bool ConnectionSuccessful;
        public string Message;
    }
}
