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
    public class PlayerProfileRepository : IPlayerProfileRepository
    {
        private readonly GameContext _context;

        public PlayerProfileRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<string?> GetUserNameByIdAsync(Guid id)
        {
            return await _context.PlayerProfiles
                .Where(p => p.Id == id)
                .Select(p => p.UserName)
                .FirstOrDefaultAsync();
        }

        //GET for authentication.
        public async Task<PlayerProfile?> GetByEmailAsync(string email)
        {
            return await _context.PlayerProfiles
                .AsNoTracking()
                .Include(profile => profile.WorldPlayers)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        //GET for getting entire PlayerProfile object with navigation properties.
        public async Task<PlayerProfile?> GetByIdAsync(Guid id)
        {
            return await _context.PlayerProfiles
                .Include(profile => profile.WorldPlayers)
                    .ThenInclude(worldPlayer => worldPlayer.Cities)
                .FirstOrDefaultAsync(profile => profile.Id == id);
        }

        //Used in authentication
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.PlayerProfiles
                .AnyAsync(u => u.Email == email);
        }

        public async Task AddAsync(PlayerProfile playerProfile)
        {
            await _context.PlayerProfiles.AddAsync(playerProfile);
            await _context.SaveChangesAsync();
        }

    }
}
