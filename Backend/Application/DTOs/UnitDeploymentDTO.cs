using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record UnitDeploymentDTO(
    Guid Id,
    UnitTypeEnum Type,
    int Quantity,
    UnitDeploymentMovementStatusEnum Status,
    DateTime? ArrivalTime,
    Guid OriginCityId,
    Guid? TargetCityId,
    string? TargetCityName
);
}
