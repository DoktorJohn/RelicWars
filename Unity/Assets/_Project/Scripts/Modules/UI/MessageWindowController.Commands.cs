using Project.Modules.Messaging;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
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
            Guid receiverId = ResolveRecipientId();
            if (receiverId == Guid.Empty)
            {
                SetMessageState("No recipient selected");
                return;
            }

            string subject = _subjectInput != null ? _subjectInput.value : "No Subject";
            var version = _requestVersion;
            SetSendingState(true);

            StartCoroutine(NetworkManager.Instance.Messaging.StartConversation(senderId, receiverId, subject, content, NetworkManager.Instance.JwtToken, (conversation) =>
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
                    _state.ClearRecipient();

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

        private Guid ResolveRecipientId()
        {
            if (_recipientInput == null) return Guid.Empty;

            if (_state.SelectedRecipientId != Guid.Empty)
            {
                return _state.SelectedRecipientId;
            }

            var exactMatch = _state.Suggestions.FirstOrDefault(s =>
                s.Username.Equals(_recipientInput.value, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return exactMatch.WorldPlayerId;
            }

            if (Guid.TryParse(_recipientInput.value, out var parsedId))
            {
                return parsedId;
            }

            return Guid.Empty;
        }
    }
}
