using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.IServices
{
    public interface IMessagingService
    {
        Task<ConversationDTO> StartConversationAsync(Guid senderId, IEnumerable<Guid> participantIds, string subject, string content);
        Task<MessageDTO> ReplyToConversationAsync(Guid requestorId, Guid conversationId, string content);
        Task<List<ConversationDTO>> GetConversationsAsync(Guid worldPlayerId);
        Task<List<MessageDTO>> GetMessagesAsync(Guid conversationId, Guid requestorId, DateTime? before, int take);
        Task MarkConversationAsReadAsync(Guid conversationId, Guid requestorId);
        Task MarkMessageAsReadAsync(Guid messageId, Guid requestorId);
        Task DeleteConversationAsync(Guid conversationId, Guid requestorId);
        Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query);
        Task<bool> HasUnreadMessagesAsync(Guid worldPlayerId);
        Task<int> CountUnreadMessagesAsync(Guid worldPlayerId);
    }
}
