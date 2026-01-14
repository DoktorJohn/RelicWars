using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IPlayerProfileRepository
    {
        Task<PlayerProfile?> GetByEmailAsync(string email);
        Task<PlayerProfile?> GetByIdAsync(Guid id);
        Task AddAsync(PlayerProfile playerProfile);
        Task<bool> ExistsByEmailAsync(string email);
        Task<string?> GetUserNameByIdAsync(Guid id);
    }
}
