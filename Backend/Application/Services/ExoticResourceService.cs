using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.StaticData.Generators;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ExoticResourceService : IExoticResourceService
    {
        private enum CityResourceType
        {
            Wood,
            Stone,
            Metal
        }

        private readonly ICityRepository _cityRepository;
        private readonly IWorldIslandRepository _worldIslandRepository;
        private readonly IWorldRepository _worldRepository;
        private readonly IWorldMapObjectRepository _worldMapObjectRepository;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly IResourceService _resourceService;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly ExoticResourceDataReader _exoticResourceDataReader;
        private readonly ILogger<ExoticResourceService> _logger;
        private readonly ITransactionManager _transactionManager;
        private readonly IDailyObjectiveService? _dailyObjectiveService;

        public ExoticResourceService(
            ICityRepository cityRepository,
            IWorldIslandRepository worldIslandRepository,
            IWorldRepository worldRepository,
            IWorldMapObjectRepository worldMapObjectRepository,
            IPlayerAccessService playerAccessService,
            IResourceService resourceService,
            IWorldPlayerService worldPlayerService,
            ExoticResourceDataReader exoticResourceDataReader,
            ILogger<ExoticResourceService> logger,
            ITransactionManager transactionManager,
            IDailyObjectiveService? dailyObjectiveService = null)
        {
            _cityRepository = cityRepository;
            _worldIslandRepository = worldIslandRepository;
            _worldRepository = worldRepository;
            _worldMapObjectRepository = worldMapObjectRepository;
            _playerAccessService = playerAccessService;
            _resourceService = resourceService;
            _worldPlayerService = worldPlayerService;
            _exoticResourceDataReader = exoticResourceDataReader;
            _logger = logger;
            _transactionManager = transactionManager;
            _dailyObjectiveService = dailyObjectiveService;
        }

        public async Task<List<CityExoticResourceDTO>> SyncCityExoticResourcesAsync(City city, DateTime currentDateTime)
        {
            var island = await GetIslandForCityAsync(city);
            ValidateCityExoticResources(city);

            DateTime intervalStart = city.LastExoticResourceUpdate;
            double totalRate = island.ExoticResources.Sum(resource => CalculateProductionBreakdown(resource).FinalValuePerHour);
            SyncCityExoticResourcesInternal(city, island, currentDateTime);
            if (_dailyObjectiveService != null && city.WorldPlayerId.HasValue)
                await _dailyObjectiveService.ApplyProductionAsync(
                    city.WorldPlayerId.Value,
                    intervalStart,
                    currentDateTime,
                    exoticResourcesPerHour: totalRate);

            return MapCityExoticResources(city);
        }

        public async Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesAsync(Guid islandId)
        {
            var island = await _worldIslandRepository.GetByIdAsync(islandId);
            if (island == null)
            {
                return new List<WorldIslandExoticResourceDTO>();
            }

            return MapIslandExoticResources(island);
        }

        public async Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesForCityAsync(City city)
        {
            var island = await GetIslandForCityAsync(city);
            return MapIslandExoticResources(island);
        }

        public async Task<List<CityExoticResourceProductionDTO>> GetProductionBreakdownsForCityAsync(City city)
        {
            var island = await GetIslandForCityAsync(city);

            return island.ExoticResources
                .OrderBy(resource => resource.SlotIndex)
                .Select(resource => new CityExoticResourceProductionDTO(
                    resource.SlotIndex,
                    resource.ResourceType,
                    CalculateProductionBreakdown(resource)))
                .ToList();
        }

        public async Task<ExoticResourceInvestmentResponseDTO> InvestAsync(Guid cityId, ExoticResourceInvestmentRequestDTO request)
        {
            if (request.SlotIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(request.SlotIndex), "SlotIndex skal være nul eller højere.");

            if (!AreValidInvestmentAmounts(request))
                throw new ArgumentOutOfRangeException(nameof(request), "Investeringsbeløb skal være endelige tal på nul eller højere.");

            if (request.WoodAmount == 0 && request.StoneAmount == 0 && request.MetalAmount == 0 && request.CoinAmount == 0)
                throw new ArgumentException("Der skal investeres mindst én ressource.", nameof(request));

            var city = await _playerAccessService.RequireOwnedCityAsync(cityId);
            var currentDateTime = DateTime.UtcNow;
            var nativeSnapshot = _resourceService.CalculateCityResources(city, currentDateTime);
            city.Wood = nativeSnapshot.Wood;
            city.Stone = nativeSnapshot.Stone;
            city.Metal = nativeSnapshot.Metal;
            city.LastResourceUpdate = currentDateTime;
            if (city.WorldPlayer == null)
                throw new InvalidOperationException("Byens ejer blev ikke fundet.");

            await _worldPlayerService.SyncGlobalResourcesAsync(city.WorldPlayer, currentDateTime);
            var island = await GetIslandForCityAsync(city);
            var islandCities = await GetCitiesOnIslandAsync(island);

            foreach (var islandCity in islandCities)
            {
                ValidateCityExoticResources(islandCity);
                SyncCityExoticResourcesInternal(islandCity, island, currentDateTime);
            }

            ValidateCityExoticResources(city);

            var targetSlot = island.ExoticResources
                .SingleOrDefault(resource => resource.SlotIndex == request.SlotIndex)
                ?? throw new InvalidOperationException($"Slot {request.SlotIndex} blev ikke fundet på øen.");

            if (targetSlot.Tier >= 10)
                throw new InvalidOperationException("Slotten er allerede på tier 10.");

            DeductCityResource(city, CityResourceType.Wood, request.WoodAmount);
            DeductCityResource(city, CityResourceType.Stone, request.StoneAmount);
            DeductCityResource(city, CityResourceType.Metal, request.MetalAmount);
            DeductPlayerResource(city.WorldPlayer, request.CoinAmount);

            targetSlot.WoodInvestment += request.WoodAmount;
            targetSlot.StoneInvestment += request.StoneAmount;
            targetSlot.MetalInvestment += request.MetalAmount;
            targetSlot.CoinInvestment += request.CoinAmount;

            var upgraded = false;
            while (targetSlot.Tier < 10)
            {
                var nextTier = _exoticResourceDataReader.GetTierData(targetSlot.ResourceType, targetSlot.Tier + 1);

                if (!CanAffordTier(targetSlot, nextTier))
                    break;

                targetSlot.WoodInvestment -= nextTier.WoodCost;
                targetSlot.StoneInvestment -= nextTier.StoneCost;
                targetSlot.MetalInvestment -= nextTier.MetalCost;
                targetSlot.CoinInvestment -= nextTier.CoinsCost;
                targetSlot.Tier++;
                upgraded = true;
            }

            await _transactionManager.ExecuteAsync(async () =>
            {
                await _cityRepository.UpdateRangeAsync(islandCities);
                await _worldIslandRepository.UpdateAsync(island);
            });

            if (upgraded)
            {
                _logger.LogInformation(
                    "Exotic resource slot {SlotIndex} on island {IslandId} advanced to tier {Tier}.",
                    targetSlot.SlotIndex,
                    island.Id,
                    targetSlot.Tier);
            }

            return new ExoticResourceInvestmentResponseDTO(
                city.Id,
                island.Id,
                targetSlot.SlotIndex,
                targetSlot.Tier,
                MapIslandExoticResources(island),
                MapCityExoticResources(city));
        }

        private async Task<WorldIsland> GetIslandForCityAsync(City city)
        {
            var worldSeed = await _worldRepository.GetWorldSeedAsync(city.WorldId)
                ?? throw new InvalidOperationException("World seed blev ikke fundet.");

            if (!WorldGenerationService.TryGetIslandCoordinates(city.X, city.Y, worldSeed, out int cellX, out int cellY))
                throw new InvalidOperationException("City ligger ikke på en gyldig island.");

            return await _worldIslandRepository.GetByCellAsync(city.WorldId, cellX, cellY)
                ?? throw new InvalidOperationException("Island blev ikke fundet for city.");
        }

        private async Task<List<City>> GetCitiesOnIslandAsync(WorldIsland island)
        {
            var worldSeed = await _worldRepository.GetWorldSeedAsync(island.WorldId)
                ?? throw new InvalidOperationException("World seed blev ikke fundet.");

            int radius = WorldGenerationService.MaximumIslandRadius + 1;
            int areaSize = radius * 2 + 1;
            var nearbyMapObjects = await _worldMapObjectRepository.GetObjectsInAreaAsync(
                island.WorldId,
                checked((short)(island.CenterX - radius)),
                checked((short)(island.CenterY - radius)),
                checked((byte)areaSize),
                checked((byte)areaSize));

            var cityIds = nearbyMapObjects
                .Where(mapObject => mapObject.Type == MapObjectTypeEnum.City && mapObject.ReferenceEntityId.HasValue)
                .Select(mapObject => mapObject.ReferenceEntityId!.Value)
                .ToList();

            var nearbyCities = await _cityRepository.GetCitiesByListOfIdsAsync(cityIds);

            return nearbyCities
                .Where(city => WorldGenerationService.TryGetIslandCoordinates(
                    city.X, city.Y, worldSeed, out int cellX, out int cellY)
                    && cellX == island.CellX
                    && cellY == island.CellY)
                .ToList();
        }

        private void SyncCityExoticResourcesInternal(City city, WorldIsland island, DateTime currentDateTime)
        {
            ValidateCityExoticResources(city);

            if (city.LastExoticResourceUpdate > currentDateTime)
                city.LastExoticResourceUpdate = currentDateTime;

            var hoursPassed = (currentDateTime - city.LastExoticResourceUpdate).TotalHours;
            if (hoursPassed <= 0)
                return;

            foreach (var cityResource in city.ExoticResources)
            {
                var islandResource = island.ExoticResources.FirstOrDefault(resource => resource.ResourceType == cityResource.ResourceType);
                if (islandResource == null)
                    continue;

                var productionBreakdown = CalculateProductionBreakdown(islandResource);
                cityResource.Amount += productionBreakdown.FinalValuePerHour * hoursPassed;
            }

            city.LastExoticResourceUpdate = currentDateTime;
        }

        private ProductionBreakdownDTO CalculateProductionBreakdown(WorldIslandExoticResource resource)
        {
            var tierData = _exoticResourceDataReader.GetTierData(resource.ResourceType, resource.Tier);

            return new ProductionBreakdownDTO(
                tierData.OutputPerHour,
                0,
                0,
                tierData.OutputPerHour);
        }

        private static void ValidateCityExoticResources(City city)
        {
            var expectedTypes = Enum.GetValues<ExoticResourceTypeEnum>();
            var actualTypes = city.ExoticResources
                .Select(resource => resource.ResourceType)
                .Distinct()
                .ToHashSet();

            if (expectedTypes.All(actualTypes.Contains) && actualTypes.Count == expectedTypes.Length)
                return;

            var missingTypes = expectedTypes
                .Where(type => !actualTypes.Contains(type));

            throw new InvalidOperationException(
                $"City {city.Id} har en ufuldstændig exotic resource-beholdning. " +
                $"Aktuelt antal rækker: {city.ExoticResources.Count}/{expectedTypes.Length}. " +
                $"Manglende typer: {string.Join(", ", missingTypes)}. Kør de ventende databasemigrationer.");
        }

        private static bool AreValidInvestmentAmounts(ExoticResourceInvestmentRequestDTO request)
        {
            return double.IsFinite(request.WoodAmount)
                && double.IsFinite(request.StoneAmount)
                && double.IsFinite(request.MetalAmount)
                && double.IsFinite(request.CoinAmount)
                && request.WoodAmount >= 0
                && request.StoneAmount >= 0
                && request.MetalAmount >= 0
                && request.CoinAmount >= 0;
        }

        private static void DeductCityResource(City city, CityResourceType resourceType, double amount)
        {
            if (amount <= 0)
                return;

            double currentValue = resourceType switch
            {
                CityResourceType.Wood => city.Wood,
                CityResourceType.Stone => city.Stone,
                CityResourceType.Metal => city.Metal,
                _ => throw new ArgumentOutOfRangeException(nameof(resourceType))
            };

            if (currentValue < amount)
                throw new InvalidOperationException($"City har ikke nok {resourceType} til at investere.");

            switch (resourceType)
            {
                case CityResourceType.Wood:
                    city.Wood -= amount;
                    break;
                case CityResourceType.Stone:
                    city.Stone -= amount;
                    break;
                case CityResourceType.Metal:
                    city.Metal -= amount;
                    break;
            }
        }

        private static void DeductPlayerResource(Domain.User.WorldPlayer player, double amount)
        {
            if (amount <= 0)
                return;

            if (player.Coins < amount)
                throw new InvalidOperationException("Player har ikke nok coins til at investere.");

            player.Coins -= amount;
        }

        private static bool CanAffordTier(WorldIslandExoticResource targetSlot, ExoticResourceTierData nextTier)
        {
            return targetSlot.WoodInvestment >= nextTier.WoodCost
                && targetSlot.StoneInvestment >= nextTier.StoneCost
                && targetSlot.MetalInvestment >= nextTier.MetalCost
                && targetSlot.CoinInvestment >= nextTier.CoinsCost;
        }

        private List<WorldIslandExoticResourceDTO> MapIslandExoticResources(WorldIsland island)
        {
            return island.ExoticResources
                .OrderBy(resource => resource.SlotIndex)
                .Select(resource =>
                {
                    var tierData = _exoticResourceDataReader.GetTierData(resource.ResourceType, resource.Tier);
                    var nextTier = resource.Tier < 10 ? _exoticResourceDataReader.GetTierData(resource.ResourceType, resource.Tier + 1) : null;
                    var progressPercent = nextTier == null
                        ? 100
                        : CalculateProgressPercent(resource, nextTier);

                    return new WorldIslandExoticResourceDTO(
                        resource.SlotIndex,
                        resource.ResourceType,
                        resource.Tier,
                        progressPercent,
                        tierData.OutputPerHour,
                        resource.WoodInvestment,
                        resource.StoneInvestment,
                        resource.MetalInvestment,
                        resource.CoinInvestment,
                        nextTier?.WoodCost ?? 0,
                        nextTier?.StoneCost ?? 0,
                        nextTier?.MetalCost ?? 0,
                        nextTier?.CoinsCost ?? 0);
                })
                .ToList();
        }

        private List<CityExoticResourceDTO> MapCityExoticResources(City city)
        {
            ValidateCityExoticResources(city);

            return city.ExoticResources
                .OrderBy(resource => resource.ResourceType)
                .Select(resource => new CityExoticResourceDTO(resource.ResourceType, resource.Amount))
                .ToList();
        }

        private static double CalculateProgressPercent(WorldIslandExoticResource resource, ExoticResourceTierData nextTier)
        {
            var woodProgress = nextTier.WoodCost == 0 ? 1 : resource.WoodInvestment / nextTier.WoodCost;
            var stoneProgress = nextTier.StoneCost == 0 ? 1 : resource.StoneInvestment / nextTier.StoneCost;
            var metalProgress = nextTier.MetalCost == 0 ? 1 : resource.MetalInvestment / nextTier.MetalCost;
            var coinProgress = nextTier.CoinsCost == 0 ? 1 : resource.CoinInvestment / nextTier.CoinsCost;

            return Math.Clamp(Math.Min(Math.Min(woodProgress, stoneProgress), Math.Min(metalProgress, coinProgress)) * 100.0, 0, 100);
        }
    }
}
