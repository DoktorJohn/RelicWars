using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Utility;
using Domain.Abstraction;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;

namespace Application.Tests;

public class BuildingServiceTests
{
    [Fact]
    public async Task QueueUpgradeAsync_AllowsConsecutiveLevelsOfSameBuildingInQueue()
    {
        var player = new WorldPlayer { Id = Guid.NewGuid() };
        var city = new City
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Wood = 1_000_000_000,
            Stone = 1_000_000_000,
            Metal = 1_000_000_000,
            Buildings =
            [
                new Building { CityId = Guid.NewGuid(), Type = BuildingTypeEnum.TownHall, Level = 1 }
            ]
        };
        var jobRepository = new MemoryBuildingJobRepository();
        var modifierService = new PassThroughModifierService();
        var service = new BuildingService(
            modifierService,
            new MemoryCityRepository(city),
            jobRepository,
            new CurrentCityResourceService(),
            TestData.BuildingReader(),
            new FixedCityStatService(),
            new ConstructionTimeCalculator(modifierService),
            new TestPlayerAccessService(cities: [city]),
            new ImmediateTransactionManager());

        var queuedResults = new List<BuildingResult>();
        for (int index = 0; index < 7; index++)
        {
            queuedResults.Add(await service.QueueUpgradeAsync(city.Id, BuildingTypeEnum.TownHall));
        }

        BuildingResult queueFullResult = await service.QueueUpgradeAsync(city.Id, BuildingTypeEnum.TownHall);

