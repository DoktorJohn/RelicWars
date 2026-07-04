using Domain.Entities;
using Domain.User;

namespace Application.Interfaces.IServices
{
    public interface IDeploymentPermissionService
    {
        bool CanAttack(WorldPlayer sourcePlayer, City targetCity);
        Task<bool> CanSupportAsync(WorldPlayer sourcePlayer, City targetCity);
    }
}
