using Application.Interfaces;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services.Workers;
using Domain.Enums;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace Application.Tests;

public class CityWorkerTests
{
    [Fact]
    public async Task PlayerBatch_ProcessesDifferentPlayersInParallelAndSamePlayerChronologically()
    {
        var firstPlayerId = Guid.NewGuid();
        var state = new WorkerState([
            Job(firstPlayerId, TestData.Now.AddMinutes(-4)),
            Job(firstPlayerId, TestData.Now.AddMinutes(-3)),
            Job(Guid.NewGuid(), TestData.Now.AddMinutes(-2)),
            Job(Guid.NewGuid(), TestData.Now.AddMinutes(-1))
        ]);
        var worker = CreateWorker(state);

        var result = await worker.ProcessPlayerJobsBatchAsync(CancellationToken.None);

        Assert.Equal(4, result.FoundCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(state.MaximumConcurrency > 1);
        Assert.Equal(
            state.InitialJobs.Where(job => job.WorldPlayerId == firstPlayerId).OrderBy(job => job.ExecutionTime).Select(job => job.Id),
            state.ProcessedJobs.Where(entry => entry.WorldPlayerId == firstPlayerId).Select(entry => entry.JobId));
        Assert.Equal(4, state.SaveChangesCalls);
        Assert.Empty(state.Jobs);
    }

    [Fact]
    public async Task PlayerBatch_FailedJobRollsBackStopsItsAggregateAndDoesNotBlockOtherPlayer()
    {
        var failingPlayerId = Guid.NewGuid();
        var failingJob = Job(failingPlayerId, TestData.Now.AddMinutes(-3));
        var laterSamePlayerJob = Job(failingPlayerId, TestData.Now.AddMinutes(-2));
        var otherPlayerJob = Job(Guid.NewGuid(), TestData.Now.AddMinutes(-1));
        var state = new WorkerState([failingJob, laterSamePlayerJob, otherPlayerJob])
        {
            FailingJobId = failingJob.Id
        };
        var worker = CreateWorker(state);

        var result = await worker.ProcessPlayerJobsBatchAsync(CancellationToken.None);

        Assert.Equal(1, result.ErrorCount);
        Assert.DoesNotContain(state.ProcessedJobs, entry => entry.JobId == laterSamePlayerJob.Id);
        Assert.Contains(state.ProcessedJobs, entry => entry.JobId == otherPlayerJob.Id);
        Assert.Contains(failingJob.Id, state.Jobs.Keys);
        Assert.Contains(laterSamePlayerJob.Id, state.Jobs.Keys);
        Assert.DoesNotContain(otherPlayerJob.Id, state.Jobs.Keys);
        Assert.Equal(1, state.SaveChangesCalls);
    }

    private static CityWorker CreateWorker(WorkerState state)
    {
        var services = new ServiceCollection();
        services.AddSingleton(state);
        services.AddScoped<WorkerSession>();
        services.AddScoped<IJobRepository, WorkerJobRepository>();
        services.AddScoped<IJobService, RecordingJobService>();
        services.AddScoped<ITransactionManager, RecordingTransactionManager>();
        var provider = services.BuildServiceProvider();

        return new CityWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CityWorker>.Instance,
            TimeProvider.System);
    }

    private static BuildingJob Job(Guid playerId, DateTime executionTime) => new()
    {
        Id = Guid.NewGuid(),
        WorldPlayerId = playerId,
        CityId = Guid.NewGuid(),
        BuildingType = BuildingTypeEnum.TownHall,
        TargetLevel = 2,
        ExecutionTime = executionTime
    };

    private sealed class WorkerState
    {
        private int _currentConcurrency;
        private int _maximumConcurrency;
        private int _saveChangesCalls;

        public WorkerState(IEnumerable<BuildingJob> jobs)
        {
            InitialJobs = jobs.ToList();
            Jobs = new ConcurrentDictionary<Guid, BuildingJob>(InitialJobs.ToDictionary(job => job.Id));
        }

