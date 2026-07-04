using Application.Interfaces.IRepositories;
using Application.Services;
using Domain.Entities;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using System.Linq;

namespace Application.Tests;

public class ResearchServiceTests
{
    [Fact]
    public async Task GetResearchTreeAsync_ReturnsActiveJobAndMarksOnlyTheActiveResearchAsResearching()
    {
        var playerId = Guid.NewGuid();
        var activeResearchId = "ECON_PROD_2";
        var player = new WorldPlayer
        {
            Id = playerId,
            ResearchPoints = 100,
            CompletedResearches = new List<Research>
            {
                new() { ResearchId = "ECON_PROD_1", CompletedAt = DateTime.UtcNow.AddHours(-1) }
            }
        };
        var job = new ResearchJob
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = playerId,
            ResearchId = activeResearchId,
            ExecutionTime = DateTime.UtcNow.AddMinutes(30),
            IsCompleted = false
        };

        var service = new ResearchService(
            new MemoryResearchJobRepository(job),
            new MemoryWorldPlayerRepository(player),
            new TestPlayerAccessService([player]),
            TestData.ResearchReader(),
            new ImmediateTransactionManager());

        var tree = await service.GetResearchTreeAsync(playerId);

        Assert.NotNull(tree.ActiveJob);
        Assert.Equal(job.Id, tree.ActiveJob!.JobId);
        Assert.Equal(activeResearchId, tree.ActiveJob.ResearchId);

        var activeNode = tree.Nodes.Single(n => n.Id == activeResearchId);
        Assert.True(activeNode.IsResearching);
        Assert.All(tree.Nodes.Where(n => n.Id != activeResearchId), n => Assert.False(n.IsResearching));
    }

    [Fact]
    public async Task QueueResearchAsync_DeductsPointsAndCreatesJob()
    {
        var researchReader = TestData.ResearchReader();
        var researchNode = researchReader.GetAll().First(node => string.IsNullOrEmpty(node.ParentId));
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            ResearchPoints = researchNode.ResearchPointCost + 25
        };
        var jobRepository = new MemoryResearchJobRepository();
        var service = new ResearchService(
            jobRepository,
            new MemoryWorldPlayerRepository(player),
            new TestPlayerAccessService([player]),
            researchReader,
            new ImmediateTransactionManager());

        var result = await service.QueueResearchAsync(player.Id, researchNode.Id);

        Assert.True(result.Success);
        Assert.Contains(researchNode.Name, result.Message);
        Assert.Equal(25d, player.ResearchPoints, 3);
        var addedJob = Assert.Single(jobRepository.AddedJobs);
        var researchJob = Assert.IsType<ResearchJob>(addedJob);
        Assert.Equal(player.Id, researchJob.WorldPlayerId);
        Assert.Equal(researchNode.Id, researchJob.ResearchId);
        Assert.False(researchJob.IsCompleted);
    }

    [Fact]
    public async Task CancelResearchAsync_RefundsPointsAndDeletesJob()
    {
        var researchReader = TestData.ResearchReader();
        var researchNode = researchReader.GetAll().First(node => string.IsNullOrEmpty(node.ParentId));
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            ResearchPoints = 0
        };
        var job = new ResearchJob
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = player.Id,
            ResearchId = researchNode.Id,
            ExecutionTime = DateTime.UtcNow.AddHours(1),
            IsCompleted = false
        };
        var jobRepository = new MemoryResearchJobRepository(job);
        var service = new ResearchService(
            jobRepository,
            new MemoryWorldPlayerRepository(player),
            new TestPlayerAccessService([player]),
            researchReader,
            new ImmediateTransactionManager());

        var result = await service.CancelResearchAsync(player.Id, job.Id);

        Assert.True(result.Success);
        Assert.Equal(researchNode.ResearchPointCost, player.ResearchPoints, 3);
        Assert.Contains(job.Id, jobRepository.DeletedJobIds);
    }
}

internal sealed class MemoryWorldPlayerRepository : IWorldPlayerRepository
{
    private readonly WorldPlayer _player;

    public MemoryWorldPlayerRepository(WorldPlayer player)
    {
        _player = player;
    }

    public Task<WorldPlayer?> GetByIdAsync(Guid id) => Task.FromResult<WorldPlayer?>(id == _player.Id ? _player : null);

    public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => GetByIdAsync(id);

    public Task AddAsync(WorldPlayer user) => Task.CompletedTask;

    public Task UpdateAsync(WorldPlayer user) => Task.CompletedTask;

    public Task DeleteAsync(Guid id) => Task.CompletedTask;

    public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult<List<WorldPlayer>>(new List<WorldPlayer> { _player });

    public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) => Task.FromResult<WorldPlayer?>(null);

    public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) => Task.FromResult(new List<WorldPlayer>());

    public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) => Task.FromResult(new List<WorldPlayer>());
}

internal sealed class MemoryResearchJobRepository : IJobRepository
{
    private readonly ResearchJob? _job;
    public List<BaseJob> AddedJobs { get; } = [];
    public List<Guid> DeletedJobIds { get; } = [];

    public MemoryResearchJobRepository(ResearchJob? job = null)
    {
        _job = job;
    }

    public Task<BaseJob?> GetByIdAsync(Guid id) => Task.FromResult<BaseJob?>(_job != null && id == _job.Id ? _job : null);

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
        DeletedJobIds.Add(jobId);
        return Task.CompletedTask;
    }

    public Task<ResearchJob?> GetResearchJobAsync(Guid userId) => Task.FromResult<ResearchJob?>(_job != null && userId == _job.WorldPlayerId ? _job : null);

    public Task<List<RecruitmentJob>> GetRecruitmentJobsAsync(Guid cityId) => Task.FromResult(new List<RecruitmentJob>());

    public Task<List<ResearchJob>> GetResearchJobsByIdAsync(Guid id) =>
        Task.FromResult(_job != null && id == _job.WorldPlayerId ? new List<ResearchJob> { _job } : new List<ResearchJob>());
}
