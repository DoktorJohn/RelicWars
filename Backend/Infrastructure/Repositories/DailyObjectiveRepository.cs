using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public sealed class DailyObjectiveRepository : IDailyObjectiveRepository
    {
        private readonly GameContext _context;

        public DailyObjectiveRepository(GameContext context)
        {
            _context = context;
        }

        public Task<DailyObjectiveSet?> GetByWorldPlayerIdAsync(Guid worldPlayerId) =>
            _context.DailyObjectiveSets
                .Include(set => set.Assignments)
                .SingleOrDefaultAsync(set => set.WorldPlayerId == worldPlayerId);

        public async Task<DailyObjectiveSet> ReplaceAsync(DailyObjectiveSet? existingSet, DailyObjectiveSet replacement)
        {
            if (existingSet != null)
            {
                _context.DailyObjectiveAssignments.RemoveRange(existingSet.Assignments);
                existingSet.Assignments.Clear();
                existingSet.DayStartUtc = replacement.DayStartUtc;
                existingSet.DateLastModified = replacement.DateLastModified;
                foreach (var assignment in replacement.Assignments)
                {
                    assignment.DailyObjectiveSetId = existingSet.Id;
                    existingSet.Assignments.Add(assignment);
                }
                return existingSet;
            }
            await _context.DailyObjectiveSets.AddAsync(replacement);
            return replacement;
        }
    }
}
