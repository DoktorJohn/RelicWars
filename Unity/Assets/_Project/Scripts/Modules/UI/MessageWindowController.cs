using Assets.Scripts.Domain.Enums;
using Project.Modules.Messaging;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Network.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public partial class MessageWindowController : BaseWindow
    {
        protected override string WindowName => "Inbox";
        protected override string VisualContainerName => "WindowFrame";
        protected override string HeaderName => "Header";

        private ScrollView _conversationList;
        private ScrollView _messageList;
        private TextField _messageInput;
        private Button _sendButton;
        private Button _newConversationButton;
        private Button _deleteConversationButton;
        private VisualElement _newMessageHeader;
        private TextField _recipientInput;
        private VisualElement _recipientChips;
        private TextField _subjectInput;
        private VisualElement _suggestionList;
        private Label _conversationTitle;
        private VisualElement _inputArea;
        private Button _reportPickerButton;
        private Button _removeReportButton;
        private Label _selectedReportLabel;
        private ScrollView _reportPickerList;

        private const int SearchMinimumLength = 2;
        private const float SearchDebounceSeconds = 0.3f;
        private const int MessagePageSize = 50;

        private readonly MessagingStateManager _state = new();
        private bool _isInitialized = false;
        private Coroutine _recipientSearchCoroutine;
        private bool _deleteConfirmationPending;
        private int _requestVersion;
        private bool _hasOlderMessages;
        private bool _isLoadingOlderMessages;
        private DateTime? _oldestLoadedMessageCursor;
        private readonly List<BattleReportDTO> _shareableReports = new();
        private Guid? _selectedReportId;
        private bool _reportsLoaded;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUI();
            UpdateInputAreaState();
            LoadShareableReports(version);
            
            LoadConversations(version, () =>
            {
                if (dataPayload != null)
                {
                    if (dataPayload is Guid targetUserId)
                    {
                        StartNewMessageMode();
                        LoadRecipientProfile(targetUserId, version);
                    }
                    else if (dataPayload is string targetIdStr && Guid.TryParse(targetIdStr, out var tid))
                    {
                        StartNewMessageMode();
                        LoadRecipientProfile(tid, version);
                    }
                    else
                    {
                        CompleteDeferredOpen(version);
                    }
                }
                else
                {
                    if (_conversationTitle != null)
                    {
                        _conversationTitle.text = string.Empty;
                    }

                    CompleteDeferredOpen(version);
                }
            });
        }

        private void InitializeUI()
        {
            if (_isInitialized) return;
            if (Root == null) return;

            _conversationList = Root.Q<ScrollView>("ConversationList");
            _messageList = Root.Q<ScrollView>("MessageList");
            _messageInput = Root.Q<TextField>("MessageInput");
            _sendButton = Root.Q<Button>("SendButton");
            _newConversationButton = Root.Q<Button>("NewConversationButton");
            _deleteConversationButton = Root.Q<Button>("DeleteConversationButton");
            _newMessageHeader = Root.Q<VisualElement>("NewMessageHeader");
            _recipientInput = Root.Q<TextField>("RecipientInput");
            _recipientChips = Root.Q<VisualElement>("RecipientChips");
            _subjectInput = Root.Q<TextField>("SubjectInput");
            _suggestionList = Root.Q<VisualElement>("SuggestionList");
            _conversationTitle = Root.Q<Label>("ConversationTitle");
            _inputArea = Root.Q<VisualElement>("InputArea");
            _reportPickerButton = Root.Q<Button>("ReportPickerButton");
            _removeReportButton = Root.Q<Button>("RemoveReportButton");
            _selectedReportLabel = Root.Q<Label>("SelectedReportLabel");
            _reportPickerList = Root.Q<ScrollView>("ReportPickerList");

            if (_recipientInput != null)
            {
                _recipientInput.RegisterValueChangedCallback(OnRecipientInputChanged);
                _recipientInput.RegisterCallback<KeyDownEvent>(OnRecipientKeyDown, TrickleDown.TrickleDown);
            }

            if (_messageInput != null)
            {
                _messageInput.multiline = true;
                _messageInput.RegisterCallback<KeyDownEvent>(OnMessageInputKeyDown);
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
            if (_deleteConversationButton != null)
            {
                _deleteConversationButton.clicked -= OnDeleteConversationClicked;
                _deleteConversationButton.clicked += OnDeleteConversationClicked;
            }
            if (_reportPickerButton != null)
            {
                _reportPickerButton.clicked -= ToggleReportPicker;
                _reportPickerButton.clicked += ToggleReportPicker;
            }
            if (_removeReportButton != null)
            {
                _removeReportButton.clicked -= RemoveSelectedReport;
                _removeReportButton.clicked += RemoveSelectedReport;
            }

            _isInitialized = true;
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            _recipientSearchCoroutine = null;

            _deleteConfirmationPending = false;
            _state.SetSending(false);
            ResetMessagePaging();

            if (_sendButton != null)
            {
                _sendButton.SetEnabled(true);
                _sendButton.text = "SEND";
            }
        }

        private void LoadRecipientProfile(Guid targetUserId, int version)
        {
            if (_recipientInput != null)
            {
                _recipientInput.SetValueWithoutNotify(string.Empty);
            }

            if (NetworkManager.Instance == null)
            {
                _state.AddRecipient(targetUserId, targetUserId.ToString());
                RenderRecipientChips();
                UpdateNewMessageTitle();
                CompleteDeferredOpen(version);
                return;
            }

            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(targetUserId, NetworkManager.Instance.JwtToken, profile =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (profile != null)
                {
                    _state.AddRecipient(targetUserId, profile.UserName);
                }
                else
                {
                    _state.AddRecipient(targetUserId, targetUserId.ToString());
                }

                RenderRecipientChips();
                UpdateNewMessageTitle();
                CompleteDeferredOpen(version);
            }));
        }

        private void OnMessageInputKeyDown(KeyDownEvent evt)
        {
            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && evt.ctrlKey)
            {
                OnSendClicked();
                evt.StopPropagation();
            }
        }



        private void SetSendingState(bool isSending)
        {
            _state.SetSending(isSending);
            if (_sendButton != null)
            {
                _sendButton.SetEnabled(!isSending);
                _sendButton.text = isSending ? "SENDING" : "SEND";
            }
        }

        private void SetConversationState(string text)
        {
            if (_conversationList == null) return;
            _conversationList.Clear();
            var label = new Label(text);
            label.AddToClassList("message-window-state-label");
            _conversationList.Add(label);
        }

        private void SetMessageState(string text)
        {
            if (_messageList == null) return;
            _messageList.Clear();
            var label = new Label(text);
            label.AddToClassList("message-window-state-label");
            _messageList.Add(label);
        }

        private void ResetMessagePaging()
        {
            _hasOlderMessages = false;
            _isLoadingOlderMessages = false;
            _oldestLoadedMessageCursor = null;
        }

        private Guid ResolveCurrentWorldPlayerId()
        {
            if (NetworkManager.Instance == null || string.IsNullOrWhiteSpace(NetworkManager.Instance.WorldPlayerId))
            {
                return Guid.Empty;
            }

            return Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out var worldPlayerId)
                ? worldPlayerId
                : Guid.Empty;
        }

        private void LoadShareableReports(int version)
        {
            var worldPlayerId = ResolveCurrentWorldPlayerId();
            if (worldPlayerId == Guid.Empty || NetworkManager.Instance == null)
            {
                return;
            }

            _reportsLoaded = false;
            StartCoroutine(NetworkManager.Instance.BattleReports.GetBattleReports(worldPlayerId, NetworkManager.Instance.JwtToken, reports =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                _shareableReports.Clear();
                if (reports != null)
                {
                    _shareableReports.AddRange(reports.Where(report => report.IsPublic).OrderByDescending(report => report.OccurredAt));
                }
                _reportsLoaded = true;
                RenderReportPicker();
            }));
        }

        private void ToggleReportPicker()
        {
            if (_reportPickerList == null)
            {
                return;
            }
            RenderReportPicker();
            _reportPickerList.style.display = _reportPickerList.style.display == DisplayStyle.Flex
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private void RenderReportPicker()
        {
            if (_reportPickerList == null)
            {
                return;
            }

            _reportPickerList.Clear();
            if (!_reportsLoaded)
            {
                _reportPickerList.Add(new Label("Loading public reports..."));
                return;
            }

            if (_shareableReports.Count == 0)
            {
                var empty = new Label("No public reports. Mark a report public in Reports first.");
                empty.AddToClassList("message-window-state-label");
                _reportPickerList.Add(empty);
                return;
            }

            foreach (var report in _shareableReports)
            {
                var reportId = report.Id;
                var button = new Button(() => SelectReportAttachment(reportId))
                {
                    text = $"{report.Title} — {report.OccurredAt.ToUniversalTime():MMM d, HH:mm} UTC"
                };
                button.AddToClassList("report-picker-item");
                _reportPickerList.Add(button);
            }
        }

        private void SelectReportAttachment(Guid reportId)
        {
            var report = _shareableReports.FirstOrDefault(entry => entry.Id == reportId);
            if (report == null)
            {
                return;
            }
            _selectedReportId = report.Id;
            if (_selectedReportLabel != null) _selectedReportLabel.text = report.Title;
            if (_removeReportButton != null) _removeReportButton.style.display = DisplayStyle.Flex;
            if (_reportPickerList != null) _reportPickerList.style.display = DisplayStyle.None;
        }

        private void RemoveSelectedReport()
        {
            _selectedReportId = null;
            if (_selectedReportLabel != null) _selectedReportLabel.text = string.Empty;
            if (_removeReportButton != null) _removeReportButton.style.display = DisplayStyle.None;
            if (_reportPickerList != null) _reportPickerList.style.display = DisplayStyle.None;
        }
    }
}
