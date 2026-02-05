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
        Task<UnitDeploymentDTO> DeployUnitsAsync(DeployUnitRequestDTO dto);
        Task<UnitDeploymentDTO> MoveUnits(MoveUnitRequestDTO dto);
        Task<UnitDeploymentDTO> AbortMovementAsync(Guid unitDeploymentId);
    }
}
