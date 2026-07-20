using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Infrastructure.Repositories
{
    public sealed class DailyObjectiveRepository : IDailyObjectiveRepository
    {
        private readonly GameContext _context;

        public DailyObjectiveRepository(GameContext context)
        {
            _context = context;
        }

        public async Task AcquirePlayerLockAsync(Guid worldPlayerId)
        {
            var transaction = _context.Database.CurrentTransaction
                ?? throw new InvalidOperationException("Daily objective locks require an active transaction.");
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 15000;
                SELECT @result;
                """;
            var resource = command.CreateParameter();
            resource.ParameterName = "@resource";
            resource.DbType = DbType.String;
            resource.Value = $"RelicWars:DailyObjective:{worldPlayerId}";
            command.Parameters.Add(resource);

            int result = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (result < 0)
                throw new DbUpdateConcurrencyException($"Could not acquire daily objective lock for world player {worldPlayerId}.");
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

        public void ResetTrackedState(Guid worldPlayerId)
        {
            var sets = _context.ChangeTracker.Entries<DailyObjectiveSet>()
                .Where(entry => entry.Entity.WorldPlayerId == worldPlayerId)
                .ToList();
            var setIds = sets.Select(entry => entry.Entity.Id).ToHashSet();

            foreach (var assignment in _context.ChangeTracker.Entries<DailyObjectiveAssignment>()
                         .Where(entry => setIds.Contains(entry.Entity.DailyObjectiveSetId))
                         .ToList())
            {
                assignment.State = EntityState.Detached;
            }

            foreach (var set in sets)
            {
                set.State = EntityState.Detached;
            }
        }
    }
}
