using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IUnitDeploymentRepository
    {
        Task<List<UnitDeployment>> GetUnitDeploymentsWithStacksByListOfIdsAsync(List<Guid> ids);
        Task AddAsync(UnitDeployment deployment);
        Task<List<UnitDeployment>> GetActiveDeploymentsAsync();
        Task UpdateAsync(UnitDeployment deployment);
        Task DeleteAsync(UnitDeployment deployment);
        Task<UnitDeployment?> GetByIdAsync(Guid id);
    }
}
