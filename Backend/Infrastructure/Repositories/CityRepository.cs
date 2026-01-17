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

        public async Task<City?> GetByIdAsync(Guid cityIdentifier)
        {
            return await _context.Cities
                .Include(city => city.Buildings)
                .Include(city => city.UnitStacks)
                .Include(city => city.WorldPlayer)
                    .ThenInclude(player => player.ModifiersAppliedToWorldPlayer)
                .FirstOrDefaultAsync(city => city.Id == cityIdentifier);
        }

        public async Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityIdentifier)
        {
            return await _context.Cities
                .Include(city => city.Buildings) // Needed for Senate
                .Include(city => city.WorldPlayer) // Needed for modifiers
                    .ThenInclude(player => player.ModifiersAppliedToWorldPlayer)
                .Include(city => city.UnitStacks)
                .FirstOrDefaultAsync(city => city.Id == cityIdentifier);
        }

        public async Task<List<City>> GetAllAsync()
        {
            return await _context.Cities
                .Include(cityEntity => cityEntity.Buildings)

                .Include(cityEntity => cityEntity.UnitStacks)

                .Include(cityEntity => cityEntity.WorldPlayer)
                    .ThenInclude(playerEntity => playerEntity!.PlayerProfile)

                .Include(cityEntity => cityEntity.WorldPlayer)
                    .ThenInclude(playerEntity => playerEntity!.ModifiersAppliedToWorldPlayer)

                .ToListAsync();
        }

        public async Task UpdateAsync(City city)
        {
            _context.Cities.Update(city);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(City city)
        {
            await _context.Cities.AddAsync(city);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(List<City> cities)
        {
            _context.Cities.UpdateRange(cities);
            await _context.SaveChangesAsync();
        }

    }
}
