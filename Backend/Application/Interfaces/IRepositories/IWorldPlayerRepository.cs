using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IWorldPlayerRepository
    {
        Task<WorldPlayer?> GetByIdAsync(Guid id);

        Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id);

        Task AddAsync(WorldPlayer user);
        Task UpdateAsync(WorldPlayer user);
        Task DeleteAsync(Guid id);
        Task<List<WorldPlayer>>? GetAllAsync();
        Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId);


    }
}
