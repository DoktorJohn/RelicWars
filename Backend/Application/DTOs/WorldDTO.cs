using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record WorldDTO(
    Guid Id,
    string Name,
    string Abbreviation,
    int XAxis,
    int YAxis
);

    public record GameWorldAvailableResponseDTO(
        Guid WorldId,
        string WorldName,
        int CurrentPlayerCount,
        int MaxPlayerCapacity,
        bool IsCurrentPlayerMember);

}
