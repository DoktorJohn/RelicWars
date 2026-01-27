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

    public record WorldPlayerProfileDTO(
        Guid worldPlayerId,
        string UserName,
        int TotalPoints,
        int Ranking,
        int CityCount,
        string AllianceName,
        IdeologyTypeEnum Ideology,
        Guid AllianceId,
        Guid WorldId
    );
}
