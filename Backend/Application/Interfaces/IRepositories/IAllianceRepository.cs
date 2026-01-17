using Domain.Entities;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IAllianceRepository
    {
        Task<Alliance?> GetByIdAsync(Guid id);
        Task AddAsync(Alliance alliance);
        Task UpdateAsync(Alliance alliance);
        Task DeleteAsync(Alliance alliance);

        Task<bool> NameExistsAsync(string name);
        Task<Alliance?> GetByIdWithMembersAsync(Guid id);
        
    }
}
