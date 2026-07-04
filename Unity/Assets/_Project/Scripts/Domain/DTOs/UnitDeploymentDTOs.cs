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
