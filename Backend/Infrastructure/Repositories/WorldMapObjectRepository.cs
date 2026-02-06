using Application.Interfaces.IRepositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class WorldMapObjectRepository : IWorldMapObjectRepository
    {
        private readonly GameContext _context;

        public WorldMapObjectRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<List<WorldMapObject>> GetObjectsByTypeAsync(Guid id, MapObjectTypeEnum type)
        {
            return await _context.WorldMapObjects
                .AsNoTracking()
                .Where(x => x.Type == MapObjectTypeEnum.City).ToListAsync();
        }

        public async Task<WorldMapObject?> GetCityOnCoordinatesAsync(Guid worldId, short X, short Y)
        {
            return await _context.WorldMapObjects
                .AsNoTracking()
                .Where(city => city.WorldId == worldId && city.X == X && city.Y == Y && city.Type == Domain.Enums.MapObjectTypeEnum.City).FirstOrDefaultAsync();
        }

        public async Task AddAsync(WorldMapObject worldMapObject)
        {
            await _context.WorldMapObjects.AddAsync(worldMapObject);
            await _context.SaveChangesAsync();
        }

        public async Task<List<WorldMapObject>> GetObjectsInAreaAsync(Guid worldId, short startX, short startY, byte width, byte height)
        {
            return await _context.WorldMapObjects
                .AsNoTracking()
                .Where(o => o.WorldId == worldId
                         && o.X >= startX && o.X < startX + width
                         && o.Y >= startY && o.Y < startY + height)
                .ToListAsync();
        }
        public async Task DeleteAtCoordinatesAsync(Guid worldId, short x, short y)
        {
            await _context.WorldMapObjects
                .Where(o => o.WorldId == worldId && o.X == x && o.Y == y)
                .ExecuteDeleteAsync();

        }

        public async Task DeleteByReferenceIdAsync(Guid referenceEntityId)
        {
            await _context.WorldMapObjects
                .Where(o => o.ReferenceEntityId == referenceEntityId)
                .ExecuteDeleteAsync();
        }

        public async Task UpdateAsync(WorldMapObject worldMapObject)
        {
            _context.WorldMapObjects.Update(worldMapObject);
            await _context.SaveChangesAsync();
        }

        public async Task<WorldMapObject?> GetWorldMapObjectByReferenceIdAsync(Guid referenceId)
        {
            return await _context.WorldMapObjects.Where(x => x.ReferenceEntityId == referenceId).FirstOrDefaultAsync();
        }
    }
}
