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
        Task<UnitDeploymentDTO> DeployUnitDeploymentAsync(DeployUnitRequestDTO dto);
        Task<UnitDeploymentDTO> MoveUnitDeployment(MoveUnitRequestDTO dto);
        Task<UnitDeploymentDTO> HaltUnitDeploymentAsync(Guid unitDeploymentId);
        Task<UnitDeploymentDTO> ReturnToOriginCityAsync(Guid unitDeploymentId);
    }
}
