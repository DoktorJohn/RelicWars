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

        public async Task<List<BattleReport>> GetByUserIdAsync(Guid userId)
        {
            return await _context.BattleReports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.OccurredAt)
                .ToListAsync();
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
    }
}
