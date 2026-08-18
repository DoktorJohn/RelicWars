using Project.Scripts.Domain.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiDailyRewardItemView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private Color collectableBackgroundColor = new(0.45f, 1f, 0.45f, 1f);

        private Color _defaultBackgroundColor = Color.white;

        private void Awake()
        {
            if (background != null) _defaultBackgroundColor = background.color;
        }

        public void Bind(DailyObjectiveRewardDTO reward, Sprite rewardIcon, bool isCollectable)
        {
            if (background != null)
                background.color = isCollectable ? collectableBackgroundColor : _defaultBackgroundColor;
            if (icon != null) icon.sprite = rewardIcon;
            if (amountLabel != null) amountLabel.text = $"{reward.Amount:N0}";
        }
    }
}
