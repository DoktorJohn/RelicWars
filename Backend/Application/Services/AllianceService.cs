using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class AllianceService : IAllianceService
    {
        private readonly IAllianceRepository _allianceRepo;
        private readonly IWorldPlayerRepository _playerRepo;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly IRankingService _rankingService;
        private readonly ILogger<AllianceService> _logger;
        private readonly ITransactionManager _transactionManager;

        public AllianceService(
            IAllianceRepository allianceRepo,
            IWorldPlayerRepository playerRepo,
            IPlayerAccessService playerAccessService,
            ILogger<AllianceService> logger,
            IRankingService rankingService,
            ITransactionManager transactionManager)
        {
            _allianceRepo = allianceRepo;
            _playerRepo = playerRepo;
            _playerAccessService = playerAccessService;
            _logger = logger;
            _rankingService = rankingService;
            _transactionManager = transactionManager;
        }

        public async Task<AllianceDTO> GetAllianceInfo(Guid allianceId)
        {
            var alliance = await _allianceRepo.GetByIdAsync(allianceId);
            if (alliance == null) throw new KeyNotFoundException("Alliance not found.");
            return await MapAllianceAsync(alliance);
        }

        public async Task<AllianceDTO> CreateAlliance(CreateAllianceDTO dto)
        {
            var founder = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerIdFounder);

            if (founder.AllianceId.HasValue)
            {
                throw new InvalidOperationException("Player is already in an alliance.");
            }

            var name = dto.Name.Trim();
            var normalizedName = name.ToUpperInvariant();
            var tag = dto.Tag.Trim().ToUpperInvariant();
            if (name.Length is < 3 or > 20) throw new ArgumentException("Alliance name must be between 3 and 20 characters.");
            if (tag.Length is < 3 or > 4) throw new ArgumentException("Alliance tag must be between 3 and 4 characters.");
            bool nameExists = await _allianceRepo.NameExistsAsync(founder.WorldId, name);
            if (nameExists) throw new InvalidOperationException("Alliance name is already in use in this world.");

            var newAlliance = new Alliance
            {
                Name = name,
                NormalizedName = normalizedName,
                Tag = tag,
                Description = string.Empty,
                WorldId = founder.WorldId,
                Members = new List<WorldPlayer> { founder }
            };

            try
            {
                await _transactionManager.ExecuteAsync(async () =>
                {
                    await _allianceRepo.AddAsync(newAlliance);
                    founder.AllianceId = newAlliance.Id;
                    founder.AllianceRole = AllianceRoleEnum.Founder;
                    await _playerRepo.UpdateAsync(founder);
                });
            }
            catch (DbUpdateException exception)
            {
                if (await _allianceRepo.NameExistsAsync(founder.WorldId, normalizedName))
                {
                    throw new InvalidOperationException("Alliance name is already in use in this world.", exception);
                }

                throw;
            }

            return await MapAllianceAsync(newAlliance);
        }

        public async Task<bool> DisbandAlliance(DisbandAllianceDTO dto)
        {
            var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerId);
            var alliance = await _allianceRepo.GetByIdWithMembersAsync(dto.AllianceId); // Vigtigt at inkludere medlemmer
            if (alliance == null) return false;

            // Rettighedstjek: Kun lederen kan opløse
            if (player.AllianceId != alliance.Id || player.AllianceRole != AllianceRoleEnum.Founder)
            {
                throw new Exception("Only the Alliance Leader can disband the alliance.");
            }

            // Fjern alliancen fra alle medlemmer
            // Bemærk: Dette kræver at vi har hentet medlemmerne. 
            // Hvis 'Members' kun er Guids, skal vi hente alle spillere med AllianceId == dto.AllianceId
            var members = await _playerRepo.GetAllByAllianceIdAsync(dto.AllianceId);

            await _transactionManager.ExecuteAsync(async () =>
            {
                foreach (var member in members)
                {
                    member.AllianceId = null;
                    member.AllianceRole = AllianceRoleEnum.None;
                    await _playerRepo.UpdateAsync(member);
                }

                await _allianceRepo.DeleteRelationsForAllianceAsync(alliance.Id);
                await _allianceRepo.DeleteAsync(alliance);
            });
            return true;
        }

        public async Task<bool> InviteToAlliance(InviteToAllianceDTO dto)
        {
            var inviter = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerIdInviter);
            var invited = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdInvited);
            if (invited == null) throw new KeyNotFoundException("Target player not found.");
            if (!inviter.AllianceId.HasValue || inviter.AllianceRole != AllianceRoleEnum.Founder)
                throw new UnauthorizedAccessException("Only an alliance founder can invite players.");
            if (invited.WorldId != inviter.WorldId) throw new ArgumentException("Players must be in the same world.");
            if (invited.AllianceId.HasValue) throw new InvalidOperationException("Target player is already in an alliance.");

            var alliance = await _allianceRepo.GetByIdWithMembersAsync(inviter.AllianceId.Value)
                ?? throw new KeyNotFoundException("Alliance not found.");
            if (alliance.Members.Count >= alliance.MaxPlayers) throw new InvalidOperationException("Alliance is full.");
            if (await _allianceRepo.PendingInvitationExistsAsync(alliance.Id, invited.Id, DateTime.UtcNow))
                throw new InvalidOperationException("A pending invitation already exists for this player.");

            await _allianceRepo.AddInvitationAsync(new AllianceInvitation
            {
                AllianceId = alliance.Id,
                InvitedWorldPlayerId = invited.Id,
                InvitedByWorldPlayerId = inviter.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            return true;
        }

        public async Task<bool> KickPlayer(KickPlayerFromAllianceDTO dto)
        {
            var kicker = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerIdKicker);
            var kicked = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdKicked);
            if (kicked == null) return false;

            // Er de i samme alliance?
            if (!kicker.AllianceId.HasValue || !kicked.AllianceId.HasValue || kicker.AllianceId != kicked.AllianceId)
                throw new Exception("Players are not in the same alliance.");

            // Rettighedstjek: Man skal være Officer+ for at kicke, og man kan ikke kicke en med højere/samme rank
            if (!CanManageMembers(kicker.AllianceRole)) throw new UnauthorizedAccessException("Insufficient permissions.");
            if (kicked.AllianceRole == AllianceRoleEnum.Founder ||
                kicker.AllianceRole == AllianceRoleEnum.Leader && kicked.AllianceRole != AllianceRoleEnum.Member)
                throw new UnauthorizedAccessException("Cannot kick a member with equal or higher rank.");

            // Udfør Kick
            var allianceId = kicked.AllianceId.Value;

            var alliance = await _allianceRepo.GetByIdAsync(allianceId);
            await _transactionManager.ExecuteAsync(async () =>
            {
                kicked.AllianceId = null;
                kicked.AllianceRole = AllianceRoleEnum.None;
                await _playerRepo.UpdateAsync(kicked);

                if (alliance != null && alliance.Members.Contains(kicked))
                {
                    alliance.Members.Remove(kicked);
                    await _allianceRepo.UpdateAsync(alliance);
                }
            });

            return true;
        }

        public async Task<List<AllianceInvitationDTO>> GetInvitations(Guid worldPlayerId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            var invitations = await _allianceRepo.GetInvitationsForPlayerAsync(worldPlayerId, DateTime.UtcNow);
            return invitations.Select(i => new AllianceInvitationDTO(
                i.Id, i.AllianceId, i.Alliance.Name, i.Alliance.Tag, i.InvitedByWorldPlayerId,
                i.InvitedByWorldPlayer.PlayerProfile.UserName ?? "Unknown", i.ExpiresAt)).ToList();
        }

        public async Task<List<AllianceInvitedPlayerDTO>> GetInvitedPlayers(Guid worldPlayerId)
        {
            var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            if (!player.AllianceId.HasValue)
                throw new InvalidOperationException("Player is not in an alliance.");

            var invitations = await _allianceRepo.GetInvitationsForAllianceAsync(
                player.AllianceId.Value,
                DateTime.UtcNow);
            return invitations.Select(invitation => new AllianceInvitedPlayerDTO(
                invitation.Id,
                invitation.InvitedWorldPlayerId,
                invitation.InvitedWorldPlayer.PlayerProfile.UserName ?? "Unknown",
                invitation.InvitedByWorldPlayerId,
                invitation.InvitedByWorldPlayer.PlayerProfile.UserName ?? "Unknown",
                invitation.ExpiresAt)).ToList();
        }

        public async Task<bool> CancelInvitation(CancelAllianceInvitationDTO dto)
        {
            var founder = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerId);
            if (!founder.AllianceId.HasValue || founder.AllianceRole != AllianceRoleEnum.Founder)
                throw new UnauthorizedAccessException("Only an alliance founder can cancel invitations.");

            var invitation = await _allianceRepo.GetInvitationByIdAsync(dto.InvitationId)
                ?? throw new KeyNotFoundException("Alliance invitation not found.");
            if (invitation.AllianceId != founder.AllianceId.Value)
                throw new UnauthorizedAccessException("Invitation does not belong to the founder's alliance.");

            await _allianceRepo.DeleteInvitationAsync(invitation);
            return true;
        }

        public async Task<AllianceDTO> AcceptInvitation(RespondToAllianceInvitationDTO dto)
        {
            var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerId);
            if (player.AllianceId.HasValue) throw new InvalidOperationException("Player is already in an alliance.");
            var invitation = await RequireInvitationAsync(dto, player);
            var alliance = await _allianceRepo.GetByIdWithMembersAsync(invitation.AllianceId)
                ?? throw new KeyNotFoundException("Alliance not found.");
            if (alliance.Members.Count >= alliance.MaxPlayers) throw new InvalidOperationException("Alliance is full.");

            await _transactionManager.ExecuteAsync(async () =>
            {
                player.AllianceId = alliance.Id;
                player.AllianceRole = AllianceRoleEnum.Member;
                await _playerRepo.UpdateAsync(player);
                await _allianceRepo.DeleteInvitationsForPlayerAsync(player.Id);
            });

            var updatedAlliance = await _allianceRepo.GetByIdWithMembersAsync(alliance.Id)
                ?? throw new KeyNotFoundException("Alliance not found.");
            return await MapAllianceAsync(updatedAlliance);
        }

        public async Task<bool> DeclineInvitation(RespondToAllianceInvitationDTO dto)
        {
            var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerId);
            var invitation = await RequireInvitationAsync(dto, player);
            await _allianceRepo.DeleteInvitationAsync(invitation);
            return true;
        }

        public async Task<bool> LeaveAlliance(LeaveAllianceDTO dto)
        {
            var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerId);
            if (!player.AllianceId.HasValue) throw new InvalidOperationException("Player is not in an alliance.");
            if (player.AllianceRole == AllianceRoleEnum.Founder)
                throw new InvalidOperationException("The founder must disband the alliance instead of leaving.");
            player.AllianceId = null;
            player.AllianceRole = AllianceRoleEnum.None;
            await _playerRepo.UpdateAsync(player);
            return true;
        }

        public async Task<AllianceDTO> SetMemberRole(SetAllianceMemberRoleDTO dto)
        {
            var actor = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerIdActor);
            var target = await _playerRepo.GetByIdAsync(dto.WorldPlayerIdTarget)
                ?? throw new KeyNotFoundException("Target player not found.");
            if (!actor.AllianceId.HasValue || actor.AllianceId != target.AllianceId || actor.WorldId != target.WorldId)
                throw new ArgumentException("Players must belong to the same alliance and world.");
            if (actor.AllianceRole != AllianceRoleEnum.Founder)
                throw new UnauthorizedAccessException("Only a founder can change member roles.");
            if (dto.Role is AllianceRoleEnum.None or AllianceRoleEnum.Founder)
                throw new ArgumentException("Members can only be assigned Leader or Member.");
            if (actor.Id == target.Id)
                throw new InvalidOperationException("A founder cannot change their own role.");
            if (target.AllianceRole == AllianceRoleEnum.Founder)
                throw new InvalidOperationException("Another founder cannot be demoted.");

            target.AllianceRole = dto.Role;
            await _playerRepo.UpdateAsync(target);
            var alliance = await _allianceRepo.GetByIdWithMembersAsync(actor.AllianceId.Value)
                ?? throw new KeyNotFoundException("Alliance not found.");
            return await MapAllianceAsync(alliance);
        }

        public async Task<AllianceDTO> UpdateDescription(UpdateAllianceDescriptionDTO dto)
        {
            var actor = await _playerAccessService.RequireOwnedWorldPlayerAsync(dto.WorldPlayerId);
            if (actor.AllianceId != dto.AllianceId || !CanManageMembers(actor.AllianceRole))
                throw new UnauthorizedAccessException("You do not have permission to edit the alliance description.");
            var description = dto.Description.Trim();
            if (description.Length > 500)
                throw new ArgumentException("Alliance description cannot exceed 500 characters.");
            var alliance = await _allianceRepo.GetByIdWithMembersAsync(dto.AllianceId)
                ?? throw new KeyNotFoundException("Alliance not found.");
            if (alliance.WorldId != actor.WorldId) throw new ArgumentException("Alliance belongs to another world.");
            alliance.Description = description;
            alliance.DateLastModified = DateTime.UtcNow;
            await _allianceRepo.UpdateAsync(alliance);
            return await MapAllianceAsync(alliance);
        }

        public async Task<List<AllianceSearchResultDTO>> SearchAlliances(Guid worldId, string query)
        {
            query = query.Trim();
            if (query.Length < 2) throw new ArgumentException("Search query must contain at least 2 characters.");
            var alliances = await _allianceRepo.SearchAsync(worldId, query, 20);
            return alliances.Select(a => new AllianceSearchResultDTO(a.Id, a.Name, a.Tag, a.Description, a.Members.Count)).ToList();
        }

        public async Task<AllianceGeopoliticsDTO> GetGeopolitics(Guid allianceId)
        {
            var alliance = await _allianceRepo.GetByIdAsync(allianceId)
                ?? throw new KeyNotFoundException("Alliance not found.");
            var relations = await _allianceRepo.GetRelationsAsync(allianceId);
            var pointsByAlliance = (await _rankingService.GetRankings())
                .Where(entry => entry.AllianceId.HasValue)
                .GroupBy(entry => entry.AllianceId!.Value)
                .ToDictionary(group => group.Key, group => group.Sum(entry => (long)entry.TotalPoints));
            AllianceRelationDTO Map(AllianceRelation relation) => MapRelation(relation, alliance.Id, pointsByAlliance);
            return new AllianceGeopoliticsDTO(
                relations.Where(r => r.RelationType == AllianceRelationTypeEnum.Pact && r.Status == AllianceRelationStatusEnum.Active).Select(Map).ToList(),
                relations.Where(r => r.RelationType == AllianceRelationTypeEnum.War && r.Status == AllianceRelationStatusEnum.Active).Select(Map).ToList(),
                relations.Where(r => r.RelationType == AllianceRelationTypeEnum.Pact && r.Status == AllianceRelationStatusEnum.Pending && r.RespondingAllianceId == allianceId).Select(Map).ToList(),
                relations.Where(r => r.RelationType == AllianceRelationTypeEnum.Pact && r.Status == AllianceRelationStatusEnum.Pending && r.InitiatorAllianceId == allianceId).Select(Map).ToList());
        }

        public async Task<AllianceRelationDTO> SendPactInvite(SendPactInviteDTO dto)
        {
            var actor = await RequireDiplomacyActorAsync(dto.WorldPlayerId, dto.AllianceId);
            var target = await RequireTargetAllianceAsync(actor, dto.TargetAllianceId);
            var (allianceA, allianceB) = CanonicalPair(dto.AllianceId, target.Id);
            var existing = await _allianceRepo.GetOpenRelationsBetweenAsync(allianceA, allianceB);
            if (existing.Any(r => r.RelationType == AllianceRelationTypeEnum.War && r.Status == AllianceRelationStatusEnum.Active))
                throw new InvalidOperationException("A pact cannot be proposed during an active war.");
            if (existing.Any(r => r.RelationType == AllianceRelationTypeEnum.Pact))
                throw new InvalidOperationException("An active or pending pact already exists.");

            var relation = new AllianceRelation
            {
                WorldId = actor.WorldId,
                AllianceIdA = allianceA,
                AllianceIdB = allianceB,
                RelationType = AllianceRelationTypeEnum.Pact,
                Status = AllianceRelationStatusEnum.Pending,
                InitiatorAllianceId = dto.AllianceId,
                RespondingAllianceId = target.Id
            };
            await _allianceRepo.AddRelationAsync(relation);
            relation.AllianceA = dto.AllianceId == allianceA ? await RequireAllianceAsync(dto.AllianceId) : target;
            relation.AllianceB = dto.AllianceId == allianceB ? await RequireAllianceAsync(dto.AllianceId) : target;
            return await MapRelationAsync(relation, dto.AllianceId);
        }

        public async Task<AllianceRelationDTO> RespondToPactInvite(RespondToPactInviteDTO dto)
        {
            var relation = await _allianceRepo.GetRelationByIdAsync(dto.RelationId)
                ?? throw new KeyNotFoundException("Pact invitation not found.");
            if (relation.RelationType != AllianceRelationTypeEnum.Pact || relation.Status != AllianceRelationStatusEnum.Pending)
                throw new InvalidOperationException("Pact invitation is no longer pending.");
            var actor = await RequireDiplomacyActorAsync(dto.WorldPlayerId, relation.RespondingAllianceId);
            if (actor.WorldId != relation.WorldId) throw new ArgumentException("Relation belongs to another world.");
            relation.Status = dto.Accept ? AllianceRelationStatusEnum.Active : AllianceRelationStatusEnum.Declined;
            relation.RespondedAt = DateTime.UtcNow;
            relation.DateLastModified = DateTime.UtcNow;
            await _allianceRepo.UpdateRelationsAsync(new[] { relation });
            return await MapRelationAsync(relation, relation.RespondingAllianceId);
        }

        public async Task<AllianceRelationDTO> DeclareWar(DeclareWarDTO dto)
        {
            var actor = await RequireDiplomacyActorAsync(dto.WorldPlayerId, dto.AllianceId);
            var target = await RequireTargetAllianceAsync(actor, dto.TargetAllianceId);
            var (allianceA, allianceB) = CanonicalPair(dto.AllianceId, target.Id);
            var existing = await _allianceRepo.GetOpenRelationsBetweenAsync(allianceA, allianceB);
            if (existing.Any(r => r.RelationType == AllianceRelationTypeEnum.War && r.Status == AllianceRelationStatusEnum.Active))
                throw new InvalidOperationException("The alliances are already at war.");

            var now = DateTime.UtcNow;
            foreach (var pact in existing.Where(r => r.RelationType == AllianceRelationTypeEnum.Pact))
            {
                pact.Status = AllianceRelationStatusEnum.Ended;
                pact.RespondedAt = now;
                pact.DateLastModified = now;
            }
            var war = new AllianceRelation
            {
                WorldId = actor.WorldId,
                AllianceIdA = allianceA,
                AllianceIdB = allianceB,
                RelationType = AllianceRelationTypeEnum.War,
                Status = AllianceRelationStatusEnum.Active,
                InitiatorAllianceId = dto.AllianceId,
                RespondingAllianceId = target.Id,
                RespondedAt = now
            };
            await _transactionManager.ExecuteAsync(async () =>
            {
                if (existing.Count > 0) await _allianceRepo.UpdateRelationsAsync(existing);
                await _allianceRepo.AddRelationAsync(war);
            });
            war.AllianceA = dto.AllianceId == allianceA ? await RequireAllianceAsync(dto.AllianceId) : target;
            war.AllianceB = dto.AllianceId == allianceB ? await RequireAllianceAsync(dto.AllianceId) : target;
            return await MapRelationAsync(war, dto.AllianceId);
        }

        private async Task<WorldPlayer> RequireDiplomacyActorAsync(Guid worldPlayerId, Guid allianceId)
        {
            var actor = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            if (actor.AllianceId != allianceId || !CanManageMembers(actor.AllianceRole))
                throw new UnauthorizedAccessException("You do not have permission to manage alliance diplomacy.");
            return actor;
        }

        private async Task<Alliance> RequireTargetAllianceAsync(WorldPlayer actor, Guid targetAllianceId)
        {
            if (actor.AllianceId == targetAllianceId) throw new ArgumentException("An alliance cannot target itself.");
            var target = await RequireAllianceAsync(targetAllianceId);
            if (target.WorldId != actor.WorldId) throw new ArgumentException("Alliances must be in the same world.");
            return target;
        }

        private async Task<Alliance> RequireAllianceAsync(Guid allianceId) =>
            await _allianceRepo.GetByIdAsync(allianceId) ?? throw new KeyNotFoundException("Alliance not found.");

        private static (Guid A, Guid B) CanonicalPair(Guid first, Guid second) =>
            first.CompareTo(second) < 0 ? (first, second) : (second, first);

        private async Task<AllianceRelationDTO> MapRelationAsync(AllianceRelation relation, Guid viewerAllianceId)
        {
            var pointsByAlliance = (await _rankingService.GetRankings())
                .Where(entry => entry.AllianceId.HasValue)
                .GroupBy(entry => entry.AllianceId!.Value)
                .ToDictionary(group => group.Key, group => group.Sum(entry => (long)entry.TotalPoints));
            return MapRelation(relation, viewerAllianceId, pointsByAlliance);
        }

        private static AllianceRelationDTO MapRelation(
            AllianceRelation relation,
            Guid viewerAllianceId,
            IReadOnlyDictionary<Guid, long> pointsByAlliance)
        {
            var other = relation.AllianceIdA == viewerAllianceId ? relation.AllianceB : relation.AllianceA;
            return new AllianceRelationDTO(relation.Id, other.Id, other.Name, other.Tag,
                pointsByAlliance.GetValueOrDefault(other.Id), relation.RelationType, relation.Status,
                relation.RespondingAllianceId == viewerAllianceId, relation.DateCreated);
        }

        private async Task<AllianceInvitation> RequireInvitationAsync(RespondToAllianceInvitationDTO dto, WorldPlayer player)
        {
            var invitation = await _allianceRepo.GetInvitationByIdAsync(dto.InvitationId)
                ?? throw new KeyNotFoundException("Alliance invitation not found.");
            if (invitation.InvitedWorldPlayerId != player.Id) throw new UnauthorizedAccessException("This invitation belongs to another player.");
            if (invitation.ExpiresAt <= DateTime.UtcNow) throw new InvalidOperationException("Alliance invitation has expired.");
            return invitation;
        }

        private async Task<AllianceDTO> MapAllianceAsync(Alliance alliance)
        {
            var rankings = await _rankingService.GetRankings();
            var pointsByPlayer = rankings.ToDictionary(r => r.WorldPlayerId, r => (int)r.TotalPoints);
            var members = alliance.Members
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .OrderBy(m => m.AllianceRole)
                .ThenBy(m => m.PlayerProfile?.UserName)
                .Select(m => new AllianceMemberDTO(m.Id, m.PlayerProfile?.UserName ?? "Unknown", m.AllianceRole,
                    pointsByPlayer.GetValueOrDefault(m.Id), m.Cities?.Count ?? 0))
                .ToList();
            return new AllianceDTO(alliance.Id, alliance.Name, alliance.Tag, alliance.Description, alliance.BannerImageUrl,
                members.Sum(m => (long)m.TotalPoints), members.Count, alliance.MaxPlayers, members);
        }

        private static bool CanManageMembers(AllianceRoleEnum role) =>
            role == AllianceRoleEnum.Founder || role == AllianceRoleEnum.Leader;
    }
}
