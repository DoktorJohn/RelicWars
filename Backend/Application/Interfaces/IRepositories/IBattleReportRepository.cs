using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IBattleReportRepository
    {
        Task AddAsync(BattleReport report);
        Task<BattleReport?> GetByIdAsync(Guid reportId);
        Task<List<BattleReport>> GetByUserIdAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid reportId);
        Task DeleteAsync(Guid reportId);
    }
}
