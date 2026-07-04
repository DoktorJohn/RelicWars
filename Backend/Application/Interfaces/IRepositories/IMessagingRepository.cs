using Domain.Entities;
using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IMessagingRepository
    {
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
        Task<Conversation?> GetConversationForAccessAsync(Guid conversationId);
        Task<List<Message>> GetMessagesForConversationAsync(Guid conversationId, DateTime? before, int take);
        Task<List<ConversationDTO>> GetConversationSummariesForPlayerAsync(Guid worldPlayerId);
        Task AddConversationAsync(Conversation conversation);
        Task AddMessageAsync(Message message);
        Task UpdateConversationAsync(Conversation conversation);
        Task<ConversationParticipant?> GetConversationParticipantAsync(Guid conversationId, Guid worldPlayerId);
        Task UpdateConversationParticipantAsync(ConversationParticipant participant);
        Task<Message?> GetMessageAsync(Guid messageId);
        Task UpdateMessageAsync(Message message);
        Task<bool> HasUnreadMessagesAsync(Guid worldPlayerId);
        Task<int> CountUnreadMessagesAsync(Guid worldPlayerId);
    }
}
