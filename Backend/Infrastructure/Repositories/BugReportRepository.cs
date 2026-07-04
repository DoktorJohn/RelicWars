using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;

namespace Infrastructure.Repositories
{
    public class BugReportRepository : IBugReportRepository
    {
        private readonly GameContext _context;

        public BugReportRepository(GameContext context)
        {
            _context = context;
        }

        public async Task AddAsync(BugReport bugReport)
        {
            await _context.BugReports.AddAsync(bugReport);
            await _context.SaveChangesAsync();
        }
    }
}
