using System;

using System.Collections.Generic;

namespace Project.Scripts.Domain.DTOs
{
    public enum AllianceRoleDTO { Founder, Leader, Member, None }
    public enum AllianceRelationTypeDTO { Pact, War }
    public enum AllianceRelationStatusDTO { Pending, Active, Declined, Ended, Expired }
    [Serializable]
    public class AllianceDTO
    {
        public Guid Id;
        public string Name;
        public string Tag;
        public string Description;
        public string BannerImageUrl;
        public long TotalPoints;
        public int MemberCount;
        public int MaxPlayers;
        public List<AllianceMemberDTO> Members;
    }

    [Serializable]
    public class AllianceMemberDTO { public Guid WorldPlayerId; public string UserName; public AllianceRoleDTO Role; public int TotalPoints; }

    [Serializable]
    public class AllianceInvitationDTO
    {
        public Guid Id; public Guid AllianceId; public string AllianceName; public string AllianceTag;
        public Guid InvitedByWorldPlayerId; public string InvitedByUserName; public DateTime ExpiresAt;
    }

    [Serializable]
    public class CreateAllianceDTO
    {
        public Guid WorldPlayerIdFounder;
        public string Name;
        public string Tag;
    }

    [Serializable]
    public class DisbandAllianceDTO
    {
        public Guid WorldPlayerId;
        public Guid AllianceId;
    }

    [Serializable]
    public class InviteToAllianceDTO
    {
        public Guid WorldPlayerIdInviter;
        public Guid WorldPlayerIdInvited;
    }

    [Serializable]
    public class KickPlayerFromAllianceDTO
    {
        public Guid WorldPlayerIdKicker;
        public Guid WorldPlayerIdKicked;
    }

    [Serializable]
    public class RespondToAllianceInvitationDTO { public Guid WorldPlayerId; public Guid InvitationId; }

    [Serializable]
    public class LeaveAllianceDTO { public Guid WorldPlayerId; }

    [Serializable]
    public class SetAllianceMemberRoleDTO { public Guid WorldPlayerIdActor; public Guid WorldPlayerIdTarget; public AllianceRoleDTO Role; }
    [Serializable]
    public class UpdateAllianceDescriptionDTO { public Guid WorldPlayerId; public Guid AllianceId; public string Description; }
    [Serializable]
    public class AllianceSearchResultDTO { public Guid Id; public string Name; public string Tag; public string Description; public int MemberCount; }
    [Serializable]
    public class AllianceRelationDTO
    {
        public Guid Id; public Guid OtherAllianceId; public string OtherAllianceName; public string OtherAllianceTag;
        public AllianceRelationTypeDTO RelationType; public AllianceRelationStatusDTO Status; public bool IsIncoming; public DateTime CreatedAt;
    }
    [Serializable]
    public class AllianceGeopoliticsDTO
    {
        public List<AllianceRelationDTO> ActivePacts; public List<AllianceRelationDTO> ActiveWars;
        public List<AllianceRelationDTO> IncomingPactInvites; public List<AllianceRelationDTO> OutgoingPactInvites;
    }
    [Serializable]
    public class SendPactInviteDTO { public Guid WorldPlayerId; public Guid AllianceId; public Guid TargetAllianceId; }
    [Serializable]
    public class RespondToPactInviteDTO { public Guid WorldPlayerId; public Guid RelationId; public bool Accept; }
    [Serializable]
    public class DeclareWarDTO { public Guid WorldPlayerId; public Guid AllianceId; public Guid TargetAllianceId; }
}
