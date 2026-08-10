using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Services
{
    public class MessagingService : IMessagingService
    {
        private const int MaxSubjectLength = 120;
        private const int MaxMessageContentLength = 5000;
        private const int MinSearchQueryLength = 2;
        private const int MaxSearchQueryLength = 50;
        private const int DefaultMessagePageSize = 50;
        private const int MaxMessagePageSize = 100;

        private readonly IMessagingRepository _messagingRepository;
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly IBattleReportRepository _battleReportRepository;

        public MessagingService(IMessagingRepository messagingRepository, IWorldPlayerRepository worldPlayerRepository, IPlayerAccessService playerAccessService, IBattleReportRepository battleReportRepository)
        {
            _messagingRepository = messagingRepository;
            _worldPlayerRepository = worldPlayerRepository;
            _playerAccessService = playerAccessService;
            _battleReportRepository = battleReportRepository;
        }

        public async Task<ConversationDTO> StartConversationAsync(Guid senderId, IEnumerable<Guid> participantIds, string subject, string content, Guid? battleReportId = null)
        {
            var sanitizedContent = ValidateMessageContent(content, battleReportId);
            var sanitizedSubject = ValidateSubject(subject);

            var sender = await _playerAccessService.RequireOwnedWorldPlayerAsync(senderId);
            var report = await RequireShareableReportAsync(senderId, battleReportId);

            if (participantIds == null)
                throw new ArgumentException("You must add at least one recipient.", nameof(participantIds));

            var normalizedParticipantIds = participantIds
                .Where(id => id != Guid.Empty && id != senderId)
                .Distinct()
                .ToList();

            if (normalizedParticipantIds.Count == 0)
                throw new ArgumentException("You must add at least one recipient.");

            var participants = new List<WorldPlayer>();
            foreach (var participantId in normalizedParticipantIds)
            {
                var participant = await _worldPlayerRepository.GetByIdAsync(participantId);
                if (participant == null) throw new KeyNotFoundException("Participant not found");
                if (participant.WorldId != sender.WorldId)
                    throw new ArgumentException("All participants must be in the same world.");
                participants.Add(participant);
            }

            var sentAt = DateTime.UtcNow;
            var allParticipants = new List<WorldPlayer> { sender };
            allParticipants.AddRange(participants);
            var conversationId = Guid.NewGuid();

            var conversation = new Conversation
            {
                Id = conversationId,
                LastMessageDate = sentAt,
                Subject = sanitizedSubject,
                Participants = allParticipants.Select(player => new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    WorldPlayerId = player.Id,
                    WorldPlayer = player,
                    JoinedAt = sentAt,
                    LastReadAt = player.Id == senderId ? sentAt : null
                }).ToList()
            };

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = senderId,
                Content = sanitizedContent,
                SentAt = sentAt,
                IsRead = false
            };

            if (battleReportId.HasValue)
            {
                message.ReportAttachment = CreateAttachment(message.Id, report!);
            }

            conversation.Messages.Add(message);
            await _messagingRepository.AddConversationAsync(conversation);
            if (message.ReportAttachment != null)
            {
                message.ReportAttachment.BattleReport = report;
            }

            return BuildConversationDto(conversation, senderId);
        }

        public async Task<MessageDTO> ReplyToConversationAsync(Guid requestorId, Guid conversationId, string content, Guid? battleReportId = null)
        {
            var sanitizedContent = ValidateMessageContent(content, battleReportId);
            await _playerAccessService.RequireOwnedWorldPlayerAsync(requestorId);

            var conversation = await _messagingRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null) throw new KeyNotFoundException("Conversation not found");
            EnsureActiveParticipant(conversation, requestorId);
            var report = await RequireShareableReportAsync(requestorId, battleReportId);

            var participant = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId == requestorId);
            if (participant == null) throw new UnauthorizedAccessException("User is not a participant in this conversation");

            var sentAt = DateTime.UtcNow;
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderId = requestorId,
                Content = sanitizedContent,
                SentAt = sentAt,
                IsRead = false
            };

            if (battleReportId.HasValue)
            {
                message.ReportAttachment = CreateAttachment(message.Id, report!);
            }

            await _messagingRepository.AddMessageAsync(message);

            conversation.LastMessageDate = sentAt;
            await _messagingRepository.UpdateConversationAsync(conversation);

            participant.LastReadAt = sentAt;
            await _messagingRepository.UpdateConversationParticipantAsync(participant);
            if (message.ReportAttachment != null)
            {
                message.ReportAttachment.BattleReport = report;
            }

            return new MessageDTO
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = message.SenderId,
                SenderAllianceId = GetParticipantAllianceId(conversation, requestorId),
                SenderName = GetParticipantName(conversation, requestorId),
                SenderAllianceName = GetParticipantAllianceName(conversation, requestorId),
                SentAt = message.SentAt,
                IsRead = true,
                ReportAttachment = MapAttachment(message.ReportAttachment)
            };
        }

        public async Task<List<ConversationDTO>> GetConversationsAsync(Guid worldPlayerId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            return await _messagingRepository.GetConversationSummariesForPlayerAsync(worldPlayerId);
        }

        public async Task<List<MessageDTO>> GetMessagesAsync(Guid conversationId, Guid requestorId, DateTime? before, int take)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(requestorId);
            var conversation = await _messagingRepository.GetConversationForAccessAsync(conversationId);
            if (conversation == null) throw new KeyNotFoundException("Conversation not found");

            EnsureActiveParticipant(conversation, requestorId);

            var participant = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId == requestorId);
            var lastReadAt = participant?.LastReadAt;
            var pageSize = NormalizePageSize(take);
            var messages = await _messagingRepository.GetMessagesForConversationAsync(conversationId, before, pageSize);

            var dtos = messages.Select(m => new MessageDTO
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderAllianceId = m.Sender.Alliance?.Id,
                SenderName = m.Sender.PlayerProfile?.UserName ?? "Unknown",
                SenderAllianceName = m.Sender.Alliance?.Name ?? string.Empty,
                SentAt = m.SentAt,
                IsRead = m.SenderId == requestorId || (lastReadAt.HasValue && m.SentAt <= lastReadAt.Value),
                ReportAttachment = MapAttachment(m.ReportAttachment)
            }).ToList();

            return dtos;
        }

        public async Task MarkConversationAsReadAsync(Guid conversationId, Guid requestorId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(requestorId);
            var participant = await _messagingRepository.GetConversationParticipantAsync(conversationId, requestorId);
            EnsureActiveParticipant(participant);

            participant!.LastReadAt = DateTime.UtcNow;
            await _messagingRepository.UpdateConversationParticipantAsync(participant);
        }

        public async Task MarkMessageAsReadAsync(Guid messageId, Guid requestorId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(requestorId);
            var message = await _messagingRepository.GetMessageAsync(messageId);
            if (message == null) throw new KeyNotFoundException("Message not found");

            var participant = await _messagingRepository.GetConversationParticipantAsync(message.ConversationId, requestorId);
            EnsureActiveParticipant(participant);

            if (message.SenderId != requestorId)
            {
                participant!.LastReadAt = DateTime.UtcNow;
                await _messagingRepository.UpdateConversationParticipantAsync(participant);
            }
        }

        public async Task<List<PlayerSearchResultDTO>> SearchPlayersAsync(Guid worldId, string query)
        {
            await _playerAccessService.RequireWorldMembershipAsync(worldId);
            var sanitizedQuery = ValidateSearchQuery(query);
            if (sanitizedQuery == null) return new List<PlayerSearchResultDTO>();

            var players = await _worldPlayerRepository.SearchPlayersByUsernameAsync(worldId, sanitizedQuery);
            return players.Select(p => new PlayerSearchResultDTO
            {
                WorldPlayerId = p.Id,
                Username = p.PlayerProfile?.UserName ?? "Unknown"
            }).ToList();
        }

        public async Task<bool> HasUnreadMessagesAsync(Guid worldPlayerId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            return await _messagingRepository.HasUnreadMessagesAsync(worldPlayerId);
        }

        public async Task DeleteConversationAsync(Guid conversationId, Guid requestorId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(requestorId);
            var participant = await _messagingRepository.GetConversationParticipantAsync(conversationId, requestorId);
            EnsureActiveParticipant(participant);

            participant!.DeletedAt = DateTime.UtcNow;
            await _messagingRepository.UpdateConversationParticipantAsync(participant);
        }

        public async Task<int> CountUnreadMessagesAsync(Guid worldPlayerId)
        {
            await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
            return await _messagingRepository.CountUnreadMessagesAsync(worldPlayerId);
        }

        private static ConversationDTO BuildConversationDto(Conversation conversation, Guid viewerId)
        {
            var viewerParticipant = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId == viewerId);
            var participantDtos = conversation.Participants.Select(p => new ConversationParticipantDTO
            {
                WorldPlayerId = p.WorldPlayerId,
                Username = p.WorldPlayer?.PlayerProfile?.UserName ?? "Unknown",
                LastReadAt = p.LastReadAt
            }).ToList();

            var otherNames = conversation.Participants
                .Where(p => p.WorldPlayerId != viewerId)
                .Select(p => p.WorldPlayer?.PlayerProfile?.UserName ?? "Unknown")
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (otherNames.Count == 0)
            {
                otherNames = conversation.Participants
                    .Select(p => p.WorldPlayer?.PlayerProfile?.UserName ?? "Unknown")
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
            }

            return new ConversationDTO
            {
                Id = conversation.Id,
                ParticipantId = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId != viewerId)?.WorldPlayerId ?? Guid.Empty,
                ParticipantName = otherNames.Count > 0 ? string.Join(", ", otherNames) : "Unknown",
                Participants = participantDtos,
                IsGroupConversation = conversation.Participants.Count > 2,
                Subject = conversation.Subject,
                LastMessageContent = GetMessagePreview(conversation.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()),
                LastMessageDate = conversation.LastMessageDate,
                UnreadCount = viewerParticipant == null
                    ? 0
                    : conversation.Messages.Count(m => m.SenderId != viewerId && m.SentAt > (viewerParticipant.LastReadAt ?? DateTime.MinValue))
            };
        }

        private static string ValidateMessageContent(string content, Guid? battleReportId)
        {
            var sanitizedContent = content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sanitizedContent) && !battleReportId.HasValue) throw new ArgumentException("Either message content or a report is required.");
            if (sanitizedContent.Length > MaxMessageContentLength) throw new ArgumentException($"Message content cannot exceed {MaxMessageContentLength} characters.");
            return sanitizedContent;
        }

        private async Task<BattleReport?> RequireShareableReportAsync(Guid senderId, Guid? battleReportId)
        {
            if (!battleReportId.HasValue)
            {
                return null;
            }

            var report = await _battleReportRepository.GetByIdAsync(battleReportId.Value);
            if (report == null)
            {
                throw new KeyNotFoundException("Battle report not found.");
            }

            if (report.WorldPlayerId != senderId)
            {
                throw new UnauthorizedAccessException("Battle report does not belong to the sender.");
            }

            if (!report.IsPublic)
            {
                throw new InvalidOperationException("Battle report is private.");
            }

            return report;
        }

        private static MessageReportAttachment CreateAttachment(Guid messageId, BattleReport report) => new()
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            BattleReportId = report.Id
        };

        private static ReportAttachmentDTO? MapAttachment(MessageReportAttachment? attachment)
        {
            if (attachment == null)
            {
                return null;
            }

            var report = attachment.BattleReport;
            if (report == null || !report.IsPublic)
            {
                return new ReportAttachmentDTO { IsAvailable = false };
            }

            return new ReportAttachmentDTO
            {
                IsAvailable = true,
                Report = new SharedBattleReportDTO
                {
                    Id = report.Id,
                    ReportType = report.ReportType,
                    Title = report.Title,
                    Body = report.Body,
                    OccurredAt = report.OccurredAt,
                    AttackerLosses = ParseJson<List<UnitStackDTO>>(report.AttackerLossesJson) ?? new(),
                    DefenderLosses = ParseJson<List<UnitStackDTO>>(report.DefenderLossesJson) ?? new(),
                    RevivedUnits = ParseJson<List<UnitStackDTO>>(report.RevivedUnitsJson) ?? new(),
                    AppliedModifiers = ParseJson<List<string>>(report.AppliedModifiersJson) ?? new()
                }
            };
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

        private static T? ParseJson<T>(string json)
        {
            try
            {
                return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        private static string ValidateSubject(string subject)
        {
            var sanitizedSubject = string.IsNullOrWhiteSpace(subject) ? "No Subject" : subject.Trim();
            if (sanitizedSubject.Length > MaxSubjectLength) throw new ArgumentException($"Subject cannot exceed {MaxSubjectLength} characters.");
            return sanitizedSubject;
        }

        private static string? ValidateSearchQuery(string query)
        {
            var sanitizedQuery = query?.Trim() ?? string.Empty;
            if (sanitizedQuery.Length < MinSearchQueryLength) return null;
            if (sanitizedQuery.Length > MaxSearchQueryLength) throw new ArgumentException($"Search query cannot exceed {MaxSearchQueryLength} characters.");
            return sanitizedQuery;
        }

        private static int NormalizePageSize(int take)
        {
            if (take <= 0) return DefaultMessagePageSize;
            return Math.Min(take, MaxMessagePageSize);
        }

        private static void EnsureActiveParticipant(Conversation conversation, Guid worldPlayerId)
        {
            var participant = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId == worldPlayerId);
            if (participant == null || participant.DeletedAt.HasValue)
                throw new UnauthorizedAccessException("User is not a participant in this conversation");
        }

        private static void EnsureActiveParticipant(ConversationParticipant? participant)
        {
            if (participant == null || participant.DeletedAt.HasValue)
                throw new UnauthorizedAccessException("User is not a participant in this conversation");
        }

        private static string GetParticipantName(Conversation conversation, Guid worldPlayerId)
        {
            var participant = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId == worldPlayerId);
            if (participant != null)
                return participant.WorldPlayer?.PlayerProfile?.UserName ?? "Unknown";

            return "Unknown";
        }

        private static string GetParticipantAllianceName(Conversation conversation, Guid worldPlayerId)
        {
            var participant = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId == worldPlayerId);
            if (participant != null)
                return participant.WorldPlayer?.Alliance?.Name ?? string.Empty;

            return string.Empty;
        }

        private static Guid? GetParticipantAllianceId(Conversation conversation, Guid worldPlayerId)
        {
            var participant = conversation.Participants.FirstOrDefault(p => p.WorldPlayerId == worldPlayerId);
            if (participant != null)
                return participant.WorldPlayer?.Alliance?.Id;

            return null;
        }
    }
}
