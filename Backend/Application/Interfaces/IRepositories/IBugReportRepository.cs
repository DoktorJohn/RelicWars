using Domain.Entities;

namespace Application.Interfaces.IRepositories
{
    public interface IBugReportRepository
    {
        Task AddAsync(BugReport bugReport);
    }
}
