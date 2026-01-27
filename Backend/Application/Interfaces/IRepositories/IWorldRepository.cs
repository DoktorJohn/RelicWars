using Domain.Entities;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IWorldRepository
    {
        Task<List<World>>? GetAllAsync();
        Task<World?> GetByIdAsync(Guid id);
        Task<int?> GetWorldSeedAsync(Guid worldId);
        Task<List<WorldMapObject>> GetObjectsInAreaAsync(Guid worldId, short startX, short startY, byte width, byte height);
    }
}
