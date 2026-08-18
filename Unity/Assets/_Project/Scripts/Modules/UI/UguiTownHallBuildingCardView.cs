using System;
using Assets.Scripts.Domain.State;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiTownHallBuildingCardView : MonoBehaviour
    {
        [SerializeField] private BuildingTypeEnum buildingType;
        [SerializeField] private LargeBuildingButton buildingButton;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text constructionTimeText;
        [SerializeField] private TMP_Text woodCostText;
        [SerializeField] private TMP_Text stoneCostText;
        [SerializeField] private TMP_Text metalCostText;
        [SerializeField] private Color unaffordableCostColor = new Color(0.75f, 0.08f, 0.08f, 1f);

        private Action<BuildingTypeEnum> _clicked;
        private AvailableBuildingDTO _building;
        private Color _woodDefaultColor;
        private Color _stoneDefaultColor;
        private Color _metalDefaultColor;
        private bool _defaultColorsCaptured;
        public BuildingTypeEnum BuildingType => buildingType;

        private void Awake() => ResolveReferences();

        public void Initialize(Action<BuildingTypeEnum> clicked)
        {
            ResolveReferences();
            InferBuildingTypeFromAuthoredName();
            _clicked = clicked;
            if (buildingButton != null)
            {
                buildingButton.OnButtonActivatedClicked -= HandleClicked;
                buildingButton.OnButtonActivatedClicked += HandleClicked;
            }
        }

        public void Dispose()
        {
            if (buildingButton != null) buildingButton.OnButtonActivatedClicked -= HandleClicked;
            _clicked = null;
        }

        public void Bind(AvailableBuildingDTO building)
        {
            if (building == null) return;
            _building = building;
            SetText(levelText, building.CurrentLevel.HasValue ? $"Lvl {building.CurrentLevel.Value}" : "Not built");
            SetText(constructionTimeText, FormatDuration(building.ConstructionTimeInSeconds));
            SetText(woodCostText, FormatAmount(building.WoodCost));
            SetText(stoneCostText, FormatAmount(building.StoneCost));
            SetText(metalCostText, FormatAmount(building.MetalCost));
            RefreshAffordability(Project.Modules.City.CityStateManager.Instance?.CurrentResources ?? default);
        }

        public void RefreshAffordability(CityResourceState resources)
        {
            if (_building == null) return;
            CaptureDefaultCostColors();
            SetCostColor(woodCostText, resources.WoodAmount >= _building.WoodCost, _woodDefaultColor);
            SetCostColor(stoneCostText, resources.StoneAmount >= _building.StoneCost, _stoneDefaultColor);
            SetCostColor(metalCostText, resources.MetalAmount >= _building.MetalCost, _metalDefaultColor);
        }

        private void HandleClicked(LargeBuildingButton _) => _clicked?.Invoke(buildingType);

        private void ResolveReferences()
        {
            buildingButton ??= GetComponent<LargeBuildingButton>();

            // LargeBuildingButton only receives pointer events where a Graphic is hit.
            // Give the complete authored card rect an invisible raycast surface so gaps
            // between icon, labels and resource wrappers remain clickable.
            Graphic clickSurface = GetComponent<Graphic>();
            if (clickSurface == null)
            {
                Image transparentSurface = gameObject.AddComponent<Image>();
                transparentSurface.color = Color.clear;
                clickSurface = transparentSurface;
            }
            clickSurface.raycastTarget = true;

            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "Level label") levelText ??= text;
                else if (text.name == "Construction time label") constructionTimeText ??= text;
            }

            TMP_Text[] costs = Array.FindAll(GetComponentsInChildren<TMP_Text>(true), text => text.name == "TMP Cost");
            if (costs.Length > 0) woodCostText ??= costs[0];
            if (costs.Length > 1) stoneCostText ??= costs[1];
            if (costs.Length > 2) metalCostText ??= costs[2];
            CaptureDefaultCostColors();
        }

        private void CaptureDefaultCostColors()
        {
            if (_defaultColorsCaptured || woodCostText == null || stoneCostText == null || metalCostText == null) return;
            _woodDefaultColor = woodCostText.color;
            _stoneDefaultColor = stoneCostText.color;
            _metalDefaultColor = metalCostText.color;
            _defaultColorsCaptured = true;
        }

        private void SetCostColor(TMP_Text target, bool affordable, Color defaultColor)
        {
            if (target != null) target.color = affordable ? defaultColor : unaffordableCostColor;
        }

        private void InferBuildingTypeFromAuthoredName()
        {
            string value = gameObject.name.Replace(" building", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (value.Equals("Stonequarry", StringComparison.OrdinalIgnoreCase)) value = "StoneQuarry";
            else if (value.Equals("Metalmine", StringComparison.OrdinalIgnoreCase)) value = "MetalMine";
            else if (value.Equals("Marketplace", StringComparison.OrdinalIgnoreCase)) value = "MarketPlace";
            if (Enum.TryParse(value, true, out BuildingTypeEnum parsed)) buildingType = parsed;
        }

        private static string FormatAmount(double value) => Math.Ceiling(value).ToString("N0").Replace(",", " ");
        private static string FormatDuration(int seconds)
        {
            TimeSpan value = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return value.Days > 0 ? $"{value.Days}d {value.Hours:00}h {value.Minutes:00}m" : $"{value.Hours:00}h {value.Minutes:00}m {value.Seconds:00}s";
        }
        private static void SetText(TMP_Text target, string value) { if (target != null) target.text = value; }
    }
}
