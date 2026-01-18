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
            // 1. Hent entitets-data (Navn, Alliance) fra databasen
            // Vi har brug for en repo metode der Includer PlayerProfile og Alliance
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(worldPlayerId);

            if (worldPlayer == null)
            {
                throw new KeyNotFoundException($"WorldPlayer med ID {worldPlayerId} blev ikke fundet.");
            }

            // Hent profilnavnet (hvis det ikke var inkluderet i GetByIdAsync)
            // Ideelt set bør _worldPlayerRepository.GetByIdAsync inkludere PlayerProfile.
            string userName = worldPlayer.PlayerProfile?.UserName
                              ?? await _profileRepository.GetUserNameByIdAsync(worldPlayer.PlayerProfileId);

            // 2. Hent statistik (Point, Rank, CityCount) fra RankingService (Snapshot)
            // Dette er meget hurtigere end at beregne det live.
            int rank = 0;
            int totalPoints = 0;
            int cityCount = 0;

            try
            {
                // Vi forsøger at slå op i det cachede snapshot
                var rankingData = await _rankingService.GetRankingById(worldPlayerId);
                rank = rankingData.Rank;
                totalPoints = rankingData.TotalPoints;
                cityCount = rankingData.CityCount;
            }
            catch (KeyNotFoundException)
            {
                // Hvis spilleren er helt ny og ikke kommet med i snapshot endnu (går op til 1 min),
                // så defaulter vi bare til 0 eller henter data manuelt.
                // Her defaulter vi til 0/1 for at holde det simpelt og hurtigt.
                cityCount = 1;
                _logger.LogWarning($"Spiller {worldPlayerId} fandtes ikke i Ranking Snapshot (sandsynligvis nyoprettet).");
            }

            // 3. Map til DTO
            return new WorldPlayerProfileDTO(
                worldPlayerId,
                userName ?? "Unknown",
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