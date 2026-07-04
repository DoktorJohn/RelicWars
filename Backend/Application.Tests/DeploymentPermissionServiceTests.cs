using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.User;

namespace Application.Tests;

public class DeploymentPermissionServiceTests
{
    private readonly DeploymentPermissionService _service = new(new TestAllianceRepository());

    [Fact]
    public void CanAttack_AllowsEnemyAndRejectsOwnAlliedOwnerlessNpcAndCrossWorldTargets()
    {
        var source = Player();
        var enemy = Player(source.WorldId);
        var ally = Player(source.WorldId);
        source.AllianceId = Guid.NewGuid();
        ally.AllianceId = source.AllianceId;

        Assert.True(_service.CanAttack(source, City(enemy)));
        Assert.False(_service.CanAttack(source, City(source)));
        Assert.False(_service.CanAttack(source, City(ally)));
        Assert.False(_service.CanAttack(source, OwnerlessCity(source.WorldId)));
        Assert.False(_service.CanAttack(source, City(enemy, isNpc: true)));
        Assert.False(_service.CanAttack(source, City(Player())));
    }

    [Fact]
    public async Task CanSupportAsync_AllowsOwnAlliedNeutralAndNonWarringTargets()
    {
        var source = Player();
        var ally = Player(source.WorldId);
        var neutral = Player(source.WorldId);
        var otherAlliance = Player(source.WorldId);
        source.AllianceId = Guid.NewGuid();
        ally.AllianceId = source.AllianceId;
        otherAlliance.AllianceId = Guid.NewGuid();

        Assert.True(await _service.CanSupportAsync(source, City(source)));
        Assert.True(await _service.CanSupportAsync(source, City(ally)));
        Assert.True(await _service.CanSupportAsync(source, City(neutral)));
        Assert.True(await _service.CanSupportAsync(source, City(otherAlliance)));
    }

    [Fact]
    public async Task CanSupportAsync_RejectsActiveWarOwnerlessNpcAndCrossWorldTargets()
    {
        var source = Player();
        var enemy = Player(source.WorldId);
        source.AllianceId = Guid.NewGuid();
        enemy.AllianceId = Guid.NewGuid();
        var activeWar = new AllianceRelation
        {
            AllianceIdA = source.AllianceId.Value,
            AllianceIdB = enemy.AllianceId.Value,
            RelationType = AllianceRelationTypeEnum.War,
            Status = AllianceRelationStatusEnum.Active
        };
        var service = new DeploymentPermissionService(new TestAllianceRepository([activeWar]));

        Assert.False(await service.CanSupportAsync(source, City(enemy)));
        Assert.False(await service.CanSupportAsync(source, OwnerlessCity(source.WorldId)));
        Assert.False(await service.CanSupportAsync(source, City(enemy, isNpc: true)));
        Assert.False(await service.CanSupportAsync(source, City(Player())));
    }

    private static WorldPlayer Player(Guid? worldId = null) => new()
    {
        Id = Guid.NewGuid(),
        WorldId = worldId ?? Guid.NewGuid(),
        PlayerProfileId = Guid.NewGuid(),
        PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Player" }
    };

    private static City City(WorldPlayer owner, bool isNpc = false) => new()
    {
        Id = Guid.NewGuid(),
        WorldId = owner.WorldId,
        WorldPlayerId = owner.Id,
        WorldPlayer = owner,
        IsNPC = isNpc
    };

    private static City OwnerlessCity(Guid worldId) => new()
    {
        Id = Guid.NewGuid(),
        WorldId = worldId
    };
}
