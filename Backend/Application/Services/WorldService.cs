using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
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

        public WorldService(IWorldRepository worldRepository)
        {
            _worldRepository = worldRepository;
        }

        public async Task<WorldMapChunkResponseDTO?> GetWorldMapChunk(GetWorldMapChunkDTO dto)
        {
            // 1. Get Seed from Repository
            var seed = await _worldRepository.GetWorldSeedAsync(dto.worldId);
            if (seed == null) return null;

            // 2. Generate Terrain Data (CPU Bound - No DB cost)
            byte[] terrainArray = new byte[dto.width * dto.height];
            int index = 0;
            for (short x = dto.startX; x < dto.startX + dto.width; x++)
            {
                for (short y = dto.startY; y < dto.startY + dto.height; y++)
                {
                    // Deterministic generation
                    var biome = WorldGenerationService.CalculateWorldMapBiomeVariant(x, y, seed.Value);
                    terrainArray[index++] = (byte)biome;
                }
            }

            // 3. Get Map Objects from Repository
            var entities = await _worldRepository.GetObjectsInAreaAsync(
                dto.worldId, dto.startX, dto.startY, dto.width, dto.height);

            // 4. Map to DTO
            var mapObjects = entities.Select(o => new WorldMapObjectDTO
            {
                X = o.X,
                Y = o.Y,
                Type = (byte)o.Type,
                ReferenceEntityId = o.ReferenceEntityId
            }).ToList();

            return new WorldMapChunkResponseDTO
            {
                WorldSeed = seed.Value,
                ChunkX = dto.startX,
                ChunkY = dto.startY,
                Width = dto.width,
                Height = dto.height,
                TerrainData = terrainArray,
                MapObjects = mapObjects
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
