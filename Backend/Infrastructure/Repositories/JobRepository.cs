using Application.Interfaces.IRepositories;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly GameContext _context;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(GameContext context, ILogger<JobRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId)
        {
            return await _context.Jobs
                .OfType<RecruitmentJob>()
                .Where(j => j.CityId == cityId && !j.IsCompleted)
                .OrderBy(j => j.ExecutionTime)
                .ToListAsync();
        }

        private IQueryable<BaseJob> ActiveJobs => _context.Jobs
        .Where(j => !j.IsCompleted);

        public async Task<ResearchJob?> GetResearchJobAsync(Guid userId)
        {
            return await _context.Jobs
        .OfType<ResearchJob>()
        .Where(j => !j.IsCompleted && j.UserId == userId)
        .FirstOrDefaultAsync();
        }

        public async Task<BaseJob?> GetByIdAsync(Guid id)
        {
            return await _context.Jobs.FindAsync(id);
        }

        public async Task<List<BaseJob>> GetDueJobsAsync(DateTime now)
        {
            return await ActiveJobs
            .Where(j => j.ExecutionTime <= now)
            .OrderBy(j => j.ExecutionTime) // Sortering her til sidst
            .ToListAsync();
        }


        public async Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId)
        {
            return await ActiveJobs
            .OfType<BuildingJob>()
            .Where(j => j.CityId == cityId)
            .OrderBy(j => j.ExecutionTime)
            .ToListAsync();
        }

        public async Task AddAsync(BaseJob job)
        {
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BaseJob job)
        {
            // Vi kalder Update eksplicit for at håndtere "detached" entiteter korrekt
            _context.Jobs.Update(job);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid jobId)
        {
            await _context.Jobs
                .Where(j => j.Id == jobId)
                .ExecuteDeleteAsync();
        }
    }
}
