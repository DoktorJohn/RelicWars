using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using Domain.User;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorldPlayerService : IWorldPlayerService
    {
        private readonly IWorldMapObjectRepository _worldMapObjectRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly IPlayerProfileRepository _profileRepository;
        private readonly IRankingService _rankingService;
        private readonly IResourceService _resourceService;
        private readonly IWorldRepository _worldRepo;
        private readonly CityPointCalculator _cityPointCalculator;
        private readonly ILogger<WorldPlayerService> _logger;
        private readonly Random _randomGenerator = new Random();

        public WorldPlayerService(
            IWorldPlayerRepository worldPlayerRepository,
            IPlayerProfileRepository profileRepository,
            ICityRepository cityRepository,
            IRankingService rankingService,
            IResourceService resourceService,
            IWorldRepository worldRepo,
            IWorldMapObjectRepository worldMapObjectRepository,
            ILogger<WorldPlayerService> logger,
            CityPointCalculator cityPointCalculator)
        {
            _worldPlayerRepository = worldPlayerRepository;
            _profileRepository = profileRepository;
            _cityRepository = cityRepository;
            _rankingService = rankingService;
            _resourceService = resourceService;
            _worldRepo = worldRepo;
            _logger = logger;
            _worldMapObjectRepository = worldMapObjectRepository;
            _cityPointCalculator = cityPointCalculator;
        }

        public void UpdateGlobalResourceState(WorldPlayer player, DateTime currentDateTime)
        {
            _logger.LogInformation("[WorldPlayerService] Updating Global Resource State for Player {PlayerId}. Old Silver: {Silver}, Old LastUpdate: {LastUpdate}", player.Id, player.Silver, player.LastResourceUpdate);
            var globalSnapshot = _resourceService.CalculateGlobalResources(player, currentDateTime);

            player.Silver = globalSnapshot.SilverAmount;
            player.ResearchPoints = globalSnapshot.ResearchPoints;
            player.IdeologyFocusPoints = globalSnapshot.IdeologyFocusPoints;
            player.LastResourceUpdate = currentDateTime;

            _logger.LogInformation("[WorldPlayerService] Global economy state synchronized for Player: {PlayerId}. New Silver: {Silver}, Rate: {Rate}", player.Id, player.Silver, globalSnapshot.SilverProductionPerHour);
        }

        public async Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId)
        {
            _logger.LogInformation("[WorldPlayerService] GetWorldPlayerEconomyAsync called for Player {PlayerId}", worldPlayerId);
            var player = await _worldPlayerRepository.GetByIdAsync(worldPlayerId);
            if (player == null)
            {
                _logger.LogWarning("[WorldPlayerService] Player {PlayerId} not found", worldPlayerId);
                throw new KeyNotFoundException($"WorldPlayer med ID {worldPlayerId} blev ikke fundet.");
            }

            var currentDateTime = DateTime.UtcNow;
            UpdateGlobalResourceState(player, currentDateTime);
            await _worldPlayerRepository.UpdateAsync(player); // Persist the updated resources

            var globalSnapshot = _resourceService.CalculateGlobalResources(player, currentDateTime);
            
            _logger.LogInformation("[WorldPlayerService] Returning economy DTO for {PlayerId}. Silver: {Silver}, Rate: {Rate}", player.Id, player.Silver, globalSnapshot.SilverProductionPerHour);

            // Fetch cities efficiently for the dropdown
            var cities = await _cityRepository.GetCitiesByWorldPlayerIdAsync(player.Id);
            var cityDtos = cities.Select(c => new CityDTO(c.Id, c.Name, c.X, c.Y, 0)).OrderBy(c => c.CityName).ToList();

            return new WorldPlayerEconomyDTO
            {
                WorldPlayerId = player.Id,
                CurrentSilverAmount = Math.Floor(player.Silver),
                CurrentResearchPoints = Math.Floor(player.ResearchPoints),
                CurrentIdeologyFocusPoints = Math.Floor(player.IdeologyFocusPoints),
                SilverProductionPerHour = globalSnapshot.SilverProductionPerHour,
                ResearchPointsPerHour = globalSnapshot.ResearchPointsPerHour,
                IdeologyFocusPointsPerHour = globalSnapshot.IdeologyFocusPointsPerHour,
                PlayerCities = cityDtos,
                LastUpdated = currentDateTime
            };
        }

        public async Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId)
        {
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(worldPlayerId);
            if (worldPlayer == null)
            {
                throw new KeyNotFoundException($"OwnerWorldPlayer med ID {worldPlayerId} blev ikke fundet.");
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

            // Beregn spawn koordinater baseret på eksisterende byer i verdenen
            var spawnCoordinates = await CalculateNextAlphaTestSpawnCoordinatesAsync(targetWorldId);

            var newlyCreatedWorldParticipation = new WorldPlayer
            {
                Id = Guid.NewGuid(),
                PlayerProfileId = playerProfileId,
                WorldId = targetWorldId,
                Silver = 1000,
                ResearchPoints = 10,
                IdeologyFocusPoints = 100,
                Ideology = IdeologyTypeEnum.None,
                LastResourceUpdate = DateTime.UtcNow,
                Cities = new List<City>()
            };

            targetGameWorld.PlayerCount++;

            var initialPlayerCapitalCity = CreateStartingCity(
                playerProfileUsername,
                newlyCreatedWorldParticipation.Id,
                targetWorldId,
                spawnCoordinates.X,
                spawnCoordinates.Y);

            newlyCreatedWorldParticipation.Cities.Add(initialPlayerCapitalCity);
            await _worldPlayerRepository.AddAsync(newlyCreatedWorldParticipation);

            var worldMapObject = new WorldMapObject
            {
                WorldId = targetWorldId,
                X = (short)initialPlayerCapitalCity.X,
                Y = (short)initialPlayerCapitalCity.Y,
                Type = MapObjectTypeEnum.City,
                ReferenceEntityId = initialPlayerCapitalCity.Id
            };

            await _worldMapObjectRepository.AddAsync(worldMapObject);

            _logger.LogInformation("Player {Username} spawned at coordinates {X},{Y}", playerProfileUsername, spawnCoordinates.X, spawnCoordinates.Y);

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
                return new WorldPlayerSelectIdeologyResponse(false, "OwnerWorldPlayer not found.");

            if (worldPlayer.Ideology != IdeologyTypeEnum.None)
                return new WorldPlayerSelectIdeologyResponse(false, "Ideology already selected.");

            worldPlayer.Ideology = request.Ideology;

            await _worldPlayerRepository.UpdateAsync(worldPlayer);

            _logger.LogInformation("Player {Id} selected ideology: {Ideology}", worldPlayer.Id, request.Ideology);

            return new WorldPlayerSelectIdeologyResponse(true, $"Ideology {request.Ideology} confirmed.");
        }

        private async Task<(int X, int Y)> CalculateNextAlphaTestSpawnCoordinatesAsync(Guid worldId)
        {
            // Hent alle eksisterende byer på kortet for at tjekke distancer
            var existingCities = await _worldMapObjectRepository.GetObjectsByTypeAsync(worldId, MapObjectTypeEnum.City);

            // Regel 1: Første by på serveren er altid 50,50
            if (existingCities == null || !existingCities.Any())
            {
                return (50, 50);
            }

            int attempts = 0;
            while (attempts < 500) // Sikkerheds-loop for at undgå uendelige beregninger
            {
                attempts++;

                // Regel 2: Vælg en tilfældig eksisterende by som udgangspunkt for det nye spawn
                int randomIndex = _randomGenerator.Next(0, existingCities.Count());
                var anchorCity = existingCities.ElementAt(randomIndex);

                // Regel 3: Generer offset inden for 3-6 (Next er eksklusiv øvre grænse, så 3, 7)
                int offsetX = _randomGenerator.Next(3, 7);
                int offsetY = _randomGenerator.Next(3, 7);

                // Tilfældig retning (positiv eller negativ)
                if (_randomGenerator.Next(0, 2) == 0) offsetX *= -1;
                if (_randomGenerator.Next(0, 2) == 0) offsetY *= -1;

                int candidateX = anchorCity.X + offsetX;
                int candidateY = anchorCity.Y + offsetY;

                // Regel 4: Valider mod ALLE byer. Må aldrig være inden for 2 x og 2 y.
                // Det betyder at afstanden i både X og Y skal være mindst 3 til samtlige byer.
                bool isPositionValid = true;
                foreach (var otherCity in existingCities)
                {
                    int distanceX = Math.Abs(candidateX - otherCity.X);
                    int distanceY = Math.Abs(candidateY - otherCity.Y);

                    // Hvis vi er inden for 2 felter på begge akser, er det en kollision med sikkerhedszonen
                    if (distanceX <= 2 && distanceY <= 2)
                    {
                        isPositionValid = false;
                        break;
                    }
                }

                if (isPositionValid)
                {
                    return (candidateX, candidateY);
                }
            }

            // Fallback hvis clusteret er totalt proppet (ekstremt usandsynligt i alpha)
            _logger.LogWarning("[WorldPlayerService] Kunne ikke finde valid cluster-plads efter 500 forsøg. Ekspanderer søgning.");
            return (50 + attempts, 50 + attempts);
        }

        private City CreateStartingCity(string userName, Guid worldPlayerId, Guid worldId, int x, int y)
        {
            var city = new City
            {
                Name = $"{userName}'s Capital",
                WorldId = worldId,
                WorldPlayerId = worldPlayerId,
                X = x,
                Y = y,
                Wood = 5000,
                Stone = 5000,
                Metal = 5000,
                LastResourceUpdate = DateTime.UtcNow,
                Buildings = new List<Building>()
            };

            city.Buildings.Add(new Building { Type = BuildingTypeEnum.TownHall, Level = 18 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Warehouse, Level = 18 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Housing, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.TimberCamp, Level = 18 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.StoneQuarry, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.MetalMine, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Workshop, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.University, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Barracks, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Wall, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Stable, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.MarketPlace, Level = 18 });

            city.Points = _cityPointCalculator.CalculateTotalPointsForCity(city);

            return city;
        }
    }
}