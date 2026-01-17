using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context; // Husk at bruge dit rigtige Context namespace
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class AllianceRepository : IAllianceRepository
    {
        private readonly GameContext _context;

        public AllianceRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<Alliance?> GetByIdAsync(Guid id)
        {
            return await _context.Alliances
                .Include(x => x.Members)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAsync(Alliance alliance)
        {
            await _context.Alliances.AddAsync(alliance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Alliance alliance)
        {
            _context.Alliances.Update(alliance);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Alliance alliance)
        {
            _context.Alliances.Remove(alliance);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> NameExistsAsync(string name)
        {
            return await _context.Alliances
                .AnyAsync(a => a.Name.ToLower() == name.ToLower());
        }

        public async Task<Alliance?> GetByIdWithMembersAsync(Guid id)
        {
            return await _context.Alliances
                .Include(a => a.Members) 
                .FirstOrDefaultAsync(a => a.Id == id);
        }
    }
}