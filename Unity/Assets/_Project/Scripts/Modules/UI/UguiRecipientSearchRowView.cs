using System;
using Project.Scripts.Domain.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    /// <summary>
    /// Binds one player-search result to RecipientSearchRow.prefab.
    /// PlayerNameLabel is the only visual field in this row.
    /// </summary>
    public sealed class UguiRecipientSearchRowView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text playerNameLabel;
        [SerializeField] private Color normalBackgroundColor = new(0f, 0f, 0f, 0f);
        [SerializeField] private Color hoverBackgroundColor = new(0f, 0f, 0f, 0.24f);

        private Action _onSelected;
        private Image _hoverBackground;

        private void Awake()
        {
            _hoverBackground = transform.Find("HoverBackground")?.GetComponent<Image>();
            SetHovered(false);
        }

        private void OnDisable() => SetHovered(false);

        public void Bind(PlayerSearchResultDTO player, Action onSelected)
        {
            _onSelected = onSelected;
            if (playerNameLabel != null) playerNameLabel.text = player?.Username ?? string.Empty;
        }

        public void BindMessage(string message)
        {
            _onSelected = null;
            if (playerNameLabel != null) playerNameLabel.text = message ?? string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _onSelected?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);

        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

        private void SetHovered(bool hovered)
        {
            if (_hoverBackground != null)
                _hoverBackground.color = hovered ? hoverBackgroundColor : normalBackgroundColor;
        }
    }
}
