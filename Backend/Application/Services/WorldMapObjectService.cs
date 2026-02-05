using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Abstraction;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorldMapObjectService : IWorldMapObjectService
    {
        private readonly IWorldMapObjectRepository _mapObjectRepo;
        private readonly ILogger<WorldMapObjectService> _logger;

        public WorldMapObjectService(IWorldMapObjectRepository mapObjectRepo, ILogger<WorldMapObjectService> logger)
        {
            _mapObjectRepo = mapObjectRepo;
            _logger = logger;
        }

        public async Task AddEntityToWorldMapAsync(IMapEntity entity)
        {
            var mapObject = new WorldMapObject
            {
                WorldId = entity.WorldId,
                X = (short)entity.X,
                Y = (short)entity.Y,
                Type = entity.MapObjectType,
                ReferenceEntityId = entity.Id
            };

            await _mapObjectRepo.AddAsync(mapObject);
            _logger.LogInformation($"Registreret {entity.MapObjectType} ({entity.Id}) på kortet ved {entity.X},{entity.Y}");
        }

        public async Task UpdateEntityPositionOnWorldMapAsync(IMapEntity entity, int oldX, int oldY)
        {
            await _mapObjectRepo.DeleteAtCoordinatesAsync(entity.WorldId, (short)oldX, (short)oldY);

            await AddEntityToWorldMapAsync(entity);
        }

        public async Task RemoveEntityFromWorldMapAsync(IMapEntity entity)
        {
            await _mapObjectRepo.DeleteAtCoordinatesAsync(entity.WorldId, (short)entity.X, (short)entity.Y);
            _logger.LogInformation($"Fjernet {entity.MapObjectType} ({entity.Id}) fra kortet.");
        }
    }
}
