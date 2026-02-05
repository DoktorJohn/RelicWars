using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record MoveUnitRequestDTO(
        Guid UnitDeploymentId,
        int TargetX,
        int TargetY);

    public record DeployUnitRequestDTO(
        Guid OriginCityId,
        int TargetX,
        int TargetY,
        List<UnitSelectionDTO> UnitsToDeploy,
        Guid WorldPlayerId
    );

    public record UnitSelectionDTO(
        UnitTypeEnum Type,
        int Quantity
    );

    public record UnitDeploymentDTO(
        Guid Id,
        string Name,
        Guid WorldPlayerId,
        Guid OriginCityId,
        CityDTO OriginCity,
        Guid? TargetCityId,
        CityDTO? TargetCity,
        UnitDeploymentMovementStatusEnum Status,
        DateTime ArrivalTime,
        DateTime NextStepTime,
        DateTime LastStepTime,
        int CurrentX,
        int CurrentY,
        int NextX,
        int NextY,
        int FinalX,
        int FinalY,
        int Mobility,
        string RemainingPathJson,
        List<UnitStackDTO> UnitStacks,
        string WorldPlayerUserName
    );

}
