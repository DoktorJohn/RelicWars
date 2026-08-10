using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.DTOs
{
    public record AllianceDTO(
    Guid Id,
    string Name,
    string Tag,
    string Description,
    string BannerImageUrl,
    long TotalPoints,
    int MemberCount,
    int MaxPlayers,
    List<AllianceMemberDTO> Members
);

    public record AllianceMemberDTO(Guid WorldPlayerId, string UserName, AllianceRoleEnum Role, int TotalPoints);
    public record AllianceInvitationDTO(Guid Id, Guid AllianceId, string AllianceName, string AllianceTag,
        Guid InvitedByWorldPlayerId, string InvitedByUserName, DateTime ExpiresAt);
    public record AllianceInvitedPlayerDTO(Guid InvitationId, Guid WorldPlayerId, string UserName,
        Guid InvitedByWorldPlayerId, string InvitedByUserName, DateTime ExpiresAt);
    public record InviteToAllianceDTO(Guid WorldPlayerIdInviter, Guid WorldPlayerIdInvited);
    public record RespondToAllianceInvitationDTO(Guid WorldPlayerId, Guid InvitationId);
    public record CancelAllianceInvitationDTO(Guid WorldPlayerId, Guid InvitationId);
    public record LeaveAllianceDTO(Guid WorldPlayerId);
    public record DisbandAllianceDTO(Guid WorldPlayerId, Guid AllianceId);
    public record CreateAllianceDTO(Guid WorldPlayerIdFounder, string Name, string Tag);
    public record KickPlayerFromAllianceDTO(Guid WorldPlayerIdKicker, Guid WorldPlayerIdKicked);
    public record SetAllianceMemberRoleDTO(Guid WorldPlayerIdActor, Guid WorldPlayerIdTarget, AllianceRoleEnum Role);
    public record UpdateAllianceDescriptionDTO(Guid WorldPlayerId, Guid AllianceId, string Description);
    public record AllianceSearchResultDTO(Guid Id, string Name, string Tag, string Description, int MemberCount);
    public record AllianceRelationDTO(Guid Id, Guid OtherAllianceId, string OtherAllianceName, string OtherAllianceTag,
        AllianceRelationTypeEnum RelationType, AllianceRelationStatusEnum Status, bool IsIncoming, DateTime CreatedAt);
    public record AllianceGeopoliticsDTO(List<AllianceRelationDTO> ActivePacts, List<AllianceRelationDTO> ActiveWars,
        List<AllianceRelationDTO> IncomingPactInvites, List<AllianceRelationDTO> OutgoingPactInvites);
    public record SendPactInviteDTO(Guid WorldPlayerId, Guid AllianceId, Guid TargetAllianceId);
    public record RespondToPactInviteDTO(Guid WorldPlayerId, Guid RelationId, bool Accept);
    public record DeclareWarDTO(Guid WorldPlayerId, Guid AllianceId, Guid TargetAllianceId);
}
