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
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorldPlayerService : IWorldPlayerService
    {
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly IPlayerProfileRepository _profileRepository;
        private readonly IRankingService _rankingService; // NY: Injekteret til stats
        private readonly ILogger<WorldPlayerService> _logger;

        public WorldPlayerService(
            IWorldPlayerRepository worldPlayerRepository,
            IPlayerProfileRepository profileRepository,
            IRankingService rankingService, // NY parameter
            ILogger<WorldPlayerService> logger)
        {
            _worldPlayerRepository = worldPlayerRepository;
            _profileRepository = profileRepository;
            _rankingService = rankingService;
            _logger = logger;
        }

        public async Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId)
        {
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(worldPlayerId);

            if (worldPlayer == null)
            {
                throw new KeyNotFoundException($"WorldPlayer med ID {worldPlayerId} blev ikke fundet.");
            }

            string userName = worldPlayer.PlayerProfile?.UserName ?? "Unknown";

            // 2. Hent statistik fra RankingService (Snapshot)
            int rank = 0;
            int totalPoints = worldPlayer.Cities.Sum(c => c.Points); // Default til live point fra DB
            int cityCount = worldPlayer.Cities.Count; // Default til live antal fra DB

            // Forsøg at få fat i snapshot-data for at få den rigtige Rank
            var rankingData = await _rankingService.GetRankingById(worldPlayerId);

            if (rankingData != null)
            {
                rank = rankingData.Rank;
                totalPoints = rankingData.TotalPoints;
                cityCount = rankingData.CityCount;
            }
            else
            {
                _logger.LogInformation("Spiller {PlayerId} er ikke i ranglisten endnu. Bruger live-data fra databasen.", worldPlayerId);
                // rank forbliver 0 (unranked)
            }

            // 3. Map til DTO
            return new WorldPlayerProfileDTO(
                worldPlayerId,
                userName,
                totalPoints,
                rank,
                cityCount,
                worldPlayer.Alliance?.Name ?? "Ingen Alliance",
                worldPlayer.Alliance?.Id ?? Guid.Empty
            );
        }

        // ... Din eksisterende AssignPlayerToGameWorldAsync metode forbliver uændret ...
        public async Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid profileId, Guid worldId)
        {
            // (Koden er skjult for overblikkets skyld, men er den samme som du postede)
            // ...
            var existingWorldPlayer = await _worldPlayerRepository.GetByProfileAndWorldAsync(profileId, worldId);

            if (existingWorldPlayer != null)
            {
                var cityId = existingWorldPlayer.Cities.FirstOrDefault()?.Id;
                return new WorldPlayerJoinResponse(true, "Welcome back. Loading existing city data.", cityId, existingWorldPlayer.Id);
            }

            var profileName = await _profileRepository.GetUserNameByIdAsync(profileId);

            if (string.IsNullOrEmpty(profileName))
            {
                return new WorldPlayerJoinResponse(false, "Player profile could not be identified.", null, null);
            }

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
                return new WorldPlayerJoinResponse(false, "An error occurred while joining the world.", null, null);
            }

            return new WorldPlayerJoinResponse(true, "New character created and capital assigned.", startCity.Id, newWorldPlayer.Id);
        }

        private City CreateStartingCity(string userName, Guid worldPlayerId)
        {
            var city = new City
            {
                Name = $"{userName}'s Capital",
                Wood = 500,
                Stone = 500,
                Metal = 500,
                LastResourceUpdate = DateTime.UtcNow,
                Buildings = new List<Building>()
            };

            city.Buildings.Add(new Building { Type = BuildingTypeEnum.TownHall, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Warehouse, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Housing, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.TimberCamp, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.StoneQuarry, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.MetalMine, Level = 1 });

            return city;
        }
    }
}