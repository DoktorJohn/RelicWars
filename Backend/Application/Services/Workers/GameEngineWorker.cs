using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
using Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services.Workers
{
    public class GameEngineWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<GameEngineWorker> _logger;
        private DateTime _lastResourceSave = DateTime.UtcNow;
        private DateTime _lastRankingGeneration = DateTime.UtcNow;
        private DateTime _lastDailySave = DateTime.UtcNow;

        public GameEngineWorker(IServiceProvider services, ILogger<GameEngineWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Game Engine Orchestrator started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (IServiceScope serviceScope = _services.CreateScope())
                {
                    var scopedProvider = serviceScope.ServiceProvider;
                    try
                    {
                        // 1. Kør bygge/rekrutterings jobs
                        var cityJobWorker = scopedProvider.GetRequiredService<CityWorker>();
                        await cityJobWorker.ProcessCityJobsAsync();

                        // 2. Kør hær-bevægelser (Ankomster/Retur)
                        var unitDeploymentWorker = scopedProvider.GetRequiredService<UnitDeploymentWorker>();
                        await unitDeploymentWorker.ProcessMilitaryMovementsAsync();

                        if ((DateTime.UtcNow - _lastRankingGeneration).TotalMinutes >= 10)
                        {
                            await SynchronizeAllPlayerPointsAndRankings(scopedProvider);
                            _lastRankingGeneration = DateTime.UtcNow;
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "GameEngine Loop Error");
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
        }


        private async Task SynchronizeAllPlayerPointsAndRankings(IServiceProvider scopedProvider)
        {
            _logger.LogInformation("[Ranking] Starter generering af globale rankings...");

            var cityDataRepository = scopedProvider.GetRequiredService<ICityRepository>();
            var buildingDataReader = scopedProvider.GetRequiredService<BuildingDataReader>();

            var allCities = await cityDataRepository.GetAllAsync();
            string rankingPath = "rankings.json";

            RankingGenerator.GenerateRankingSnapshot(rankingPath, allCities, buildingDataReader);

            _logger.LogInformation("[Ranking] Rankings snapshot er blevet gemt succesfuldt.");
        }
    }
}
