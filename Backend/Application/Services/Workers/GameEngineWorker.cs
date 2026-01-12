using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services.Workers
{
    public class GameEngineWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<GameEngineWorker> _logger;
        private DateTime _lastGlobalSave = DateTime.UtcNow;

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
                // OBJEKTIV RETTELSE: Vi opretter et scope her.
                using (IServiceScope serviceScope = _services.CreateScope())
                {
                    // Vi bruger scope.ServiceProvider til at løse scoped services.
                    IServiceProvider scopedProvider = serviceScope.ServiceProvider;

                    try
                    {
                        // 1. By-logik (Bygninger, rekruttering)
                        var cityJobProcessingWorker = scopedProvider.GetRequiredService<CityWorker>();
                        await cityJobProcessingWorker.ProcessCityJobsAsync();

                        // 2. Militær-logik (Bevægelser, kamp, loot)
                        var unitDeploymentWorker = scopedProvider.GetRequiredService<UnitDeploymentWorker>();
                        await unitDeploymentWorker.ProcessMilitaryMovementsAsync();

                        // 3. Database Sync (Kør kun hvis intervallet er overskredet)
                        if ((DateTime.UtcNow - _lastGlobalSave).TotalMinutes >= 0.5)
                        {
                            // VIGTIGT: Vi sender scopedProvider videre, IKKE _rootServiceProvider.
                            await SynchronizeAllPlayerCitiesResourceStatesAsync(scopedProvider);
                            _lastGlobalSave = DateTime.UtcNow;
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "En fejl opstod under kørsel af GameEngine-løkken.");
                    }
                }

                // Vent 1 sekund før næste iteration
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
    }
}
