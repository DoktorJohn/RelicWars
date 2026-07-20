using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using Domain.User;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorldPlayerService : IWorldPlayerService
    {
        private readonly IWorldMapObjectRepository _worldMapObjectRepository;
        private readonly IWorldMapObjectService _worldMapObjectService;
        private readonly ICityRepository _cityRepository;
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly IPlayerProfileRepository _profileRepository;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly IRankingService _rankingService;
        private readonly IResourceService _resourceService;
        private readonly IWorldRepository _worldRepo;
        private readonly CityPointCalculator _cityPointCalculator;
        private readonly ILogger<WorldPlayerService> _logger;
        private readonly IDailyObjectiveService? _dailyObjectiveService;
        private readonly ITransactionManager _transactionManager;

        public WorldPlayerService(
            IWorldPlayerRepository worldPlayerRepository,
            IPlayerProfileRepository profileRepository,
            ICityRepository cityRepository,
            IRankingService rankingService,
            IResourceService resourceService,
            IWorldRepository worldRepo,
            IWorldMapObjectRepository worldMapObjectRepository,
            IWorldMapObjectService worldMapObjectService,
            IPlayerAccessService playerAccessService,
            ILogger<WorldPlayerService> logger,
            CityPointCalculator cityPointCalculator,
            ITransactionManager transactionManager,
            IDailyObjectiveService? dailyObjectiveService = null)
        {
            _worldPlayerRepository = worldPlayerRepository;
            _profileRepository = profileRepository;
            _cityRepository = cityRepository;
            _rankingService = rankingService;
            _resourceService = resourceService;
            _worldRepo = worldRepo;
            _logger = logger;
            _worldMapObjectRepository = worldMapObjectRepository;
            _worldMapObjectService = worldMapObjectService;
            _playerAccessService = playerAccessService;
            _cityPointCalculator = cityPointCalculator;
            _transactionManager = transactionManager;
            _dailyObjectiveService = dailyObjectiveService;
        }

        public void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime)
        {
            _logger.LogInformation("[WorldPlayerService] Updating Global Resource State for Player {PlayerId}. Old Coins: {Coins}, Old LastUpdate: {LastUpdate}", player.Id, player.Coins, player.LastResourceUpdate);
            var globalSnapshot = _resourceService.CalculateGlobalResources(player, currentDateTime);

            player.Coins = globalSnapshot.CoinsAmount;
            player.ResearchPoints = globalSnapshot.ResearchPoints;
            player.IdeologyFocusPoints = globalSnapshot.IdeologyFocusPoints;
            player.LastResourceUpdate = currentDateTime;

            _logger.LogInformation("[WorldPlayerService] Global economy state synchronized for Player: {PlayerId}. New Coins: {Coins}, Rate: {Rate}", player.Id, player.Coins, globalSnapshot.CoinsProductionPerHour);
        }

        public async Task SyncGlobalResourcesAsync(WorldPlayer player, DateTime currentDateTime)
        {
            DateTime intervalStart = player.LastResourceUpdate;
            var snapshot = _resourceService.CalculateGlobalResources(player, currentDateTime);
            if (_dailyObjectiveService != null)
                await _dailyObjectiveService.ApplyProductionAsync(
                    player.Id,
                    intervalStart,
                    currentDateTime,
                    Math.Max(0, snapshot.CoinsProductionPerHour));
            SyncGlobalResources(player, currentDateTime);
        }

        public async Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId)
        {
            _logger.LogInformation("[WorldPlayerService] GetWorldPlayerEconomyAsync called for Player {PlayerId}", worldPlayerId);
            var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);

            var currentDateTime = DateTime.UtcNow;
            await SyncGlobalResourcesAsync(player, currentDateTime);
            await _worldPlayerRepository.UpdateAsync(player); // Persist the updated resources

            var globalSnapshot = _resourceService.CalculateGlobalResources(player, currentDateTime);
            
            _logger.LogInformation("[WorldPlayerService] Returning economy DTO for {PlayerId}. Coins: {Coins}, Rate: {Rate}", player.Id, player.Coins, globalSnapshot.CoinsProductionPerHour);

            // Fetch cities efficiently for the dropdown
            var cities = await _cityRepository.GetCitiesByWorldPlayerIdAsync(player.Id);
            var cityDtos = cities.Select(c => new CityDTO(c.Id, c.Name, c.X, c.Y, 0, c.IsNPC)).OrderBy(c => c.CityName).ToList();
            var cityResourceSnapshots = player.Cities
                .Select(city => _resourceService.CalculateCityResources(city, currentDateTime))
                .ToList();

            return new WorldPlayerEconomyDTO
            {
                WorldPlayerId = player.Id,
                CurrentCoinsAmount = Math.Floor(player.Coins),
                CurrentResearchPoints = Math.Floor(player.ResearchPoints),
                CurrentIdeologyFocusPoints = Math.Floor(player.IdeologyFocusPoints),
                CoinsProductionPerHour = globalSnapshot.CoinsProductionPerHour,
                ResearchPointsPerHour = globalSnapshot.ResearchPointsPerHour,
                IdeologyFocusPointsPerHour = globalSnapshot.IdeologyFocusPointsPerHour,
                TotalWoodAmount = cityResourceSnapshots.Sum(snapshot => snapshot.Wood),
                TotalStoneAmount = cityResourceSnapshots.Sum(snapshot => snapshot.Stone),
                TotalMetalAmount = cityResourceSnapshots.Sum(snapshot => snapshot.Metal),
                TotalPopulationAmount = globalSnapshot.TotalAvailablePopulation,
                PlayerCities = cityDtos,
                LastUpdated = currentDateTime
            };
        }

        public async Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId)
        {
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(worldPlayerId)
                ?? throw new KeyNotFoundException("World player not found.");

            await _playerAccessService.RequireWorldMembershipAsync(worldPlayer.WorldId);

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
                worldPlayer.PlayerProfile?.Description ?? string.Empty,
                worldPlayer.Alliance?.Name ?? "Ingen Alliance",
                worldPlayer.Ideology,
                worldPlayer.Alliance?.Id ?? Guid.Empty,
                worldPlayer.WorldId,
                (worldPlayer.Cities ?? new List<City>())
                    .OrderByDescending(city => city.Points)
                    .ThenBy(city => city.Name)
                    .Select(city => new CityDTO(city.Id, city.Name, city.X, city.Y, city.Points, city.IsNPC))
                    .ToList()
            );
        }

        public async Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description)
        {
            var worldPlayer = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            var sanitizedDescription = SanitizeDescription(description);

            if (worldPlayer.PlayerProfile == null)
            {
                throw new KeyNotFoundException("Player profile not found.");
            }

            worldPlayer.PlayerProfile.Description = sanitizedDescription;
            worldPlayer.PlayerProfile.ModifiedAt = DateTime.UtcNow;
            await _profileRepository.UpdateAsync(worldPlayer.PlayerProfile);

            return await GetWorldPlayerProfileAsync(worldPlayerId);
        }

        public async Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query)
        {
            await _playerAccessService.RequireWorldMembershipAsync(worldId);
            if (string.IsNullOrWhiteSpace(query)) return new List<PlayerSearchResultDTO>();

            var players = await _worldPlayerRepository.SearchPlayersByUsernameAsync(worldId, query);
            return players.Select(p => new PlayerSearchResultDTO
            {
                WorldPlayerId = p.Id,
                Username = p.PlayerProfile?.UserName ?? "Unknown"
            }).ToList();
        }

        public async Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid targetWorldId)
        {
            var playerProfileId = _playerAccessService.GetAuthenticatedProfileId();
            var existingGameWorldParticipation = await _worldPlayerRepository.GetByProfileAndWorldAsync(playerProfileId, targetWorldId);

            if (existingGameWorldParticipation != null)
            {
                return CreateWelcomeBackResponse(existingGameWorldParticipation);
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

            try
            {
                return await _transactionManager.ExecuteAsync(async () =>
                {
                    var participationInsideTransaction = await _worldPlayerRepository
                        .GetByProfileAndWorldAsync(playerProfileId, targetWorldId);
                    if (participationInsideTransaction != null)
                    {
                        return CreateWelcomeBackResponse(participationInsideTransaction);
                    }

                    var spawnCoordinates = await CalculateNextCoastalSpawnCoordinatesAsync(targetGameWorld);
                    var newlyCreatedWorldParticipation = new WorldPlayer
                    {
                        Id = Guid.NewGuid(),
                        PlayerProfileId = playerProfileId,
                        WorldId = targetWorldId,
                        Coins = 1000,
                        ResearchPoints = 10,
                        IdeologyFocusPoints = 100,
                        Ideology = IdeologyTypeEnum.None,
                        LastResourceUpdate = DateTime.UtcNow,
                        Cities = new List<City>()
                    };

                    var initialPlayerCapitalCity = CreateStartingCity(
                        playerProfileUsername,
                        newlyCreatedWorldParticipation.Id,
                        targetWorldId,
                        spawnCoordinates.X,
                        spawnCoordinates.Y);

                    newlyCreatedWorldParticipation.Cities.Add(initialPlayerCapitalCity);
                    await _worldPlayerRepository.AddAsync(newlyCreatedWorldParticipation);
                    await _worldMapObjectService.AddEntityToWorldMapAsync(initialPlayerCapitalCity);

                    _logger.LogInformation(
                        "Player {Username} spawned at coordinates {X},{Y}",
                        playerProfileUsername,
                        spawnCoordinates.X,
                        spawnCoordinates.Y);

                    return new WorldPlayerJoinResponse(
                        ConnectionSuccessful: true,
                        Message: "New character successfully created in world.",
                        ActiveCityId: initialPlayerCapitalCity.Id,
                        WorldPlayerId: newlyCreatedWorldParticipation.Id,
                        SelectedIdeology: newlyCreatedWorldParticipation.Ideology);
                });
            }
            catch (DbUpdateException exception)
            {
                var winningParticipation = await _worldPlayerRepository
                    .GetByProfileAndWorldAsync(playerProfileId, targetWorldId);
                if (winningParticipation == null)
                {
                    throw;
                }

                _logger.LogWarning(
                    exception,
                    "Concurrent world join lost uniqueness race for profile {ProfileId} and world {WorldId}; reusing participation {WorldPlayerId}.",
                    playerProfileId,
                    targetWorldId,
                    winningParticipation.Id);
                return CreateWelcomeBackResponse(winningParticipation);
            }
        }

        private static WorldPlayerJoinResponse CreateWelcomeBackResponse(WorldPlayer participation)
        {
            var primaryCityId = participation.Cities
                .OrderBy(city => city.Id)
                .Select(city => (Guid?)city.Id)
                .FirstOrDefault();

            return new WorldPlayerJoinResponse(
                ConnectionSuccessful: true,
                Message: "Welcome back.",
                ActiveCityId: primaryCityId,
                WorldPlayerId: participation.Id,
                SelectedIdeology: participation.Ideology);
        }


        public async Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request)
        {
            var worldPlayer = await _playerAccessService.RequireOwnedWorldPlayerAsync(request.WorldPlayerId);

            if (worldPlayer.Ideology != IdeologyTypeEnum.None)
                return new WorldPlayerSelectIdeologyResponse(false, "Ideology already selected.");

            worldPlayer.Ideology = request.Ideology;

            await _worldPlayerRepository.UpdateAsync(worldPlayer);

            _logger.LogInformation("Player {Id} selected ideology: {Ideology}", worldPlayer.Id, request.Ideology);

            return new WorldPlayerSelectIdeologyResponse(true, $"Ideology {request.Ideology} confirmed.");
        }

        private async Task<(int X, int Y)> CalculateNextCoastalSpawnCoordinatesAsync(World world)
        {
            const int islandCellSize = WorldGenerationService.IslandCellSize;

            var existingCityMapObjects = await _worldMapObjectRepository.GetObjectsByTypeAsync(world.Id, MapObjectTypeEnum.City);
            var occupiedSitesByIsland = existingCityMapObjects
                .Select(city => WorldGenerationService.TryGetIslandCoordinates(city.X, city.Y, world.MapSeed, out int islandX, out int islandY)
                    ? new { City = city, Island = (islandX, islandY) }
                    : null)
                .Where(entry => entry != null)
                .GroupBy(entry => entry!.Island)
                .ToDictionary(group => group.Key, group => group.Select(entry => entry!.City).ToList());
            var existingCityIds = existingCityMapObjects
                .Where(mapObject => mapObject.ReferenceEntityId.HasValue)
                .Select(mapObject => mapObject.ReferenceEntityId!.Value)
                .ToList();
            var playerCities = (await _cityRepository.GetCitiesByListOfIdsAsync(existingCityIds))
                .Where(city => !city.IsNPC && city.WorldPlayerId.HasValue)
                .ToList();
            var playerCityCountsByIsland = playerCities
                .Select(city => WorldGenerationService.TryGetIslandCoordinates(
                    city.X,
                    city.Y,
                    world.MapSeed,
                    out int islandX,
                    out int islandY)
                        ? new { Island = (islandX, islandY) }
                        : null)
                .Where(entry => entry != null)
                .GroupBy(entry => entry!.Island)
                .ToDictionary(group => group.Key, group => group.Count());

            int minimumX = -world.Width / 2;
            int maximumX = minimumX + world.Width - 1;
            int minimumY = -world.Height / 2;
            int maximumY = minimumY + world.Height - 1;
            int minimumCellX = minimumX / islandCellSize - 1;
            int maximumCellX = maximumX / islandCellSize + 1;
            int minimumCellY = minimumY / WorldGenerationService.IslandRowHeight - 1;
            int maximumCellY = maximumY / WorldGenerationService.IslandRowHeight + 1;

            var islandCandidates = playerCityCountsByIsland
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => Math.Abs(pair.Key.Item1 - 1) + Math.Abs(pair.Key.Item2 - 1))
                .ThenBy(pair => pair.Key.Item1)
                .ThenBy(pair => pair.Key.Item2)
                .Select(pair => pair.Key)
                .Concat(
                    from islandX in Enumerable.Range(minimumCellX, maximumCellX - minimumCellX + 1)
                    from islandY in Enumerable.Range(minimumCellY, maximumCellY - minimumCellY + 1)
                    let island = (islandX, islandY)
                    where !playerCityCountsByIsland.ContainsKey(island)
                    where WorldGenerationService.IsIslandCellActive(islandX, islandY, world.MapSeed)
                    orderby Math.Abs(islandX - 1) + Math.Abs(islandY - 1), islandX, islandY
                    select island);

            foreach (var island in islandCandidates)
            {
                occupiedSitesByIsland.TryGetValue(island, out var islandCities);
                islandCities ??= new List<WorldMapObject>();
                var islandDefinition = WorldGenerationService.GetIslandDefinition(island.Item1, island.Item2, world.MapSeed);

                var bestCandidate = PlayerCitySiteGenerator.FindNextSite(
                    islandDefinition,
                    world.MapSeed,
                    minimumX,
                    maximumX,
                    minimumY,
                    maximumY,
                    islandCities.Select(city => ((int)city.X, (int)city.Y)).ToList());

                if (bestCandidate.HasValue)
                    return bestCandidate.Value;
            }

            throw new InvalidOperationException("No coastal city position is available in this world.");
        }

        private static string SanitizeDescription(string description)
        {
            var sanitizedDescription = description?.Trim() ?? string.Empty;
            if (sanitizedDescription.Length > 500)
            {
                throw new ArgumentException("Profile description must be 500 characters or fewer.");
            }

            return sanitizedDescription;
        }

        private static List<CityExoticResource> CreateInitialExoticResources()
        {
            return Enum.GetValues<ExoticResourceTypeEnum>()
                .Select(resourceType => new CityExoticResource
                {
                    Id = Guid.NewGuid(),
                    ResourceType = resourceType,
                    Amount = 0
                })
                .ToList();
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
                LastExoticResourceUpdate = DateTime.UtcNow,
                ExoticResources = CreateInitialExoticResources(),
                Buildings = new List<Building>()
            };

            city.Buildings.Add(new Building { Type = BuildingTypeEnum.TownHall, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Warehouse, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.Housing, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.TimberCamp, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.StoneQuarry, Level = 1 });
            city.Buildings.Add(new Building { Type = BuildingTypeEnum.MetalMine, Level = 1 });

            city.Points = _cityPointCalculator.CalculateTotalPointsForCity(city);

            return city;
        }
    }
}
