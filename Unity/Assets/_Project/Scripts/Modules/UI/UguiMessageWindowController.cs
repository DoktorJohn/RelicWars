using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Modules.Messaging;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiMessageWindowController : MonoBehaviour
    {
        private const int ConversationPageSize = 10;
        private const int MessagePageSize = 4;
        private const float RecipientSearchDelay = 0.3f;
        private const float RecipientSearchRowHeight = 46f;
        private const float RecipientSearchRowInset = 4f;

        [Header("Views")]
        [SerializeField] private GameObject newMessageView;
        [SerializeField] private GameObject messageView;
        [SerializeField] private Transform conversationRowsContainer;
        [SerializeField] private UguiMessageConversationRowView conversationRowTemplate;
        [SerializeField] private RectTransform recipientSuggestionsDropdown;
        [SerializeField] private UguiRecipientSearchRowView recipientSearchRowPrefab;
        [SerializeField] private RectTransform recipientSearchRowEditorPreview;
        [SerializeField] private Transform messageRowsContainer;
        [SerializeField] private RectTransform messageRowsFrame;
        [SerializeField] private RectTransform yourMessageTemplate;
        [SerializeField] private RectTransform theirMessageTemplate;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField conversationSearchInput;
        [SerializeField] private TMP_InputField recipientsInput;
        [SerializeField] private TMP_InputField subjectInput;
        [SerializeField] private TMP_InputField newConversationInput;
        [SerializeField] private TMP_InputField replyInput;

        [Header("Labels")]
        [SerializeField] private TMP_Text recipientHeaderLabel;
        [SerializeField] private TMP_Text subjectHeaderLabel;
        [SerializeField] private TMP_Text newMessageRecipientsLabel;
        [SerializeField] private Text pageLabel;

        [Header("Recipient scrolling")]
        [SerializeField] private ScrollRect newMessageRecipientsScroller;
        [SerializeField] private ScrollRect messageRecipientsScroller;

        [Header("Actions")]
        [SerializeField] private CarvedPressButton newConversationButton;
        [SerializeField] private CarvedPressButton deleteConversationButton;
        [SerializeField] private RectTransform newConversationSendButtonRoot;
        [SerializeField] private RectTransform replySendButtonRoot;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;

        private readonly MessagingStateManager _state = new();
        private readonly List<List<MessageDTO>> _messagePages = new();
        private readonly List<DateTime?> _olderPageCursors = new();
        private readonly List<UguiRecipientSearchRowView> _recipientSearchRows = new();
        private CarvedPressButton _newConversationSendButton;
        private CarvedPressButton _replySendButton;
        private Coroutine _recipientSearchCoroutine;
        private Guid _currentPlayerId;
        private int _conversationPage;
        private int _messagePage;
        private int _requestVersion;
        private bool _loadingMessages;
        private bool _hasOlderMessages;
        private bool _deleteConfirmationPending;
        private bool _deletingConversation;
        private bool _suppressRecipientInput;

        private void Awake()
        {
            _newConversationSendButton = newConversationSendButtonRoot?.GetComponentInChildren<CarvedPressButton>(true);
            _replySendButton = replySendButtonRoot?.GetComponentInChildren<CarvedPressButton>(true);
            if (conversationRowTemplate != null) conversationRowTemplate.gameObject.SetActive(false);
            if (recipientSearchRowEditorPreview != null) recipientSearchRowEditorPreview.gameObject.SetActive(false);
            SetRecipientSuggestionsVisible(false);
            if (yourMessageTemplate != null) yourMessageTemplate.gameObject.SetActive(false);
            if (theirMessageTemplate != null) theirMessageTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _requestVersion++;
            ResolveCurrentPlayer();
            RegisterListeners();
            StartNewConversationMode();
            LoadConversations(_requestVersion);
        }

        private void OnDisable()
        {
            _requestVersion++;
            UnregisterListeners();
            StopAllCoroutines();
            _recipientSearchCoroutine = null;
            SetRecipientSuggestionsVisible(false);
            _deletingConversation = false;
            ResetDeleteConfirmation();
            SetSending(false);
        }

        private void RegisterListeners()
        {
            if (conversationSearchInput != null) conversationSearchInput.onValueChanged.AddListener(OnConversationSearchChanged);
            if (recipientsInput != null) recipientsInput.onValueChanged.AddListener(OnRecipientsTextChanged);
            if (newConversationButton != null) newConversationButton.OnButtonActivatedClicked += OnNewConversationClicked;
            if (deleteConversationButton != null) deleteConversationButton.OnButtonActivatedClicked += OnDeleteConversationClicked;
            if (_newConversationSendButton != null) _newConversationSendButton.OnButtonActivatedClicked += OnNewConversationSendClicked;
            if (_replySendButton != null) _replySendButton.OnButtonActivatedClicked += OnReplySendClicked;
            if (previousPageButton != null) previousPageButton.onClick.AddListener(PreviousPage);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        }

        private void UnregisterListeners()
        {
            if (conversationSearchInput != null) conversationSearchInput.onValueChanged.RemoveListener(OnConversationSearchChanged);
            if (recipientsInput != null) recipientsInput.onValueChanged.RemoveListener(OnRecipientsTextChanged);
            if (newConversationButton != null) newConversationButton.OnButtonActivatedClicked -= OnNewConversationClicked;
            if (deleteConversationButton != null) deleteConversationButton.OnButtonActivatedClicked -= OnDeleteConversationClicked;
            if (_newConversationSendButton != null) _newConversationSendButton.OnButtonActivatedClicked -= OnNewConversationSendClicked;
            if (_replySendButton != null) _replySendButton.OnButtonActivatedClicked -= OnReplySendClicked;
            if (previousPageButton != null) previousPageButton.onClick.RemoveListener(PreviousPage);
            if (nextPageButton != null) nextPageButton.onClick.RemoveListener(NextPage);
        }

        private void ResolveCurrentPlayer()
        {
            _currentPlayerId = NetworkManager.Instance != null &&
                               Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid id)
                ? id
                : Guid.Empty;
        }

        private void LoadConversations(int version, Action onLoaded = null)
        {
            if (!CanUseMessaging())
            {
                RenderConversationState("Messaging unavailable");
                return;
            }

            RenderConversationState("Loading conversations...");
            StartCoroutine(NetworkManager.Instance.Messaging.GetConversations(
                _currentPlayerId,
                NetworkManager.Instance.JwtToken,
                conversations =>
                {
                    if (!CanApply(version)) return;
                    if (conversations == null)
                    {
                        RenderConversationState("Failed to load conversations");
                        return;
                    }

                    _state.SetConversations(conversations);
                    _conversationPage = 0;
                    RenderConversationList();
                    onLoaded?.Invoke();
                }));
        }

        private void RenderConversationList()
        {
            string query = conversationSearchInput?.text?.Trim() ?? string.Empty;
            List<ConversationDTO> filtered = _state.Conversations
                .Where(conversation => MatchesConversation(conversation, query))
                .OrderByDescending(conversation => conversation.LastMessageDate)
                .ToList();

            int pageCount = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)ConversationPageSize));
            _conversationPage = Mathf.Clamp(_conversationPage, 0, pageCount - 1);
            ClearConversationRows();

            foreach (ConversationDTO conversation in filtered
                         .Skip(_conversationPage * ConversationPageSize)
                         .Take(ConversationPageSize))
            {
                UguiMessageConversationRowView row = Instantiate(conversationRowTemplate, conversationRowsContainer, false);
                row.gameObject.SetActive(true);
                bool selected = _state.SelectedConversation?.Id == conversation.Id;
                row.BindConversation(conversation, _currentPlayerId, selected, () => SelectConversation(conversation));
            }

            if (filtered.Count == 0) RenderConversationState("No conversations");
            UpdatePagination(pageCount, _conversationPage);
        }

        private static bool MatchesConversation(ConversationDTO conversation, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            return Contains(conversation.Subject, query) ||
                   Contains(conversation.ParticipantName, query) ||
                   Contains(conversation.LastMessageContent, query) ||
                   conversation.Participants?.Any(participant => Contains(participant?.Username, query)) == true;
        }

        private static bool Contains(string value, string query) =>
            !string.IsNullOrWhiteSpace(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private void SelectConversation(ConversationDTO conversation)
        {
            if (conversation == null) return;
            _state.SelectConversation(conversation);
            SetRecipientSuggestionsVisible(false);
            newMessageView?.SetActive(false);
            messageView?.SetActive(true);
            ResetDeleteConfirmation();
            SetConversationHeader(conversation);
            ClearMessagePaging();
            RenderConversationList();
            LoadMessagePage(null, true);
        }

        private void LoadMessagePage(DateTime? before, bool replacePages)
        {
            ConversationDTO selected = _state.SelectedConversation;
            if (selected == null || !CanUseMessaging() || _loadingMessages) return;

            int version = _requestVersion;
            Guid conversationId = selected.Id;
            _loadingMessages = true;
            UpdateActionState();
            StartCoroutine(NetworkManager.Instance.Messaging.GetMessages(
                _currentPlayerId,
                conversationId,
                before,
                MessagePageSize,
                NetworkManager.Instance.JwtToken,
                messages =>
                {
                    if (!CanApply(version) || _state.SelectedConversation?.Id != conversationId) return;
                    _loadingMessages = false;
                    if (messages == null)
                    {
                        RenderMessageState("Failed to load messages");
                        UpdateActionState();
                        return;
                    }

                    List<MessageDTO> ordered = messages.OrderBy(message => message.SentAt).ThenBy(message => message.Id).ToList();
                    if (!replacePages && ordered.Count == 0)
                    {
                        _hasOlderMessages = false;
                        RenderCurrentMessagePage();
                        return;
                    }

                    if (replacePages)
                    {
                        _messagePages.Clear();
                        _olderPageCursors.Clear();
                        _messagePage = 0;
                    }
                    _messagePages.Add(ordered);
                    _olderPageCursors.Add(ordered.Count > 0 ? ordered.Min(message => message.SentAt).ToUniversalTime() : null);
                    _hasOlderMessages = messages.Count >= MessagePageSize;
                    _messagePage = _messagePages.Count - 1;
                    RenderCurrentMessagePage();
                    MarkSelectedConversationRead(conversationId, version);
                }));
        }

        private void RenderCurrentMessagePage()
        {
            ClearMessageRows();
            if (_messagePages.Count == 0 || _messagePages[_messagePage].Count == 0)
            {
                RenderMessageState("No messages");
                UpdateActionState();
                return;
            }

            foreach (MessageDTO message in _messagePages[_messagePage])
            {
                RectTransform template = message.SenderId == _currentPlayerId ? yourMessageTemplate : theirMessageTemplate;
                RectTransform instance = Instantiate(template, messageRowsContainer, false);
                instance.gameObject.SetActive(true);
                UguiMessageBubbleView bubble = instance.GetComponent<UguiMessageBubbleView>();
                if (bubble != null) bubble.Bind(message);
            }

            UpdateActionState();
        }

        private void MarkSelectedConversationRead(Guid conversationId, int version)
        {
            StartCoroutine(NetworkManager.Instance.Messaging.MarkConversationAsRead(
                _currentPlayerId,
                conversationId,
                NetworkManager.Instance.JwtToken,
                success =>
                {
                    if (!CanApply(version) || !success || _state.SelectedConversation?.Id != conversationId) return;
                    _state.MarkSelectedRead();
                    RenderConversationList();
                    MessagingStateEvents.RaiseUnreadStateChanged();
                }));
        }

        private void OnNewConversationClicked(CarvedPressButton _) => StartNewConversationMode();

        private void StartNewConversationMode()
        {
            _state.StartComposing();
            ClearMessagePaging();
            messageView?.SetActive(false);
            newMessageView?.SetActive(true);
            if (newConversationInput != null) newConversationInput.SetTextWithoutNotify(string.Empty);
            if (subjectInput != null) subjectInput.SetTextWithoutNotify(string.Empty);
            SetRecipientsText(string.Empty);
            UpdateComposeRecipientsLabel();
            SetRecipientSuggestionsVisible(false);
            ResetDeleteConfirmation();
            RenderConversationList();
        }

        private void OnConversationSearchChanged(string _)
        {
            _conversationPage = 0;
            RenderConversationList();
        }

        private void OnRecipientsTextChanged(string value)
        {
            if (_suppressRecipientInput) return;
            ReconcileRemovedRecipients(value);
            UpdateComposeRecipientsLabel();
            string query = GetRecipientQuery(value);
            _state.SetSearchQuery(query);
            if (_recipientSearchCoroutine != null) StopCoroutine(_recipientSearchCoroutine);
            if (query.Length < 2)
            {
                _state.SetSuggestions(null);
                SetRecipientSuggestionsVisible(false);
                return;
            }
            _recipientSearchCoroutine = StartCoroutine(SearchRecipientsDebounced(query, _requestVersion));
        }

        private IEnumerator SearchRecipientsDebounced(string query, int version)
        {
            yield return new WaitForSecondsRealtime(RecipientSearchDelay);
            if (!CanApply(version) || query != _state.LatestSearchQuery || NetworkManager.Instance.ActiveWorldId == Guid.Empty) yield break;

            yield return NetworkManager.Instance.Messaging.SearchPlayers(
                NetworkManager.Instance.ActiveWorldId,
                query,
                NetworkManager.Instance.JwtToken,
                suggestions =>
                {
                    if (!CanApply(version) || query != _state.LatestSearchQuery) return;
                    _state.SetSuggestions(suggestions?.Where(suggestion =>
                        suggestion != null && suggestion.WorldPlayerId != Guid.Empty &&
                        suggestion.WorldPlayerId != _currentPlayerId && !_state.HasRecipient(suggestion.WorldPlayerId)));
                    RenderRecipientSuggestions();
                });
        }

        private void RenderRecipientSuggestions()
        {
            ClearRecipientSuggestionRows();
            if (recipientSuggestionsDropdown == null || recipientSearchRowPrefab == null) return;

            SetRecipientSuggestionsVisible(true);
            if (_state.Suggestions.Count == 0)
            {
                UguiRecipientSearchRowView emptyRow = CreateRecipientSearchRow();
                if (emptyRow == null) return;
                emptyRow.gameObject.SetActive(true);
                emptyRow.BindMessage("No players found");
                return;
            }

            foreach (PlayerSearchResultDTO suggestion in _state.Suggestions.Take(5))
            {
                UguiRecipientSearchRowView row = CreateRecipientSearchRow();
                if (row == null) return;
                row.gameObject.SetActive(true);
                row.Bind(suggestion, () => AddRecipient(suggestion));
            }
        }

        private void AddRecipient(PlayerSearchResultDTO suggestion)
        {
            if (!_state.AddRecipient(suggestion)) return;
            SetRecipientsText(string.Join("; ", _state.SelectedRecipients.Select(recipient => recipient.Username)) + "; ");
            _state.SetSearchQuery(string.Empty);
            _state.SetSuggestions(null);
            UpdateComposeRecipientsLabel();
            SetRecipientSuggestionsVisible(false);
            recipientsInput?.ActivateInputField();
        }

        private void SetRecipientSuggestionsVisible(bool visible)
        {
            if (!visible) ClearRecipientSuggestionRows();
            if (recipientSuggestionsDropdown != null)
                recipientSuggestionsDropdown.gameObject.SetActive(visible);
        }

        private void ClearRecipientSuggestionRows()
        {
            foreach (UguiRecipientSearchRowView row in _recipientSearchRows)
                if (row != null) Destroy(row.gameObject);
            _recipientSearchRows.Clear();
        }

        private UguiRecipientSearchRowView CreateRecipientSearchRow()
        {
            UguiRecipientSearchRowView row = Instantiate(recipientSearchRowPrefab, recipientSuggestionsDropdown, false);
            RectTransform rowRect = row.transform as RectTransform;
            int rowIndex = _recipientSearchRows.Count;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(RecipientSearchRowInset, -RecipientSearchRowInset - rowIndex * RecipientSearchRowHeight);
                rowRect.sizeDelta = new Vector2(-RecipientSearchRowInset * 2f, RecipientSearchRowHeight);
            }
            row.transform.SetAsLastSibling();
            _recipientSearchRows.Add(row);
            return row;
        }

        private void ReconcileRemovedRecipients(string value)
        {
            string[] acceptedNames = value.Split(';').Select(token => token.Trim()).Where(token => token.Length > 0).ToArray();
            foreach (PlayerSearchResultDTO recipient in _state.SelectedRecipients.ToList())
                if (!acceptedNames.Any(name => string.Equals(name, recipient.Username, StringComparison.OrdinalIgnoreCase)))
                    _state.RemoveRecipient(recipient.WorldPlayerId);
        }

        private static string GetRecipientQuery(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            int separator = value.LastIndexOf(';');
            return value.Substring(separator + 1).Trim();
        }

        private void SetRecipientsText(string value)
        {
            if (recipientsInput == null) return;
            _suppressRecipientInput = true;
            recipientsInput.SetTextWithoutNotify(value ?? string.Empty);
            recipientsInput.caretPosition = recipientsInput.text.Length;
            _suppressRecipientInput = false;
        }

        private void OnNewConversationSendClicked(CarvedPressButton _)
        {
            if (_state.IsSending || _state.SelectedRecipients.Count == 0 || string.IsNullOrWhiteSpace(newConversationInput?.text)) return;
            if (!CanUseMessaging()) return;

            string content = newConversationInput.text;
            string subject = string.IsNullOrWhiteSpace(subjectInput?.text) ? "No Subject" : subjectInput.text.Trim();
            List<Guid> recipients = _state.SelectedRecipients.Select(recipient => recipient.WorldPlayerId).Distinct().ToList();
            int version = _requestVersion;
            SetSending(true);
            StartCoroutine(NetworkManager.Instance.Messaging.StartConversation(
                _currentPlayerId,
                recipients,
                subject,
                content,
                null,
                NetworkManager.Instance.JwtToken,
                conversation =>
                {
                    if (!CanApply(version)) return;
                    SetSending(false);
                    if (conversation == null) return;
                    newConversationInput.SetTextWithoutNotify(string.Empty);
                    subjectInput?.SetTextWithoutNotify(string.Empty);
                    MessagingStateEvents.RaiseUnreadStateChanged();
                    LoadConversations(version, () =>
                    {
                        ConversationDTO loaded = _state.Conversations.FirstOrDefault(item => item.Id == conversation.Id) ?? conversation;
                        SelectConversation(loaded);
                    });
                }));
        }

        private void OnDeleteConversationClicked(CarvedPressButton _)
        {
            ConversationDTO selected = _state.SelectedConversation;
            if (selected == null || _deletingConversation || !CanUseMessaging()) return;

            if (!_deleteConfirmationPending)
            {
                _deleteConfirmationPending = true;
                deleteConversationButton?.SetTextOnLabel("CONFIRM");
                return;
            }

            Guid conversationId = selected.Id;
            int version = _requestVersion;
            _deletingConversation = true;
            SetButtonEnabled(deleteConversationButton, false);
            StartCoroutine(NetworkManager.Instance.Messaging.DeleteConversation(
                _currentPlayerId,
                conversationId,
                NetworkManager.Instance.JwtToken,
                success =>
                {
                    if (!CanApply(version)) return;
                    _deletingConversation = false;
                    SetButtonEnabled(deleteConversationButton, true);
                    if (!success)
                    {
                        ResetDeleteConfirmation();
                        Debug.LogError("[UguiMessageWindow] Failed to delete conversation.");
                        return;
                    }

                    _state.RemoveSelectedConversation();
                    ClearMessagePaging();
                    ResetDeleteConfirmation();
                    messageView?.SetActive(false);
                    newMessageView?.SetActive(true);
                    RenderConversationList();
                    MessagingStateEvents.RaiseUnreadStateChanged();
                }));
        }

        private void OnReplySendClicked(CarvedPressButton _)
        {
            if (_state.IsSending || _state.SelectedConversation == null || string.IsNullOrWhiteSpace(replyInput?.text)) return;
            if (!CanUseMessaging()) return;

            Guid conversationId = _state.SelectedConversation.Id;
            string content = replyInput.text;
            int version = _requestVersion;
            SetSending(true);
            StartCoroutine(NetworkManager.Instance.Messaging.ReplyToConversation(
                _currentPlayerId,
                conversationId,
                content,
                null,
                NetworkManager.Instance.JwtToken,
                message =>
                {
                    if (!CanApply(version)) return;
                    SetSending(false);
                    if (_state.SelectedConversation?.Id != conversationId) return;
                    if (message == null) return;
                    replyInput.SetTextWithoutNotify(string.Empty);
                    MessagingStateEvents.RaiseUnreadStateChanged();
                    ClearMessagePaging();
                    LoadMessagePage(null, true);
                    LoadConversations(version);
                }));
        }

        private void PreviousPage()
        {
            if (_conversationPage <= 0) return;
            _conversationPage--;
            RenderConversationList();
        }

        private void NextPage()
        {
            int filteredCount = _state.Conversations.Count(conversation => MatchesConversation(conversation, conversationSearchInput?.text?.Trim()));
            if ((_conversationPage + 1) * ConversationPageSize >= filteredCount) return;
            _conversationPage++;
            RenderConversationList();
        }

        private void UpdatePagination(int pageCount, int pageIndex)
        {
            if (pageLabel != null) pageLabel.text = $"INBOX {pageIndex + 1}/{Mathf.Max(1, pageCount)}";
            if (previousPageButton != null) previousPageButton.interactable = pageIndex > 0;
            if (nextPageButton != null)
                nextPageButton.interactable = pageIndex + 1 < pageCount;
        }

        private void SetConversationHeader(ConversationDTO conversation)
        {
            if (conversation == null) return;
            if (recipientHeaderLabel != null)
            {
                IEnumerable<string> names = conversation.Participants?
                    .Where(participant => participant.WorldPlayerId != _currentPlayerId)
                    .Select(participant => participant.Username)
                    .Where(name => !string.IsNullOrWhiteSpace(name)) ?? Enumerable.Empty<string>();
                string joined = string.Join("; ", names.Distinct());
                recipientHeaderLabel.text = !string.IsNullOrWhiteSpace(conversation.ParticipantName)
                    ? conversation.ParticipantName
                    : !string.IsNullOrWhiteSpace(joined)
                        ? joined
                        : "Unknown recipient";
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(recipientHeaderLabel.rectTransform);
                if (messageRecipientsScroller?.viewport != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(messageRecipientsScroller.viewport);
                ResetHorizontalScroll(messageRecipientsScroller, 0f);
            }
            if (subjectHeaderLabel != null)
                subjectHeaderLabel.text = string.IsNullOrWhiteSpace(conversation.Subject) ? "No Subject" : conversation.Subject;
        }

        private void SetSending(bool sending)
        {
            _state.SetSending(sending);
            if (newConversationInput != null) newConversationInput.interactable = !sending;
            if (subjectInput != null) subjectInput.interactable = !sending;
            if (replyInput != null) replyInput.interactable = !sending;
            if (recipientsInput != null) recipientsInput.interactable = !sending;
            SetButtonEnabled(_newConversationSendButton, !sending);
            SetButtonEnabled(_replySendButton, !sending);
            SetButtonEnabled(deleteConversationButton, !sending && !_deletingConversation);
            UpdateActionState();
        }

        private void UpdateComposeRecipientsLabel()
        {
            if (newMessageRecipientsLabel != null)
            {
                newMessageRecipientsLabel.text = string.Join("; ", _state.SelectedRecipients.Select(recipient => recipient.Username));
                ResetHorizontalScroll(newMessageRecipientsScroller, 1f);
            }
        }

        private static void ResetHorizontalScroll(ScrollRect scrollRect, float normalizedPosition)
        {
            if (scrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            scrollRect.horizontalNormalizedPosition = normalizedPosition;
        }

        private void ResetDeleteConfirmation()
        {
            _deleteConfirmationPending = false;
            deleteConversationButton?.SetTextOnLabel("DELETE");
        }

        private void UpdateActionState()
        {
            // Inbox pagination is independent of message-history loading.
        }

        private static void SetButtonEnabled(CarvedPressButton button, bool enabled)
        {
            if (button == null) return;
            button.enabled = enabled;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.interactable = enabled;
                group.blocksRaycasts = enabled;
            }
        }

        private void RenderConversationState(string text)
        {
            ClearConversationRows();
            if (conversationRowTemplate == null || conversationRowsContainer == null) return;
            UguiMessageConversationRowView row = Instantiate(conversationRowTemplate, conversationRowsContainer, false);
            row.gameObject.SetActive(true);
            row.BindState(text);
        }

        private void RenderMessageState(string text)
        {
            ClearMessageRows();
            RectTransform template = theirMessageTemplate != null ? theirMessageTemplate : yourMessageTemplate;
            if (template == null || messageRowsContainer == null) return;
            RectTransform instance = Instantiate(template, messageRowsContainer, false);
            instance.gameObject.SetActive(true);
            UguiMessageBubbleView bubble = instance.GetComponent<UguiMessageBubbleView>();
            if (bubble != null) bubble.Bind(new MessageDTO { Content = text });
        }

        private void ClearConversationRows()
        {
            if (conversationRowsContainer == null) return;
            for (int index = conversationRowsContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = conversationRowsContainer.GetChild(index);
                if (conversationRowTemplate == null || child != conversationRowTemplate.transform) Destroy(child.gameObject);
            }
        }

        private void ClearMessageRows()
        {
            if (messageRowsContainer == null) return;
            for (int index = messageRowsContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = messageRowsContainer.GetChild(index);
                if ((messageRowsFrame == null || child != messageRowsFrame) &&
                    (yourMessageTemplate == null || child != yourMessageTemplate) &&
                    (theirMessageTemplate == null || child != theirMessageTemplate))
                    Destroy(child.gameObject);
            }
        }

        private void ClearMessagePaging()
        {
            _messagePages.Clear();
            _olderPageCursors.Clear();
            _messagePage = 0;
            _hasOlderMessages = false;
            _loadingMessages = false;
            ClearMessageRows();
        }

        private bool CanUseMessaging() => NetworkManager.Instance?.Messaging != null && _currentPlayerId != Guid.Empty;
        private bool CanApply(int version) => isActiveAndEnabled && version == _requestVersion;

        /* FUTURE UGUI CONTROLS (pseudocode only):
         * attachReportButton.clicked => open report picker => selectedReportId
         * send/reply => include selectedReportId, then clear it after success
         * Message history deliberately remains page-based through the authored pagination controls.
         */
    }
}
