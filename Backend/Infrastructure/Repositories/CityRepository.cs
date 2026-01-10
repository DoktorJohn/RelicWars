using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CityRepository : ICityRepository
    {
        private readonly GameContext _context;

        public CityRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<City?> GetByIdAsync(Guid cityId)
        {
            return await _context.Cities
                .Include(c => c.Buildings)
                .Include(c => c.UnitStacks)
                .FirstOrDefaultAsync(c => c.Id == cityId);
        }

        public async Task<List<City>> GetAllAsync()
        {
            return await _context.Cities
                .Include(c => c.UnitStacks)
                .ToListAsync();
        }

        public async Task UpdateAsync(City city)
        {
            _context.Cities.Update(city);
            // Dette er den vigtigste linje for at din 'skip' kommando virker!
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(City city)
        {
            await _context.Cities.AddAsync(city);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(List<City> cities)
        {
            await _context.SaveChangesAsync();
        }

        public async Task<City> GetCityWithBuildingsByCityIdentifierAsync(Guid cityIdentifier)
        {
            return await _context.Cities
                .Include(city => city.Buildings)
                .FirstOrDefaultAsync(city => city.Id == cityIdentifier);
        }

    }
}
