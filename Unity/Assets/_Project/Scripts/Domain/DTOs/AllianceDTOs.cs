using System;

namespace Project.Scripts.Domain.DTOs
{
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
}