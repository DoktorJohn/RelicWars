using Assets.Scripts.Domain.Enums;
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
        private void LoadConversations(int version, Action onComplete = null)
        {
            if (NetworkManager.Instance == null)
            {
                SetConversationState("Messaging unavailable");
                onComplete?.Invoke();
                return;
            }
            
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds))
            {
                SetConversationState("Messaging unavailable");
                onComplete?.Invoke();
                return;
            }

            if (!Guid.TryParse(playerIds, out Guid worldPlayerId))
            {
                SetConversationState("Messaging unavailable");
                onComplete?.Invoke();
                return;
            }

            SetConversationState("Loading conversations...");
            
            StartCoroutine(NetworkManager.Instance.Messaging.GetConversations(worldPlayerId, NetworkManager.Instance.JwtToken, (conversations) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (conversations != null)
                {
                    _state.SetConversations(conversations);
                    RenderConversationList();
                }
                else
                {
                    SetConversationState("Failed to load conversations");
                }
                onComplete?.Invoke();
            }));
        }

        private void RenderConversationList()
        {
            if (_conversationList == null) return;
            _conversationList.Clear();

            if (_state.Conversations.Count == 0)
            {
                SetConversationState("No conversations");
                return;
            }
            
            foreach (var conv in _state.Conversations)
            {
                var item = new VisualElement();
                item.AddToClassList("conversation-item");
            if (_state.SelectedConversation != null && _state.SelectedConversation.Id == conv.Id) item.AddToClassList("selected");

                var participantRow = CreateConversationParticipantsRow(conv);
                if (participantRow != null)
                {
                    item.Add(participantRow);
                }

                var subjectText = string.IsNullOrEmpty(conv.Subject) || conv.Subject == "No Subject" ? string.Empty : conv.Subject;
                var subjectLabel = new Label(subjectText);
                subjectLabel.AddToClassList("conversation-subject-name");
                item.Add(subjectLabel);

                if (conv.UnreadCount > 0)
                {
                    var unread = new Label(conv.UnreadCount > 99 ? "99+" : conv.UnreadCount.ToString());
                    unread.AddToClassList("conversation-unread-badge");
                    item.Add(unread);
                }

                item.RegisterCallback<ClickEvent>(evt => SelectConversation(conv));
                _conversationList.Add(item);
            }
        }

        private VisualElement CreateConversationParticipantsRow(ConversationDTO conversation)
        {
            if (conversation == null)
            {
                return null;
            }

            var row = new VisualElement();
            row.AddToClassList("conversation-participant-chips");

            var currentPlayerId = ResolveCurrentWorldPlayerId();
            var participants = conversation.Participants?.Where(participant => participant.WorldPlayerId != Guid.Empty).ToList() ?? new List<ConversationParticipantDTO>();
            if (participants.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(conversation.ParticipantName))
                {
                    var participantLabel = new Label(conversation.ParticipantName);
                    participantLabel.AddToClassList("conversation-sender-name");
                    row.Add(participantLabel);
                    return row;
                }

                return null;
            }

            var displayParticipants = participants.Where(participant => participant.WorldPlayerId != currentPlayerId).ToList();
            if (displayParticipants.Count == 0)
            {
                displayParticipants = participants;
            }

            foreach (var participant in displayParticipants)
            {
                row.Add(WindowNavigationHelper.CreateLinkButton(
                    participant.Username,
                    () => WindowNavigationHelper.OpenProfile(participant.WorldPlayerId),
                    "conversation-participant-chip"));
            }

            return row;
        }

        private void SelectConversation(ConversationDTO conversation)
        {
            if (conversation == null) return;
            _state.SelectConversation(conversation);
            ResetDeleteConfirmation();
            if (_newMessageHeader != null) _newMessageHeader.style.display = DisplayStyle.None;
            if (_deleteConversationButton != null) _deleteConversationButton.style.display = DisplayStyle.Flex;
            SetActiveConversationTitle(conversation);
            
            UpdateInputAreaState();
            ResetMessagePaging();
            LoadMessages(conversation.Id, _requestVersion);
        }

        private void LoadMessages(Guid conversationId, int version)
        {
            if (NetworkManager.Instance == null) return;
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds)) return;
            if (!Guid.TryParse(playerIds, out Guid worldPlayerId)) return;

            StartCoroutine(NetworkManager.Instance.Messaging.GetMessages(worldPlayerId, conversationId, null, MessagePageSize, NetworkManager.Instance.JwtToken, (messages) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion || _state.SelectedConversation == null || _state.SelectedConversation.Id != conversationId)
                {
                    return;
                }

                if (messages != null)
                {
                    var orderedMessages = OrderMessages(messages);
                    _state.SetMessages(orderedMessages);
                    UpdateMessagePagingState(orderedMessages, messages.Count);
                    RenderMessages(orderedMessages, true);

                    StartCoroutine(NetworkManager.Instance.Messaging.MarkConversationAsRead(worldPlayerId, conversationId, NetworkManager.Instance.JwtToken, (success) =>
                    {
                        if (!isActiveAndEnabled || version != _requestVersion)
                        {
                            return;
                        }

                        if (success)
                        {
                            if (_state.SelectedConversation != null && _state.SelectedConversation.Id == conversationId)
                            {
                                _state.MarkSelectedRead();
                                RenderConversationList();
                            }
                            MessagingStateEvents.RaiseUnreadStateChanged();
                        }
                    }));
                }
                else
                {
                    ResetMessagePaging();
                    SetMessageState("Failed to load messages");
                }
            }));
        }

        private void LoadOlderMessages()
        {
            if (_isLoadingOlderMessages || !_hasOlderMessages || !_oldestLoadedMessageCursor.HasValue)
            {
                return;
            }

            if (_state.SelectedConversation == null || NetworkManager.Instance == null)
            {
                return;
            }

            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds) || !Guid.TryParse(playerIds, out Guid worldPlayerId))
            {
                return;
            }

            var conversationId = _state.SelectedConversation.Id;
            var version = _requestVersion;
            var before = _oldestLoadedMessageCursor.Value;
            var previousScrollY = _messageList?.scrollOffset.y ?? 0f;
            var previousContentHeight = _messageList?.contentContainer.layout.height ?? 0f;
            _isLoadingOlderMessages = true;
            RenderMessages(_state.Messages, false);

            StartCoroutine(NetworkManager.Instance.Messaging.GetMessages(worldPlayerId, conversationId, before, MessagePageSize, NetworkManager.Instance.JwtToken, (messages) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion || _state.SelectedConversation == null || _state.SelectedConversation.Id != conversationId)
                {
                    return;
                }

                _isLoadingOlderMessages = false;

                if (messages == null)
                {
                    RenderMessages(_state.Messages, false);
                    return;
                }

                if (messages.Count == 0)
                {
                    _hasOlderMessages = false;
                    RenderMessages(_state.Messages, false);
                    return;
                }

                var mergedMessages = OrderMessages(messages.Concat(_state.Messages)
                    .GroupBy(message => message.Id)
                    .Select(group => group.First()));

                _state.SetMessages(mergedMessages);
                UpdateMessagePagingState(mergedMessages, messages.Count);
                RenderMessages(mergedMessages, false, previousScrollY, previousContentHeight);
            }));
        }

        private void UpdateMessagePagingState(List<MessageDTO> messages, int loadedCount)
        {
            _hasOlderMessages = loadedCount >= MessagePageSize;
            _oldestLoadedMessageCursor = messages.Count > 0
                ? messages.Min(message => message.SentAt).ToUniversalTime()
                : null;
        }

        private static List<MessageDTO> OrderMessages(IEnumerable<MessageDTO> messages)
        {
            return messages?
                .OrderBy(message => message.SentAt)
                .ThenBy(message => message.Id)
                .ToList() ?? new List<MessageDTO>();
        }

        private void AddLoadOlderMessagesButton()
        {
            if (!_hasOlderMessages || _messageList == null)
            {
                return;
            }

            var button = new Button(LoadOlderMessages)
            {
                text = _isLoadingOlderMessages ? "LOADING" : "LOAD OLDER"
            };
            button.AddToClassList("message-load-older-button");
            button.SetEnabled(!_isLoadingOlderMessages);
            _messageList.Add(button);
        }

        private void RenderMessages(List<MessageDTO> messages, bool scrollToBottom, float? previousScrollY = null, float? previousContentHeight = null)
        {
            if (_messageList == null) return;
            _messageList.Clear();
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds)) return;
            if (!Guid.TryParse(playerIds, out Guid worldPlayerId)) return;

            if (messages.Count == 0)
            {
                SetMessageState("No messages");
                return;
            }

            AddLoadOlderMessagesButton();

            DateTime? previousDay = null;
            foreach (var msg in messages)
            {
                var localSentAt = msg.SentAt.ToLocalTime();
                if (!previousDay.HasValue || previousDay.Value.Date != localSentAt.Date)
                {
                    var divider = new Label(localSentAt.ToString("MMM d, yyyy"));
                    divider.AddToClassList("message-date-divider");
                    _messageList.Add(divider);
                    previousDay = localSentAt.Date;
                }

                var bubble = new VisualElement();
                bubble.AddToClassList("message-bubble");
                if (msg.SenderId == worldPlayerId) bubble.AddToClassList("mine");
                else bubble.AddToClassList("theirs");

                var metaRow = new VisualElement();
                metaRow.AddToClassList("message-meta-row");
                metaRow.Add(WindowNavigationHelper.CreateLinkButton(
                    msg.SenderName,
                    () => WindowNavigationHelper.OpenProfile(msg.SenderId),
                    "message-meta-link"));

                if (!string.IsNullOrWhiteSpace(msg.SenderAllianceName))
                {
                    if (msg.SenderAllianceId.HasValue && msg.SenderAllianceId.Value != Guid.Empty)
                    {
                        metaRow.Add(WindowNavigationHelper.CreateLinkButton(
                            $"[{msg.SenderAllianceName}]",
                            () => WindowNavigationHelper.OpenAlliance(msg.SenderAllianceId.Value),
                            "message-meta-link"));
                    }
                    else
                    {
                        var allianceLabel = new Label($"[{msg.SenderAllianceName}]");
                        allianceLabel.AddToClassList("message-meta-link");
                        metaRow.Add(allianceLabel);
                    }
                }

                var time = new Label(localSentAt.ToString("HH:mm"));
                time.AddToClassList("message-meta");
                metaRow.Add(time);
                bubble.Add(metaRow);

                var content = new Label(msg.Content);
                content.AddToClassList("message-content-label");
                bubble.Add(content);

                _messageList.Add(bubble);
            }

            if (scrollToBottom)
            {
                _messageList.schedule.Execute(() => _messageList.scrollOffset = new Vector2(0, _messageList.contentContainer.layout.height));
            }
            else if (previousScrollY.HasValue && previousContentHeight.HasValue)
            {
                _messageList.schedule.Execute(() =>
                {
                    var heightDelta = _messageList.contentContainer.layout.height - previousContentHeight.Value;
                    _messageList.scrollOffset = new Vector2(0, Mathf.Max(0f, previousScrollY.Value + heightDelta));
                });
            }
        }

        private void SetActiveConversationTitle(ConversationDTO conversation)
        {
            if (_conversationTitle == null || conversation == null) return;

            _conversationTitle.text = string.IsNullOrWhiteSpace(conversation.Subject) ? string.Empty : conversation.Subject;
        }
    }
}
