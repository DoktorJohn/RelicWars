using Project.Scripts.Domain.DTOs;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiMessageBubbleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageLabel;

        public void Bind(MessageDTO message)
        {
            if (messageLabel != null) messageLabel.text = message?.Content ?? string.Empty;
        }
    }
}
