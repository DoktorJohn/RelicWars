using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorldPlayerService : IWorldPlayerService
    {
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly IPlayerProfileRepository _profileRepository;
        private readonly ILogger<WorldPlayerService> _logger;

        public WorldPlayerService(
            IWorldPlayerRepository worldPlayerRepository,
            IPlayerProfileRepository profileRepository,
            ILogger<WorldPlayerService> logger)
        {
            _worldPlayerRepository = worldPlayerRepository;
            _profileRepository = profileRepository;
            _logger = logger;
        }

        public async Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid profileId, Guid worldId)
        {
            // 1. Efficiency: Check if relationship exists WITHOUT loading the Profile or Cities.
            // This is a specialized, lightweight query.
            var existingWorldPlayer = await _worldPlayerRepository.GetByProfileAndWorldAsync(profileId, worldId);

            if (existingWorldPlayer != null)
            {
                // Note: GetByProfileAndWorldAsync should Include(Cities) solely for this ID retrieval to keep it performant.
                var cityId = existingWorldPlayer.Cities.FirstOrDefault()?.Id;
                return new WorldPlayerJoinResponse(true, "Welcome back. Loading existing city data.", cityId);
            }

            // 2. Fetch only necessary Profile data (UserName) for naming the city.
            // Do NOT include WorldPlayers/Cities here.
            var profileName = await _profileRepository.GetUserNameByIdAsync(profileId);

            if (string.IsNullOrEmpty(profileName))
            {
                return new WorldPlayerJoinResponse(false, "Player profile could not be identified.", null);
            }

            // 3. Create new entities
            var newWorldPlayer = new WorldPlayer
            {
                PlayerProfileId = profileId,
                WorldId = worldId,
                Silver = 1000,
                Cities = new List<City>()
            };

            var startCity = CreateStartingCity(profileName, newWorldPlayer.Id);

            newWorldPlayer.Cities.Add(startCity);

            try
            {
                await _worldPlayerRepository.AddAsync(newWorldPlayer);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create WorldPlayer for Profile {ProfileId} on World {WorldId}", profileId, worldId);
                return new WorldPlayerJoinResponse(false, "An error occurred while joining the world.", null);
            }

            return new WorldPlayerJoinResponse(true, "New character created and capital assigned.", startCity.Id);
        }

        private City CreateStartingCity(string userName, Guid worldPlayerId)
        {
            var city = new City
            {
                Name = $"{userName}'s Capital",
                // WorldPlayerId = worldPlayerId, // Not needed if we add via navigation property
                Wood = 500,
                Stone = 500,
                Metal = 500,
                LastResourceUpdate = DateTime.UtcNow,
                Buildings = new List<Building>()
            };

            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Senate, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Warehouse, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Housing, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.TimberCamp, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.StoneQuarry, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.MetalMine, Level = 1 });

            return city;
        }
    }
}
