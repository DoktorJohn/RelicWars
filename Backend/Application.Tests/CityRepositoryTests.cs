using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class CityRepositoryTests
{
    [Fact]
    public async Task GetForJobProcessingAsync_LoadsOriginDeploymentStacksForEveryPlayerCity()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var worldId = Guid.NewGuid();
        var profile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Player" };
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            PlayerProfile = profile,
            PlayerProfileId = profile.Id,
            Cities = []
        };
        var processedCity = City(player, "Processed");
        var deploymentOrigin = City(player, "Deployment origin");
        player.Cities.AddRange([processedCity, deploymentOrigin]);
        deploymentOrigin.OriginUnitDeployments.Add(new UnitDeployment
        {
            Id = Guid.NewGuid(),
            OriginCity = deploymentOrigin,
            OriginCityId = deploymentOrigin.Id,
            OwnerWorldPlayer = player,
            WorldPlayerId = player.Id,
            WorldId = worldId,
            Phase = UnitDeploymentPhaseEnum.Outbound,
            UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving,
            UnitStacks =
            [
                new UnitStack
                {
                    Id = Guid.NewGuid(),
                    Type = UnitTypeEnum.Militia,
                    Quantity = 4,
                    WorldPlayerId = player.Id
                }
            ]
        });

        await using (var writeContext = new GameContext(options))
        {
            writeContext.WorldPlayers.Add(player);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new GameContext(options);
        var result = await new CityRepository(readContext)
            .GetForJobProcessingAsync(processedCity.Id, includeWorldPlayer: true);

        Assert.NotNull(result?.WorldPlayer);
        var loadedOrigin = Assert.Single(result.WorldPlayer.Cities, city => city.Id == deploymentOrigin.Id);
        var loadedDeployment = Assert.Single(loadedOrigin.OriginUnitDeployments);
        Assert.Equal(4, Assert.Single(loadedDeployment.UnitStacks).Quantity);
    }

    private static City City(WorldPlayer player, string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        WorldId = player.WorldId,
        WorldPlayer = player,
        WorldPlayerId = player.Id,
        Buildings = [],
        UnitStacks = [],
        OriginUnitDeployments = []
    };
}
