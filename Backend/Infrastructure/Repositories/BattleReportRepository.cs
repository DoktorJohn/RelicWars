using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class BattleReportRepository : IBattleReportRepository
    {
        private readonly GameContext _context;
        public BattleReportRepository(GameContext context) => _context = context;

        public async Task AddAsync(BattleReport report)
        {
            await _context.BattleReports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<BattleReport?> GetByIdAsync(Guid reportId)
        {
            return await _context.BattleReports
                .AsNoTracking()
                .FirstOrDefaultAsync(report => report.Id == reportId);
        }

        public async Task<List<BattleReport>> GetByUserIdAsync(Guid worldPlayerId)
        {
            return await _context.BattleReports
                .AsNoTracking()
                .Where(r => r.WorldPlayerId == worldPlayerId)
                .OrderByDescending(r => r.OccurredAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid worldPlayerId)
        {
            return await _context.BattleReports
                .AsNoTracking()
                .CountAsync(report => report.WorldPlayerId == worldPlayerId && !report.IsRead);
        }

        public async Task MarkAsReadAsync(Guid reportId)
        {
            var report = await _context.BattleReports.FindAsync(reportId);
            if (report != null)
            {
                report.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid reportId)
        {
            var report = await _context.BattleReports.FindAsync(reportId);
            if (report != null)
            {
                _context.BattleReports.Remove(report);
                await _context.SaveChangesAsync();
            }
        }
    }
}
