using Application.Interfaces.IRepositories;
using Application.DTOs;
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

        public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId)
        {
            return await _context.Conversations
                .AsSplitQuery()
                .Include(c => c.Participants).ThenInclude(p => p.WorldPlayer).ThenInclude(wp => wp.PlayerProfile)
                .Include(c => c.Participants).ThenInclude(p => p.WorldPlayer).ThenInclude(wp => wp.Alliance)
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender).ThenInclude(p => p.PlayerProfile)
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender).ThenInclude(p => p.Alliance)
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.ReportAttachment).ThenInclude(attachment => attachment!.BattleReport)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<Conversation?> GetConversationForAccessAsync(Guid conversationId)
        {
            return await _context.Conversations
                .AsSplitQuery()
                .Include(c => c.Participants).ThenInclude(p => p.WorldPlayer).ThenInclude(wp => wp.PlayerProfile)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<List<Message>> GetMessagesForConversationAsync(Guid conversationId, DateTime? before, int take)
        {
            var query = _context.Messages
                .AsNoTracking()
                .Include(m => m.Sender).ThenInclude(p => p.PlayerProfile)
                .Include(m => m.Sender).ThenInclude(p => p.Alliance)
                .Include(m => m.ReportAttachment).ThenInclude(attachment => attachment!.BattleReport)
                .Where(m => m.ConversationId == conversationId);

            if (before.HasValue)
            {
                query = query.Where(m => m.SentAt < before.Value);
            }

            return await query
                .OrderByDescending(m => m.SentAt)
                .Take(take)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<ConversationDTO>> GetConversationSummariesForPlayerAsync(Guid worldPlayerId)
        {
            var conversations = await _context.Conversations
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Participants).ThenInclude(p => p.WorldPlayer).ThenInclude(wp => wp.PlayerProfile)
                .Include(c => c.Messages).ThenInclude(m => m.ReportAttachment).ThenInclude(attachment => attachment!.BattleReport)
                .Where(c => c.Participants.Any(p => p.WorldPlayerId == worldPlayerId && p.DeletedAt == null))
                .OrderByDescending(c => c.LastMessageDate)
                .ToListAsync();

            return conversations.Select(c =>
            {
                var viewerParticipant = c.Participants.FirstOrDefault(p => p.WorldPlayerId == worldPlayerId);
                var participantDtos = c.Participants.Select(p => new ConversationParticipantDTO
                {
                    WorldPlayerId = p.WorldPlayerId,
                    Username = p.WorldPlayer?.PlayerProfile?.UserName ?? "Unknown",
                    LastReadAt = p.LastReadAt
                }).ToList();

                var displayNames = c.Participants
                    .Where(p => p.WorldPlayerId != worldPlayerId)
                    .Select(p => p.WorldPlayer?.PlayerProfile?.UserName ?? "Unknown")
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();

                if (displayNames.Count == 0)
                {
                    displayNames = c.Participants
                        .Select(p => p.WorldPlayer?.PlayerProfile?.UserName ?? "Unknown")
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToList();
                }

                return new ConversationDTO
                {
                    Id = c.Id,
                    ParticipantId = c.Participants.FirstOrDefault(p => p.WorldPlayerId != worldPlayerId)?.WorldPlayerId ?? Guid.Empty,
                    ParticipantName = displayNames.Count > 0 ? string.Join(", ", displayNames) : "Unknown",
                    Participants = participantDtos,
                    IsGroupConversation = c.Participants.Count > 2,
                    Subject = c.Subject,
                    LastMessageContent = GetMessagePreview(c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()),
                    LastMessageDate = c.LastMessageDate,
                    UnreadCount = viewerParticipant == null
                        ? 0
                        : c.Messages.Count(m => m.SenderId != worldPlayerId && m.SentAt > (viewerParticipant.LastReadAt ?? DateTime.MinValue))
                };
            }).ToList();
        }

        private static string GetMessagePreview(Message? message)
        {
            if (message == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(message.Content))
            {
                return message.Content;
            }

            return message.ReportAttachment?.BattleReport is { IsPublic: true } report
                ? report.Title
                : "Report unavailable";
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

        public async Task<ConversationParticipant?> GetConversationParticipantAsync(Guid conversationId, Guid worldPlayerId)
        {
            return await _context.ConversationParticipants
                .Include(p => p.Conversation)
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.WorldPlayerId == worldPlayerId);
        }

        public async Task UpdateConversationParticipantAsync(ConversationParticipant participant)
        {
            _context.ConversationParticipants.Update(participant);
            await _context.SaveChangesAsync();
        }

        public async Task<Message?> GetMessageAsync(Guid messageId)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasUnreadMessagesAsync(Guid worldPlayerId)
        {
            return await _context.ConversationParticipants
                .AsNoTracking()
                .Where(p => p.WorldPlayerId == worldPlayerId && p.DeletedAt == null)
                .AnyAsync(p => p.Conversation.Messages.Any(m =>
                    m.SenderId != worldPlayerId &&
                    m.SentAt > (p.LastReadAt ?? DateTime.MinValue)));
        }

        public async Task<int> CountUnreadMessagesAsync(Guid worldPlayerId)
        {
            return await _context.ConversationParticipants
                .AsNoTracking()
                .Where(p => p.WorldPlayerId == worldPlayerId && p.DeletedAt == null)
                .SelectMany(p => p.Conversation.Messages.Where(m =>
                    m.SenderId != worldPlayerId &&
                    m.SentAt > (p.LastReadAt ?? DateTime.MinValue)))
                .CountAsync();
        }
    }
}
