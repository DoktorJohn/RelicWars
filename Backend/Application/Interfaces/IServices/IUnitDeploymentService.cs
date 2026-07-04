using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IUnitDeploymentService
    {
        Task<OwnedUnitDeploymentDTO> AttackCityDeploymentAsync(AttackCityDeploymentRequestDTO dto);
        Task<OwnedUnitDeploymentDTO> SupportCityDeploymentAsync(SupportCityDeploymentRequestDTO dto);
        Task<OwnedUnitDeploymentDTO> RecallAsync(Guid deploymentId);
        Task<DeploymentTravelEstimateDTO> EstimateTravelAsync(DeploymentTravelEstimateRequestDTO dto) =>
            Task.FromResult(new DeploymentTravelEstimateDTO(0, DateTime.UtcNow));
        Task<List<OwnedUnitDeploymentDTO>> GetDeploymentsAsync(Guid worldPlayerId);
        Task<List<IncomingAttackDTO>> GetIncomingAttacksAsync(Guid worldPlayerId) =>
            Task.FromResult(new List<IncomingAttackDTO>());
    }
}
