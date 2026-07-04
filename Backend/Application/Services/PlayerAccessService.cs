using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.User;
using Domain.Workers;

namespace Application.Services
{
    public class PlayerAccessService : IPlayerAccessService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IUnitDeploymentRepository _unitDeploymentRepository;

        public PlayerAccessService(
            ICurrentUserService currentUserService,
            IWorldPlayerRepository worldPlayerRepository,
            ICityRepository cityRepository,
            IUnitDeploymentRepository unitDeploymentRepository)
        {
            _currentUserService = currentUserService;
            _worldPlayerRepository = worldPlayerRepository;
            _cityRepository = cityRepository;
            _unitDeploymentRepository = unitDeploymentRepository;
        }

        public Guid GetAuthenticatedProfileId() => _currentUserService.GetProfileId();

        public async Task<WorldPlayer> RequireOwnedWorldPlayerAsync(Guid worldPlayerId)
        {
            var profileId = GetAuthenticatedProfileId();
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(worldPlayerId);

            if (worldPlayer == null)
            {
                throw new KeyNotFoundException($"WorldPlayer med ID {worldPlayerId} blev ikke fundet.");
            }

            if (worldPlayer.PlayerProfileId != profileId)
            {
                throw new UnauthorizedAccessException("WorldPlayer tilhører ikke den autentificerede profil.");
            }

            return worldPlayer;
        }

        public async Task<WorldPlayer> RequireWorldMembershipAsync(Guid worldId)
        {
            var profileId = GetAuthenticatedProfileId();
            var worldPlayer = await _worldPlayerRepository.GetByProfileAndWorldAsync(profileId, worldId);

            if (worldPlayer == null)
            {
                throw new UnauthorizedAccessException("Den autentificerede profil er ikke medlem af denne verden.");
            }

            return worldPlayer;
        }

        public async Task<City> RequireOwnedCityAsync(Guid cityId)
        {
            var profileId = GetAuthenticatedProfileId();
            var city = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityId);

            if (city == null)
            {
                throw new KeyNotFoundException($"By med ID {cityId} blev ikke fundet.");
            }

            if (city.WorldPlayer?.PlayerProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Byen tilhører ikke den autentificerede profil.");
            }

            return city;
        }

        public async Task<City> RequireOwnedCityForTownHallAsync(Guid cityId)
        {
            var profileId = GetAuthenticatedProfileId();
            var city = await _cityRepository.GetTownHallCityByCityIdentifierAsync(cityId);

            if (city == null)
            {
                throw new KeyNotFoundException($"By med ID {cityId} blev ikke fundet.");
            }

            if (city.WorldPlayer?.PlayerProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Byen tilhører ikke den autentificerede profil.");
            }

            return city;
        }

        public async Task<UnitDeployment> RequireOwnedUnitDeploymentAsync(Guid unitDeploymentId)
        {
            var profileId = GetAuthenticatedProfileId();
            var deployment = await _unitDeploymentRepository.GetByIdAsync(unitDeploymentId);

            if (deployment == null)
            {
                throw new KeyNotFoundException($"UnitDeployment med ID {unitDeploymentId} blev ikke fundet.");
            }

            if (deployment.OwnerWorldPlayer?.PlayerProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Hæren tilhører ikke den autentificerede profil.");
            }

            return deployment;
        }
    }
}
