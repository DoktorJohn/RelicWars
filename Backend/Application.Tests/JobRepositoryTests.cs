using Domain.Enums;
using Domain.Workers;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class JobRepositoryTests
{
    [Fact]
    public async Task SeparateDueQueries_IsolatePlayerJobFromOlderNpcBacklog()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = DateTime.UtcNow;
        var npcJobs = Enumerable.Range(0, 125)
            .Select(index => new BuildingJob
            {
                Id = Guid.NewGuid(),
                WorldPlayerId = Guid.Empty,
                CityId = Guid.NewGuid(),
                BuildingType = BuildingTypeEnum.TimberCamp,
                TargetLevel = 1,
                ExecutionTime = now.AddMinutes(-200 + index)
            })
            .ToList();
        var playerJob = new BuildingJob
        {
            Id = Guid.NewGuid(),
            WorldPlayerId = Guid.NewGuid(),
            CityId = Guid.NewGuid(),
            BuildingType = BuildingTypeEnum.TownHall,
            TargetLevel = 2,
            ExecutionTime = now.AddMinutes(-1)
        };

        await using (var writeContext = new GameContext(options))
        {
            writeContext.Jobs.AddRange(npcJobs);
            writeContext.Jobs.Add(playerJob);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new GameContext(options);
        var repository = new JobRepository(readContext, NullLogger<JobRepository>.Instance);

        var playerResult = await repository.GetDuePlayerJobsAsync(now, 100, Array.Empty<Guid>());
        var npcResult = await repository.GetDueNPCBuildingJobsAsync(now, 100, Array.Empty<Guid>());

        Assert.Single(playerResult);
        Assert.Equal(playerJob.Id, playerResult[0].Id);
        Assert.Equal(100, npcResult.Count);
        Assert.Equal(npcJobs.Take(100).Select(job => job.Id), npcResult.Select(job => job.Id));
    }
}
