using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context; // Husk at bruge dit rigtige Context namespace
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class AllianceRepository : IAllianceRepository
    {
        private readonly GameContext _context;

        public AllianceRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<Alliance?> GetByIdAsync(Guid id)
        {
            return await _context.Alliances
                .Include(x => x.Members).ThenInclude(m => m.PlayerProfile)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAsync(Alliance alliance)
        {
            await _context.Alliances.AddAsync(alliance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Alliance alliance)
        {
            _context.Alliances.Update(alliance);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Alliance alliance)
        {
            _context.Alliances.Remove(alliance);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> NameExistsAsync(Guid worldId, string name)
        {
            string normalizedName = name.Trim().ToUpperInvariant();
            return await _context.Alliances
                .AnyAsync(alliance => alliance.WorldId == worldId && alliance.NormalizedName == normalizedName);
        }

        public async Task<Alliance?> GetByIdWithMembersAsync(Guid id)
        {
            return await _context.Alliances
                .Include(a => a.Members).ThenInclude(m => m.PlayerProfile)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public Task<List<AllianceInvitation>> GetInvitationsForPlayerAsync(Guid worldPlayerId, DateTime now) =>
            _context.AllianceInvitations
                .Include(i => i.Alliance)
                .Include(i => i.InvitedByWorldPlayer).ThenInclude(p => p.PlayerProfile)
                .Where(i => i.InvitedWorldPlayerId == worldPlayerId && i.ExpiresAt > now)
                .OrderByDescending(i => i.DateCreated)
                .ToListAsync();

        public Task<List<AllianceInvitation>> GetInvitationsForAllianceAsync(Guid allianceId, DateTime now) =>
            _context.AllianceInvitations
                .AsNoTracking()
                .Include(i => i.InvitedWorldPlayer).ThenInclude(p => p.PlayerProfile)
                .Include(i => i.InvitedByWorldPlayer).ThenInclude(p => p.PlayerProfile)
                .Where(i => i.AllianceId == allianceId && i.ExpiresAt > now)
                .OrderByDescending(i => i.DateCreated)
                .ToListAsync();

        public Task<AllianceInvitation?> GetInvitationByIdAsync(Guid invitationId) =>
            _context.AllianceInvitations.Include(i => i.Alliance).FirstOrDefaultAsync(i => i.Id == invitationId);

        public Task<bool> PendingInvitationExistsAsync(Guid allianceId, Guid worldPlayerId, DateTime now) =>
            _context.AllianceInvitations.AnyAsync(i => i.AllianceId == allianceId && i.InvitedWorldPlayerId == worldPlayerId && i.ExpiresAt > now);

        public async Task AddInvitationAsync(AllianceInvitation invitation)
        {
            await _context.AllianceInvitations.AddAsync(invitation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvitationAsync(AllianceInvitation invitation)
        {
            _context.AllianceInvitations.Remove(invitation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvitationsForPlayerAsync(Guid worldPlayerId)
        {
            var invitations = await _context.AllianceInvitations.Where(i => i.InvitedWorldPlayerId == worldPlayerId).ToListAsync();
            _context.AllianceInvitations.RemoveRange(invitations);
            await _context.SaveChangesAsync();
        }

        public Task<List<Alliance>> SearchAsync(Guid worldId, string query, int limit) =>
            _context.Alliances.Include(a => a.Members)
                .Where(a => a.WorldId == worldId && (a.Name.Contains(query) || a.Tag.Contains(query)))
                .OrderBy(a => a.Name).Take(limit).ToListAsync();

        public Task<List<AllianceRelation>> GetRelationsAsync(Guid allianceId) =>
            _context.AllianceRelations.Include(r => r.AllianceA).Include(r => r.AllianceB)
                .Where(r => r.AllianceIdA == allianceId || r.AllianceIdB == allianceId)
                .OrderByDescending(r => r.DateCreated).ToListAsync();

        public Task<AllianceRelation?> GetRelationByIdAsync(Guid relationId) =>
            _context.AllianceRelations.Include(r => r.AllianceA).Include(r => r.AllianceB)
                .FirstOrDefaultAsync(r => r.Id == relationId);

        public Task<List<AllianceRelation>> GetOpenRelationsBetweenAsync(Guid allianceIdA, Guid allianceIdB) =>
            _context.AllianceRelations.Include(r => r.AllianceA).Include(r => r.AllianceB)
                .Where(r => r.AllianceIdA == allianceIdA && r.AllianceIdB == allianceIdB &&
                    (r.Status == Domain.Enums.AllianceRelationStatusEnum.Pending || r.Status == Domain.Enums.AllianceRelationStatusEnum.Active))
                .ToListAsync();

        public Task<bool> AreAtWarAsync(Guid allianceIdA, Guid allianceIdB) =>
            _context.AllianceRelations.AnyAsync(relation =>
                relation.RelationType == Domain.Enums.AllianceRelationTypeEnum.War &&
                relation.Status == Domain.Enums.AllianceRelationStatusEnum.Active &&
                ((relation.AllianceIdA == allianceIdA && relation.AllianceIdB == allianceIdB) ||
                 (relation.AllianceIdA == allianceIdB && relation.AllianceIdB == allianceIdA)));

        public async Task AddRelationAsync(AllianceRelation relation)
        {
            await _context.AllianceRelations.AddAsync(relation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRelationsAsync(IEnumerable<AllianceRelation> relations)
        {
            _context.AllianceRelations.UpdateRange(relations);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRelationsForAllianceAsync(Guid allianceId)
        {
            var relations = await _context.AllianceRelations
                .Where(r => r.AllianceIdA == allianceId || r.AllianceIdB == allianceId).ToListAsync();
            _context.AllianceRelations.RemoveRange(relations);
            await _context.SaveChangesAsync();
        }
    }
}
