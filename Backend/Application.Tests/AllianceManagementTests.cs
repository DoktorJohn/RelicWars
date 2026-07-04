using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class AllianceManagementTests
{
    [Fact]
    public async Task FounderCanPromoteMemberButLeaderCannotChangeRoles()
    {
        var setup = CreateSetup();
        var result = await setup.Service.SetMemberRole(new SetAllianceMemberRoleDTO(setup.Founder.Id, setup.Member.Id, AllianceRoleEnum.Leader));

        Assert.Equal(AllianceRoleEnum.Leader, result.Members.Single(m => m.WorldPlayerId == setup.Member.Id).Role);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => setup.Service.SetMemberRole(
            new SetAllianceMemberRoleDTO(setup.Member.Id, setup.Founder.Id, AllianceRoleEnum.Member)));
    }

    [Fact]
    public async Task MemberCannotChangeAllianceRoles()
    {
        var setup = CreateSetup();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => setup.Service.SetMemberRole(
            new SetAllianceMemberRoleDTO(setup.Member.Id, setup.Founder.Id, AllianceRoleEnum.Leader)));
    }

    [Fact]
    public async Task LeaderCanUpdateDescriptionAndDescriptionIsValidated()
    {
        var setup = CreateSetup(memberRole: AllianceRoleEnum.Leader);
        var result = await setup.Service.UpdateDescription(new UpdateAllianceDescriptionDTO(setup.Member.Id, setup.Alliance.Id, "A deliberate description"));

        Assert.Equal("A deliberate description", result.Description);
        await Assert.ThrowsAsync<ArgumentException>(() => setup.Service.UpdateDescription(
            new UpdateAllianceDescriptionDTO(setup.Member.Id, setup.Alliance.Id, new string('x', 501))));
    }

    [Fact]
    public async Task LeaderCanClearDescription()
    {
        var setup = CreateSetup(memberRole: AllianceRoleEnum.Leader);

        var result = await setup.Service.UpdateDescription(
            new UpdateAllianceDescriptionDTO(setup.Member.Id, setup.Alliance.Id, "   "));

        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public async Task NewAllianceStartsWithoutDescription()
    {
        var setup = CreateSetup();
        setup.Founder.AllianceId = null;
        setup.Founder.Alliance = null;
        setup.Founder.AllianceRole = AllianceRoleEnum.None;
        await setup.Context.SaveChangesAsync();

        var result = await setup.Service.CreateAlliance(
            new CreateAllianceDTO(setup.Founder.Id, "Fresh Alliance", "NEW"));

        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public async Task DeclaringWarEndsPactAndCreatesActiveWar()
    {
        var setup = CreateSetup();
        var target = AddAlliance(setup.Context, setup.World, "Target", "TRG");
        var pact = new AllianceRelation
        {
            Id = Guid.NewGuid(), WorldId = setup.World.Id, AllianceIdA = Min(setup.Alliance.Id, target.Id),
            AllianceIdB = Max(setup.Alliance.Id, target.Id), RelationType = AllianceRelationTypeEnum.Pact,
            Status = AllianceRelationStatusEnum.Active, InitiatorAllianceId = setup.Alliance.Id, RespondingAllianceId = target.Id
        };
        setup.Context.AllianceRelations.Add(pact);
        await setup.Context.SaveChangesAsync();

        var result = await setup.Service.DeclareWar(new DeclareWarDTO(setup.Founder.Id, setup.Alliance.Id, target.Id));

        Assert.Equal(AllianceRelationTypeEnum.War, result.RelationType);
        Assert.Equal(AllianceRelationStatusEnum.Active, result.Status);
        Assert.Equal(AllianceRelationStatusEnum.Ended, (await setup.Context.AllianceRelations.FindAsync(pact.Id))!.Status);
    }

    private static TestSetup CreateSetup(AllianceRoleEnum memberRole = AllianceRoleEnum.Member)
    {
        var options = new DbContextOptionsBuilder<GameContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new GameContext(options);
        var world = new World { Id = Guid.NewGuid(), Name = "World" };
        var alliance = AddAlliance(context, world, "Alliance", "ALL");
        var founder = AddPlayer(context, world, alliance, "Founder", AllianceRoleEnum.Founder);
        var member = AddPlayer(context, world, alliance, "Member", memberRole);
        context.SaveChanges();

        var service = new AllianceService(new AllianceRepository(context), new WorldPlayerRepository(context),
            new TestPlayerAccessService([founder, member]), NullLogger<AllianceService>.Instance,
            new EmptyRankingService(), new ImmediateTransactionManager());
        return new TestSetup(context, service, world, alliance, founder, member);
    }

    private static Alliance AddAlliance(GameContext context, World world, string name, string tag)
    {
        if (context.Entry(world).State == EntityState.Detached) context.World.Add(world);
        var alliance = new Alliance { Id = Guid.NewGuid(), WorldId = world.Id, World = world, Name = name, Tag = tag, Description = "Description" };
        context.Alliances.Add(alliance);
        return alliance;
    }

    private static WorldPlayer AddPlayer(GameContext context, World world, Alliance alliance, string name, AllianceRoleEnum role)
    {
        var profile = new PlayerProfile { Id = Guid.NewGuid(), UserName = name };
        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(), WorldId = world.Id, World = world, AllianceId = alliance.Id, Alliance = alliance,
            AllianceRole = role, PlayerProfileId = profile.Id, PlayerProfile = profile
        };
        context.WorldPlayers.Add(player);
        return player;
    }

    private static Guid Min(Guid a, Guid b) => a.CompareTo(b) < 0 ? a : b;
    private static Guid Max(Guid a, Guid b) => a.CompareTo(b) > 0 ? a : b;

    private sealed class EmptyRankingService : IRankingService
    {
        public Task<List<RankingEntryData>> GetRankings() => Task.FromResult(new List<RankingEntryData>());
        public Task<RankingEntryData?> GetRankingById(Guid worldPlayerId) => Task.FromResult<RankingEntryData?>(null);
    }

    private sealed record TestSetup(GameContext Context, AllianceService Service, World World, Alliance Alliance,
        WorldPlayer Founder, WorldPlayer Member);
}
