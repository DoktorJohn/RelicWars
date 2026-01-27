using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class WorldRepository : IWorldRepository
    {
        private readonly GameContext _context;

        public WorldRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<List<World>>? GetAllAsync()
        {
            var worlds = await _context.World.ToListAsync();
            return worlds;
        }

        public async Task<World?> GetByIdAsync(Guid id)
        {
            return await _context.World.FindAsync(id);
        }

        public async Task<int?> GetWorldSeedAsync(Guid worldId)
        {
            return await _context.World
                .AsNoTracking()
                .Where(w => w.Id == worldId)
                .Select(w => (int?)w.MapSeed)
                .FirstOrDefaultAsync();
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
    }
}
