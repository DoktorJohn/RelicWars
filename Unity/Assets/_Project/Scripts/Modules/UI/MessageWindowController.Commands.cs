using Project.Modules.Messaging;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public partial class MessageWindowController
    {
        private void OnSendClicked()
        {
            if (_state.IsSending) return;

            if (_messageInput == null)
            {
                return;
            }

            var content = _messageInput.value;
            if (string.IsNullOrWhiteSpace(content)) 
            {
                SetMessageState("Message content is empty");
                return;
            }
            
            if (NetworkManager.Instance == null)
            {
                SetMessageState("Messaging service unavailable");
                return;
            }
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds) || !Guid.TryParse(playerIds, out Guid senderId))
            {
                SetMessageState("Messaging service unavailable");
                return;
            }

            if (_state.SelectedConversation != null)
            {
                SendReply(senderId, content);
                return;
            }

            if (_state.IsComposing)
            {
                StartConversation(senderId, content);
                return;
            }

            SetMessageState(string.Empty);
        }

        private void SendReply(Guid senderId, string content)
        {
            Guid conversationId = _state.SelectedConversation.Id;
            var version = _requestVersion;
            SetSendingState(true);

            StartCoroutine(NetworkManager.Instance.Messaging.ReplyToConversation(senderId, conversationId, content, NetworkManager.Instance.JwtToken, (response) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                SetSendingState(false);
                if (response != null)
                {
                    _messageInput.value = "";
                    MessagingStateEvents.RaiseUnreadStateChanged();
                    LoadMessages(conversationId, _requestVersion);
                    LoadConversations(_requestVersion);
                }
                else
                {
                    SetMessageState("Message send failed");
                    Debug.LogError("[MessageWindow] Reply failed: Server returned null response.");
                }
            }));
        }

        private void StartConversation(Guid senderId, string content)
        {
            var recipientIds = ResolveRecipientIds(senderId);
            if (recipientIds.Count == 0)
            {
                SetMessageState("No recipient selected");
                return;
            }

            string subject = _subjectInput != null ? _subjectInput.value : "No Subject";
            var version = _requestVersion;
            SetSendingState(true);

            StartCoroutine(NetworkManager.Instance.Messaging.StartConversation(senderId, recipientIds, subject, content, NetworkManager.Instance.JwtToken, (conversation) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                SetSendingState(false);
                if (conversation != null)
                {
                    _messageInput.value = "";
                    if (_recipientInput != null) _recipientInput.value = "";
                    if (_subjectInput != null) _subjectInput.value = "";
                    _state.ClearRecipients();
                    RenderRecipientChips();

                    LoadConversations(_requestVersion, () =>
                    {
                        var loadedConversation = _state.Conversations.FirstOrDefault(c => c.Id == conversation.Id) ?? conversation;
                        SelectConversation(loadedConversation);
                    });
                }
                else
                {
                    SetMessageState("Message send failed");
                    Debug.LogError("[MessageWindow] Start conversation failed: Server returned null response.");
                }
            }));
        }

        private List<Guid> ResolveRecipientIds(Guid senderId)
        {
            var recipientIds = _state.SelectedRecipients
                .Select(recipient => recipient.WorldPlayerId)
                .Where(recipientId => recipientId != Guid.Empty && recipientId != senderId)
                .Distinct()
                .ToList();

            var inputValue = _recipientInput?.value?.Trim();
            if (string.IsNullOrWhiteSpace(inputValue))
            {
                return recipientIds;
            }

            var exactMatch = _state.Suggestions.FirstOrDefault(s =>
                s != null && string.Equals(s.Username, inputValue, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null && exactMatch.WorldPlayerId != senderId)
            {
                recipientIds.Add(exactMatch.WorldPlayerId);
            }
            else if (Guid.TryParse(inputValue, out var parsedId) && parsedId != senderId)
            {
                recipientIds.Add(parsedId);
            }

            return recipientIds.Distinct().ToList();
        }
    }
}
