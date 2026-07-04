using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WorldIslandRepository : IWorldIslandRepository
    {
        private readonly GameContext _context;

        public WorldIslandRepository(GameContext context)
        {
            _context = context;
        }

        public Task<WorldIsland?> GetByCellAsync(Guid worldId, int cellX, int cellY)
        {
            return _context.WorldIslands
                .Include(island => island.ExoticResources)
                .SingleOrDefaultAsync(island =>
                    island.WorldId == worldId
                    && island.CellX == cellX
                    && island.CellY == cellY);
        }

        public Task<WorldIsland?> GetByIdAsync(Guid islandId)
        {
            return _context.WorldIslands
                .Include(island => island.ExoticResources)
                .SingleOrDefaultAsync(island => island.Id == islandId);
        }

        public Task<List<WorldIsland>> GetInAreaAsync(Guid worldId, int startX, int startY, int width, int height)
        {
            int endX = startX + width;
            int endY = startY + height;
            return _context.WorldIslands
                .AsNoTracking()
                .Where(island => island.WorldId == worldId
                    && island.CenterX >= startX && island.CenterX < endX
                    && island.CenterY >= startY && island.CenterY < endY)
                .ToListAsync();
        }

        public async Task UpdateAsync(WorldIsland island)
        {
            _context.WorldIslands.Update(island);
            await _context.SaveChangesAsync();
        }
    }
}
