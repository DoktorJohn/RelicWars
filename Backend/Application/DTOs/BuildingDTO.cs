using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record BuildingDTO(
    Guid Id,
    string Type,
    int Level,
    DateTime? UpgradeFinished,
    bool IsUpgrading
);

    public record BuildingResult(bool Success, string Message);

}
