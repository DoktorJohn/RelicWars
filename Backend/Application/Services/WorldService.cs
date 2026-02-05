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
        private readonly IUnitDeploymentRepository _unitDeploymentRepository;

        public WorldService(IWorldRepository worldRepository, IWorldMapObjectRepository worldMapObject, ICityRepository cityRepository, IUnitDeploymentRepository unitDeploymentRepository)
        {
            _worldRepository = worldRepository;
            _worldMapObject = worldMapObject;
            _cityRepository = cityRepository;
            _unitDeploymentRepository = unitDeploymentRepository;
        }

        public async Task<WorldMapChunkResponseDTO?> GetWorldMapChunk(GetWorldMapChunkDTO dto)
        {
            // 1. Get Seed from Repository
            var worldSeed = await _worldRepository.GetWorldSeedAsync(dto.worldId);
            if (worldSeed == null) return null;

            // 3. Get Map Objects from Repository
            var mapObjectEntities = await _worldMapObject.GetObjectsInAreaAsync(
                dto.worldId, dto.startX, dto.startY, dto.width, dto.height);

            var cityIdentifiers = mapObjectEntities
                .Where(o => o.Type == MapObjectTypeEnum.City && o.ReferenceEntityId.HasValue)
                .Select(o => o.ReferenceEntityId!.Value)
                .ToList();

            var unitDeploymentIdentifiers = mapObjectEntities
                .Where(o => o.Type == MapObjectTypeEnum.UnitDeployment && o.ReferenceEntityId.HasValue)
                .Select(o => o.ReferenceEntityId!.Value)
                .ToList();

            var cityEntities = await _cityRepository.GetCitiesByListOfIdsAsync(cityIdentifiers);
            var unitDeploymentEntities = await _unitDeploymentRepository.GetUnitDeploymentsWithStacksByListOfIdsAsync(unitDeploymentIdentifiers);

            return new WorldMapChunkResponseDTO
            {
                WorldSeed = worldSeed.Value,
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

                UnitDeployments = unitDeploymentEntities.Select(ud => new UnitDeploymentDTO(
                    ud.Id,
                    ud.Name,
                    ud.WorldPlayerId,
                    ud.OriginCityId,
                    new CityDTO(
                            ud.OriginCity.Id,
                            ud.OriginCity.Name,
                            ud.OriginCity.X,
                            ud.OriginCity.Y,
                            ud.OriginCity.Points
                        ),

                    ud.TargetCityId ?? Guid.Empty,
                    ud.TargetCity != null
                            ? new CityDTO(
                                ud.TargetCity.Id,
                                ud.TargetCity.Name,
                                ud.TargetCity.X,
                                ud.TargetCity.Y,
                                ud.TargetCity.Points
                            )
                            : null,

                    ud.UnitDeploymentMovementStatus,
                    ud.ArrivalTime,
                    ud.NextStepTime,
                    ud.LastStepTime,
                    ud.CurrentX,
                    ud.CurrentY,
                    ud.NextX,
                    ud.NextY,
                    ud.FinalX,
                    ud.FinalY,
                    ud.Mobility,
                    ud.RemainingPathJson ?? "",
                    ud.UnitStacks.Select(us => new UnitStackDTO(us.Type, us.Quantity)).ToList(),
                    ud.OwnerWorldPlayer!.PlayerProfile.UserName ?? ""
                )).ToList()
            };
        }

        public async Task<List<WorldAvailableResponseDTO>> ObtainAllActiveGameWorldsAsync()
        {
            var activeWorlds = await _worldRepository.GetAllAsync();

            if (activeWorlds == null) return new List<WorldAvailableResponseDTO>();

            return activeWorlds.Select(world => new WorldAvailableResponseDTO(
                world.Id,
                world.Name,
                world.PlayerCount,
                1000,
                false
            )).ToList();
        }
    }
}
