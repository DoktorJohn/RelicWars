using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record WorldPlayerDTO(Guid PlayerProfileId, Guid WorldId);

    public record WorldPlayerJoinResponse(
        bool ConnectionSuccessful,
        string Message,
        Guid? ActiveCityId,
        Guid? WorldPlayerId
    );

    public record WorldPlayerProfileDTO(
        Guid worldPlayerId,
        string UserName,
        int TotalPoints,
        int Ranking,
        int CityCount,
        string AllianceName,
        Guid AllianceId
    );
}
