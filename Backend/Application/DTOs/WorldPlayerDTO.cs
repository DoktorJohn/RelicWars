using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record WorldPlayerDTO(Guid PlayerProfileId, Guid WorldId);
    public record SelectIdeologyRequest(Guid WorldPlayerId, IdeologyTypeEnum Ideology);

    public record WorldPlayerJoinResponse(
        bool ConnectionSuccessful,
        string Message,
        Guid? ActiveCityId,
        Guid? WorldPlayerId,
        IdeologyTypeEnum SelectedIdeology
    );

    public record WorldPlayerSelectIdeologyResponse(
        bool ConnectionSuccessful,
        string Message
    );

    public class WorldPlayerEconomyDTO
    {
        public Guid WorldPlayerId { get; set; }
        public double CurrentCoinsAmount { get; set; }
        public double CurrentResearchPoints { get; set; }
        public double CurrentIdeologyFocusPoints { get; set; }
        public double CoinsProductionPerHour { get; set; }
        public double ResearchPointsPerHour { get; set; }
        public double IdeologyFocusPointsPerHour { get; set; }
        public double TotalWoodAmount { get; set; }
        public double TotalStoneAmount { get; set; }
        public double TotalMetalAmount { get; set; }
        public double TotalPopulationAmount { get; set; }
        public List<CityDTO> PlayerCities { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public record WorldPlayerProfileDTO(
        Guid worldPlayerId,
        string UserName,
        int TotalPoints,
        int Ranking,
        int CityCount,
        string Description,
        string AllianceName,
        IdeologyTypeEnum Ideology,
        Guid AllianceId,
        AllianceRoleEnum AllianceRole,
        Guid WorldId,
        List<CityDTO> Cities
    );

    public record UpdateWorldPlayerDescriptionRequestDTO(string Description);
}
