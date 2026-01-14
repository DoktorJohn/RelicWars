using Application.Interfaces.IRepositories;
using Domain.User;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class WorldPlayerRepository : IWorldPlayerRepository
    {
        private readonly GameContext _context;

        public WorldPlayerRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId)
        {
            return await _context.WorldPlayers
                .AsNoTracking()
                .Include(wp => wp.Cities)
                .FirstOrDefaultAsync(wp => wp.PlayerProfileId == profileId && wp.WorldId == worldId);
        }

        public async Task<WorldPlayer?> GetByIdAsync(Guid id)
        {
            return await _context.WorldPlayers.FindAsync(id);
        }

        public async Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id)
        {
            return await _context.WorldPlayers
                .Include(u => u.CompletedResearches)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task AddAsync(WorldPlayer user)
        {
            await _context.WorldPlayers.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(WorldPlayer user)
        {
            _context.WorldPlayers.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.WorldPlayers.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<WorldPlayer>>? GetAllAsync()
        {
            var users = await _context.WorldPlayers.ToListAsync();
            return users;
        }
    }
}
