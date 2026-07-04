using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces;
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
        private const int BatchSize = 100;
        private readonly IJobRepository _jobRepo;
        private readonly IJobService _jobService;
        private readonly ILogger<CityWorker> _logger;
        private readonly ITransactionManager _transactionManager;

        public CityWorker(IJobRepository jobRepo, IJobService jobService, ILogger<CityWorker> logger, ITransactionManager transactionManager)
        {
            _jobRepo = jobRepo;
            _jobService = jobService;
            _logger = logger;
            _transactionManager = transactionManager;
        }

        public async Task ProcessCityJobsAsync()
        {
            var dueJobs = await _jobRepo.GetDueJobsAsync(DateTime.UtcNow, BatchSize);

            foreach (var job in dueJobs)
            {
                try
                {
                    await _transactionManager.ExecuteAsync(async () =>
                    {
                        await _jobService.ProcessAsync(job);

                        if (job.IsCompleted)
                        {
                            _logger.LogInformation("[CityWorker] Job {JobId} completed. Deleting from queue.", job.Id);
                            await _jobRepo.DeleteAsync(job.Id);
                        }
                        else
                        {
                            await _jobRepo.UpdateAsync(job);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process job {JobId}", job.Id);
                }
            }
        }
    }
}
