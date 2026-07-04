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
                    .ThenInclude(city => city.ExoticResources)
                .FirstOrDefaultAsync(wp => wp.PlayerProfileId == profileId && wp.WorldId == worldId);
        }

        public async Task<WorldPlayer?> GetByIdAsync(Guid id)
        {
            return await _context.WorldPlayers
                .AsSplitQuery()
                .Include(wp => wp.PlayerProfile)
                .Include(wp => wp.Alliance)
                .Include(wp => wp.CompletedResearches)
                .Include(wp => wp.ModifiersInternal)
                .Include(wp => wp.Cities)
                    .ThenInclude(c => c.Buildings)
                .Include(wp => wp.Cities)
                    .ThenInclude(c => c.UnitStacks)
                .Include(wp => wp.Cities)
                    .ThenInclude(c => c.ExoticResources)
                .Include(wp => wp.Cities)
                    .ThenInclude(c => c.OriginUnitDeployments)
                        .ThenInclude(d => d.UnitStacks)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<WorldPlayer?> GetByIdIdeologyOverviewAsync(Guid id)
        {
            return await _context.WorldPlayers
                .Include(wp => wp.PlayerProfile)
                .FirstOrDefaultAsync(wp => wp.Id == id);
        }

        public async Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id)
        {
            return await _context.WorldPlayers
                .Include(u => u.CompletedResearches)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId)
        {
            return await _context.WorldPlayers
                .Where(player => player.AllianceId == allianceId)
                .ToListAsync();
        }

        public async Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery)
        {
            if (string.IsNullOrWhiteSpace(usernameQuery)) return new List<WorldPlayer>();
            
            // EF Core translates .Contains() to SQL LIKE '%query%', which is case-insensitive by default in SQL Server unless a CS collation is used.
            // Removing explicit ToLower() to ensure better index usage and compatibility.
            return await _context.WorldPlayers
                .Include(wp => wp.PlayerProfile)
                .Where(wp => wp.WorldId == worldId && wp.PlayerProfile.UserName.Contains(usernameQuery))
                .Take(10) 
                .ToListAsync();
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
