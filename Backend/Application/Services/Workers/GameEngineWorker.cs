using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Generators;
using Domain.StaticData.Readers;
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
                    IServiceProvider scopedProvider = serviceScope.ServiceProvider;

                    try
                    {
                        var cityJobProcessingWorker = scopedProvider.GetRequiredService<CityWorker>();
                        await cityJobProcessingWorker.ProcessCityJobsAsync();

                        var unitDeploymentWorker = scopedProvider.GetRequiredService<UnitDeploymentWorker>();
                        await unitDeploymentWorker.ProcessMilitaryMovementsAsync();

                        if ((DateTime.UtcNow - _lastResourceSave).TotalMinutes >= 0.5)
                        {
                            await SynchronizeAllPlayerCitiesResourceStatesAsync(scopedProvider);
                            _lastResourceSave = DateTime.UtcNow;
                        }

                        if ((DateTime.UtcNow - _lastRankingGeneration).TotalMinutes >= 0.1)
                        {
                            await SynchronizeAllPlayerPointsAndRankings(scopedProvider);
                            _lastRankingGeneration = DateTime.UtcNow;
                        }

                        if ((DateTime.UtcNow.Date != _lastDailySave.Date))
                        {
                            // await SynchroinzeAllPlayerMedalsAndDailyAwards
                            _lastDailySave = DateTime.UtcNow;
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "En fejl opstod under kørsel af GameEngine-løkken.");
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task SynchronizeAllPlayerCitiesResourceStatesAsync(IServiceProvider scopedProvider)
        {
            // Nu kan vi sikkert løse ICityRepository, da det sker via et scope.
            var cityDataRepository = scopedProvider.GetRequiredService<ICityRepository>();
            var resourceCalculationService = scopedProvider.GetRequiredService<IResourceService>();

            var allCitiesInWorld = await cityDataRepository.GetAllAsync();
            var currentSystemTime = DateTime.UtcNow;

            _logger.LogInformation($"[Sync] Starter synkronisering for {allCitiesInWorld.Count} entiteter.");

            foreach (var cityEntity in allCitiesInWorld)
            {
                // Vi logger kun for spillere for at undgå NPC-støj
                bool isPlayerOwnedCity = cityEntity.WorldPlayer != null;

                var calculatedResourceSnapshot = resourceCalculationService.CalculateCurrent(cityEntity, currentSystemTime);

                if (isPlayerOwnedCity)
                {
                    _logger.LogInformation($"[Sync-PLAYER] By: {cityEntity.Name} | Wood: {cityEntity.Wood:F2} -> {calculatedResourceSnapshot.Wood:F2} | Rate: {calculatedResourceSnapshot.WoodProductionPerHour}/t");
                }

                cityEntity.Wood = calculatedResourceSnapshot.Wood;
                cityEntity.Stone = calculatedResourceSnapshot.Stone;
                cityEntity.Metal = calculatedResourceSnapshot.Metal;
                cityEntity.LastResourceUpdate = currentSystemTime;
            }

            await cityDataRepository.UpdateRangeAsync(allCitiesInWorld);
            _logger.LogInformation("[Sync] Database synkronisering fuldført.");
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
