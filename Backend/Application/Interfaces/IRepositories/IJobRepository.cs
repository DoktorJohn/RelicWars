using Domain.Workers;
using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IJobRepository
    {
        Task<BaseJob?> GetByIdAsync(Guid id);
        Task<List<BaseJob>> GetDueJobsAsync(DateTime now);
        Task<List<BaseJob>> GetJobsByCityAsync(Guid cityId); // Til Dashboardet
        Task AddAsync(BaseJob job);
        Task UpdateAsync(BaseJob job); // Til Recruitment fremskridt
        Task DeleteAsync(Guid jobId);
        Task<BaseJob?> GetActiveResearchJobForUserAsync(Guid userId);
    }
}
