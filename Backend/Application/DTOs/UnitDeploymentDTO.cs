using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record UnitDeploymentDTO(
    Guid Id,
    string Type,
    int Quantity,
    string Status,
    DateTime ArrivalTime,
    Guid OriginCityId,
    Guid TargetCityId,
    string TargetCityName
);
}
