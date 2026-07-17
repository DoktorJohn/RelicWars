using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.User;

namespace Application.Services
{
    public class DeploymentPermissionService : IDeploymentPermissionService
    {
        private readonly IAllianceRepository _allianceRepository;

        public DeploymentPermissionService(IAllianceRepository allianceRepository)
        {
            _allianceRepository = allianceRepository;
        }

        public bool CanAttack(WorldPlayer sourcePlayer, City targetCity)
        {
            if (sourcePlayer.WorldId != targetCity.WorldId)
            {
                return false;
            }

            if (targetCity.IsNPC && targetCity.WorldPlayerId == null)
            {
                return true;
            }

            var targetPlayer = targetCity.WorldPlayer;
            if (targetPlayer == null || targetCity.WorldPlayerId == null || targetCity.IsNPC)
            {
                return false;
            }

            if (sourcePlayer.Id == targetPlayer.Id)
            {
                return false;
            }

            return !sourcePlayer.AllianceId.HasValue || sourcePlayer.AllianceId != targetPlayer.AllianceId;
        }

        public async Task<bool> CanSupportAsync(WorldPlayer sourcePlayer, City targetCity)
        {
            if (sourcePlayer.WorldId != targetCity.WorldId)
            {
                return false;
            }

            if (targetCity.IsNPC && targetCity.WorldPlayerId == null)
            {
                return true;
            }

            var targetPlayer = targetCity.WorldPlayer;
            if (targetPlayer == null || targetCity.WorldPlayerId == null || targetCity.IsNPC)
            {
                return false;
            }

            if (sourcePlayer.Id == targetPlayer.Id ||
                !sourcePlayer.AllianceId.HasValue ||
                !targetPlayer.AllianceId.HasValue ||
                sourcePlayer.AllianceId == targetPlayer.AllianceId)
            {
                return true;
            }

            return !await _allianceRepository.AreAtWarAsync(
                sourcePlayer.AllianceId.Value,
                targetPlayer.AllianceId.Value);
        }
    }
}
