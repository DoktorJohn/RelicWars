using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class MessagingService : IMessagingService
    {
        private readonly IMessagingRepository _messagingRepository;
        private readonly IWorldPlayerRepository _worldPlayerRepository;

        public MessagingService(IMessagingRepository messagingRepository, IWorldPlayerRepository worldPlayerRepository)
        {
            _messagingRepository = messagingRepository;
            _worldPlayerRepository = worldPlayerRepository;
        }

        public async Task<MessageDTO> SendMessageAsync(Guid senderId, Guid receiverId, string content, string? subject = null, Guid? conversationId = null)
        {
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Message content cannot be empty.");

            Conversation? conversation = null;

            // 1. If conversationId provided, try finding it
            if (conversationId.HasValue && conversationId.Value != Guid.Empty)
            {
                conversation = await _messagingRepository.GetConversationByIdAsync(conversationId.Value);
            }
            
            // 2. If no conversation found (or none provided), create a NEW one
            // Previously: Found *the* conversation between sender and receiver.
            // Now: We want to support multiple conversations.
            // If "subject" is provided, we assume it's a NEW thread unless conversationId was explicitly given.
            if (conversation == null)
            {
                // New logic: Don't search for "existing" conversation by participants alone if subject is provided.
                // Or maybe we still should? The user requested: "I can have many conversations with the same user."
                // So, if sending a NEW message from the UI, we should create a NEW conversation.
                
                // If it's a reply in an existing thread, the UI should provide conversationId.
                // If it's a new message, UI should provide subject (and NO conversationId).
                
                var receiver = await _worldPlayerRepository.GetByIdAsync(receiverId);
                if (receiver == null) throw new KeyNotFoundException("Receiver not found");
                
                var sender = await _worldPlayerRepository.GetByIdAsync(senderId);
                if (sender == null) throw new KeyNotFoundException("Sender not found");

                conversation = new Conversation
                {
                    Participant1Id = senderId,
                    Participant2Id = receiverId,
                    LastMessageDate = DateTime.UtcNow,
                    Participant1 = sender,
                    Participant2 = receiver,
                    Subject = !string.IsNullOrWhiteSpace(subject) ? subject : "No Subject"
                };
                await _messagingRepository.AddConversationAsync(conversation);
            }

            // Need sender name for DTO later
            string senderName = "Unknown";
            if (conversation.Participant1Id == senderId) senderName = conversation.Participant1?.PlayerProfile?.UserName ?? "Unknown";
            else if (conversation.Participant2Id == senderId) senderName = conversation.Participant2?.PlayerProfile?.UserName ?? "Unknown";
            
            // If senderName is still unknown (e.g. freshly created without full load), try loading it or accept it might be null for now
            if (senderName == "Unknown")
            {
                var s = await _worldPlayerRepository.GetByIdAsync(senderId);
                if (s != null) senderName = s.PlayerProfile?.UserName ?? "Unknown";
            }

            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _messagingRepository.AddMessageAsync(message);
            
            conversation.LastMessageDate = message.SentAt;
            await _messagingRepository.UpdateConversationAsync(conversation);

            return new MessageDTO
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = message.SenderId,
                SenderName = senderName,
                SentAt = message.SentAt,
                IsRead = message.IsRead
            };
        }

        public async Task<List<ConversationDTO>> GetConversationsAsync(Guid worldPlayerId)
        {
            var conversations = await _messagingRepository.GetConversationsForPlayerAsync(worldPlayerId);
            var result = new List<ConversationDTO>();

            foreach (var conv in conversations)
            {
                var otherParticipant = conv.Participant1Id == worldPlayerId ? conv.Participant2 : conv.Participant1;
                var lastMessage = conv.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                var unreadCount = conv.Messages.Count(m => m.SenderId != worldPlayerId && !m.IsRead);

                result.Add(new ConversationDTO
                {
                    Id = conv.Id,
                    ParticipantId = otherParticipant?.Id ?? Guid.Empty,
                    ParticipantName = otherParticipant?.PlayerProfile?.UserName ?? "Unknown",
                    Subject = conv.Subject,
                    LastMessageContent = lastMessage?.Content ?? string.Empty,
                    LastMessageDate = conv.LastMessageDate,
                    UnreadCount = unreadCount
                });
            }

            return result;
        }

        public async Task<List<MessageDTO>> GetMessagesAsync(Guid conversationId, Guid requestorId)
        {
            var conversation = await _messagingRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null) throw new KeyNotFoundException("Conversation not found");

            if (conversation.Participant1Id != requestorId && conversation.Participant2Id != requestorId)
                throw new UnauthorizedAccessException("User is not a participant in this conversation");

            var dtos = conversation.Messages.Select(m => new MessageDTO
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderName = m.Sender.PlayerProfile?.UserName ?? "Unknown",
                SentAt = m.SentAt,
                IsRead = m.IsRead
            }).ToList();

            return dtos;
        }

        public async Task MarkMessageAsReadAsync(Guid messageId, Guid requestorId)
        {
            var message = await _messagingRepository.GetMessageAsync(messageId);
            if (message != null && message.SenderId != requestorId && !message.IsRead)
            {
                 await _messagingRepository.UpdateMessageAsync(message);
            }
        }

        public async Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<PlayerSearchResultDTO>();

            var players = await _worldPlayerRepository.SearchPlayersByUsernameAsync(worldId, query);
            return players.Select(p => new PlayerSearchResultDTO
            {
                WorldPlayerId = p.Id,
                Username = p.PlayerProfile?.UserName ?? "Unknown"
            }).ToList();
        }

        public async Task<bool> HasUnreadMessagesAsync(Guid worldPlayerId)
        {
            var conversations = await _messagingRepository.GetConversationsForPlayerAsync(worldPlayerId);
            foreach (var conv in conversations)
            {
                if (conv.Messages.Any(m => m.SenderId != worldPlayerId && !m.IsRead))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
