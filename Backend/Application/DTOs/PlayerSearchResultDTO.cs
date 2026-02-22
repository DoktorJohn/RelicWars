using System;

namespace Application.DTOs
{
    public class PlayerSearchResultDTO
    {
        public Guid WorldPlayerId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}
