using Domain.Entities;
using Domain.User;
using Domain.Workers;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IPlayerAccessService
    {
        Guid GetAuthenticatedProfileId();
        Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId);
        Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId);
        Task<City> RequireOwnedCityAsync(Guid cityId);
        Task<City> RequireOwnedCityForTownHallAsync(Guid cityId);
        Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId);
    }
}
