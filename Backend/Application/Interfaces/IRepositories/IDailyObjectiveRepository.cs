using Domain.Entities;

namespace Application.Interfaces.IRepositories
{
    public interface IDailyObjectiveRepository
    {
        Task<DailyObjectiveSet?> GetByWorldPlayerIdAsync(Guid worldPlayerId);
        Task<DailyObjectiveSet> ReplaceAsync(DailyObjectiveSet? existingSet, DailyObjectiveSet replacement);
    }
}
