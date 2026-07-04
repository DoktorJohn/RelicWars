using Domain.Entities;

namespace Application.Interfaces.IRepositories
{
    public interface IWorldIslandRepository
    {
        Task<WorldIsland?> GetByCellAsync(Guid worldId, int cellX, int cellY);
        Task<WorldIsland?> GetByIdAsync(Guid islandId);
        Task<List<WorldIsland>> GetInAreaAsync(Guid worldId, int startX, int startY, int width, int height);
        Task UpdateAsync(WorldIsland island);
    }
}