        Assert.All(queuedResults, result => Assert.True(result.Success, result.Message));
        Assert.False(queueFullResult.Success);
        Assert.Equal("Byggekøen er fuld.", queueFullResult.Message);
        Assert.Equal([2, 3, 4, 5, 6, 7, 8], jobRepository.BuildingJobs.Select(job => job.TargetLevel));
        Assert.All(
            jobRepository.BuildingJobs.Zip(jobRepository.BuildingJobs.Skip(1)),
            pair => Assert.True(pair.Second.ExecutionTime > pair.First.ExecutionTime));
    }

    [Fact]
    public async Task CancelQueuedUpgradeAsync_OnlyRemovesLastFutureJob()
    {
        var player = new WorldPlayer { Id = Guid.NewGuid() };
        var city = new City { Id = Guid.NewGuid(), WorldPlayerId = player.Id, WorldPlayer = player };
        var first = new BuildingJob { Id = Guid.NewGuid(), CityId = city.Id, ExecutionTime = DateTime.UtcNow.AddHours(1), TargetLevel = 2 };
        var last = new BuildingJob { Id = Guid.NewGuid(), CityId = city.Id, ExecutionTime = DateTime.UtcNow.AddHours(2), TargetLevel = 3 };
        var jobs = new MemoryBuildingJobRepository();
        jobs.BuildingJobs.AddRange([first, last]);
        var modifierService = new PassThroughModifierService();
        var service = new BuildingService(
            modifierService, new MemoryCityRepository(city), jobs, new CurrentCityResourceService(),
            TestData.BuildingReader(), new FixedCityStatService(), new ConstructionTimeCalculator(modifierService),
            new TestPlayerAccessService(cities: [city]), new ImmediateTransactionManager());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelQueuedUpgradeAsync(city.Id, first.Id));
        List<BuildingDTO> remaining = await service.CancelQueuedUpgradeAsync(city.Id, last.Id);

        Assert.Single(remaining);
        Assert.Equal(first.Id, remaining[0].Id);
        Assert.Equal(first.Id, Assert.Single(jobs.BuildingJobs).Id);
    }

    [Fact]
    public async Task QueueUpgradeAsync_WhenNextLevelExceedsStaticDataMaximum_ReturnsMaximumLevelResult()
    {
        var player = new WorldPlayer { Id = Guid.NewGuid() };
        var city = new City
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Wood = 1_000_000_000,
            Stone = 1_000_000_000,
            Metal = 1_000_000_000,
            Buildings =
            [
                new Building { CityId = Guid.NewGuid(), Type = BuildingTypeEnum.TownHall, Level = 20 }
            ]
        };
        var jobRepository = new MemoryBuildingJobRepository();
        var modifierService = new PassThroughModifierService();
        var service = new BuildingService(
            modifierService,
            new MemoryCityRepository(city),
            jobRepository,
            new CurrentCityResourceService(),
            TestData.BuildingReader(),
            new FixedCityStatService(),
            new ConstructionTimeCalculator(modifierService),
            new TestPlayerAccessService(cities: [city]),
            new ImmediateTransactionManager());

        BuildingResult result = await service.QueueUpgradeAsync(city.Id, BuildingTypeEnum.TownHall);

        Assert.False(result.Success);
        Assert.Equal("Maksimum niveau nået.", result.Message);
        Assert.Empty(jobRepository.BuildingJobs);
    }

    [Fact]
    public async Task QueueNPCUpgradeAsync_ForOwnerlessNPCCity_UsesNormalBuildingQueueWithoutPlayerAccess()
    {
        var city = new City
        {
            Id = Guid.NewGuid(),
            IsNPC = true,
            Wood = 1_000,
            Stone = 1_000,
            Metal = 1_000
        };
        var jobRepository = new MemoryBuildingJobRepository();
        var modifierService = new PassThroughModifierService();
        var service = new BuildingService(
            modifierService,
            new MemoryCityRepository(city),
            jobRepository,
            new CurrentCityResourceService(),
            TestData.BuildingReader(),
            new FixedCityStatService(),
            new ConstructionTimeCalculator(modifierService),
            new TestPlayerAccessService(),
            new ImmediateTransactionManager());

        BuildingResult result = await service.QueueNPCUpgradeAsync(city.Id, BuildingTypeEnum.TownHall);

        Assert.True(result.Success, result.Message);
        var job = Assert.Single(jobRepository.BuildingJobs);
        Assert.Equal(city.Id, job.CityId);
        Assert.Equal(BuildingTypeEnum.TownHall, job.BuildingType);
        Assert.Equal(Guid.Empty, job.WorldPlayerId);
    }

    private sealed class MemoryBuildingJobRepository : IJobRepository
    {
        public List<BuildingJob> BuildingJobs { get; } = [];

        public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(
            BuildingJobs.Where(job => job.CityId == cityId).OrderBy(job => job.ExecutionTime).ToList());

        public Task AddAsync(BaseJob job)
        {
            BuildingJobs.Add((BuildingJob)job);
            return Task.CompletedTask;
        }

        public Task<BaseJob?> GetByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<List<BaseJob>> GetDueJobsAsync(DateTime now, int batchSize) => throw new NotSupportedException();
        public Task UpdateAsync(BaseJob job) => throw new NotSupportedException();
        public Task DeleteAsync(Guid jobId)
        {
            BuildingJobs.RemoveAll(job => job.Id == jobId);
            return Task.CompletedTask;
        }
        public Task<ResearchJob?> GetResearchJobAsync(Guid userId) => throw new NotSupportedException();
        public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => throw new NotSupportedException();
        public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class CurrentCityResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City city, DateTime currentDateTime) =>
            new(city.Wood, city.Stone, city.Metal, 0, 0, 0, currentDateTime);

        public CityProductionSnapshot CalculateCityProduction(WorldPlayer player, City city) => new(0, 0, 0);

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer player, DateTime currentDateTime) =>
            throw new NotSupportedException();
    }

    private sealed class PassThroughModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(
            double baseValue,
            IEnumerable<ModifierTagEnum> targetTags,
            IEnumerable<IModifierProvider> providers) => CreateResult(baseValue);

        public ModifierCalculationResult CalculateCityValue(
            City city,
            double baseValue,
            params ModifierTagEnum[] targetTags) => CreateResult(baseValue);

        public ModifierCalculationResult CalculatePlayerValue(
            WorldPlayer player,
            double baseValue,
            params ModifierTagEnum[] targetTags) => CreateResult(baseValue);

        public ModifierCalculationResult CalculateCityUnitValue(
            City city,
            UnitData unit,
            double baseValue,
            params ModifierTagEnum[] targetTags) => CreateResult(baseValue);

        private static ModifierCalculationResult CreateResult(double baseValue) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }
}
