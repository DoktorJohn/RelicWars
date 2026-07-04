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
    public class IdeologyFocusRepository : IIdeologyFocusRepository
    {
        private readonly GameContext _context;
        private readonly TimeProvider _timeProvider;

        public IdeologyFocusRepository(GameContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<List<IdeologyFocus>?> GetAllActive()
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            return await _context.IdeologyFocuses
                 .Where(x => x.TimeOfIdeologyFinished.HasValue && x.TimeOfIdeologyFinished >= now)
                 .ToListAsync();
        }

        public async Task<List<IdeologyFocus>?> GetAll()
        {
            return await _context.IdeologyFocuses
                 .ToListAsync();
        }

        public async Task<List<IdeologyFocus>?> GetAllByCityPlayer(Guid cityId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            return await _context.IdeologyFocuses
                .Where(x => x.CityId == cityId)
                .Where(x => x.TimeOfIdeologyStarted <= now &&
                            (x.TimeOfIdeologyFinished == null || x.TimeOfIdeologyFinished > now))
                .ToListAsync();
        }

        public async Task UpdateAsync(IdeologyFocus ideologyFocus)
        {
            _context.IdeologyFocuses.Update(ideologyFocus);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(IdeologyFocus ideologyFocus)
        {
            await _context.IdeologyFocuses.AddAsync(ideologyFocus);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteExpiredFocusesForCityAsync(Guid cityId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var expiredFocuses = await _context.IdeologyFocuses
                .Where(x => x.CityId == cityId &&
                            x.TimeOfIdeologyFinished.HasValue &&
                            x.TimeOfIdeologyFinished <= now)
                .ToListAsync();

            if (expiredFocuses.Any())
            {
                _context.IdeologyFocuses.RemoveRange(expiredFocuses);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(IdeologyFocus ideologyFocus)
        {
            _context.IdeologyFocuses.Remove(ideologyFocus);
            await _context.SaveChangesAsync();
        }
    }
}
