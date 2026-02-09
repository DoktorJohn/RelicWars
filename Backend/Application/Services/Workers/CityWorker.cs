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
            // Hent alle jobs der er forfaldne
            var dueJobs = await _jobRepo.GetDueJobsAsync(DateTime.UtcNow);

            foreach (var job in dueJobs)
            {
                try
                {
                    // Service opdaterer City og selve job-objektet i hukommelsen
                    await _jobService.ProcessAsync(job);

                    // Nu beslutter vi hvad der skal ske i databasen
                    if (job.IsCompleted)
                    {
                        _logger.LogInformation("[CityWorker] Job {JobId} completed. Deleting from queue.", job.Id);
                        await _jobRepo.DeleteAsync(job.Id);
                    }
                    else
                    {
                        // For RecruitmentJobs der kun er delvist færdige, gemmer vi fremskridtet
                        await _jobRepo.UpdateAsync(job);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process job {JobId}", job.Id);
                }
            }
        }
    }
}