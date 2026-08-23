using Application.Interfaces.IRepositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Tests;

public class ResearchServiceTests
{
    [Fact]
    public async Task GetResearchTreeAsync_ReturnsRateAndProjectedActiveProgress()
    {
        var (reader, node) = ResearchableNode();
        var player = PlayerWithUniversity(1);
        var progress = new ResearchProgressService(TestData.ResearchRateCalculator());
        var job = new ResearchJob { Id = Guid.NewGuid(), WorldPlayerId = player.Id, ResearchId = node.Id };
        progress.Initialize(job, player, node.ResearchTimeInSeconds, TestData.Now.AddSeconds(-30));
        var service = CreateService(player, new MemoryResearchJobRepository(job), reader);

        var tree = await service.GetResearchTreeAsync(player.Id);

        Assert.Equal(1d, tree.ResearchRate.BaseResearchPower, 6);
        Assert.Equal(1d, tree.ResearchRate.SpeedMultiplier, 6);
        Assert.NotNull(tree.ActiveJob);
        Assert.Equal(50d, tree.ActiveJob!.ProgressPercentage, 6);
        Assert.Equal(TestData.Now.AddSeconds(30), tree.ActiveJob.ExpectedCompletionTime);
        Assert.True(tree.Nodes.Single().IsResearching);
        Assert.False(tree.Nodes.Single().CanStart);
        Assert.Equal(TestData.Now, tree.ServerTimeUtc);
    }

    [Fact]
    public async Task QueueResearchAsync_CreatesWorkJobWithoutPlayerPayment()
    {
        var (reader, node) = ResearchableNode();
        var player = PlayerWithUniversity(1);
        var jobs = new MemoryResearchJobRepository();
        var service = CreateService(player, jobs, reader);

        var result = await service.QueueResearchAsync(player.Id, node.Id);

        Assert.True(result.Success);
        var job = Assert.IsType<ResearchJob>(Assert.Single(jobs.AddedJobs));
        Assert.Equal(node.ResearchTimeInSeconds, job.TotalWorkSeconds);
        Assert.Equal(node.ResearchTimeInSeconds, job.RemainingWorkSeconds);
        Assert.Equal(1d, job.AppliedSpeedMultiplier, 6);
        Assert.Equal(TestData.Now.AddSeconds(node.ResearchTimeInSeconds), job.ExecutionTime);
    }

    [Fact]
    public async Task QueueResearchAsync_RejectsResearchWithUnmetPrerequisites()
    {
        var node = new ResearchData
        {
            Id = "LOCKED",
            Name = "Locked research",
            ResearchType = ResearchTypeEnum.Economy,
            IsResearchable = true,
            ResearchTimeInSeconds = 60,
            PrerequisiteRule = ResearchPrerequisiteRule.RequiresAll,
            PrerequisiteIds = ["REQUIRED"]
        };
        var reader = Reader(node);
        var player = PlayerWithUniversity(1);
        var jobs = new MemoryResearchJobRepository();
        var service = CreateService(player, jobs, reader);

        var result = await service.QueueResearchAsync(player.Id, node.Id);

        Assert.False(result.Success);
        Assert.Contains("Forudsætningerne", result.Message);
        Assert.Empty(jobs.AddedJobs);
    }

    [Fact]
    public async Task GetResearchTreeAsync_OnlyStartNodesCanStartForNewPlayer()
    {
        var reader = TestData.ResearchReader();
        var player = PlayerWithUniversity(1);
        var service = CreateService(player, new MemoryResearchJobRepository(), reader);

        var tree = await service.GetResearchTreeAsync(player.Id);

        var expected = reader.GetAll()
            .Where(node => node.PrerequisiteRule == ResearchPrerequisiteRule.Start)
            .Select(node => node.Id)
            .OrderBy(id => id)
            .ToArray();
        var actual = tree.Nodes.Where(node => node.CanStart).Select(node => node.Id).OrderBy(id => id).ToArray();
        Assert.Equal(expected, actual);
        Assert.All(tree.Nodes.Where(node => node.PrerequisiteRule != ResearchPrerequisiteRule.Start),
            node => Assert.True(node.IsLocked));
    }

