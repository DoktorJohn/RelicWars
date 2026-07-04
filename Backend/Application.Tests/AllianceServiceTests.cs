using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.User;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public class AllianceServiceTests
{
    [Fact]
    public async Task AcceptInvitationDoesNotDuplicateNewMemberInReturnedAlliance()
    {
        var alliance = new Alliance
        {
            Id = Guid.NewGuid(),
            Name = "Test Alliance",
            Tag = "TST",
            Members = new List<WorldPlayer>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AllianceId = Guid.Empty,
                    AllianceRole = AllianceRoleEnum.Founder,
                    PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Founder" }
                }
            }
        };
        alliance.Members[0].AllianceId = alliance.Id;

        var player = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "New Member" },
            WorldId = Guid.NewGuid()
        };

        var invitation = new AllianceInvitation
        {
            Id = Guid.NewGuid(),
            AllianceId = alliance.Id,
            Alliance = alliance,
            InvitedWorldPlayerId = player.Id,
            InvitedWorldPlayer = player,
            InvitedByWorldPlayerId = alliance.Members[0].Id,
            InvitedByWorldPlayer = alliance.Members[0],
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var repo = new MemoryAllianceRepository(alliance, invitation);
        var playerRepo = new MemoryWorldPlayerRepository(player, alliance);
        var service = new AllianceService(
            repo,
            playerRepo,
            new TestPlayerAccessService([player]),
            NullLogger<AllianceService>.Instance,
            new FixedRankingService(),
            new ImmediateTransactionManager());

        var result = await service.AcceptInvitation(new RespondToAllianceInvitationDTO(player.Id, invitation.Id));

        Assert.Equal(2, result.MemberCount);
        Assert.Single(result.Members, m => m.WorldPlayerId == player.Id);
        Assert.Single(repo.Alliance!.Members, m => m.Id == player.Id);
    }

    [Fact]
    public async Task GetInvitationsIncludesInviterId()
    {
        var founder = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            AllianceRole = AllianceRoleEnum.Founder,
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Founder" }
        };

        var alliance = new Alliance
        {
            Id = Guid.NewGuid(),
            Name = "Test Alliance",
            Tag = "TST",
            Members = new List<WorldPlayer> { founder }
        };
        founder.AllianceId = alliance.Id;

        var invited = new WorldPlayer
        {
            Id = Guid.NewGuid(),
            PlayerProfile = new PlayerProfile { Id = Guid.NewGuid(), UserName = "Invited" },
            WorldId = Guid.NewGuid()
        };

        var invitation = new AllianceInvitation
        {
            Id = Guid.NewGuid(),
            AllianceId = alliance.Id,
            Alliance = alliance,
            InvitedWorldPlayerId = invited.Id,
            InvitedWorldPlayer = invited,
            InvitedByWorldPlayerId = founder.Id,
            InvitedByWorldPlayer = founder,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var service = new AllianceService(
            new MemoryAllianceRepository(alliance, invitation),
            new MemoryWorldPlayerRepository(invited, alliance),
            new TestPlayerAccessService([invited]),
            NullLogger<AllianceService>.Instance,
            new FixedRankingService(),
            new ImmediateTransactionManager());

        var result = await service.GetInvitations(invited.Id);

        var dto = Assert.Single(result);
        Assert.Equal(founder.Id, dto.InvitedByWorldPlayerId);
    }

    private sealed class FixedRankingService : IRankingService
    {
        public Task<List<RankingEntryData>> GetRankings() => Task.FromResult(new List<RankingEntryData>());

        public Task<RankingEntryData?> GetRankingById(Guid worldPlayerId) =>
            Task.FromResult<RankingEntryData?>(null);
    }

    private sealed class MemoryAllianceRepository : IAllianceRepository
    {
        public Alliance? Alliance { get; }
        private readonly List<AllianceInvitation> _invitations = new();
        private readonly List<AllianceRelation> _relations = new();

        public MemoryAllianceRepository(Alliance alliance, AllianceInvitation invitation)
        {
            Alliance = alliance;
            _invitations.Add(invitation);
        }

        public Task<Alliance?> GetByIdAsync(Guid id) => Task.FromResult<Alliance?>(Alliance?.Id == id ? Alliance : null);
        public Task<Alliance?> GetByIdWithMembersAsync(Guid id) => Task.FromResult<Alliance?>(Alliance?.Id == id ? Alliance : null);
        public Task AddAsync(Alliance alliance) => Task.CompletedTask;
        public Task UpdateAsync(Alliance alliance) => Task.CompletedTask;
        public Task DeleteAsync(Alliance alliance) => Task.CompletedTask;
        public Task<bool> NameExistsAsync(Guid worldId, string name) => Task.FromResult(false);
        public Task<List<AllianceInvitation>> GetInvitationsForPlayerAsync(Guid worldPlayerId, DateTime now) =>
            Task.FromResult(_invitations.Where(i => i.InvitedWorldPlayerId == worldPlayerId && i.ExpiresAt > now).ToList());
        public Task<AllianceInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
            Task.FromResult(_invitations.SingleOrDefault(i => i.Id == invitationId));
        public Task<bool> PendingInvitationExistsAsync(Guid allianceId, Guid worldPlayerId, DateTime now) =>
            Task.FromResult(_invitations.Any(i => i.AllianceId == allianceId && i.InvitedWorldPlayerId == worldPlayerId && i.ExpiresAt > now));
        public Task AddInvitationAsync(AllianceInvitation invitation)
        {
            _invitations.Add(invitation);
            return Task.CompletedTask;
        }

        public Task DeleteInvitationAsync(AllianceInvitation invitation)
        {
            _invitations.Remove(invitation);
            return Task.CompletedTask;
        }

        public Task DeleteInvitationsForPlayerAsync(Guid worldPlayerId)
        {
            _invitations.RemoveAll(i => i.InvitedWorldPlayerId == worldPlayerId);
            return Task.CompletedTask;
        }
        public Task<List<Alliance>> SearchAsync(Guid worldId, string query, int limit) =>
            Task.FromResult(Alliance?.WorldId == worldId ? new List<Alliance> { Alliance } : new List<Alliance>());
        public Task<List<AllianceRelation>> GetRelationsAsync(Guid allianceId) =>
            Task.FromResult(_relations.Where(r => r.AllianceIdA == allianceId || r.AllianceIdB == allianceId).ToList());
        public Task<AllianceRelation?> GetRelationByIdAsync(Guid relationId) =>
            Task.FromResult(_relations.SingleOrDefault(r => r.Id == relationId));
        public Task<List<AllianceRelation>> GetOpenRelationsBetweenAsync(Guid allianceIdA, Guid allianceIdB) =>
            Task.FromResult(_relations.Where(r => r.AllianceIdA == allianceIdA && r.AllianceIdB == allianceIdB &&
                r.Status is AllianceRelationStatusEnum.Pending or AllianceRelationStatusEnum.Active).ToList());
        public Task<bool> AreAtWarAsync(Guid allianceIdA, Guid allianceIdB) =>
            Task.FromResult(_relations.Any(r => r.RelationType == AllianceRelationTypeEnum.War &&
                r.Status == AllianceRelationStatusEnum.Active &&
                ((r.AllianceIdA == allianceIdA && r.AllianceIdB == allianceIdB) ||
                 (r.AllianceIdA == allianceIdB && r.AllianceIdB == allianceIdA))));
        public Task AddRelationAsync(AllianceRelation relation)
        {
            _relations.Add(relation);
            return Task.CompletedTask;
        }
        public Task UpdateRelationsAsync(IEnumerable<AllianceRelation> relations) => Task.CompletedTask;
        public Task DeleteRelationsForAllianceAsync(Guid allianceId)
        {
            _relations.RemoveAll(r => r.AllianceIdA == allianceId || r.AllianceIdB == allianceId);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryWorldPlayerRepository : IWorldPlayerRepository
    {
        private readonly WorldPlayer _player;
        private readonly Alliance _alliance;

        public MemoryWorldPlayerRepository(WorldPlayer player, Alliance alliance)
        {
            _player = player;
            _alliance = alliance;
        }

        public Task<WorldPlayer?> GetByIdAsync(Guid id) => Task.FromResult<WorldPlayer?>(id == _player.Id ? _player : null);
        public Task<WorldPlayer?> GetByIdWithResearchAsync(Guid id) => GetByIdAsync(id);
        public Task AddAsync(WorldPlayer user) => Task.CompletedTask;
        public Task UpdateAsync(WorldPlayer user)
        {
            if (user.AllianceId == _alliance.Id && !_alliance.Members.Any(member => member.Id == user.Id))
                _alliance.Members.Add(user);
            return Task.CompletedTask;
        }
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<List<WorldPlayer>>? GetAllAsync() => Task.FromResult(new List<WorldPlayer> { _player });
        public Task<WorldPlayer?> GetByProfileAndWorldAsync(Guid profileId, Guid worldId) => Task.FromResult<WorldPlayer?>(null);
        public Task<List<WorldPlayer>> GetAllByAllianceIdAsync(Guid allianceId) =>
            Task.FromResult(allianceId == _alliance.Id ? _alliance.Members.ToList() : new List<WorldPlayer>());
        public Task<List<WorldPlayer>> SearchPlayersByUsernameAsync(Guid worldId, string usernameQuery) => Task.FromResult(new List<WorldPlayer>());
    }
}
