using Assets.Scripts.Domain.Enums;
using Project.Modules.Messaging;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public partial class MessageWindowController
    {
        private void OnNewMessageClicked()
        {
            StartNewMessageMode();
        }

        private void OnRecipientInputChanged(ChangeEvent<string> evt)
        {
            var query = evt.newValue?.Trim() ?? string.Empty;
            _state.SetSearchQuery(query);

            if (query.Length < SearchMinimumLength)
            {
                _suggestionList.style.display = DisplayStyle.None;
                _state.SetSuggestions(null);
                return;
            }

            if (_state.SelectedRecipientId != Guid.Empty && query == _state.Suggestions.FirstOrDefault(s => s.WorldPlayerId == _state.SelectedRecipientId)?.Username)
            {
                return;
            }

            _state.ClearRecipient();
            if (_recipientSearchCoroutine != null)
            {
                StopCoroutine(_recipientSearchCoroutine);
            }
            if (_suggestionList != null)
            {
                _suggestionList.style.display = DisplayStyle.None;
            }
            _recipientSearchCoroutine = StartCoroutine(SearchPlayersDebounced(query));
        }

        private void OnDeleteConversationClicked()
        {
            if (_state.SelectedConversation == null || NetworkManager.Instance == null) return;
            if (!Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid worldPlayerId)) return;

            if (!_deleteConfirmationPending)
            {
                _deleteConfirmationPending = true;
                _deleteConversationButton.text = "CONFIRM";
                return;
            }

            var conversationId = _state.SelectedConversation.Id;
            var version = _requestVersion;
            _deleteConversationButton.SetEnabled(false);
            StartCoroutine(NetworkManager.Instance.Messaging.DeleteConversation(
                worldPlayerId, conversationId, NetworkManager.Instance.JwtToken, success =>
                {
                    if (!isActiveAndEnabled || version != _requestVersion)
                    {
                        return;
                    }

                    _deleteConversationButton.SetEnabled(true);
                    if (!success)
                    {
                        ResetDeleteConfirmation();
                        SetMessageState("Failed to delete conversation");
                        return;
                    }

                    _state.RemoveSelectedConversation();
                    ResetMessagePaging();
                    ResetDeleteConfirmation();
                    _deleteConversationButton.style.display = DisplayStyle.None;
                    if (_conversationTitle != null) _conversationTitle.text = string.Empty;
                    SetMessageState(string.Empty);
                    UpdateInputAreaState();
                    RenderConversationList();
                    MessagingStateEvents.RaiseUnreadStateChanged();
                }));
        }

        private void ResetDeleteConfirmation()
        {
            _deleteConfirmationPending = false;
            if (_deleteConversationButton != null) _deleteConversationButton.text = "DELETE";
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
                _state.MoveSuggestion(1);
                RenderSuggestions();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                _state.MoveSuggestion(-1);
                RenderSuggestions();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (_state.SelectedSuggestionIndex >= 0 && _state.SelectedSuggestionIndex < _state.Suggestions.Count)
                {
                    SelectSuggestion(_state.Suggestions[_state.SelectedSuggestionIndex]);
                    evt.StopPropagation();
                }
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                _suggestionList.style.display = DisplayStyle.None;
                evt.StopPropagation();
            }
        }

        private IEnumerator SearchPlayersDebounced(string query)
        {
            yield return new WaitForSeconds(SearchDebounceSeconds);
            if (query != _state.LatestSearchQuery) yield break;
            SearchPlayers(query);
        }

        private void SearchPlayers(string query)
        {
            if (NetworkManager.Instance == null) return;
            var worldId = NetworkManager.Instance.ActiveWorldId;
            if (worldId == Guid.Empty)
            {
                if (_suggestionList != null)
                {
                    _suggestionList.style.display = DisplayStyle.None;
                }
                return;
            }

            var requestedQuery = query.Trim();
            var version = _requestVersion;
            StartCoroutine(NetworkManager.Instance.Messaging.SearchPlayers(worldId, query, NetworkManager.Instance.JwtToken, (results) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (requestedQuery != _state.LatestSearchQuery) return;

                if (results != null && results.Count > 0)
                {
                    _state.SetSuggestions(results);
                    _suggestionList.style.display = DisplayStyle.Flex;
                    RenderSuggestions();
                }
                else
                {
                    _state.SetSuggestions(null);
                    _suggestionList.style.display = DisplayStyle.None;
                }
            }));
        }

        private void RenderSuggestions()
        {
            _suggestionList.Clear();
            for (int i = 0; i < _state.Suggestions.Count; i++)
            {
                var suggestion = _state.Suggestions[i];
                var label = new Label(suggestion.Username);
                label.AddToClassList("suggestion-item");
                if (i == _state.SelectedSuggestionIndex) label.AddToClassList("selected");
                
                label.RegisterCallback<ClickEvent>(evt => SelectSuggestion(suggestion));
                _suggestionList.Add(label);
            }
        }

        private void SelectSuggestion(PlayerSearchResultDTO suggestion)
        {
            if (suggestion == null) return;
            _state.SelectRecipient(suggestion);
            _recipientInput.SetValueWithoutNotify(suggestion.Username);
            _suggestionList.style.display = DisplayStyle.None;
            if (_conversationTitle != null) _conversationTitle.text = "New Message to: " + suggestion.Username;
            if (_subjectInput != null) _subjectInput.Focus();
        }

        private void UpdateInputAreaState()
        {
            if (_inputArea != null)
            {
                bool active = _state.SelectedConversation != null || _state.IsComposing;
                _inputArea.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_newMessageHeader != null)
            {
                _newMessageHeader.style.display = _state.IsComposing ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void StartNewMessageMode()
        {
            _state.StartComposing();
            ResetMessagePaging();
            ResetDeleteConfirmation();
            if (_deleteConversationButton != null) _deleteConversationButton.style.display = DisplayStyle.None;
            if (_newMessageHeader != null) _newMessageHeader.style.display = DisplayStyle.Flex;
            if (_messageList != null) _messageList.Clear();
            if (_recipientInput != null) _recipientInput.value = "";
            if (_subjectInput != null) _subjectInput.value = "";
            if (_conversationTitle != null) _conversationTitle.text = "New Message";
            SetMessageState("Write a message");
            RenderConversationList();
            UpdateInputAreaState();
        }
        
    }
}
