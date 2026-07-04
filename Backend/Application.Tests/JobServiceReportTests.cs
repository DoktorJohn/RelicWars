using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services.Jobs;
using Application.Services;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class JobServiceReportTests
{
    [Fact]
    public async Task ProcessAsync_BuildingJobCreatesBuildingCompletionReport()
    {
        var setup = CreateSetup();
        var job = new BuildingJob
        {
            Id = Guid.NewGuid(),
            CityId = setup.City.Id,
            WorldPlayerId = setup.Player.Id,
            BuildingType = BuildingTypeEnum.Barracks,
            TargetLevel = 2,
            ExecutionTime = TestData.Now,
            IsCompleted = false
        };

        await setup.Service.ProcessAsync(job);

        Assert.True(job.IsCompleted);
        Assert.Single(setup.Reports.AddedReports);
        Assert.Equal(ReportTypeEnum.BuildingCompleted, setup.Reports.AddedReports[0].ReportType);
        Assert.Contains("Construction completed", setup.Reports.AddedReports[0].Title);
        Assert.Contains("Barracks level 2", setup.Reports.AddedReports[0].Body);
    }

    [Fact]
    public async Task ProcessAsync_CompletedRecruitmentJobCreatesRecruitmentCompletionReport()
    {
        var setup = CreateSetup();
        var job = new RecruitmentJob
        {
            Id = Guid.NewGuid(),
            CityId = setup.City.Id,
            WorldPlayerId = setup.Player.Id,
            UnitType = UnitTypeEnum.Militia,
            TotalQuantity = 3,
            CompletedQuantity = 0,
            SecondsPerUnit = 1,
            LastTickTime = DateTime.UtcNow.AddMinutes(-5),
            ExecutionTime = DateTime.UtcNow.AddSeconds(-1),
            IsCompleted = false
        };

        await setup.Service.ProcessAsync(job);

        Assert.True(job.IsCompleted);
        Assert.Single(setup.Reports.AddedReports);
        Assert.Equal(ReportTypeEnum.RecruitmentCompleted, setup.Reports.AddedReports[0].ReportType);
        Assert.Contains("Training completed", setup.Reports.AddedReports[0].Title);
        Assert.Contains("3 Militia", setup.Reports.AddedReports[0].Body);
    }

    [Fact]
    public async Task ProcessAsync_IncompleteRecruitmentJobDoesNotCreateReport()
    {
        var setup = CreateSetup();
        var job = new RecruitmentJob
        {
            Id = Guid.NewGuid(),
            CityId = setup.City.Id,
            WorldPlayerId = setup.Player.Id,
            UnitType = UnitTypeEnum.Militia,
            TotalQuantity = 5,
            CompletedQuantity = 0,
            SecondsPerUnit = 1000,
            LastTickTime = DateTime.UtcNow,
            ExecutionTime = DateTime.UtcNow.AddMinutes(10),
            IsCompleted = false
        };

        await setup.Service.ProcessAsync(job);

        Assert.False(job.IsCompleted);
        Assert.Empty(setup.Reports.AddedReports);
    }

    private static Setup CreateSetup()
    {
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            CompletedResearches = new List<Research>(),
            Cities = new List<City>()
        };

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = "Capital",
            WorldPlayerId = player.Id,
            WorldPlayer = player,
            Buildings = new List<Building>(),
            UnitStacks = new List<UnitStack>()
        };
        player.Cities.Add(city);

        var reports = new TrackingBattleReportRepository();
        var service = new JobService(
            reports,
            new NoOpResourceService(),
            new MemoryCityRepository(city),
            NullLogger<JobService>.Instance,
            new NoOpWorldPlayerRepository(),
            new NoOpWorldPlayerService(),
            new CityPointCalculator(TestData.BuildingReader()));

        return new Setup(service, reports, player, city);
    }

    private sealed record Setup(JobService Service, TrackingBattleReportRepository Reports, WorldPlayer Player, City City);

    private sealed class TrackingBattleReportRepository : IBattleReportRepository
    {
        public List<BattleReport> AddedReports { get; } = [];

        public Task AddAsync(BattleReport report)
        {
            AddedReports.Add(report);
            return Task.CompletedTask;
        }

        public Task<BattleReport?> GetByIdAsync(Guid reportId) => Task.FromResult<BattleReport?>(null);
        public Task<List<BattleReport>> GetByUserIdAsync(Guid userId) => Task.FromResult(new List<BattleReport>());
        public Task<int> GetUnreadCountAsync(Guid userId) => Task.FromResult(0);
        public Task MarkAsReadAsync(Guid reportId) => Task.CompletedTask;
        public Task DeleteAsync(Guid reportId) => Task.CompletedTask;
    }

    private sealed class NoOpWorldPlayerRepository : IWorldPlayerRepository
    {
        public Task<WorldPlayer?> GetByIdAsync(Guid id) => Task.FromResult<WorldPlayer?>(null);
        public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => Task.FromResult<WorldPlayer?>(null);
        public Task AddAsync(WorldPlayer user) => Task.CompletedTask;
        public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult(new List<WorldPlayer>());
        public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) => Task.FromResult<WorldPlayer?>(null);
        public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) => Task.FromResult(new List<WorldPlayer>());
        public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) => Task.FromResult(new List<WorldPlayer>());
    }

    private sealed class NoOpWorldPlayerService : IWorldPlayerService
    {
        public Task<WorldPlayerJoinResponse> AssignPlayerToGameWorldAsync(Guid worldId) => Task.FromResult(new WorldPlayerJoinResponse(false, string.Empty, null, null, IdeologyTypeEnum.None));
        public Task<WorldPlayerProfileDTO> GetWorldPlayerProfileAsync(Guid worldPlayerId) => Task.FromResult(new WorldPlayerProfileDTO(Guid.Empty, string.Empty, 0, 0, 0, string.Empty, string.Empty, IdeologyTypeEnum.None, Guid.Empty, Guid.Empty, new List<CityDTO>()));
        public Task<WorldPlayerProfileDTO> UpdateWorldPlayerDescriptionAsync(Guid worldPlayerId, string description) => Task.FromResult(new WorldPlayerProfileDTO(Guid.Empty, string.Empty, 0, 0, 0, string.Empty, string.Empty, IdeologyTypeEnum.None, Guid.Empty, Guid.Empty, new List<CityDTO>()));
        public Task<WorldPlayerEconomyDTO> GetWorldPlayerEconomyAsync(Guid worldPlayerId) => Task.FromResult(new WorldPlayerEconomyDTO());
        public Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query) => Task.FromResult(new List<PlayerSearchResultDTO>());
        public void SyncGlobalResources(WorldPlayer player, DateTime currentDateTime) { }
        public Task<WorldPlayerSelectIdeologyResponse> SelectIdeology(SelectIdeologyRequest request) => Task.FromResult(new WorldPlayerSelectIdeologyResponse(false, string.Empty));
    }

    private sealed class NoOpResourceService : IResourceService
    {
        public CityResourceSnapshot CalculateCityResources(City cityEntity, DateTime currentDateTime) =>
            new CityResourceSnapshot(cityEntity.Wood, cityEntity.Stone, cityEntity.Metal, 0, 0, 0, currentDateTime);

        public GlobalResourceSnapshot CalculateGlobalResources(WorldPlayer playerEntity, DateTime currentDateTime) =>
            new GlobalResourceSnapshot(0, 0, 0, 0, 0, 0, currentDateTime);
    }
}
