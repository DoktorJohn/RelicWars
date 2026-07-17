using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project.Modules.Messaging
{
    public sealed class MessagingStateManager
    {
        public List<ConversationDTO> Conversations { get; private set; } = new();
        public List<MessageDTO> Messages { get; private set; } = new();
        public List<PlayerSearchResultDTO> Suggestions { get; private set; } = new();
        public List<PlayerSearchResultDTO> SelectedRecipients { get; private set; } = new();
        public ConversationDTO SelectedConversation { get; private set; }
        public int SelectedSuggestionIndex { get; private set; } = -1;
        public bool IsComposing { get; private set; }
        public bool IsSending { get; private set; }
        public string LatestSearchQuery { get; private set; } = string.Empty;

        public void SetConversations(IEnumerable<ConversationDTO> conversations) =>
            Conversations = conversations?.ToList() ?? new List<ConversationDTO>();

        public void SelectConversation(ConversationDTO conversation)
        {
            SelectedConversation = conversation;
            ClearRecipients();
            IsComposing = false;
        }

        public void StartComposing()
        {
            SelectedConversation = null;
            ClearRecipients();
            Messages.Clear();
            IsComposing = true;
        }

        public void SetMessages(IEnumerable<MessageDTO> messages) =>
            Messages = messages?.ToList() ?? new List<MessageDTO>();

        public void SetSuggestions(IEnumerable<PlayerSearchResultDTO> suggestions)
        {
            Suggestions = suggestions?.ToList() ?? new List<PlayerSearchResultDTO>();
            SelectedSuggestionIndex = -1;
        }

        public void SetSearchQuery(string query) => LatestSearchQuery = query ?? string.Empty;

        public bool AddRecipient(PlayerSearchResultDTO recipient)
        {
            if (recipient == null || recipient.WorldPlayerId == Guid.Empty || HasRecipient(recipient.WorldPlayerId))
            {
                return false;
            }

            SelectedRecipients.Add(recipient);
            return true;
        }

        public bool AddRecipient(Guid worldPlayerId, string username) => AddRecipient(new PlayerSearchResultDTO
        {
            WorldPlayerId = worldPlayerId,
            Username = string.IsNullOrWhiteSpace(username) ? worldPlayerId.ToString() : username
        });

        public bool HasRecipient(Guid worldPlayerId) =>
            SelectedRecipients.Any(recipient => recipient.WorldPlayerId == worldPlayerId);

        public void RemoveRecipient(Guid worldPlayerId) =>
            SelectedRecipients.RemoveAll(recipient => recipient.WorldPlayerId == worldPlayerId);

        public void ClearRecipients()
        {
            SelectedRecipients.Clear();
            Suggestions.Clear();
            SelectedSuggestionIndex = -1;
            LatestSearchQuery = string.Empty;
        }

        public void MoveSuggestion(int delta) => SelectedSuggestionIndex = Suggestions.Count == 0 ? -1 : Math.Max(0, Math.Min(SelectedSuggestionIndex + delta, Suggestions.Count - 1));
        public void SetSending(bool sending) => IsSending = sending;

        public void MarkSelectedRead()
        {
            if (SelectedConversation != null) SelectedConversation.UnreadCount = 0;
        }

        public void RemoveSelectedConversation()
        {
            if (SelectedConversation != null)
                Conversations.RemoveAll(c => c.Id == SelectedConversation.Id);
            SelectedConversation = null;
            Messages.Clear();
            ClearRecipients();
            IsComposing = false;
        }
    }
}