    [Fact]
    public async Task GetResearchTreeAsync_HonorsRequiresAnyAndRequiresAll()
    {
        var requiresAny = new ResearchData
        {
            Id = "REQUIRES_ANY",
            Name = "Requires any",
            ResearchType = ResearchTypeEnum.Economy,
            IsResearchable = true,
            ResearchTimeInSeconds = 60,
            PrerequisiteRule = ResearchPrerequisiteRule.RequiresAny,
            PrerequisiteIds = ["MIL_n1002", "MIL_n1001"]
        };
        var requiresAll = new ResearchData
        {
            Id = "REQUIRES_ALL",
            Name = "Requires all",
            ResearchType = ResearchTypeEnum.Economy,
            IsResearchable = true,
            ResearchTimeInSeconds = 60,
            PrerequisiteRule = ResearchPrerequisiteRule.RequiresAll,
            PrerequisiteIds = ["MIL_n1002", "MIL_n1001"]
        };
        var reader = Reader(requiresAny, requiresAll);
        var player = PlayerWithUniversity(1);
        player.CompletedResearches.Add(new Research
        {
            WorldPlayerId = player.Id,
            ResearchId = requiresAny.PrerequisiteIds[0]
        });
        var service = CreateService(player, new MemoryResearchJobRepository(), reader);

        var tree = await service.GetResearchTreeAsync(player.Id);

        Assert.True(tree.Nodes.Single(node => node.Id == requiresAny.Id).CanStart);
        Assert.False(tree.Nodes.Single(node => node.Id == requiresAll.Id).CanStart);

        foreach (string prerequisiteId in requiresAll.PrerequisiteIds.Skip(1))
        {
            player.CompletedResearches.Add(new Research
            {
                WorldPlayerId = player.Id,
                ResearchId = prerequisiteId
            });
        }

        tree = await service.GetResearchTreeAsync(player.Id);
        Assert.True(tree.Nodes.Single(node => node.Id == requiresAll.Id).CanStart);
    }

    [Fact]
    public async Task QueueResearchAsync_RejectsWhileAnotherResearchIsActive()
    {
        var (reader, node) = ResearchableNode();
        var player = PlayerWithUniversity(1);
        var activeJob = new ResearchJob
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            ResearchId = "OTHER",
            ExecutionTime = TestData.Now.AddHours(1)
        };
        var jobs = new MemoryResearchJobRepository(activeJob);
        var service = CreateService(player, jobs, reader);

        var result = await service.QueueResearchAsync(player.Id, node.Id);

