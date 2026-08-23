using Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services.Workers
{
    public class GameEngineWorker : BackgroundService
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan NPCReconciliationInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RankingSnapshotInterval = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RankingSnapshotFailureDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _services;
        private readonly ILogger<GameEngineWorker> _logger;

        public GameEngineWorker(IServiceProvider services, ILogger<GameEngineWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Game Engine Orchestrator started with independent worker loops.");

            var cityWorker = _services.GetRequiredService<CityWorker>();
            return Task.WhenAll(
                RunJobLoopAsync("player jobs", cityWorker.ProcessPlayerJobsBatchAsync, stoppingToken),
                RunJobLoopAsync("NPC building completions", cityWorker.ProcessNPCBuildingJobsBatchAsync, stoppingToken),
                RunNPCReconciliationLoopAsync(stoppingToken),
                RunDeploymentLoopAsync(stoppingToken),
                RunRankingSnapshotLoopAsync(stoppingToken));
        }

        private async Task RunJobLoopAsync(
            string loopName,
            Func<CancellationToken, Task<JobBatchResult>> processBatch,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await processBatch(cancellationToken);
                    if (result.FoundCount < CityWorker.BatchSize)
                    {
                        await Task.Delay(IdleDelay, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Game engine {LoopName} loop failed.", loopName);
                    await DelayAfterFailureAsync(cancellationToken);
                }
            }
        }

        private async Task RunNPCReconciliationLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _services.CreateAsyncScope();
                    await scope.ServiceProvider
                        .GetRequiredService<NPCBuildingWorker>()
                        .ProcessBuildingQueuesAsync();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Game engine NPC reconciliation loop failed.");
                }

                await DelayAsync(NPCReconciliationInterval, cancellationToken);
            }
        }

        private async Task RunDeploymentLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _services.CreateAsyncScope();
                    await scope.ServiceProvider
                        .GetRequiredService<UnitDeploymentWorker>()
                        .ProcessMilitaryMovementsAsync();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Game engine deployment loop failed.");
                }

                await DelayAsync(IdleDelay, cancellationToken);
            }
        }

        private async Task RunRankingSnapshotLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan nextDelay = RankingSnapshotInterval;

                try
                {
                    await using var scope = _services.CreateAsyncScope();
                    await scope.ServiceProvider
                        .GetRequiredService<RankingSnapshotWorker>()
                        .RefreshSnapshotAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Game engine ranking snapshot refresh failed.");
                    nextDelay = RankingSnapshotFailureDelay;
                }

                await DelayAsync(nextDelay, cancellationToken);
            }
        }

        private static async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private Task DelayAfterFailureAsync(CancellationToken cancellationToken)
        {
            return DelayAsync(IdleDelay, cancellationToken);
        }

    }
}
