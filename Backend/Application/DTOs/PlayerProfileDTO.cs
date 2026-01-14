using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record PlayerProfileDTO(
        Guid PlayerId,
        string UserName,
        string Email,
        List<WorldPlayerDTO> WorldPlayers
    );

}
