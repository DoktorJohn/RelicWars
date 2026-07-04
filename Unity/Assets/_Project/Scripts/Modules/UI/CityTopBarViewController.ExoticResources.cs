using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Network.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public partial class CityTopBarViewController
    {
        private readonly Dictionary<ExoticResourceTypeEnum, Label> _exoticResourceValueLabels = new();

        private void InitializeExoticResourcesSection()
        {
            _exoticResourcesTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-ExoticResources-Trigger");
            _exoticResourcesTooltip = _rootVisualElement.Q<VisualElement>("City-TopBar-ExoticResources-Tooltip");
            _exoticResourcesTooltipGrid = _rootVisualElement.Q<VisualElement>("City-TopBar-ExoticResources-TooltipGrid");
            _exoticResourcesTooltipTitle = _rootVisualElement.Q<Label>("City-TopBar-ExoticResources-TooltipTitle");
            _exoticResourcesTotalLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-ExoticAmount");

            if (_exoticResourcesTooltip != null)
            {
                _exoticResourcesTooltip.style.display = DisplayStyle.None;
            }

            if (_exoticResourcesTrigger != null)
            {
                _exoticResourcesTrigger.UnregisterCallback<MouseEnterEvent>(OnExoticResourcesMouseEnter);
                _exoticResourcesTrigger.UnregisterCallback<MouseLeaveEvent>(OnExoticResourcesMouseLeave);
                _exoticResourcesTrigger.UnregisterCallback<MouseMoveEvent>(OnExoticResourcesMouseMove);

                _exoticResourcesTrigger.RegisterCallback<MouseEnterEvent>(OnExoticResourcesMouseEnter);
                _exoticResourcesTrigger.RegisterCallback<MouseLeaveEvent>(OnExoticResourcesMouseLeave);
                _exoticResourcesTrigger.RegisterCallback<MouseMoveEvent>(OnExoticResourcesMouseMove);
            }

            BuildExoticResourceTooltipRows();
            RefreshExoticResourcesSection();
        }

        private void CleanupExoticResourcesSection()
        {
            if (_exoticResourcesTrigger == null)
            {
                return;
            }

            _exoticResourcesTrigger.UnregisterCallback<MouseEnterEvent>(OnExoticResourcesMouseEnter);
            _exoticResourcesTrigger.UnregisterCallback<MouseLeaveEvent>(OnExoticResourcesMouseLeave);
            _exoticResourcesTrigger.UnregisterCallback<MouseMoveEvent>(OnExoticResourcesMouseMove);
        }

        private void RefreshExoticResourcesSection()
        {
            var exoticResources = CityStateManager.Instance?.CurrentExoticResources ?? new List<CityExoticResourceDTO>();
            var exoticResourceLookup = exoticResources
                .GroupBy(resource => resource.ResourceType)
                .ToDictionary(group => group.Key, group => group.Sum(resource => resource.Amount));

            if (_exoticResourcesTotalLabel != null)
            {
                _exoticResourcesTotalLabel.text = Math.Floor(exoticResourceLookup.Values.Sum()).ToString("N0");
            }

            if (_exoticResourcesTooltipTitle != null)
            {
                _exoticResourcesTooltipTitle.text = "EXOTIC STOCKPILE";
            }

            foreach (KeyValuePair<ExoticResourceTypeEnum, Label> resourceValueLabel in _exoticResourceValueLabels)
            {
                double amount = exoticResourceLookup.TryGetValue(resourceValueLabel.Key, out double currentAmount) ? currentAmount : 0d;
                resourceValueLabel.Value.text = amount.ToString("N0");
            }
        }

        private void BuildExoticResourceTooltipRows()
        {
            if (_exoticResourcesTooltipGrid == null)
            {
                return;
            }

            _exoticResourcesTooltipGrid.Clear();
            _exoticResourceValueLabels.Clear();

            ExoticResourceTypeEnum[] resourceTypes = Enum.GetValues(typeof(ExoticResourceTypeEnum))
                .Cast<ExoticResourceTypeEnum>()
                .ToArray();

            for (int index = 0; index < resourceTypes.Length; index += 2)
            {
                var row = new VisualElement();
                row.AddToClassList("city-exotic-tooltip-row");
                row.Add(CreateExoticTooltipItem(resourceTypes[index], true));

                if (index + 1 < resourceTypes.Length)
                {
                    row.Add(CreateExoticTooltipItem(resourceTypes[index + 1], false));
                }

                _exoticResourcesTooltipGrid.Add(row);
            }
        }

        private VisualElement CreateExoticTooltipItem(ExoticResourceTypeEnum resourceType, bool isLeftColumn)
        {
            var item = new VisualElement();
            item.AddToClassList("city-exotic-tooltip-item");
            if (isLeftColumn)
            {
                item.AddToClassList("city-exotic-tooltip-item--left");
            }

            var icon = new VisualElement();
            icon.AddToClassList("city-exotic-tooltip-item-icon");
            icon.AddToClassList(GetExoticResourceIconClass(resourceType));

            var name = new Label(resourceType.ToString().ToUpperInvariant());
            name.AddToClassList("city-exotic-tooltip-item-name");

            var value = new Label("0");
            value.AddToClassList("city-exotic-tooltip-item-value");
            _exoticResourceValueLabels[resourceType] = value;

            item.Add(icon);
            item.Add(name);
            item.Add(value);

            return item;
        }

        private void OnExoticResourcesMouseEnter(MouseEnterEvent evt)
        {
            RefreshExoticResourcesSection();
            ShowExoticResourceTooltip(evt);
        }

        private void OnExoticResourcesMouseLeave(MouseLeaveEvent evt)
        {
            HideExoticResourceTooltip();
        }

        private void OnExoticResourcesMouseMove(MouseMoveEvent evt)
        {
            UpdateExoticResourceTooltipPosition(evt);
        }

        private void ShowExoticResourceTooltip(IMouseEvent mouseEvent)
        {
            if (_exoticResourcesTooltip == null)
            {
                return;
            }

            _exoticResourcesTooltip.BringToFront();
            _exoticResourcesTooltip.style.display = DisplayStyle.Flex;
            UpdateExoticResourceTooltipPosition(mouseEvent);
        }

        private void HideExoticResourceTooltip()
        {
            if (_exoticResourcesTooltip != null)
            {
                _exoticResourcesTooltip.style.display = DisplayStyle.None;
            }
        }

        private void UpdateExoticResourceTooltipPosition(IMouseEvent mouseEvent)
        {
            if (_exoticResourcesTooltip == null || _exoticResourcesTooltip.style.display == DisplayStyle.None || _exoticResourcesTooltip.parent == null)
            {
                return;
            }

            Vector2 screenPosition = mouseEvent.mousePosition;
            Vector2 localPosition = _exoticResourcesTooltip.parent.WorldToLocal(screenPosition);

            float availableWidth = _rootVisualElement != null ? _rootVisualElement.resolvedStyle.width : 0f;
            float availableHeight = _rootVisualElement != null ? _rootVisualElement.resolvedStyle.height : 0f;
            float tooltipWidth = GetResolvedDimension(_exoticResourcesTooltip.resolvedStyle.width, 400f);
            float tooltipHeight = GetResolvedDimension(_exoticResourcesTooltip.resolvedStyle.height, 260f);
            const float viewportMargin = 10f;
            const float cursorOffset = 18f;

            float preferredLeft = localPosition.x + cursorOffset;
            if (preferredLeft + tooltipWidth > availableWidth - viewportMargin)
            {
                preferredLeft = localPosition.x - tooltipWidth - cursorOffset;
            }

            float maxLeft = Mathf.Max(viewportMargin, availableWidth - tooltipWidth - viewportMargin);
            float maxTop = Mathf.Max(48f, availableHeight - tooltipHeight - viewportMargin);

            _exoticResourcesTooltip.style.left = Mathf.Clamp(preferredLeft, viewportMargin, maxLeft);
            _exoticResourcesTooltip.style.top = Mathf.Clamp(localPosition.y + cursorOffset, 48f, maxTop);
        }

        private static float GetResolvedDimension(float resolvedDimension, float fallback)
        {
            return float.IsNaN(resolvedDimension) || resolvedDimension <= 0f ? fallback : resolvedDimension;
        }

        private static string GetExoticResourceIconClass(ExoticResourceTypeEnum resourceType)
        {
            return resourceType switch
            {
                ExoticResourceTypeEnum.Cloth => "icon-cloth",
                ExoticResourceTypeEnum.Coal => "icon-coal",
                ExoticResourceTypeEnum.Copper => "icon-copper",
                ExoticResourceTypeEnum.Cotton => "icon-cotton",
                ExoticResourceTypeEnum.Diamond => "icon-diamond",
                ExoticResourceTypeEnum.Gold => "icon-gold",
                ExoticResourceTypeEnum.Ivory => "icon-ivory",
                ExoticResourceTypeEnum.Sand => "icon-sand",
                ExoticResourceTypeEnum.Silver => "icon-silver",
                ExoticResourceTypeEnum.Sulphur => "icon-sulphur",
                _ => "icon-cloth"
            };
        }
    }
}
