using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
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
        private readonly IRankingService _rankingService;
        private readonly IResourceService _resourceService;
        private readonly IWorldRepository _worldRepo;
        private readonly ILogger<WorldPlayerService> _logger;

        public WorldPlayerService(
            IWorldPlayerRepository worldPlayerRepository,
            IPlayerProfileRepository profileRepository,
            IRankingService rankingService,
            IResourceService resourceService,
            IWorldRepository worldRepo,
            ILogger<WorldPlayerService> logger)
        {
            _worldPlayerRepository = worldPlayerRepository;
            _profileRepository = profileRepository;
            _rankingService = rankingService;
            _resourceService = resourceService;
            _worldRepo = worldRepo;
            _logger = logger;
        }

        public void UpdateGlobalResourceState(WorldPlayer player, DateTime currentDateTime)
        {
            var globalSnapshot = _resourceService.CalculateGlobalResources(player, currentDateTime);

            player.Silver = globalSnapshot.SilverAmount;
            player.ResearchPoints = globalSnapshot.ResearchPoints;
            player.IdeologyFocusPoints = globalSnapshot.IdeologyFocusPoints;
            player.LastResourceUpdate = currentDateTime;

            _logger.LogInformation("[WorldPlayerService] Global economy state synchronized for Player: {PlayerId}", player.Id);
        }

        public async Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId)
        {
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(worldPlayerId);
            if (worldPlayer == null)
            {
                throw new KeyNotFoundException($"WorldPlayer med ID {worldPlayerId} blev ikke fundet.");
            }

            int rank = 0;
            int totalPoints = worldPlayer.Cities.Sum(c => c.Points);
            int cityCount = worldPlayer.Cities.Count;

            var rankingData = await _rankingService.GetRankingById(worldPlayerId);
            if (rankingData != null)
            {
                rank = rankingData.Rank;
                totalPoints = rankingData.TotalPoints;
                cityCount = rankingData.CityCount;
            }

            return new WorldPlayerProfileDTO(
                worldPlayerId,
                worldPlayer.PlayerProfile?.UserName ?? "Unknown",
                totalPoints,
                rank,
                cityCount,
                worldPlayer.Alliance?.Name ?? "Ingen Alliance",
                worldPlayer.Ideology,
                worldPlayer.Alliance?.Id ?? Guid.Empty,
                worldPlayer.WorldId
            );
        }

        public async Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid playerProfileId, Guid targetWorldId)
        {
            var existingGameWorldParticipation = await _worldPlayerRepository.GetByProfileAndWorldAsync(playerProfileId, targetWorldId);

            if (existingGameWorldParticipation != null)
            {
                var primaryCityId = existingGameWorldParticipation.Cities.FirstOrDefault()?.Id;

                return new WorldPlayerJoinResponse(
                    ConnectionSuccessful: true,
                    Message: "Welcome back.",
                    ActiveCityId: primaryCityId,
                    WorldPlayerId: existingGameWorldParticipation.Id,
                    SelectedIdeology: existingGameWorldParticipation.Ideology
                );
            }

            var targetGameWorld = await _worldRepo.GetByIdAsync(targetWorldId);
            if (targetGameWorld == null)
            {
                return new WorldPlayerJoinResponse(
                    ConnectionSuccessful: false,
                    Message: "The requested game world does not exist.",
                    ActiveCityId: null,
                    WorldPlayerId: Guid.Empty,
                    SelectedIdeology: IdeologyTypeEnum.None
                );
            }

            var playerProfileUsername = await _profileRepository.GetUserNameByIdAsync(playerProfileId);
            if (string.IsNullOrEmpty(playerProfileUsername))
            {
                return new WorldPlayerJoinResponse(
                    ConnectionSuccessful: false,
                    Message: "Player profile authentication failed or username not found.",
                    ActiveCityId: null,
                    WorldPlayerId: Guid.Empty,
                    SelectedIdeology: IdeologyTypeEnum.None
                );
            }

            var newlyCreatedWorldParticipation = new WorldPlayer
            {
                Id = Guid.NewGuid(),
                PlayerProfileId = playerProfileId,
                WorldId = targetWorldId,
                Silver = 1000,
                Ideology = IdeologyTypeEnum.None,
                LastResourceUpdate = DateTime.UtcNow,
                Cities = new List<City>()
            };

            targetGameWorld.PlayerCount++;

            var initialPlayerCapitalCity = CreateStartingCity(playerProfileUsername, newlyCreatedWorldParticipation.Id);
            newlyCreatedWorldParticipation.Cities.Add(initialPlayerCapitalCity);

            await _worldPlayerRepository.AddAsync(newlyCreatedWorldParticipation);


            return new WorldPlayerJoinResponse(
                ConnectionSuccessful: true,
                Message: "New character successfully created in world.",
                ActiveCityId: initialPlayerCapitalCity.Id,
                WorldPlayerId: newlyCreatedWorldParticipation.Id,
                SelectedIdeology: newlyCreatedWorldParticipation.Ideology
            );
        }


        public async Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request)
        {
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(request.WorldPlayerId);

            if (worldPlayer == null)
                return new WorldPlayerSelectIdeologyResponse(false, "WorldPlayer not found.");

            if (worldPlayer.Ideology != IdeologyTypeEnum.None)
                return new WorldPlayerSelectIdeologyResponse(false, "Ideology already selected.");

            worldPlayer.Ideology = request.Ideology;

            await _worldPlayerRepository.UpdateAsync(worldPlayer);

            _logger.LogInformation("Player {Id} selected ideology: {Ideology}", worldPlayer.Id, request.Ideology);

            return new WorldPlayerSelectIdeologyResponse(true, $"Ideology {request.Ideology} confirmed.");
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
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Workshop, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.University, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Barracks, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Wall, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Stable, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.MarketPlace, Level = 1 });

            return city;
        }
    }
}