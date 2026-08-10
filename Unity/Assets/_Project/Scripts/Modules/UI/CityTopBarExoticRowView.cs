using Assets.Scripts.Domain.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class CityTopBarExoticRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text resourceNameLabel;
        [SerializeField] private TMP_Text stockLabel;
        [SerializeField] private TMP_Text rateLabel;

        public ExoticResourceTypeEnum ResourceType { get; private set; }

        public void Bind(ExoticResourceTypeEnum resourceType, Sprite icon)
        {
            ResourceType = resourceType;
            if (iconImage != null) iconImage.sprite = icon;
            if (resourceNameLabel != null) resourceNameLabel.text = resourceType.ToString().ToUpperInvariant();
        }

        public void SetValues(double amount, double production)
        {
            if (stockLabel != null) stockLabel.text = amount.ToString("N0");
            if (rateLabel == null) return;

            rateLabel.text = $"({CityTopBarViewController.FormatProductionValue(production)}/hr)";
            rateLabel.color = production < 0d
                ? new Color(0.82f, 0.22f, 0.18f, 1f)
                : new Color(0.28f, 0.55f, 0.28f, 1f);
        }
    }
}
