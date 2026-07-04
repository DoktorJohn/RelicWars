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
        Task<List<UnitDeployment>> GetActiveDeploymentsByWorldPlayerIdAsync(Guid worldPlayerId);
        Task<List<UnitDeployment>> GetIncomingAttacksByTargetOwnerIdAsync(Guid worldPlayerId) =>
            Task.FromResult(new List<UnitDeployment>());
        Task<List<UnitDeployment>> GetStationedSupportByTargetCityIdAsync(Guid targetCityId) =>
            Task.FromResult(new List<UnitDeployment>());
        Task<List<UnitDeployment>> GetStationedSupportAsync(int batchSize) =>
            Task.FromResult(new List<UnitDeployment>());
        Task AddAsync(UnitDeployment deployment);
        Task<List<UnitDeployment>> GetDueMovementsAsync(DateTime now, int batchSize);
        Task UpdateAsync(UnitDeployment deployment);
        Task DeleteAsync(UnitDeployment deployment);
        Task<UnitDeployment?> GetByIdAsync(Guid id);
    }
}
