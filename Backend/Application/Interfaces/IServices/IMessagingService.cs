using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IMessagingService
    {
        Task<MessageDTO> SendMessageAsync(Guid senderId, Guid receiverId, string content, string? subject = null, Guid? conversationId = null);
        Task<List<ConversationDTO>> GetConversationsAsync(Guid worldPlayerId);
        Task<List<MessageDTO>> GetMessagesAsync(Guid conversationId, Guid requestorId);
        Task MarkMessageAsReadAsync(Guid messageId, Guid requestorId);
        Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query);
        Task<bool> HasUnreadMessagesAsync(Guid worldPlayerId);
    }
}
