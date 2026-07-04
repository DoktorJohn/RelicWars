using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;

namespace Application.Tests;

public class RecruitmentServiceTests
{
    [Fact]
    public async Task QueueRecruitmentAsync_RejectsInsufficientPopulationAndResources()
    {
        var setup = CreateSetup(availablePopulation: 1, wood: 5, stone: 5, metal: 5);
        var unit = setup.UnitReader.GetUnit(UnitTypeEnum.Militia);
        var populationNeed = unit.PopulationCost * 2;
        var resourceNeed = new[] { unit.WoodCost * 2, unit.StoneCost * 2, unit.MetalCost * 2 };

        var populationResult = await setup.Service.QueueRecruitmentAsync(setup.Player.Id, setup.City.Id, UnitTypeEnum.Militia, 2);
        Assert.False(populationResult.Success);
        Assert.Contains("boligkapacitet", populationResult.Message);
        Assert.Empty(setup.JobRepository.AddedJobs);

        setup.City.Wood = resourceNeed[0] - 1;
        setup.City.Stone = resourceNeed[1] - 1;
        setup.City.Metal = resourceNeed[2] - 1;
        setup.City.LastResourceUpdate = DateTime.UtcNow.AddHours(-1);
        setup.CityStatService.AvailablePopulation = populationNeed + 10;

        var resourceResult = await setup.Service.QueueRecruitmentAsync(setup.Player.Id, setup.City.Id, UnitTypeEnum.Militia, 2);

        Assert.False(resourceResult.Success);
        Assert.Contains("materialer", resourceResult.Message);
        Assert.Empty(setup.JobRepository.AddedJobs);
    }

    [Fact]
    public async Task QueueRecruitmentAsync_DeductsResourcesAndCreatesJob()
    {
        var setup = CreateSetup(availablePopulation: 100, wood: 500, stone: 500, metal: 500);
        var unit = setup.UnitReader.GetUnit(UnitTypeEnum.Militia);
        var quantity = 3;
        var expectedWood = setup.City.Wood - (unit.WoodCost * quantity);
        var expectedStone = setup.City.Stone - (unit.StoneCost * quantity);
        var expectedMetal = setup.City.Metal - (unit.MetalCost * quantity);

        var result = await setup.Service.QueueRecruitmentAsync(setup.Player.Id, setup.City.Id, UnitTypeEnum.Militia, quantity);

        Assert.True(result.Success);
        Assert.Contains(setup.City.Name, result.Message);
        Assert.Equal(expectedWood, setup.City.Wood, 3);
        Assert.Equal(expectedStone, setup.City.Stone, 3);
        Assert.Equal(expectedMetal, setup.City.Metal, 3);

        var job = Assert.Single(setup.JobRepository.AddedJobs);
        var recruitmentJob = Assert.IsType<RecruitmentJob>(job);
        Assert.Equal(setup.Player.Id, recruitmentJob.WorldPlayerId);
        Assert.Equal(setup.City.Id, recruitmentJob.CityId);
        Assert.Equal(UnitTypeEnum.Militia, recruitmentJob.UnitType);
        Assert.Equal(quantity, recruitmentJob.TotalQuantity);
        Assert.False(recruitmentJob.IsCompleted);
        Assert.True(recruitmentJob.ExecutionTime > recruitmentJob.LastTickTime);
    }

    [Fact]
    public async Task GetRecruitmentQueueAsync_FiltersByCategoryAndReturnsRemainingTime()
    {
        var setup = CreateSetup(availablePopulation: 100, wood: 500, stone: 500, metal: 500);
        var now = DateTime.UtcNow;
        var infantryJob = new RecruitmentJob
        {
            Id = Guid.NewGuid(),
            CityId = setup.City.Id,
            WorldPlayerId = setup.Player.Id,
            UnitType = UnitTypeEnum.Militia,
            TotalQuantity = 5,
            CompletedQuantity = 1,
            SecondsPerUnit = 30,
            ExecutionTime = now.AddMinutes(10),
            LastTickTime = now.AddMinutes(-5),
            IsCompleted = false
        };
        var siegeJob = new RecruitmentJob
        {
            Id = Guid.NewGuid(),
            CityId = setup.City.Id,
            WorldPlayerId = setup.Player.Id,
            UnitType = UnitTypeEnum.Ballista,
            TotalQuantity = 2,
            CompletedQuantity = 0,
            SecondsPerUnit = 45,
            ExecutionTime = now.AddMinutes(20),
            LastTickTime = now.AddMinutes(-1),
            IsCompleted = false
        };
        setup.JobRepository.ActiveRecruitmentJobs.AddRange(new[] { infantryJob, siegeJob });

        var result = await setup.Service.GetRecruitmentQueueAsync(new GetRecruitmentQueueItemsDTO
        {
            CityId = setup.City.Id,
            UnitCategories = [UnitCategoryEnum.Infantry]
        });

        var item = Assert.Single(result);
        Assert.Equal(UnitTypeEnum.Militia, item.UnitType);
        Assert.Equal(4, item.Amount);
        Assert.Equal(150, item.TotalDurationSeconds);
        Assert.InRange(item.TimeRemainingSeconds, 689, 691);
    }

