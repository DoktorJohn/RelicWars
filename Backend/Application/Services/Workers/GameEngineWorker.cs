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
                using (var scope = _services.CreateScope())
                {
                    // 1. By-logik (Bygninger, rekruttering)
                    var cityWorker = scope.ServiceProvider.GetRequiredService<CityWorker>();
                    await cityWorker.ProcessCityJobsAsync();

                    // 2. Militær-logik (Bevægelser, kamp, loot)
                    var unitDeploymentWorker = scope.ServiceProvider.GetRequiredService<UnitDeploymentWorker>();
                    await unitDeploymentWorker.ProcessMilitaryMovementsAsync();

                    // 3. Database Sync (Hvert 5. minut)
                    if ((DateTime.UtcNow - _lastGlobalSave).TotalMinutes >= 5)
                    {
                        await SyncWorldState(scope.ServiceProvider);
                        _lastGlobalSave = DateTime.UtcNow;
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task SyncWorldState(IServiceProvider sp)
        {
            var cityRepo = sp.GetRequiredService<ICityRepository>();
            var resService = sp.GetRequiredService<IResourceService>();
            var cities = await cityRepo.GetAllAsync();
            var now = DateTime.UtcNow;

            foreach (var city in cities)
            {
                var snapshot = resService.CalculateCurrent(city, now);
                city.Wood = snapshot.Wood;
                city.Stone = snapshot.Stone;
                city.Metal = snapshot.Metal;
                city.LastResourceUpdate = now;
            }
            await cityRepo.UpdateRangeAsync(cities);
            _logger.LogInformation("World State synchronized.");
        }
    }
}
