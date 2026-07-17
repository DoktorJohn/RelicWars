using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers;
using Microsoft.Extensions.Logging;

namespace Application.Services.Workers
{
    public class NPCBuildingWorker
    {
        public const int TargetCityPoints = 2500;
        private const int MaximumBuildingQueueSize = 1;

        private readonly ICityRepository _cityRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IBuildingService _buildingService;
        private readonly IExoticResourceService _exoticResourceService;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly ILogger<NPCBuildingWorker> _logger;

        public NPCBuildingWorker(
            ICityRepository cityRepository,
            IJobRepository jobRepository,
            IBuildingService buildingService,
            IExoticResourceService exoticResourceService,
            BuildingDataReader buildingDataReader,
            ILogger<NPCBuildingWorker> logger)
        {
            _cityRepository = cityRepository;
            _jobRepository = jobRepository;
            _buildingService = buildingService;
            _exoticResourceService = exoticResourceService;
            _buildingDataReader = buildingDataReader;
            _logger = logger;
        }

        public async Task ProcessBuildingQueuesAsync()
        {
            var cities = await _cityRepository.GetNPCsForBuildingAutomationAsync();

            foreach (var city in cities)
            {
                try
                {
                    await FillBuildingQueueAsync(city);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to update the building queue for NPC city {CityId}.", city.Id);
                }
            }
        }

        private async Task FillBuildingQueueAsync(City city)
        {
            var buildingJobs = await _jobRepository.GetBuildingJobsAsync(city.Id);
            int projectedPoints = CalculateProjectedPoints(city, buildingJobs);

            if (projectedPoints >= TargetCityPoints || buildingJobs.Count >= MaximumBuildingQueueSize)
                return;

            DateTime currentDateTime = DateTime.UtcNow;
            await _exoticResourceService.SyncCityExoticResourcesAsync(city, currentDateTime);

            foreach (var buildingType in GetUpgradeCandidates(city, buildingJobs))
            {
                var result = await _buildingService.QueueNPCUpgradeAsync(city, buildingType);
                if (result.Success)
                {
                    break;
                }
            }
        }

        private IEnumerable<BuildingTypeEnum> GetUpgradeCandidates(City city, IReadOnlyCollection<BuildingJob> buildingJobs)
        {
            return Enum.GetValues<BuildingTypeEnum>()
                .Where(type => GetEffectiveLevel(city, buildingJobs, type) < _buildingDataReader.GetMaximumLevel(type))
                .OrderBy(type => GetEffectiveLevel(city, buildingJobs, type))
                .ThenBy(type => type);
        }

        private int CalculateProjectedPoints(City city, IReadOnlyCollection<BuildingJob> buildingJobs)
        {
            return Enum.GetValues<BuildingTypeEnum>()
                .Select(type => GetEffectiveLevel(city, buildingJobs, type) is int level && level > 0
                    ? _buildingDataReader.GetConfig<BuildingLevelData>(type, level).Points
                    : 0)
                .Sum();
        }

        private static int GetEffectiveLevel(
            City city,
            IReadOnlyCollection<BuildingJob> buildingJobs,
            BuildingTypeEnum buildingType)
        {
            int currentLevel = city.Buildings.FirstOrDefault(building => building.Type == buildingType)?.Level ?? 0;
            int queuedLevel = buildingJobs
                .Where(job => job.BuildingType == buildingType)
                .Select(job => job.TargetLevel)
                .DefaultIfEmpty(0)
                .Max();

            return Math.Max(currentLevel, queuedLevel);
        }
    }
}
