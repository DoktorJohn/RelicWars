using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IAllianceService
    {
        Task<AllianceDTO> GetAllianceInfo(Guid allianceId);
        Task<AllianceDTO> CreateAlliance(CreateAllianceDTO dto);
        Task<bool> DisbandAlliance(DisbandAllianceDTO dto);
        Task<bool> InviteToAlliance(InviteToAllianceDTO dto);
        Task<List<AllianceInvitationDTO>> GetInvitations(Guid worldPlayerId);
        Task<AllianceDTO> AcceptInvitation(RespondToAllianceInvitationDTO dto);
        Task<bool> DeclineInvitation(RespondToAllianceInvitationDTO dto);
        Task<bool> LeaveAlliance(LeaveAllianceDTO dto);
        Task<bool> KickPlayer(KickPlayerFromAllianceDTO dto);
        Task<AllianceDTO> SetMemberRole(SetAllianceMemberRoleDTO dto);
        Task<AllianceDTO> UpdateDescription(UpdateAllianceDescriptionDTO dto);
        Task<List<AllianceSearchResultDTO>> SearchAlliances(Guid worldId, string query);
        Task<AllianceGeopoliticsDTO> GetGeopolitics(Guid allianceId);
        Task<AllianceRelationDTO> SendPactInvite(SendPactInviteDTO dto);
        Task<AllianceRelationDTO> RespondToPactInvite(RespondToPactInviteDTO dto);
        Task<AllianceRelationDTO> DeclareWar(DeclareWarDTO dto);
    }
}
