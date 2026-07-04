using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class UnitDeploymentRepositoryTests
{
    [Fact]
    public async Task GetDueMovementsAsync_LoadsCompleteCombatContext()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var worldId = Guid.NewGuid();
        var allianceId = Guid.NewGuid();
        var attacker = Player(worldId, null, "Attacker");
        var defender = Player(worldId, allianceId, "Defender");
        var origin = City(attacker, "Origin", 0, 0);
        var target = City(defender, "Target", 2, 0);
        target.UnitStacks.Add(new UnitStack
        {
            Id = Guid.NewGuid(),
            Type = UnitTypeEnum.Militia,
            Quantity = 12,
            WorldPlayerId = defender.Id,
            CityId = target.Id
        });
        target.ActiveFocuses.Add(new IdeologyFocus
        {
            Id = Guid.NewGuid(),
            Name = IdeologyFocusNameEnum.RoyalMedics,
            CityId = target.Id,
            TimeOfIdeologyStarted = DateTime.UtcNow.AddMinutes(-1)
        });

        var deployment = new UnitDeployment
        {
            Id = Guid.NewGuid(),
            Name = "Attack",
            Mobility = 2,
            Type = UnitDeploymentTypeEnum.Attack,
            UnitDeploymentMovementStatus = UnitDeploymentMovementStatusEnum.Moving,
            DepartureTime = DateTime.UtcNow.AddMinutes(-5),
            ArrivalTime = DateTime.UtcNow.AddMinutes(-1),
            OriginCity = origin,
            OriginCityId = origin.Id,
            TargetCity = target,
            TargetCityId = target.Id,
            OwnerWorldPlayer = attacker,
            WorldPlayerId = attacker.Id,
            WorldId = worldId,
            UnitStacks =
            [
                new UnitStack
                {
                    Id = Guid.NewGuid(),
                    Type = UnitTypeEnum.Militia,
                    Quantity = 5,
                    WorldPlayerId = attacker.Id
                }
            ]
        };

        await using (var writeContext = new GameContext(options))
        {
            writeContext.UnitDeployments.Add(deployment);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new GameContext(options);
        var repository = new UnitDeploymentRepository(readContext);

        var result = Assert.Single(await repository.GetDueMovementsAsync(DateTime.UtcNow, 100));

        Assert.Single(result.UnitStacks);
        Assert.NotNull(result.OwnerWorldPlayer);
        Assert.NotNull(result.OriginCity.WorldPlayer);
        Assert.NotNull(result.TargetCity);
        Assert.Single(result.TargetCity.UnitStacks);
        Assert.Single(result.TargetCity.ActiveFocuses);
        Assert.Equal(defender.Id, result.TargetCity.WorldPlayer?.Id);
        Assert.Equal(allianceId, result.TargetCity.WorldPlayer?.AllianceId);
    }

    private static WorldPlayer Player(Guid worldId, Guid? allianceId, string name)
    {
        var profile = new PlayerProfile { Id = Guid.NewGuid(), UserName = name };
        return new WorldPlayer
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            AllianceId = allianceId,
            PlayerProfileId = profile.Id,
            PlayerProfile = profile
        };
    }

    private static City City(WorldPlayer owner, string name, int x, int y) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        WorldId = owner.WorldId,
        WorldPlayerId = owner.Id,
        WorldPlayer = owner,
        X = x,
        Y = y
    };
}