        Assert.False(result.Success);
        Assert.Empty(jobs.AddedJobs);
    }

    [Fact]
    public async Task GetResearchTreeAsync_SerializesNoResearchPointContract()
    {
        var player = PlayerWithUniversity(1);
        var service = CreateService(player, new MemoryResearchJobRepository(), TestData.ResearchReader());

        string json = JsonSerializer.Serialize(await service.GetResearchTreeAsync(player.Id));

        Assert.Contains("\"ResearchRate\"", json);
        Assert.Contains("\"EffectiveResearchPower\"", json);
        Assert.DoesNotContain("ResearchPoint", json);
        Assert.DoesNotContain("CanAfford", json);
        Assert.DoesNotContain("\"Effects\"", json);
    }

    [Fact]
    public async Task QueueResearchAsync_RejectsWithoutUniversity()
    {
        var (reader, node) = ResearchableNode();
        var player = new WorldPlayer { Id = Guid.NewGuid(), Cities = [] };
        var jobs = new MemoryResearchJobRepository();
        var service = CreateService(player, jobs, reader);

        var result = await service.QueueResearchAsync(player.Id, node.Id);

        Assert.False(result.Success);
        Assert.Equal("Build a University in one of your cities to begin research.", result.Message);
        Assert.Empty(jobs.AddedJobs);
    }

    [Fact]
    public async Task CancelResearchAsync_DeletesJobWithoutWorldPlayerMutation()
    {
        var (reader, node) = ResearchableNode();
        var player = PlayerWithUniversity(1);
        var job = new ResearchJob
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            ResearchId = node.Id,
            ExecutionTime = TestData.Now.AddHours(1)
        };
        var jobs = new MemoryResearchJobRepository(job);
        var service = CreateService(player, jobs, reader);

        var result = await service.CancelResearchAsync(player.Id, job.Id);

        Assert.True(result.Success);
        Assert.Contains(job.Id, jobs.DeletedJobIds);
    }

    private static ResearchService CreateService(
        WorldPlayer player,
        MemoryResearchJobRepository jobs,
        ResearchDataReader reader)
    {
        var rate = TestData.ResearchRateCalculator();
        return new ResearchService(
            jobs,
            new TestPlayerAccessService([player]),
            reader,
            new ResearchPrerequisiteEvaluator(),
            rate,
            new ResearchProgressService(rate),
            new FixedTimeProvider(TestData.Now));
    }

    private static WorldPlayer PlayerWithUniversity(int level)
    {
        var player = new WorldPlayer { Id = Guid.NewGuid(), Cities = [] };
        var city = new City
        {
            Id = Guid.NewGuid(),
            WorldPlayer = player,
            WorldPlayerId = player.Id,
            Buildings = [new Building { Type = BuildingTypeEnum.University, Level = level }]
        };
        player.Cities.Add(city);
        return player;
    }

    private static (ResearchDataReader Reader, ResearchData Node) ResearchableNode()
    {
        var node = new ResearchData
        {
            Id = "TEST_ECONOMY_RESEARCH",
            Name = "Test Economy Research",
            Description = "Researchable test fixture.",
            ResearchType = ResearchTypeEnum.Economy,
            IsResearchable = true,
            ResearchTimeInSeconds = 60
        };
        return (Reader(node), node);
    }

    private static ResearchDataReader Reader(params ResearchData[] nodes)
    {
        string path = Path.GetTempFileName();
        try
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };
            File.WriteAllText(path, JsonSerializer.Serialize(nodes, options));
            var reader = new ResearchDataReader();
            reader.Load(path);
            return reader;
        }
        finally
        {
            File.Delete(path);
        }
    }
}

internal sealed class MemoryWorldPlayerRepository : IWorldPlayerRepository
{
    private readonly List<WorldPlayer> _players;

    public MemoryWorldPlayerRepository(params WorldPlayer[] players)
    {
        _players = players.ToList();
    }

    public Task<WorldPlayer?> GetByIdAsync(Guid id) =>
        Task.FromResult(_players.FirstOrDefault(player => player.Id == id));
    public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => GetByIdAsync(id);
    public Task AddAsync(WorldPlayer user) { _players.Add(user); return Task.CompletedTask; }
    public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;
    public Task DeleteAsync(Guid id) => Task.CompletedTask;
    public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult(_players);
    public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) =>
        Task.FromResult(_players.FirstOrDefault(player => player.PlayerProfileId == profileId && player.WorldId == worldId));
    public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) =>
        Task.FromResult(_players.Where(player => player.AllianceId == allianceId).ToList());
    public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) =>
        Task.FromResult(new List<WorldPlayer>());
}

internal sealed class MemoryResearchJobRepository : IJobRepository
{
    private readonly ResearchJob? _job;
    public List<BaseJob> AddedJobs { get; } = [];
    public List<Guid> DeletedJobIds { get; } = [];

    public MemoryResearchJobRepository(ResearchJob? job = null) => _job = job;
    public Task<BaseJob?> GetByIdAsync(Guid id) => Task.FromResult<BaseJob?>(_job?.Id == id ? _job : null);
    public Task<List<BuildingJob>> GetBuildingJobsAsync(Guid cityId) => Task.FromResult(new List<BuildingJob>());
    public Task AddAsync(BaseJob job) { AddedJobs.Add(job); return Task.CompletedTask; }
    public Task UpdateAsync(BaseJob job) => Task.CompletedTask;
    public Task DeleteAsync(Guid jobId) { DeletedJobIds.Add(jobId); return Task.CompletedTask; }
    public void DeletePending(BaseJob job) { }
    public Task<ResearchJob?> GetResearchJobAsync(Guid worldPlayerId) =>
        Task.FromResult(_job?.WorldPlayerId == worldPlayerId && !_job.IsCompleted ? _job : null);
    public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => Task.FromResult(new List<RecruitmentJob>());
    public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) =>
        Task.FromResult(_job?.WorldPlayerId == id ? new List<ResearchJob> { _job } : []);
}
