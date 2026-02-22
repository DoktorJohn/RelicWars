using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class MessagingRepository : IMessagingRepository
    {
        private readonly GameContext _context;

        public MessagingRepository(GameContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetConversationAsync(Guid participant1Id, Guid participant2Id)
        {
            return await _context.Conversations
                .AsSplitQuery()
                .Include(c => c.Participant1).ThenInclude(p => p.PlayerProfile)
                .Include(c => c.Participant2).ThenInclude(p => p.PlayerProfile)
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender).ThenInclude(p => p.PlayerProfile)
                .FirstOrDefaultAsync(c =>
                    (c.Participant1Id == participant1Id && c.Participant2Id == participant2Id) ||
                    (c.Participant1Id == participant2Id && c.Participant2Id == participant1Id));
        }

        public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId)
        {
            return await _context.Conversations
                .AsSplitQuery()
                .Include(c => c.Participant1).ThenInclude(p => p.PlayerProfile)
                .Include(c => c.Participant2).ThenInclude(p => p.PlayerProfile)
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender).ThenInclude(p => p.PlayerProfile)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<List<Conversation>> GetConversationsForPlayerAsync(Guid worldPlayerId)
        {
             return await _context.Conversations
                .AsSplitQuery()
                .Include(c => c.Participant1).ThenInclude(p => p.PlayerProfile)
                .Include(c => c.Participant2).ThenInclude(p => p.PlayerProfile)
                .Include(c => c.Messages)
                .Where(c => c.Participant1Id == worldPlayerId || c.Participant2Id == worldPlayerId)
                .OrderByDescending(c => c.LastMessageDate)
                .ToListAsync();
        }

        public async Task AddConversationAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task AddMessageAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
        }
        
        public async Task UpdateConversationAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task<Message?> GetMessageAsync(Guid messageId)
        {
            return await _context.Messages.FindAsync(messageId);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
        }
    }
}
