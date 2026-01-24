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
        Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId);
        Task AddAsync(BaseJob job);
        Task UpdateAsync(BaseJob job); // Til RecruitmentSpeed fremskridt
        Task DeleteAsync(Guid jobId);
        Task<ResearchJob?> GetResearchJobAsync(Guid userId);
        Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId);
    }
}
