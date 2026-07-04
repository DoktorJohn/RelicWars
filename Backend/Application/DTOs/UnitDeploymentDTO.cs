using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record AttackCityDeploymentRequestDTO(
        Guid OriginCityId,
        Guid TargetCityId,
        List<UnitSelectionDTO> UnitsToDeploy);

    public record SupportCityDeploymentRequestDTO(
        Guid OriginCityId,
        Guid TargetCityId,
        List<UnitSelectionDTO> UnitsToDeploy);

    public record DeploymentTravelEstimateRequestDTO(
        Guid OriginCityId,
        Guid TargetCityId,
        List<UnitSelectionDTO> UnitsToDeploy);

    public record DeploymentTravelEstimateDTO(
        long DurationSeconds,
        DateTime ArrivalTime);

    public record MoveUnitRequestDTO(
        Guid UnitDeploymentId,
        int TargetX,
        int TargetY);

    public record DeployUnitRequestDTO(
        Guid OriginCityId,
        int TargetX,
        int TargetY,
        List<UnitSelectionDTO> UnitsToDeploy,
        Guid WorldPlayerId,
        UnitDeploymentTypeEnum Type = UnitDeploymentTypeEnum.Attack
    );

    public record UnitSelectionDTO(
        UnitTypeEnum Type,
        int Quantity
    );

    public record IncomingAttackDTO(
        Guid DeploymentId,
        Guid SenderWorldPlayerId,
        string SenderWorldPlayerName,
        Guid TargetCityId,
        string TargetCityName,
        int TargetX,
        int TargetY,
        DateTime ArrivalTime);

    public record OwnedUnitDeploymentDTO(
        Guid Id,
        string Name,
        Guid WorldPlayerId,
        Guid OriginCityId,
        CityDTO OriginCity,
        Guid? TargetCityId,
        CityDTO? TargetCity,
        UnitDeploymentMovementStatusEnum Status,
        UnitDeploymentPhaseEnum Phase,
        DateTime DepartureTime,
        DateTime ArrivalTime,
        int LegStartX,
        int LegStartY,
        int LegEndX,
        int LegEndY,
        DateTime? StationedAt,
        int Mobility,
        UnitDeploymentTypeEnum Type,
        List<UnitStackDTO> UnitStacks,
        string WorldPlayerUserName
    );

}
