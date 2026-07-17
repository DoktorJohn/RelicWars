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
        Task<List<BaseJob>> GetDuePlayerJobsAsync(DateTime now, int batchSize, IReadOnlyCollection<Guid> excludedJobIds) =>
            Task.FromResult(new List<BaseJob>());
        Task<List<BaseJob>> GetDueNPCBuildingJobsAsync(DateTime now, int batchSize, IReadOnlyCollection<Guid> excludedJobIds) =>
            Task.FromResult(new List<BaseJob>());
        Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId);
        Task AddAsync(BaseJob job);
        Task UpdateAsync(BaseJob job); // Til RecruitmentSpeed fremskridt
        Task DeleteAsync(Guid jobId);
        void DeletePending(BaseJob job) => throw new NotSupportedException();
        Task<ResearchJob?> GetResearchJobAsync(Guid userId);
        Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId);
        Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id);
    }
}
