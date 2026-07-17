using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services.Workers;
using Domain.Entities;
using Domain.Enums;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class NPCBuildingWorkerTests
{
    [Fact]
    public async Task ProcessBuildingQueuesAsync_FillsOnlyBuildingQueueAndSyncsExoticProduction()
    {
        var city = new City { Id = Guid.NewGuid(), IsNPC = true };
        var cityRepository = new WorkerCityRepository(city);
        var jobRepository = new WorkerJobRepository();
        var buildingService = new RecordingNPCBuildingService(jobRepository);
        var exoticResourceService = new RecordingExoticResourceService();
        var worker = new NPCBuildingWorker(
            cityRepository,
            jobRepository,
            buildingService,
            exoticResourceService,
            TestData.BuildingReader(),
            NullLogger<NPCBuildingWorker>.Instance);

        await worker.ProcessBuildingQueuesAsync();

        Assert.Single(jobRepository.BuildingJobs);
        Assert.All(jobRepository.BuildingJobs, job => Assert.IsType<BuildingJob>(job));
        Assert.Equal(1, exoticResourceService.SyncCalls);
        Assert.Equal(1, buildingService.QueueCalls);
    }

    [Fact]
    public async Task ProcessBuildingQueuesAsync_WhenProjectedPointsReachTarget_DoesNothing()
    {
        var city = new City
        {
            Id = Guid.NewGuid(),
            IsNPC = true,
            Buildings =
            [
                Building(BuildingTypeEnum.TownHall),
                Building(BuildingTypeEnum.University),
                Building(BuildingTypeEnum.Wall),
                Building(BuildingTypeEnum.Barracks),
                Building(BuildingTypeEnum.Stable),
                Building(BuildingTypeEnum.Workshop)
            ]
        };
        var cityRepository = new WorkerCityRepository(city);
        var jobRepository = new WorkerJobRepository();
        var buildingService = new RecordingNPCBuildingService(jobRepository);
        var exoticResourceService = new RecordingExoticResourceService();
        var worker = new NPCBuildingWorker(
            cityRepository,
            jobRepository,
            buildingService,
            exoticResourceService,
            TestData.BuildingReader(),
            NullLogger<NPCBuildingWorker>.Instance);

        await worker.ProcessBuildingQueuesAsync();

        Assert.Empty(jobRepository.BuildingJobs);
        Assert.Equal(0, exoticResourceService.SyncCalls);
        Assert.Equal(0, buildingService.QueueCalls);
    }

    [Fact]
    public async Task ProcessBuildingQueuesAsync_WhenNPCAlreadyHasJobs_DoesNotRefillUntilQueueIsEmpty()
    {
        var city = new City { Id = Guid.NewGuid(), IsNPC = true };
        var cityRepository = new WorkerCityRepository(city);
        var jobRepository = new WorkerJobRepository();
        jobRepository.BuildingJobs.AddRange(Enumerable.Range(1, 3).Select(level => new BuildingJob
        {
            Id = Guid.NewGuid(),
            CityId = city.Id,
            WorldPlayerId = Guid.Empty,
            BuildingType = BuildingTypeEnum.TimberCamp,
            TargetLevel = level,
            ExecutionTime = DateTime.UtcNow.AddMinutes(level)
        }));
        var buildingService = new RecordingNPCBuildingService(jobRepository);
        var worker = new NPCBuildingWorker(
            cityRepository,
            jobRepository,
            buildingService,
            new RecordingExoticResourceService(),
            TestData.BuildingReader(),
            NullLogger<NPCBuildingWorker>.Instance);

        await worker.ProcessBuildingQueuesAsync();

        Assert.Equal(3, jobRepository.BuildingJobs.Count);
        Assert.Equal(0, buildingService.QueueCalls);
    }

    private static Building Building(BuildingTypeEnum type) => new() { Type = type, Level = 20 };

    private sealed class RecordingNPCBuildingService(WorkerJobRepository jobRepository) : IBuildingService
    {
        public int QueueCalls { get; private set; }

        public Task<BuildingResult> QueueNPCUpgradeAsync(Guid cityId, BuildingTypeEnum type)
        {
            QueueCalls++;
            int targetLevel = jobRepository.BuildingJobs.Count(job => job.BuildingType == type) + 1;
            jobRepository.BuildingJobs.Add(new BuildingJob
            {
                CityId = cityId,
                BuildingType = type,
                TargetLevel = targetLevel,
                ExecutionTime = DateTime.UtcNow.AddMinutes(QueueCalls)
            });
            return Task.FromResult(new BuildingResult(true, "Queued"));
        }

        public Task<BuildingResult> QueueUpgradeAsync(Guid cityId, BuildingTypeEnum type) =>
            throw new NotSupportedException();
        public Task<List<BuildingDTO>> GetBuildingQueueAsync(Guid cityId) => throw new NotSupportedException();
        public Task<BuildingResult> RepairAsync(Guid cityId, BuildingTypeEnum type) => throw new NotSupportedException();
    }

    private sealed class RecordingExoticResourceService : IExoticResourceService
    {
        public int SyncCalls { get; private set; }

        public Task<List<CityExoticResourceDTO>> SyncCityExoticResourcesAsync(City city, DateTime currentDateTime)
        {
            SyncCalls++;
            return Task.FromResult(new List<CityExoticResourceDTO>());
        }

        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesAsync(Guid islandId) =>
            throw new NotSupportedException();
        public Task<List<WorldIslandExoticResourceDTO>> GetIslandResourcesForCityAsync(City city) =>
            throw new NotSupportedException();
        public Task<List<CityExoticResourceProductionDTO>> GetProductionBreakdownsForCityAsync(City city) =>
            throw new NotSupportedException();
        public Task<ExoticResourceInvestmentResponseDTO> InvestAsync(Guid cityId, ExoticResourceInvestmentRequestDTO request) =>
            throw new NotSupportedException();
    }

    private sealed class WorkerJobRepository : IJobRepository
    {
        public List<BuildingJob> BuildingJobs { get; } = [];

        public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(
            BuildingJobs.Where(job => job.CityId == cityId).OrderBy(job => job.ExecutionTime).ToList());

        public Task<BaseJob?> GetByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<List<BaseJob>> GetDueJobsAsync(DateTime now, int batchSize) => throw new NotSupportedException();
        public Task AddAsync(BaseJob job) => throw new NotSupportedException();
        public Task UpdateAsync(BaseJob job) => throw new NotSupportedException();
        public Task DeleteAsync(Guid jobId) => throw new NotSupportedException();
        public Task<ResearchJob?> GetResearchJobAsync(Guid userId) => throw new NotSupportedException();
        public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => throw new NotSupportedException();
        public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class WorkerCityRepository(City city) : ICityRepository
    {
        public Task<List<City>> GetNPCsForBuildingAutomationAsync() => Task.FromResult(new List<City> { city });
        public Task<City?> GetByIdAsync(Guid cityId) => Task.FromResult<City?>(city.Id == cityId ? city : null);
        public Task<List<City>> GetAllAsync() => Task.FromResult(new List<City> { city });
        public Task<List<City>> GetCitiesByListOfIdsAsync(List<Guid> ids) => throw new NotSupportedException();
        public Task UpdateAsync(City updatedCity) => throw new NotSupportedException();
        public Task UpdateRangeAsync(List<City> cities) => throw new NotSupportedException();
        public Task AddAsync(City addedCity) => throw new NotSupportedException();
        public Task AddNPCVillagesWithMapObjectsAsync(IReadOnlyCollection<City> cities) => throw new NotSupportedException();
        public Task<City?> GetCityWithBuildingsByCityIdentifierAsync(Guid cityId) => throw new NotSupportedException();
        public Task<City?> GetTownHallCityByCityIdentifierAsync(Guid cityId) => throw new NotSupportedException();
        public Task<City?> GetByCoordinatesAsync(int x, int y) => throw new NotSupportedException();
        public Task<Guid?> GetWorldPlayerIdByCityIdAsync(Guid cityId) => throw new NotSupportedException();
        public Task<List<City>> GetCitiesByWorldPlayerIdAsync(Guid worldPlayerId) => throw new NotSupportedException();
    }
}
