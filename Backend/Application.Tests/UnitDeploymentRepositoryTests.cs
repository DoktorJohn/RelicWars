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
    public async Task GetActiveDeploymentsByWorldPlayerIdAsync_ReturnsAllOwnedPhasesInDeterministicDisplayOrder()
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var worldId = Guid.NewGuid();
        var originAlliance = new Alliance { Id = Guid.NewGuid(), WorldId = worldId, Name = "Origin Alliance", Tag = "ORA" };
        var targetAlliance = new Alliance { Id = Guid.NewGuid(), WorldId = worldId, Name = "Target Alliance", Tag = "TAR" };
        var owner = Player(worldId, originAlliance.Id, "Owner");
        owner.Alliance = originAlliance;
        var other = Player(worldId, targetAlliance.Id, "Other");
        other.Alliance = targetAlliance;
        var origin = City(owner, "Origin", 0, 0);
        var target = City(other, "Target", 1, 0);
        DateTime now = DateTime.UtcNow;

        UnitDeployment Deployment(string name, UnitDeploymentPhaseEnum phase, DateTime arrival, DateTime? stationedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Mobility = 2,
            Type = phase == UnitDeploymentPhaseEnum.Stationed ? UnitDeploymentTypeEnum.Support : UnitDeploymentTypeEnum.Attack,
            Phase = phase,
            UnitDeploymentMovementStatus = phase == UnitDeploymentPhaseEnum.Stationed
                ? UnitDeploymentMovementStatusEnum.Stationed
                : UnitDeploymentMovementStatusEnum.Moving,
            DepartureTime = now.AddMinutes(-10),
            ArrivalTime = arrival,
            StationedAt = stationedAt,
            OriginCity = origin,
            OriginCityId = origin.Id,
            TargetCity = target,
            TargetCityId = target.Id,
            OwnerWorldPlayer = owner,
            WorldPlayerId = owner.Id,
            WorldId = worldId,
            UnitStacks = [new UnitStack { Id = Guid.NewGuid(), Type = UnitTypeEnum.Militia, Quantity = 2, WorldPlayerId = owner.Id }]
        };

        var returning = Deployment("Returning", UnitDeploymentPhaseEnum.Returning, now.AddMinutes(2));
        var outbound = Deployment("Outbound", UnitDeploymentPhaseEnum.Outbound, now.AddMinutes(5));
        var stationedNewest = Deployment("Stationed newest", UnitDeploymentPhaseEnum.Stationed, now.AddMinutes(-1), now.AddMinutes(-1));
        var stationedOlder = Deployment("Stationed older", UnitDeploymentPhaseEnum.Stationed, now.AddMinutes(-5), now.AddMinutes(-5));
        var otherDeployment = Deployment("Other owner", UnitDeploymentPhaseEnum.Outbound, now.AddMinutes(1));
        otherDeployment.WorldPlayerId = other.Id;
        otherDeployment.OwnerWorldPlayer = other;

        await using (var writeContext = new GameContext(options))
        {
            writeContext.UnitDeployments.AddRange(outbound, stationedOlder, otherDeployment, returning, stationedNewest);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new GameContext(options);
        var result = await new UnitDeploymentRepository(readContext)
            .GetActiveDeploymentsByWorldPlayerIdAsync(owner.Id);

        Assert.Equal([returning.Id, outbound.Id, stationedNewest.Id, stationedOlder.Id], result.Select(item => item.Id));
        Assert.All(result, item => Assert.Equal(owner.Id, item.WorldPlayerId));
        Assert.All(result, item => Assert.NotEmpty(item.UnitStacks));
        Assert.All(result, item => Assert.NotNull(item.OriginCity));
        Assert.All(result, item => Assert.NotNull(item.TargetCity));
        Assert.All(result, item => Assert.Equal("Origin", item.OriginCity.Name));
        Assert.All(result, item => Assert.Equal("Target", item.TargetCity!.Name));
        Assert.All(result, item => Assert.Equal("Owner", item.OriginCity.WorldPlayer!.PlayerProfile.UserName));
        Assert.All(result, item => Assert.Equal("Origin Alliance", item.OriginCity.WorldPlayer!.Alliance!.Name));
        Assert.All(result, item => Assert.Equal("ORA", item.OriginCity.WorldPlayer!.Alliance!.Tag));
        Assert.All(result, item => Assert.Equal("Other", item.TargetCity!.WorldPlayer!.PlayerProfile.UserName));
        Assert.All(result, item => Assert.Equal("Target Alliance", item.TargetCity!.WorldPlayer!.Alliance!.Name));
        Assert.All(result, item => Assert.Equal("TAR", item.TargetCity!.WorldPlayer!.Alliance!.Tag));
        Assert.All(result, item => Assert.Equal(2, Assert.Single(item.UnitStacks).Quantity));
    }

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
