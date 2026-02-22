using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public class MessageWindowController : BaseWindow
    {
        protected override string WindowName => "Inbox";
        protected override string VisualContainerName => "WindowFrame";
        protected override string HeaderName => "Header";

        private ScrollView _conversationList;
        private ScrollView _messageList;
        private TextField _messageInput;
        private Button _sendButton;
        private Button _newConversationButton;
        private VisualElement _newMessageHeader;
        private TextField _recipientInput;
        private TextField _subjectInput;
        private VisualElement _suggestionList;
        private Label _conversationTitle;
        private VisualElement _inputArea;

        private List<ConversationDTO> _conversations = new();
        private ConversationDTO _selectedConversation;
        private List<PlayerSearchResultDTO> _suggestions = new();
        private int _selectedSuggestionIndex = -1;
        private Guid _selectedRecipientId = Guid.Empty;
        private bool _isNewMessageMode = false;

        public override void OnOpen(object dataPayload)
        {
            InitializeUI();
            UpdateInputAreaState();
            
            LoadConversations(() =>
            {
                if (dataPayload != null)
                {
                    if (dataPayload is Guid targetUserId)
                    {
                        StartNewMessageMode();
                        _selectedRecipientId = targetUserId;
                        
                        if (_recipientInput != null)
                        {
                            _recipientInput.SetValueWithoutNotify("Loading...");
                            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(targetUserId, NetworkManager.Instance.JwtToken, (profile) =>
                            {
                                if (profile != null)
                                {
                                    _recipientInput.SetValueWithoutNotify(profile.UserName);
                                    if (_conversationTitle != null) _conversationTitle.text = "New Message to: " + profile.UserName;
                                }
                                else
                                {
                                    _recipientInput.SetValueWithoutNotify(targetUserId.ToString());
                                }
                            }));
                        }
                    }
                    else if (dataPayload is string targetIdStr && Guid.TryParse(targetIdStr, out var tid))
                    {
                        StartNewMessageMode();
                        _selectedRecipientId = tid;
                        if (_recipientInput != null)
                        {
                            _recipientInput.SetValueWithoutNotify("Loading...");
                            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(tid, NetworkManager.Instance.JwtToken, (profile) =>
                            {
                                if (profile != null)
                                {
                                    _recipientInput.SetValueWithoutNotify(profile.UserName);
                                    if (_conversationTitle != null) _conversationTitle.text = "New Message to: " + profile.UserName;
                                }
                                else
                                {
                                    _recipientInput.SetValueWithoutNotify(tid.ToString());
                                }
                            }));
                        }
                    }
                }
            });
        }

        private void InitializeUI()
        {
            if (Root == null) return;

            _conversationList = Root.Q<ScrollView>("ConversationList");
            _messageList = Root.Q<ScrollView>("MessageList");
            _messageInput = Root.Q<TextField>("MessageInput");
            _sendButton = Root.Q<Button>("SendButton");
            _newConversationButton = Root.Q<Button>("NewConversationButton");
            _newMessageHeader = Root.Q<VisualElement>("NewMessageHeader");
            _recipientInput = Root.Q<TextField>("RecipientInput");
            _subjectInput = Root.Q<TextField>("SubjectInput");
            _suggestionList = Root.Q<VisualElement>("SuggestionList");
            _conversationTitle = Root.Q<Label>("ConversationTitle");
            _inputArea = Root.Q<VisualElement>("InputArea");

            if (_recipientInput != null)
            {
                _recipientInput.RegisterValueChangedCallback(OnRecipientInputChanged);
                _recipientInput.RegisterCallback<KeyDownEvent>(OnRecipientKeyDown, TrickleDown.TrickleDown);
            }

            if (_messageInput != null)
            {
                _messageInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnSendClicked();
                        evt.StopPropagation();
                    }
                });
            }

            if (_sendButton != null)
            {
                _sendButton.clicked -= OnSendClicked;
                _sendButton.clicked += OnSendClicked;
            }
            if (_newConversationButton != null)
            {
                _newConversationButton.clicked -= OnNewMessageClicked;
                _newConversationButton.clicked += OnNewMessageClicked;
            }
        }

        private void LoadConversations(Action onComplete = null)
        {
            if (NetworkManager.Instance == null) return;
            
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds)) return;

            if (!Guid.TryParse(playerIds, out Guid worldPlayerId)) return;
            
            StartCoroutine(NetworkManager.Instance.Messaging.GetConversations(worldPlayerId, NetworkManager.Instance.JwtToken, (conversations) =>
            {
                if (conversations != null)
                {
                    _conversations = conversations;
                    RenderConversationList();
                }
                onComplete?.Invoke();
            }));
        }

        private void RenderConversationList()
        {
            if (_conversationList == null) return;
            _conversationList.Clear();
            
            foreach (var conv in _conversations)
            {
                var item = new VisualElement();
                item.AddToClassList("conversation-item");
                if (_selectedConversation != null && _selectedConversation.Id == conv.Id) item.AddToClassList("selected");
                
                var subjectText = string.IsNullOrEmpty(conv.Subject) || conv.Subject == "No Subject" ? "(No Subject)" : conv.Subject;
                var subjectLabel = new Label(subjectText);
                subjectLabel.AddToClassList("conversation-name"); 
                item.Add(subjectLabel);

                var nameLabel = new Label(conv.ParticipantName);
                nameLabel.style.fontSize = 11;
                nameLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                nameLabel.style.paddingLeft = 0;
                nameLabel.style.marginLeft = 0;
                item.Add(nameLabel);
                
                var msgLabel = new Label(conv.LastMessageContent);
                msgLabel.AddToClassList("conversation-last-msg");
                item.Add(msgLabel);
                
                if (conv.UnreadCount > 0)
                {
                    var unread = new Label($"({conv.UnreadCount})");
                    unread.style.color = Color.red;
                    unread.style.fontSize = 12;
                    item.Add(unread);
                }

                item.RegisterCallback<ClickEvent>(evt => SelectConversation(conv));
                _conversationList.Add(item);
            }
        }

        private void SelectConversation(ConversationDTO conversation)
        {
            _selectedConversation = conversation;
            _isNewMessageMode = false;
            if (_newMessageHeader != null) _newMessageHeader.style.display = DisplayStyle.None;
            if (_conversationTitle != null) 
            {
                var sub = string.IsNullOrEmpty(conversation.Subject) ? "Conversation" : conversation.Subject;
                _conversationTitle.text = $"{sub} ({conversation.ParticipantName})";
            }
            
            RenderConversationList(); 
            UpdateInputAreaState();
            
            LoadMessages(conversation.Id);
        }

        private void LoadMessages(Guid conversationId)
        {
            if (NetworkManager.Instance == null) return;
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds)) return;
            if (!Guid.TryParse(playerIds, out Guid worldPlayerId)) return;

            StartCoroutine(NetworkManager.Instance.Messaging.GetMessages(worldPlayerId, conversationId, NetworkManager.Instance.JwtToken, (messages) =>
            {
                if (messages != null)
                {
                    RenderMessages(messages);
                    
                    foreach(var m in messages)
                    {
                        if (!m.IsRead && m.SenderId != worldPlayerId)
                        {
                            StartCoroutine(NetworkManager.Instance.Messaging.MarkAsRead(worldPlayerId, m.Id, NetworkManager.Instance.JwtToken, null));
                        }
                    }
                }
            }));
        }

        private void RenderMessages(List<MessageDTO> messages)
        {
            if (_messageList == null) return;
            _messageList.Clear();
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds)) return;
            if (!Guid.TryParse(playerIds, out Guid worldPlayerId)) return;

            foreach (var msg in messages)
            {
                var bubble = new Label($"{msg.SenderName}: {msg.Content}");
                bubble.AddToClassList("message-bubble");
                if (msg.SenderId == worldPlayerId) bubble.AddToClassList("mine");
                else bubble.AddToClassList("theirs");
                
                _messageList.Add(bubble);
            }
            
            _messageList.schedule.Execute(() => _messageList.scrollOffset = new Vector2(0, _messageList.contentContainer.layout.height)); 
        }

        private void OnNewMessageClicked()
        {
            StartNewMessageMode();
        }

        private void OnRecipientInputChanged(ChangeEvent<string> evt)
        {
            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                _suggestionList.style.display = DisplayStyle.None;
                return;
            }

            if (_selectedRecipientId != Guid.Empty && _recipientInput.value == _suggestions.FirstOrDefault(s => s.WorldPlayerId == _selectedRecipientId)?.Username)
            {
                return;
            }

            _selectedRecipientId = Guid.Empty; 
            SearchPlayers(evt.newValue);
        }

        private void OnRecipientKeyDown(KeyDownEvent evt)
        {
            if (_suggestionList.style.display == DisplayStyle.None)
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    if (_subjectInput != null) _subjectInput.Focus();
                    else _messageInput.Focus();
                    evt.StopPropagation();
                }
                return;
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                _selectedSuggestionIndex = Math.Min(_selectedSuggestionIndex + 1, _suggestions.Count - 1);
                RenderSuggestions();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                _selectedSuggestionIndex = Math.Max(_selectedSuggestionIndex - 1, 0);
                RenderSuggestions();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (_selectedSuggestionIndex >= 0 && _selectedSuggestionIndex < _suggestions.Count)
                {
                    SelectSuggestion(_suggestions[_selectedSuggestionIndex]);
                    evt.StopPropagation();
                }
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                _suggestionList.style.display = DisplayStyle.None;
                evt.StopPropagation();
            }
        }

        private void SearchPlayers(string query)
        {
            if (NetworkManager.Instance == null) return;
            var worldId = NetworkManager.Instance.ActiveWorldId;
            if (worldId == Guid.Empty) return;

            StartCoroutine(NetworkManager.Instance.Messaging.SearchPlayers(worldId, query, NetworkManager.Instance.JwtToken, (results) =>
            {
                if (results != null && results.Count > 0)
                {
                    _suggestions = results;
                    _selectedSuggestionIndex = -1;
                    _suggestionList.style.display = DisplayStyle.Flex;
                    RenderSuggestions();
                }
                else
                {
                    _suggestionList.style.display = DisplayStyle.None;
                }
            }));
        }

        private void RenderSuggestions()
        {
            _suggestionList.Clear();
            for (int i = 0; i < _suggestions.Count; i++)
            {
                var suggestion = _suggestions[i];
                var label = new Label(suggestion.Username);
                label.AddToClassList("suggestion-item");
                if (i == _selectedSuggestionIndex) label.AddToClassList("selected");
                
                label.RegisterCallback<ClickEvent>(evt => SelectSuggestion(suggestion));
                _suggestionList.Add(label);
            }
        }

        private void SelectSuggestion(PlayerSearchResultDTO suggestion)
        {
            _selectedRecipientId = suggestion.WorldPlayerId;
            _recipientInput.SetValueWithoutNotify(suggestion.Username);
            _suggestionList.style.display = DisplayStyle.None;
            if (_conversationTitle != null) _conversationTitle.text = "New Message to: " + suggestion.Username;
            if (_subjectInput != null) _subjectInput.Focus();
        }

        private void UpdateInputAreaState()
        {
            if (_inputArea != null)
            {
                bool active = _selectedConversation != null || _isNewMessageMode;
                _inputArea.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_newMessageHeader != null)
            {
                _newMessageHeader.style.display = _isNewMessageMode ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void StartNewMessageMode()
        {
            _selectedConversation = null;
            _selectedRecipientId = Guid.Empty;
            _isNewMessageMode = true;
            if (_newMessageHeader != null) _newMessageHeader.style.display = DisplayStyle.Flex;
            if (_messageList != null) _messageList.Clear();
            if (_recipientInput != null) _recipientInput.value = "";
            if (_subjectInput != null) _subjectInput.value = "";
            if (_conversationTitle != null) _conversationTitle.text = "New Message";
            RenderConversationList();
            UpdateInputAreaState();
        }
        
        private void OnSendClicked()
        {
            var content = _messageInput.value;
            if (string.IsNullOrWhiteSpace(content)) 
            {
                Debug.LogWarning("[MessageWindow] Send failed: Content is empty.");
                return;
            }
            
            if (NetworkManager.Instance == null) return;
            var playerIds = NetworkManager.Instance.WorldPlayerId;
            if (string.IsNullOrEmpty(playerIds)) return;
            if (!Guid.TryParse(playerIds, out Guid senderId)) return;

            Guid receiverId = Guid.Empty;
            string subject = null;
            Guid? conversationId = null;

            if (_selectedConversation != null)
            {
                receiverId = _selectedConversation.ParticipantId;
                conversationId = _selectedConversation.Id;
            }
            else if (_isNewMessageMode)
            {
                if (_selectedRecipientId != Guid.Empty)
                {
                    receiverId = _selectedRecipientId;
                }
                else
                {
                   var exactMatch = _suggestions.FirstOrDefault(s => s.Username.Equals(_recipientInput.value, StringComparison.OrdinalIgnoreCase));
                    if (exactMatch != null)
                    {
                        receiverId = exactMatch.WorldPlayerId;
                    }
                    else if (Guid.TryParse(_recipientInput.value, out var parsedId))
                    {
                        receiverId = parsedId;
                    }
                }

                if (receiverId == Guid.Empty)
                {
                    Debug.LogError("[MessageWindow] Send failed: No recipient selected or found.");
                    return;
                }
                
                subject = _subjectInput != null ? _subjectInput.value : "No Subject";
            }
            else
            {
                Debug.LogWarning("[MessageWindow] Send failed: Not in new message mode and no conversation selected.");
                return;
            }
            
            Debug.Log($"[MessageWindow] Sending message to {receiverId}. Subject: {subject}, ConversationId: {conversationId}");

            StartCoroutine(NetworkManager.Instance.Messaging.SendMessage(senderId, receiverId, content, NetworkManager.Instance.JwtToken, (response) =>
            {
                if (response != null)
                {
                    Debug.Log("[MessageWindow] Message sent successfully.");
                    _messageInput.value = "";
                    
                    if (_isNewMessageMode)
                    {
                        LoadConversations(() => {});
                         if (_recipientInput != null) _recipientInput.value = "";
                         if (_subjectInput != null) _subjectInput.value = "";
                         _selectedRecipientId = Guid.Empty;
                         if (_conversationTitle != null) _conversationTitle.text = "New Message";
                    }
                    else
                    {
                        LoadMessages(_selectedConversation.Id);
                        LoadConversations(); 
                    }
                }
                else
                {
                    Debug.LogError("[MessageWindow] Send failed: Server returned null response.");
                }
            }, subject, conversationId));
        }
    }
}