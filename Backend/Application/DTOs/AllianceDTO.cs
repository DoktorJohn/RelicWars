using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record AllianceDTO(
    Guid Id,
    string Name,
    string Tag,
    string Description,
    string BannerImageUrl,
    long TotalPoints,
    int MemberCount,
    int MaxPlayers
);
}
