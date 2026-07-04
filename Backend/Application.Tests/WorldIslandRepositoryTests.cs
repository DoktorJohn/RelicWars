using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class WorldIslandRepositoryTests
{
    [Fact]
    public async Task GetByCellAsync_IdentifiesIslandWithinWorld()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new GameContext(options);
        var world = new World { Id = Guid.NewGuid(), Name = "World" };
        var island = new WorldIsland
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            CellX = 4,
            CellY = -3,
            CenterX = 129,
            CenterY = -97
        };
        context.AddRange(world, island);
        await context.SaveChangesAsync();

        var result = await new WorldIslandRepository(context).GetByCellAsync(world.Id, 4, -3);

        Assert.NotNull(result);
        Assert.Equal(island.Id, result.Id);
        Assert.Null(await new WorldIslandRepository(context).GetByCellAsync(world.Id, 4, -2));
    }

    [Fact]
    public async Task GetByIdAsync_LoadsExoticResources()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new GameContext(options);
        var world = new World { Id = Guid.NewGuid(), Name = "World" };
        var island = new WorldIsland
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            CellX = 4,
            CellY = -3,
            CenterX = 129,
            CenterY = -97,
        };
        var resource = new WorldIslandExoticResource
        {
            Id = Guid.NewGuid(),
            WorldIslandId = island.Id,
            SlotIndex = 0,
            ResourceType = ExoticResourceTypeEnum.Cloth,
            Tier = 1
        };

        island.ExoticResources.Add(resource);
        context.AddRange(world, island);
        await context.SaveChangesAsync();

        var result = await new WorldIslandRepository(context).GetByIdAsync(island.Id);

        Assert.NotNull(result);
        Assert.Single(result!.ExoticResources);
        Assert.Equal(ExoticResourceTypeEnum.Cloth, result.ExoticResources[0].ResourceType);
    }
}
