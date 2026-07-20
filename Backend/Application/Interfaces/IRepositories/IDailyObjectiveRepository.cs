using Domain.Entities;

namespace Application.Interfaces.IRepositories
{
    public interface IDailyObjectiveRepository
    {
        Task AcquirePlayerLockAsync(Guid worldPlayerId);
        Task<DailyObjectiveSet?> GetByWorldPlayerIdAsync(Guid worldPlayerId);
        Task<DailyObjectiveSet> ReplaceAsync(DailyObjectiveSet? existingSet, DailyObjectiveSet replacement);
        void ResetTrackedState(Guid worldPlayerId);
    }
}
