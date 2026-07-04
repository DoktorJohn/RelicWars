using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Microsoft.EntityFrameworkCore;
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

        public WorldService(IWorldRepository worldRepository, IWorldMapObjectRepository worldMapObject, ICityRepository cityRepository, IPlayerAccessService playerAccessService, IWorldIslandRepository worldIslandRepository, IExoticResourceService exoticResourceService, IDeploymentPermissionService deploymentPermissionService)
        {
            _worldRepository = worldRepository;
            _worldMapObject = worldMapObject;
            _cityRepository = cityRepository;
            _playerAccessService = playerAccessService;
            _worldIslandRepository = worldIslandRepository;
            _exoticResourceService = exoticResourceService;
            _deploymentPermissionService = deploymentPermissionService;
        }

        public async Task<WorldMapChunkResponseDTO?> GetWorldMapChunk(GetWorldMapChunkDTO dto)
        {
            await _playerAccessService.RequireWorldMembershipAsync(dto.worldId);
            var world = await _worldRepository.GetByIdAsync(dto.worldId);
            if (world == null) return null;

            // 3. Get Map Objects from Repository
            var mapObjectEntities = await _worldMapObject.GetObjectsInAreaAsync(
                dto.worldId, dto.startX, dto.startY, dto.width, dto.height);

            var cityIdentifiers = mapObjectEntities
                .Where(o => o.Type == MapObjectTypeEnum.City && o.ReferenceEntityId.HasValue)
                .Select(o => o.ReferenceEntityId!.Value)
                .ToList();

            var cityEntities = await _cityRepository.GetCitiesByListOfIdsAsync(cityIdentifiers);
            var islands = await _worldIslandRepository.GetInAreaAsync(
                dto.worldId, dto.startX, dto.startY, dto.width, dto.height);

            return new WorldMapChunkResponseDTO
            {
                WorldSeed = world.MapSeed,
                WorldWidth = world.Width,
                WorldHeight = world.Height,
                ChunkX = dto.startX,
                ChunkY = dto.startY,
                Width = dto.width,
                Height = dto.height,

                MapObjects = mapObjectEntities.Select(o => new WorldMapObjectDTO
                {
                    X = o.X,
                    Y = o.Y,
                    Type = (byte)o.Type,
                    ReferenceEntityId = o.ReferenceEntityId
                }).ToList(),

                Cities = cityEntities.Select(c => new CityDTO(
                    c.Id,
                    c.Name,
                    c.X,
                    c.Y,
                    c.Points
                )).ToList(),

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
                await _deploymentPermissionService.CanSupportAsync(requester, city));
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
                    city.WorldPlayer?.Alliance?.Name)).ToList(),
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
