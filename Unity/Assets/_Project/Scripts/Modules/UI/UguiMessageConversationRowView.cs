using System;
using Project.Scripts.Domain.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiMessageConversationRowView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text senderLabel;
        [SerializeField] private TMP_Text subjectLabel;
        [SerializeField] private TMP_Text timestampLabel;
        [SerializeField] private Image selectionGraphic;
        [SerializeField] private Color selectedColor = new(0.72f, 0.88f, 1f, 0.55f);
        [SerializeField] private Color unreadColor = new(1f, 0.91f, 0.55f, 0.45f);

        private Action _onClick;
        private Color _defaultColor;

        private void Awake()
        {
            if (selectionGraphic != null) _defaultColor = selectionGraphic.color;
        }

        public void BindConversation(ConversationDTO conversation, Guid currentPlayerId, bool selected, Action onClick)
        {
            _onClick = onClick;
            if (senderLabel != null) senderLabel.text = FormatParticipants(conversation, currentPlayerId);
            if (subjectLabel != null) subjectLabel.text = string.IsNullOrWhiteSpace(conversation.Subject) ? "No Subject" : conversation.Subject;
            if (timestampLabel != null) timestampLabel.text = conversation.LastMessageDate.ToLocalTime().ToString("dd/MM HH:mm");
            if (selectionGraphic != null)
                selectionGraphic.color = selected ? selectedColor : conversation.UnreadCount > 0 ? unreadColor : _defaultColor;
        }

        public void BindState(string text)
        {
            _onClick = null;
            if (senderLabel != null) senderLabel.text = text ?? string.Empty;
            if (subjectLabel != null) subjectLabel.text = string.Empty;
            if (timestampLabel != null) timestampLabel.text = string.Empty;
            if (selectionGraphic != null) selectionGraphic.color = _defaultColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _onClick?.Invoke();
        }

        private static string FormatParticipants(ConversationDTO conversation, Guid currentPlayerId)
        {
            if (conversation?.Participants != null)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (ConversationParticipantDTO participant in conversation.Participants)
                    if (participant != null && participant.WorldPlayerId != currentPlayerId && !string.IsNullOrWhiteSpace(participant.Username))
                        names.Add(participant.Username);
                if (names.Count > 0) return string.Join("; ", names);
            }
            return conversation?.ParticipantName ?? string.Empty;
        }
    }
}
