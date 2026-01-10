using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Workers
{
    public class CityWorker
    {
        private readonly IJobRepository _jobRepo;
        private readonly IJobService _jobService;
        private readonly ILogger<CityWorker> _logger;

        public CityWorker(IJobRepository jobRepo, IJobService jobService, ILogger<CityWorker> logger)
        {
            _jobRepo = jobRepo;
            _jobService = jobService;
            _logger = logger;
        }

        public async Task ProcessCityJobsAsync()
        {
            var dueJobs = await _jobRepo.GetDueJobsAsync(DateTime.UtcNow);
            foreach (var job in dueJobs)
            {
                try
                {
                    await _jobService.ProcessAsync(job);
                    if (job.IsCompleted) await _jobRepo.DeleteAsync(job.Id);
                    else await _jobRepo.UpdateAsync(job);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing job {job.Id}");
                }
            }
        }
    }
}