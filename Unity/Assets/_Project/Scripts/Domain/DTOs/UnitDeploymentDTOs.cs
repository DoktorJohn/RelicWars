using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Network.Models;
using Project.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class AttackCityDeploymentRequestDTO
    {
        public Guid OriginCityId { get; set; }
        public Guid TargetCityId { get; set; }
        public List<UnitSelectionDTO> UnitsToDeploy { get; set; } = new();
    }

    [Serializable]
    public class SupportCityDeploymentRequestDTO
    {
        public Guid OriginCityId { get; set; }
        public Guid TargetCityId { get; set; }
        public List<UnitSelectionDTO> UnitsToDeploy { get; set; } = new();
    }

    [Serializable]
    public class UnitSelectionDTO
    {
        public UnitTypeEnum Type { get; set; }
        public int Quantity { get; set; }
    }
    [Serializable]
    public class UnitDeploymentDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid WorldPlayerId { get; set; }
        public Guid OriginCityId { get; set; }
        public CityDTO OriginCity { get; set; }
        public Guid? TargetCityId { get; set; }
        public CityDTO? TargetCity { get; set; }
        public UnitDeploymentMovementStatusEnum Status { get; set; }
        public UnitDeploymentPhaseEnum Phase { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public int LegStartX { get; set; }
        public int LegStartY { get; set; }
        public int LegEndX { get; set; }
        public int LegEndY { get; set; }
        public DateTime? StationedAt { get; set; }
        public int Mobility { get; set; }
        public UnitDeploymentTypeEnum Type { get; set; }
        public List<UnitStackDTO> UnitStacks { get; set; } = new();
        public string WorldPlayerUserName { get; set; }
        public DeploymentLocationDTO OriginLocation { get; set; }
        public DeploymentLocationDTO TargetLocation { get; set; }
    }

    [Serializable]
    public class DeploymentLocationDTO
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsNPC { get; set; }
        public Guid? WorldPlayerId { get; set; }
        public string WorldPlayerName { get; set; }
        public Guid? AllianceId { get; set; }
        public string AllianceName { get; set; }
        public string AllianceTag { get; set; }
    }

    [Serializable]
    public class DeploymentTravelEstimateRequestDTO
    {
        public Guid OriginCityId { get; set; }
        public Guid TargetCityId { get; set; }
        public List<UnitSelectionDTO> UnitsToDeploy { get; set; } = new();
    }

    [Serializable]
    public class DeploymentTravelEstimateDTO
    {
        public long DurationSeconds { get; set; }
        public DateTime ArrivalTime { get; set; }
        public bool RequiresTransport { get; set; }
        public int RequiredTransportCapacity { get; set; }
        public int AvailableTransportCapacity { get; set; }
        public int TransportCapacityMargin { get; set; }
        public bool HasSufficientTransportCapacity { get; set; }
    }

    [Serializable]
    public class CombatSimulationRequestDTO
    {
        public Guid OriginCityId { get; set; }
        public Guid TargetCityId { get; set; }
        public List<UnitSelectionDTO> AttackerUnits { get; set; } = new();
        public List<UnitSelectionDTO> DefenderUnits { get; set; } = new();
    }

    [Serializable]
    public class CombatSimulationResultDTO
    {
        public List<UnitStackDTO> RemainingAttackers { get; set; } = new();
        public List<UnitStackDTO> RemainingDefenders { get; set; } = new();
        public List<UnitStackDTO> AttackerLosses { get; set; } = new();
        public List<UnitStackDTO> DefenderLosses { get; set; } = new();
        public List<UnitStackDTO> RevivedDefenders { get; set; } = new();
        public double LuckModifier { get; set; }
        public List<string> AppliedModifiers { get; set; } = new();
        public bool RequiresTransport { get; set; }
        public int RequiredTransportCapacity { get; set; }
        public int AvailableTransportCapacity { get; set; }
        public int TransportCapacityMargin { get; set; }
        public bool HasSufficientTransportCapacity { get; set; }
    }

    [Serializable]
    public class IncomingAttackDTO
    {
        public Guid DeploymentId { get; set; }
        public Guid SenderWorldPlayerId { get; set; }
        public string SenderWorldPlayerName { get; set; }
        public Guid TargetCityId { get; set; }
        public string TargetCityName { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public DateTime ArrivalTime { get; set; }
    }
}