        public List<BuildingJob> InitialJobs { get; }
        public ConcurrentDictionary<Guid, BuildingJob> Jobs { get; }
        public ConcurrentQueue<ProcessedJob> ProcessedJobs { get; } = new();
        public Guid? FailingJobId { get; init; }
        public int MaximumConcurrency => _maximumConcurrency;
        public int SaveChangesCalls => _saveChangesCalls;

        public void BeginProcessing()
        {
            int current = Interlocked.Increment(ref _currentConcurrency);
            int observed;
            do
            {
                observed = _maximumConcurrency;
                if (current <= observed)
                {
                    break;
                }
            }
            while (Interlocked.CompareExchange(ref _maximumConcurrency, current, observed) != observed);
        }

        public void EndProcessing() => Interlocked.Decrement(ref _currentConcurrency);
        public void RecordSave() => Interlocked.Increment(ref _saveChangesCalls);
    }

    private sealed class WorkerSession
    {
        public BaseJob? Job { get; set; }
        public bool DeletePending { get; set; }
    }

    private sealed class WorkerJobRepository(WorkerState state, WorkerSession session) : IJobRepository
    {
        public Task<List<BaseJob>> GetDuePlayerJobsAsync(DateTime now, int batchSize, IReadOnlyCollection<Guid> excludedJobIds) =>
            Task.FromResult(state.Jobs.Values
                .Where(job => job.WorldPlayerId != Guid.Empty && job.ExecutionTime <= now && !excludedJobIds.Contains(job.Id))
                .OrderBy(job => job.ExecutionTime)
                .Take(batchSize)
                .Cast<BaseJob>()
                .ToList());

        public Task<BaseJob?> GetByIdAsync(Guid id)
        {
            if (!state.Jobs.TryGetValue(id, out var source))
            {
                return Task.FromResult<BaseJob?>(null);
            }

            session.Job = new BuildingJob
            {
                Id = source.Id,
                WorldPlayerId = source.WorldPlayerId,
                CityId = source.CityId,
                BuildingType = source.BuildingType,
                TargetLevel = source.TargetLevel,
                ExecutionTime = source.ExecutionTime,
                IsCompleted = source.IsCompleted
            };
            return Task.FromResult<BaseJob?>(session.Job);
        }

        public void DeletePending(BaseJob job) => session.DeletePending = true;
        public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(new List<BuildingJob>());
        public Task AddAsync(BaseJob job) => Task.CompletedTask;
        public Task UpdateAsync(BaseJob job) => Task.CompletedTask;
        public Task DeleteAsync(Guid jobId) => Task.CompletedTask;
        public Task<ResearchJob?> GetResearchJobAsync(Guid userId) => Task.FromResult<ResearchJob?>(null);
        public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => Task.FromResult(new List<RecruitmentJob>());
        public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) => Task.FromResult(new List<ResearchJob>());
    }

    private sealed class RecordingJobService(WorkerState state) : IJobService
    {
        public async Task ProcessAsync(BaseJob job)
        {
            state.BeginProcessing();
            try
            {
                await Task.Delay(30);
                if (state.FailingJobId == job.Id)
                {
                    throw new InvalidOperationException("Expected test failure.");
                }

                state.ProcessedJobs.Enqueue(new ProcessedJob(job.Id, job.WorldPlayerId));
                job.IsCompleted = true;
            }
            finally
            {
                state.EndProcessing();
            }
        }
    }

    private sealed class RecordingTransactionManager(WorkerState state, WorkerSession session) : ITransactionManager
    {
        public Task ExecuteAsync(Func<Task> operation) => operation();
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => await operation();

        public Task SaveChangesAsync()
        {
            state.RecordSave();
            if (session.DeletePending && session.Job != null)
            {
                state.Jobs.TryRemove(session.Job.Id, out _);
            }

            return Task.CompletedTask;
        }
    }

    private sealed record ProcessedJob(Guid JobId, Guid WorldPlayerId);
}
