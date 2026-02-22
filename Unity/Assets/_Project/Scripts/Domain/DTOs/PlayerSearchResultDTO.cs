using System;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class PlayerSearchResultDTO
    {
        public Guid WorldPlayerId;
        public string Username;
    }
}