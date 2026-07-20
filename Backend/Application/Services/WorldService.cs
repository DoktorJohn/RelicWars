using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorldService : IWorldService
    {
        private readonly IWorldRepository _worldRepository;
        private readonly IWorldMapObjectRepository _worldMapObject;
        private readonly ICityRepository _cityRepository;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly IWorldIslandRepository _worldIslandRepository;
        private readonly IExoticResourceService _exoticResourceService;
        private readonly IDeploymentPermissionService _deploymentPermissionService;
        private readonly CityPointCalculator _cityPointCalculator;
        private readonly ILogger<WorldService> _logger;

        public WorldService(
            IWorldRepository worldRepository,
            IWorldMapObjectRepository worldMapObject,
            ICityRepository cityRepository,
            IPlayerAccessService playerAccessService,
            IWorldIslandRepository worldIslandRepository,
            IExoticResourceService exoticResourceService,
            IDeploymentPermissionService deploymentPermissionService,
            CityPointCalculator cityPointCalculator,
            ILogger<WorldService> logger)
        {
            _worldRepository = worldRepository;
            _worldMapObject = worldMapObject;
            _cityRepository = cityRepository;
            _playerAccessService = playerAccessService;
            _worldIslandRepository = worldIslandRepository;
            _exoticResourceService = exoticResourceService;
            _deploymentPermissionService = deploymentPermissionService;
            _cityPointCalculator = cityPointCalculator;
            _logger = logger;
        }

        public async Task<WorldMapChunkResponseDTO?> GetWorldMapChunk(GetWorldMapChunkDTO dto)
        {
            await _playerAccessService.RequireWorldMembershipAsync(dto.worldId);
            var world = await _worldRepository.GetByIdAsync(dto.worldId);
            if (world == null) return null;

            int chunkMaximumXExclusive = dto.startX + dto.width;
            int chunkMaximumYExclusive = dto.startY + dto.height;
            int worldMinimumX = -world.Width / 2;
            int worldMaximumX = worldMinimumX + world.Width - 1;
            int worldMinimumY = -world.Height / 2;
            int worldMaximumY = worldMinimumY + world.Height - 1;
            int siteSearchRadius = WorldGenerationService.MaximumIslandRadius + 2;
            int expandedMinimumX = Math.Max(worldMinimumX, dto.startX - siteSearchRadius);
            int expandedMaximumXExclusive = Math.Min(worldMaximumX + 1, chunkMaximumXExclusive + siteSearchRadius);
            int expandedMinimumY = Math.Max(worldMinimumY, dto.startY - siteSearchRadius);
            int expandedMaximumYExclusive = Math.Min(worldMaximumY + 1, chunkMaximumYExclusive + siteSearchRadius);
            int expandedWidth = expandedMaximumXExclusive - expandedMinimumX;
            int expandedHeight = expandedMaximumYExclusive - expandedMinimumY;
            int citySearchMinimumX = Math.Max(worldMinimumX, dto.startX - siteSearchRadius * 2);
            int citySearchMaximumXExclusive = Math.Min(worldMaximumX + 1, chunkMaximumXExclusive + siteSearchRadius * 2);
            int citySearchMinimumY = Math.Max(worldMinimumY, dto.startY - siteSearchRadius * 2);
            int citySearchMaximumYExclusive = Math.Min(worldMaximumY + 1, chunkMaximumYExclusive + siteSearchRadius * 2);
            int citySearchWidth = citySearchMaximumXExclusive - citySearchMinimumX;
            int citySearchHeight = citySearchMaximumYExclusive - citySearchMinimumY;

            var relevantIslands = await _worldIslandRepository.GetInAreaAsync(
                dto.worldId,
                expandedMinimumX,
                expandedMinimumY,
                expandedWidth,
                expandedHeight);
            var nearbyMapObjects = await _worldMapObject.GetObjectsInAreaAsync(
                dto.worldId,
                checked((short)citySearchMinimumX),
                checked((short)citySearchMinimumY),
                checked((byte)citySearchWidth),
                checked((byte)citySearchHeight));
            var mapObjectEntities = nearbyMapObjects
                .Where(mapObject => mapObject.X >= dto.startX
                    && mapObject.X < chunkMaximumXExclusive
                    && mapObject.Y >= dto.startY
                    && mapObject.Y < chunkMaximumYExclusive)
                .ToList();
            var cityIdentifiers = mapObjectEntities
                .Where(mapObject => mapObject.Type == MapObjectTypeEnum.City && mapObject.ReferenceEntityId.HasValue)
                .Select(mapObject => mapObject.ReferenceEntityId!.Value)
                .ToList();
            var cityEntities = await _cityRepository.GetCitiesByListOfIdsAsync(cityIdentifiers);
            var normalizedCityEntities = cityEntities
                .GroupBy(city => (city.X, city.Y))
                .Select(group =>
                {
                    var orderedCities = group.OrderBy(city => city.Id).ToList();
                    var selectedCity = orderedCities[0];
                    if (orderedCities.Count > 1)
                    {
                        _logger.LogWarning(
                            "Legacy duplicate cities at world {WorldId} coordinate ({X},{Y}); selected {SelectedCityId}, discarded {DiscardedCityIds}.",
                            dto.worldId,
                            group.Key.X,
                            group.Key.Y,
                            selectedCity.Id,
                            string.Join(",", orderedCities.Skip(1).Select(city => city.Id)));
                    }

                    return selectedCity;
                })
                .ToList();
            var nearbyCities = nearbyMapObjects
                .Where(mapObject => mapObject.Type == MapObjectTypeEnum.City)
                .ToList();
            var citiesByIsland = nearbyCities
                .Select(city => WorldGenerationService.TryGetIslandCoordinates(
                    city.X,
                    city.Y,
                    world.MapSeed,
                    out int cityIslandX,
                    out int cityIslandY)
                        ? new { City = city, Island = (cityIslandX, cityIslandY) }
                        : null)
                .Where(entry => entry != null)
                .GroupBy(entry => entry!.Island)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(entry => ((int)entry!.City.X, (int)entry.City.Y)).ToList());
            var futureCitySites = relevantIslands
                .SelectMany(island => PlayerCitySiteGenerator.GenerateFutureSites(
                    WorldGenerationService.GetIslandDefinition(island.CellX, island.CellY, world.MapSeed),
                    world.MapSeed,
                    worldMinimumX,
                    worldMaximumX,
                    worldMinimumY,
                    worldMaximumY,
                    citiesByIsland.GetValueOrDefault((island.CellX, island.CellY), [])))
                .Where(site => site.X >= dto.startX
                    && site.X < chunkMaximumXExclusive
                    && site.Y >= dto.startY
                    && site.Y < chunkMaximumYExclusive)
                .Select(site => new WorldMapCoordinateDTO(site.X, site.Y))
                .ToList();
            var islands = relevantIslands
                .Where(island => island.CenterX >= dto.startX
                    && island.CenterX < chunkMaximumXExclusive
                    && island.CenterY >= dto.startY
                    && island.CenterY < chunkMaximumYExclusive)
                .ToList();

            return new WorldMapChunkResponseDTO
            {
                WorldSeed = world.MapSeed,
                WorldWidth = world.Width,
                WorldHeight = world.Height,
                ChunkX = dto.startX,
                ChunkY = dto.startY,
                Width = dto.width,
                Height = dto.height,
                MaximumCityPoints = _cityPointCalculator.CalculateMaximumPointsForCity(),

                MapObjects = mapObjectEntities.Select(o => new WorldMapObjectDTO
                {
                    X = o.X,
                    Y = o.Y,
                    Type = (byte)o.Type,
                    ReferenceEntityId = o.ReferenceEntityId
                }).ToList(),

                Cities = normalizedCityEntities.Select(c => new CityDTO(
                    c.Id,
                    c.Name,
                    c.X,
                    c.Y,
                    c.Points,
                    c.IsNPC
                )).ToList(),

                FutureCitySites = futureCitySites,

                Islands = islands.Select(island => new WorldIslandMapDTO(
                    island.Id,
                    island.CenterX,
                    island.CenterY)).ToList()
            };
        }

        public async Task<CityInspectionDTO?> GetCityInspectionAsync(Guid cityId)
        {
            var city = await _cityRepository.GetCityWithBuildingsByCityIdentifierAsync(cityId);
            if (city == null)
            {
                return null;
            }

            var requester = await _playerAccessService.RequireWorldMembershipAsync(city.WorldId);

            return new CityInspectionDTO(
                city.Id,
                city.Name,
                city.X,
                city.Y,
                city.Points,
                city.WorldPlayerId,
                city.WorldPlayer?.PlayerProfile?.UserName,
                city.WorldPlayer?.AllianceId,
                city.WorldPlayer?.Alliance?.Name,
                _deploymentPermissionService.CanAttack(requester, city),
                await _deploymentPermissionService.CanSupportAsync(requester, city),
                city.IsNPC);
        }

        public async Task<WorldIslandDetailsDTO?> GetIslandDetailsAsync(Guid islandId)
        {
            var island = await _worldIslandRepository.GetByIdAsync(islandId);
            if (island == null)
                return null;

            var worldPlayer = await _playerAccessService.RequireWorldMembershipAsync(island.WorldId);
            var worldSeed = await _worldRepository.GetWorldSeedAsync(island.WorldId);
            if (worldSeed == null)
                return null;

            int radius = WorldGenerationService.MaximumIslandRadius + 1;
            int areaSize = radius * 2 + 1;
            var nearbyMapObjects = await _worldMapObject.GetObjectsInAreaAsync(
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

            var cities = nearbyCities
                .Where(city => WorldGenerationService.TryGetIslandCoordinates(
                    city.X, city.Y, worldSeed.Value, out int cellX, out int cellY)
                    && cellX == island.CellX
                    && cellY == island.CellY)
                .ToList();

            var exoticResources = await _exoticResourceService.GetIslandResourcesAsync(island.Id);

            return new WorldIslandDetailsDTO(
                island.Id,
                island.CenterX,
                island.CenterY,
                cities.Any(city => city.WorldPlayerId == worldPlayer.Id),
                cities
                    .OrderByDescending(city => city.Points)
                    .ThenBy(city => city.Name)
                    .Select(city => new WorldIslandCityDTO(
                    city.Id,
                    city.Name,
                    city.WorldPlayerId,
                    city.WorldPlayer?.PlayerProfile?.UserName,
                    city.X,
                    city.Y,
                    city.Points,
                    city.WorldPlayer?.AllianceId,
                    city.WorldPlayer?.Alliance?.Name,
                    city.IsNPC)).ToList(),
                exoticResources);
        }

        public async Task<List<WorldAvailableResponseDTO>> ObtainAllActiveGameWorldsAsync()
        {
            var activeWorlds = await _worldRepository.GetAllAsync();

            if (activeWorlds.Count == 0) return new List<WorldAvailableResponseDTO>();

            var playerCounts = await _worldRepository.GetPlayerCountsByWorldAsync();

            return activeWorlds.Select(world => new WorldAvailableResponseDTO(
                world.Id,
                world.Name,
                playerCounts.GetValueOrDefault(world.Id),
                false
            )).ToList();
        }
    }
}
