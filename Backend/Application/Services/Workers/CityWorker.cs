using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Application.Services.Workers
{
    public class CityWorker
    {
        public const int BatchSize = 100;
        private const int MaximumParallelAggregates = 4;
        private static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CityWorker> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly ConcurrentDictionary<Guid, DateTime> _cooldowns = new();

        public CityWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<CityWorker> logger,
            TimeProvider timeProvider)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public Task<JobBatchResult> ProcessPlayerJobsBatchAsync(CancellationToken cancellationToken)
        {
            return ProcessBatchAsync(
                "player",
                (repository, now, excluded) => repository.GetDuePlayerJobsAsync(now, BatchSize, excluded),
                job => job.WorldPlayerId,
                cancellationToken);
        }

        public Task<JobBatchResult> ProcessNPCBuildingJobsBatchAsync(CancellationToken cancellationToken)
        {
            return ProcessBatchAsync(
                "NPC building",
                (repository, now, excluded) => repository.GetDueNPCBuildingJobsAsync(now, BatchSize, excluded),
                job => ((BuildingJob)job).CityId,
                cancellationToken);
        }

        private async Task<JobBatchResult> ProcessBatchAsync(
            string queueName,
            Func<IJobRepository, DateTime, IReadOnlyCollection<Guid>, Task<List<BaseJob>>> query,
            Func<BaseJob, Guid> aggregateKey,
            CancellationToken cancellationToken)
        {
            DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
            IReadOnlyCollection<Guid> excludedJobIds = GetCoolingDownJobIds(now);
            List<BaseJob> dueJobs;

            await using (var queryScope = _scopeFactory.CreateAsyncScope())
            {
                var repository = queryScope.ServiceProvider.GetRequiredService<IJobRepository>();
                dueJobs = await query(repository, now, excludedJobIds);
            }

            var stopwatch = Stopwatch.StartNew();
            int errorCount = 0;
            var aggregates = dueJobs
                .GroupBy(aggregateKey)
                .Select(group => group.OrderBy(job => job.ExecutionTime).ThenBy(job => job.Id).ToList())
                .ToList();

            await Parallel.ForEachAsync(
                aggregates,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = MaximumParallelAggregates,
                    CancellationToken = cancellationToken
                },
                async (jobs, token) =>
                {
                    foreach (var queuedJob in jobs)
                    {
                        token.ThrowIfCancellationRequested();

                        if (!await ProcessJobAsync(queuedJob, queueName, token))
                        {
                            Interlocked.Increment(ref errorCount);
                            break;
                        }
                    }
                });

            stopwatch.Stop();
            TimeSpan oldestAge = dueJobs.Count == 0
                ? TimeSpan.Zero
                : now - dueJobs.Min(job => job.ExecutionTime);

            if (dueJobs.Count > 0)
            {
                _logger.LogInformation(
                    "[CityWorker] {QueueName} batch: {BatchSize} jobs in {DurationMs} ms, {ErrorCount} errors, oldest age {OldestAgeSeconds:F1} s.",
                    queueName,
                    dueJobs.Count,
                    stopwatch.Elapsed.TotalMilliseconds,
                    errorCount,
                    Math.Max(0, oldestAge.TotalSeconds));
            }
            else
            {
                _logger.LogDebug("[CityWorker] No due {QueueName} jobs.", queueName);
            }

            return new JobBatchResult(dueJobs.Count, errorCount, oldestAge, stopwatch.Elapsed);
        }

        private async Task<bool> ProcessJobAsync(
            BaseJob queuedJob,
            string queueName,
            CancellationToken cancellationToken)
        {
            await using var jobScope = _scopeFactory.CreateAsyncScope();
            var repository = jobScope.ServiceProvider.GetRequiredService<IJobRepository>();
            var jobService = jobScope.ServiceProvider.GetRequiredService<IJobService>();
            var transactionManager = jobScope.ServiceProvider.GetRequiredService<ITransactionManager>();

            try
            {
                var job = await repository.GetByIdAsync(queuedJob.Id);
                DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
                if (job == null || job.IsCompleted || job.ExecutionTime > now)
                {
                    return true;
                }

                await transactionManager.ExecuteAsync(async () =>
                {
                    await jobService.ProcessAsync(job);

                    if (job.IsCompleted)
                    {
                        repository.DeletePending(job);
                    }

                    await transactionManager.SaveChangesAsync();
                });

                _cooldowns.TryRemove(job.Id, out _);
                _logger.LogDebug("[CityWorker] Processed {QueueName} job {JobId}.", queueName, job.Id);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _cooldowns[queuedJob.Id] = _timeProvider.GetUtcNow().UtcDateTime.Add(FailureCooldown);
                _logger.LogError(exception, "[CityWorker] Failed to process {QueueName} job {JobId}.", queueName, queuedJob.Id);
                return false;
            }
        }

        private IReadOnlyCollection<Guid> GetCoolingDownJobIds(DateTime now)
        {
            foreach (var cooldown in _cooldowns)
            {
                if (cooldown.Value <= now)
                {
                    _cooldowns.TryRemove(cooldown.Key, out _);
                }
            }

            return _cooldowns.Keys.ToArray();
        }
    }

    public sealed record JobBatchResult(
        int FoundCount,
        int ErrorCount,
        TimeSpan OldestAge,
        TimeSpan Duration);
}
