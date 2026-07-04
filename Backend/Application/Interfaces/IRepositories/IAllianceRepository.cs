using Domain.Entities;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IAllianceRepository
    {
        Task<Alliance?> GetByIdAsync(Guid id);
        Task AddAsync(Alliance alliance);
        Task UpdateAsync(Alliance alliance);
        Task DeleteAsync(Alliance alliance);

        Task<bool> NameExistsAsync(Guid worldId, string name);
        Task<Alliance?> GetByIdWithMembersAsync(Guid id);
        Task<List<AllianceInvitation>> GetInvitationsForPlayerAsync(Guid worldPlayerId, DateTime now);
        Task<AllianceInvitation?> GetInvitationByIdAsync(Guid invitationId);
        Task<bool> PendingInvitationExistsAsync(Guid allianceId, Guid worldPlayerId, DateTime now);
        Task AddInvitationAsync(AllianceInvitation invitation);
        Task DeleteInvitationAsync(AllianceInvitation invitation);
        Task DeleteInvitationsForPlayerAsync(Guid worldPlayerId);
        Task<List<Alliance>> SearchAsync(Guid worldId, string query, int limit);
        Task<List<AllianceRelation>> GetRelationsAsync(Guid allianceId);
        Task<AllianceRelation?> GetRelationByIdAsync(Guid relationId);
        Task<List<AllianceRelation>> GetOpenRelationsBetweenAsync(Guid allianceIdA, Guid allianceIdB);
        Task<bool> AreAtWarAsync(Guid allianceIdA, Guid allianceIdB);
        Task AddRelationAsync(AllianceRelation relation);
        Task UpdateRelationsAsync(IEnumerable<AllianceRelation> relations);
        Task DeleteRelationsForAllianceAsync(Guid allianceId);
    }
}
