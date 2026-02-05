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
    public class DeployUnitRequestDTO
    {
        public Guid OriginCityId { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public List<UnitSelectionDTO> UnitsToDeploy { get; set; }
        public Guid WorldPlayerId { get; set; }
    }

    [Serializable]
    public class MoveUnitRequestDTO
    {
        public Guid UnitDeploymentId { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
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
        public DateTime? ArrivalTime { get; set; }
        public DateTime NextStepTime { get; set; }
        public DateTime LastStepTime { get; set; }
        public int CurrentX { get; set; }
        public int CurrentY { get; set; }
        public int NextX { get; set; }
        public int NextY { get; set; }
        public int FinalX { get; set; }
        public int FinalY { get; set; }
        public int Mobility { get; set; }
        public string RemainingPathJson { get; set; }
        public List<UnitStackDTO> UnitStacks { get; set; } = new();
        public string WorldPlayerUserName { get; set; }
    }
}
