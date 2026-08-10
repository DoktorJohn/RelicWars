using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public enum CityTopBarResourceType
    {
        Exotic,
        Population,
        Wood,
        Stone,
        Metal,
        Coins,
        Research,
        Ideology
    }

    public sealed class CityTopBarResourceView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler,
        IPointerClickHandler
    {
        [SerializeField] private CityTopBarResourceType resourceType;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private TMP_Text productionLabel;
        [SerializeField] private Image capacityFill;

        public CityTopBarResourceType ResourceType => resourceType;
        public Sprite Icon => iconImage != null ? iconImage.sprite : null;
        public event Action<CityTopBarResourceView, PointerEventData> PointerEntered;
        public event Action<CityTopBarResourceView, PointerEventData> PointerExited;
        public event Action<CityTopBarResourceView, PointerEventData> PointerMoved;
        public event Action<CityTopBarResourceView, PointerEventData> PointerClicked;

        public void Configure(
            CityTopBarResourceType type,
            Image icon,
            TMP_Text amount,
            TMP_Text production,
            Image fill = null)
        {
            resourceType = type;
            iconImage = icon;
            amountLabel = amount;
            productionLabel = production;
            capacityFill = fill;
        }

        public void SetAmount(string value, bool isNegative = false)
        {
            if (amountLabel == null) return;

            amountLabel.text = value;
            amountLabel.color = isNegative
                ? new Color(0.62f, 0.12f, 0.08f, 1f)
                : new Color(0.3f, 0.15f, 0.1f, 1f);
        }

        public void SetProduction(double productionPerHour)
        {
            SetDetail(
                CityTopBarViewController.FormatProductionValue(productionPerHour),
                productionPerHour < 0d);
        }

        public void SetDetail(string value, bool isNegative = false)
        {
            if (productionLabel == null) return;

            productionLabel.text = value;
            productionLabel.color = isNegative
                ? new Color(0.62f, 0.12f, 0.08f, 1f)
                : new Color(0.05f, 0.38f, 0.05f, 1f);
        }

        public void SetAmountVisible(bool visible)
        {
            if (amountLabel != null)
            {
                amountLabel.gameObject.SetActive(visible);
            }

            if (productionLabel != null)
            {
                productionLabel.gameObject.SetActive(visible);
            }
        }

        public void SetCapacityFill(float percentage)
        {
            if (capacityFill == null) return;

            float fill = Mathf.Clamp01(percentage);
            capacityFill.fillAmount = fill;
            capacityFill.color = fill < 0.95f
                ? Color.Lerp(new Color(0.9f, 0.86f, 0.72f), new Color(0.95f, 0.67f, 0.18f), fill / 0.95f)
                : Color.Lerp(new Color(0.95f, 0.67f, 0.18f), new Color(0.85f, 0.16f, 0.12f), (fill - 0.95f) / 0.05f);
        }

        public void OnPointerEnter(PointerEventData eventData) => PointerEntered?.Invoke(this, eventData);
        public void OnPointerExit(PointerEventData eventData) => PointerExited?.Invoke(this, eventData);
        public void OnPointerMove(PointerEventData eventData) => PointerMoved?.Invoke(this, eventData);
        public void OnPointerClick(PointerEventData eventData) => PointerClicked?.Invoke(this, eventData);
    }
}
