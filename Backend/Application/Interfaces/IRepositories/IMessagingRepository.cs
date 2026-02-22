using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.IRepositories
{
    public interface IMessagingRepository
    {
        Task<Conversation?> GetConversationAsync(Guid participant1Id, Guid participant2Id);
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
        Task<List<Conversation>> GetConversationsForPlayerAsync(Guid worldPlayerId);
        Task AddConversationAsync(Conversation conversation);
        Task AddMessageAsync(Message message);
        Task UpdateConversationAsync(Conversation conversation);
        Task<Message?> GetMessageAsync(Guid messageId);
        Task UpdateMessageAsync(Message message);
    }
}