    private static Setup CreateSetup(int availablePopulation, double wood, double stone, double metal)
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Player" }
        };
        player.PlayerProfileId = player.PlayerProfile.Id;
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Capital",
            WorldId = player.WorldId,
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Wood = wood,
            Stone = stone,
            Metal = metal,
            LastResourceUpdate = DateTime.UtcNow.AddHours(-1),
            UnitStacks = new List<UnitStack>()
        };
        player.Cities.Add(city);

        var unitReader = TestData.UnitReader();
        var jobRepository = new TrackingRecruitmentJobRepository();
        var cityStatService = new TrackingCityStatService { AvailablePopulation = availablePopulation };
        var service = new RecruitmentService(
            new MemoryCityRepository(city),
            jobRepository,
            new SnapshotResourceService(),
            new NoOpWorldPlayerService(),
            new TestPlayerAccessService([player], [city]),
            new NoOpResearchService(),
            unitReader,
            TestData.BuildingReader(),
            cityStatService,
            new RecruitmentTimeCalculationService(new NoOpModifierService()),
            new ImmediateTransactionManager());

        return new Setup(player, city, unitReader, jobRepository, cityStatService, service);
    }

    private sealed record Setup(
        WorldPlayer Player,
        City City,
        UnitDataReader UnitReader,
        TrackingRecruitmentJobRepository JobRepository,
        TrackingCityStatService CityStatService,
        RecruitmentService Service);

    private sealed class TrackingRecruitmentJobRepository : IJobRepository
    {
        public List<BaseJob> AddedJobs { get; } = [];
        public List<RecruitmentJob> ActiveRecruitmentJobs { get; } = [];

        public Task<BaseJob?> GetByIdAsync(Guid id) => Task.FromResult<BaseJob?>(ActiveRecruitmentJobs.FirstOrDefault(job => job.Id == id));
        public Task<List<BaseJob>> GetDueJobsAsync(DateTime now, int batchSize) => Task.FromResult(new List<BaseJob>());
        public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(new List<BuildingJob>());
        public Task AddAsync(BaseJob job)
        {
            AddedJobs.Add(job);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(BaseJob job) => Task.CompletedTask;
        public Task DeleteAsync(Guid jobId)
        {
            ActiveRecruitmentJobs.RemoveAll(job => job.Id == jobId);
            return Task.CompletedTask;
        }
        public Task<ResearchJob?> GetResearchJobAsync(Guid userId) => Task.FromResult<ResearchJob?>(null);
        public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) =>
            Task.FromResult(ActiveRecruitmentJobs.Where(job => job.CityId == cityId).ToList());
        public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) => Task.FromResult(new List<ResearchJob>());
    }

    private sealed class TrackingCityStatService : ICityStatService
    {
        public int AvailablePopulation { get; set; }
        public double GetWarehouseCapacity(City city) => 10_000;
        public int GetMaxPopulation(City city) => AvailablePopulation;
        public int GetCurrentPopulationUsage(City city, IEnumerable<BaseJob> activeJobs) => 0;
        public int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs) => AvailablePopulation;
    }

    private sealed class SnapshotResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime) =>
            new(cityEntity.Wood, cityEntity.Stone, cityEntity.Metal, 0, 0, 0, currentDateTime);

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime) =>
            new(playerEntity.Coins, playerEntity.ResearchPoints, playerEntity.IdeologyFocusPoints, 0, 0, 0, currentDateTime);
    }

    private sealed class NoOpWorldPlayerService : IWorldPlayerService
    {
        public Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid worldId) => throw new NotImplementedException();
        public Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId) => throw new NotImplementedException();
        public Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description) => throw new NotImplementedException();
        public Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId) => throw new NotImplementedException();
        public Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query) => throw new NotImplementedException();
        public void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime) { }
        public Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request) => throw new NotImplementedException();
    }

    private sealed class NoOpResearchService : IResearchService
    {
        public Task<BuildingResult> QueueResearchAsync(Guid userId, string researchId) => throw new NotImplementedException();
        public Task<BuildingResult> CancelResearchAsync(Guid userId, Guid jobId) => throw new NotImplementedException();
        public Task<List<Modifier>> GetUserResearchModifiersAsync(Guid userId) => Task.FromResult(new List<Modifier>());
        public Task<ResearchTreeDTO> GetResearchTreeAsync(Guid userId) => throw new NotImplementedException();
    }

    private sealed class NoOpModifierService : IModifierService
    {
        public ModifierCalculationResult CalculateEntityValueWithModifiers(double baseValue, IEnumerable<ModifierTagEnum> targetTags, IEnumerable<Domain.Abstraction.IModifierProvider> providers) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityValue(City city, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculatePlayerValue(WorldPlayer player, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };

        public ModifierCalculationResult CalculateCityUnitValue(City city, UnitData unit, double baseValue, params ModifierTagEnum[] targetTags) =>
            new() { BaseValue = baseValue, FinalValue = baseValue };
    }
}
