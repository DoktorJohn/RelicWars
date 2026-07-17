using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
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
        private readonly Dictionary<ExoticResourceTypeEnum, Label> _exoticResourceStockLabels = new();
        private readonly Dictionary<ExoticResourceTypeEnum, Label> _exoticResourceRateLabels = new();
        private StandardResourceType? _visibleStandardResource;

        private enum StandardResourceType
        {
            Population,
            Wood,
            Stone,
            Metal,
            Coins,
            Research,
            Ideology
        }

        private void InitializeStandardResourceTooltips()
        {
            _woodResourceTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-Wood-Trigger");
            _stoneResourceTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-Stone-Trigger");
            _metalResourceTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-Metal-Trigger");
            _populationResourceTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-Population-Trigger");
            _coinsResourceTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-Coins-Trigger");
            _researchResourceTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-Research-Trigger");
            _ideologyResourceTrigger = _rootVisualElement.Q<VisualElement>("City-TopBar-Ideology-Trigger");
            _standardResourceTooltip = _rootVisualElement.Q<VisualElement>("City-TopBar-StandardResource-Tooltip");
            _standardResourceTooltipIcon = _rootVisualElement.Q<VisualElement>("City-TopBar-StandardResource-TooltipIcon");
            _standardResourceTooltipTitle = _rootVisualElement.Q<Label>("City-TopBar-StandardResource-TooltipTitle");
            _standardResourceCityAmountLabel = _rootVisualElement.Q<Label>("City-TopBar-StandardResource-CityAmount");
            _standardResourceProductionLabel = _rootVisualElement.Q<Label>("City-TopBar-StandardResource-Production");
            _standardResourceTimeToCapacityLabel = _rootVisualElement.Q<Label>("City-TopBar-StandardResource-TimeToCapacity");
            _standardResourcePlayerTotalLabel = _rootVisualElement.Q<Label>("City-TopBar-StandardResource-PlayerTotal");
            _standardResourceCityKeyLabel = _rootVisualElement.Q<Label>("City-TopBar-StandardResource-CityKey");
            _standardResourceProductionKeyLabel = _rootVisualElement.Q<Label>("City-TopBar-StandardResource-ProductionKey");
            _standardResourceCityRow = _rootVisualElement.Q<VisualElement>("City-TopBar-StandardResource-CityRow");
            _standardResourceCapacityRow = _rootVisualElement.Q<VisualElement>("City-TopBar-StandardResource-CapacityRow");
            _populationTooltip = _rootVisualElement.Q<VisualElement>("City-TopBar-Population-Tooltip");
            _populationHousingLabel = _rootVisualElement.Q<Label>("City-TopBar-Population-Housing");
            _populationModifierLabel = _rootVisualElement.Q<Label>("City-TopBar-Population-Modifier");
            _populationTotalLabel = _rootVisualElement.Q<Label>("City-TopBar-Population-Total");
            _populationInUseLabel = _rootVisualElement.Q<Label>("City-TopBar-Population-InUse");
            _populationRemainingLabel = _rootVisualElement.Q<Label>("City-TopBar-Population-Remaining");

            HideStandardResourceTooltip();
            RegisterStandardResourceCallbacks(_populationResourceTrigger, OnPopulationResourceMouseEnter);
            RegisterStandardResourceCallbacks(_woodResourceTrigger, OnWoodResourceMouseEnter);
            RegisterStandardResourceCallbacks(_stoneResourceTrigger, OnStoneResourceMouseEnter);
            RegisterStandardResourceCallbacks(_metalResourceTrigger, OnMetalResourceMouseEnter);
            RegisterStandardResourceCallbacks(_coinsResourceTrigger, OnCoinsResourceMouseEnter);
            RegisterStandardResourceCallbacks(_researchResourceTrigger, OnResearchResourceMouseEnter);
            RegisterStandardResourceCallbacks(_ideologyResourceTrigger, OnIdeologyResourceMouseEnter);
        }

        private void CleanupStandardResourceTooltips()
        {
            UnregisterStandardResourceCallbacks(_populationResourceTrigger, OnPopulationResourceMouseEnter);
            UnregisterStandardResourceCallbacks(_woodResourceTrigger, OnWoodResourceMouseEnter);
            UnregisterStandardResourceCallbacks(_stoneResourceTrigger, OnStoneResourceMouseEnter);
            UnregisterStandardResourceCallbacks(_metalResourceTrigger, OnMetalResourceMouseEnter);
            UnregisterStandardResourceCallbacks(_coinsResourceTrigger, OnCoinsResourceMouseEnter);
            UnregisterStandardResourceCallbacks(_researchResourceTrigger, OnResearchResourceMouseEnter);
            UnregisterStandardResourceCallbacks(_ideologyResourceTrigger, OnIdeologyResourceMouseEnter);
            _visibleStandardResource = null;
        }

        private void RegisterStandardResourceCallbacks(VisualElement trigger, EventCallback<MouseEnterEvent> enterCallback)
        {
            if (trigger == null) return;

            trigger.RegisterCallback(enterCallback);
            trigger.RegisterCallback<MouseLeaveEvent>(OnStandardResourceMouseLeave);
            trigger.RegisterCallback<MouseMoveEvent>(OnStandardResourceMouseMove);
        }

        private void UnregisterStandardResourceCallbacks(VisualElement trigger, EventCallback<MouseEnterEvent> enterCallback)
        {
            if (trigger == null) return;

            trigger.UnregisterCallback(enterCallback);
            trigger.UnregisterCallback<MouseLeaveEvent>(OnStandardResourceMouseLeave);
            trigger.UnregisterCallback<MouseMoveEvent>(OnStandardResourceMouseMove);
        }

        private void OnPopulationResourceMouseEnter(MouseEnterEvent evt) => ShowStandardResourceTooltip(StandardResourceType.Population, evt);
        private void OnWoodResourceMouseEnter(MouseEnterEvent evt) => ShowStandardResourceTooltip(StandardResourceType.Wood, evt);
        private void OnStoneResourceMouseEnter(MouseEnterEvent evt) => ShowStandardResourceTooltip(StandardResourceType.Stone, evt);
        private void OnMetalResourceMouseEnter(MouseEnterEvent evt) => ShowStandardResourceTooltip(StandardResourceType.Metal, evt);
        private void OnCoinsResourceMouseEnter(MouseEnterEvent evt) => ShowStandardResourceTooltip(StandardResourceType.Coins, evt);
        private void OnResearchResourceMouseEnter(MouseEnterEvent evt) => ShowStandardResourceTooltip(StandardResourceType.Research, evt);
        private void OnIdeologyResourceMouseEnter(MouseEnterEvent evt) => ShowStandardResourceTooltip(StandardResourceType.Ideology, evt);

        private void OnStandardResourceMouseLeave(MouseLeaveEvent evt) => HideStandardResourceTooltip();

        private void OnStandardResourceMouseMove(MouseMoveEvent evt)
        {
            VisualElement visibleTooltip = _visibleStandardResource == StandardResourceType.Population
                ? _populationTooltip
                : _standardResourceTooltip;
            UpdateTooltipPosition(visibleTooltip, evt, 310f, 220f);
        }

        private void ShowStandardResourceTooltip(StandardResourceType resourceType, IMouseEvent mouseEvent)
        {
            if (_standardResourceTooltip == null) return;

            _visibleStandardResource = resourceType;
            RefreshVisibleStandardResourceTooltip();
            VisualElement visibleTooltip = resourceType == StandardResourceType.Population
                ? _populationTooltip
                : _standardResourceTooltip;
            if (visibleTooltip == null) return;

            visibleTooltip.BringToFront();
            visibleTooltip.style.display = DisplayStyle.Flex;
            UpdateTooltipPosition(visibleTooltip, mouseEvent, 310f, 220f);
        }

        private void HideStandardResourceTooltip()
        {
            _visibleStandardResource = null;
            if (_standardResourceTooltip != null)
            {
                _standardResourceTooltip.style.display = DisplayStyle.None;
            }
            if (_populationTooltip != null)
            {
                _populationTooltip.style.display = DisplayStyle.None;
            }
        }

        private void RefreshVisibleStandardResourceTooltip()
        {
            if (!_visibleStandardResource.HasValue) return;

            var cityState = CityStateManager.Instance?.CurrentResources ?? default;
            var playerState = WorldPlayerStateManager.Instance?.CurrentEconomy;

            if (_visibleStandardResource == StandardResourceType.Population)
            {
                if (_populationHousingLabel != null) _populationHousingLabel.text = cityState.HousingPopulationCapacity.ToString("N0");
                if (_populationModifierLabel != null)
                {
                    _populationModifierLabel.text = cityState.PopulationModifierBonus > 0d
                        ? $"+{cityState.PopulationModifierBonus:N0}"
                        : cityState.PopulationModifierBonus.ToString("N0");
                    _populationModifierLabel.EnableInClassList("city-standard-resource-tooltip-value--positive", cityState.PopulationModifierBonus >= 0d);
                    _populationModifierLabel.EnableInClassList("city-standard-resource-tooltip-value--negative", cityState.PopulationModifierBonus < 0d);
                }
                if (_populationTotalLabel != null) _populationTotalLabel.text = cityState.MaxPopulationCapacity.ToString("N0");
                if (_populationInUseLabel != null) _populationInUseLabel.text = cityState.CurrentPopulationUsage.ToString("N0");
                if (_populationRemainingLabel != null) _populationRemainingLabel.text = cityState.RemainingPopulation.ToString("N0");
                return;
            }

            (string name, string iconClass, double amount, double capacity, double cityProduction, double totalProduction, double playerTotal, bool isStoredResource) = _visibleStandardResource.Value switch
            {
                StandardResourceType.Wood => ("WOOD", "icon-wood", cityState.WoodAmount, cityState.WoodMaxCapacity, cityState.WoodProductionPerHour, cityState.WoodProductionPerHour, playerState?.TotalWoodAmount ?? 0d, true),
                StandardResourceType.Stone => ("STONE", "icon-stone", cityState.StoneAmount, cityState.StoneMaxCapacity, cityState.StoneProductionPerHour, cityState.StoneProductionPerHour, playerState?.TotalStoneAmount ?? 0d, true),
                StandardResourceType.Metal => ("METAL", "icon-metal", cityState.MetalAmount, cityState.MetalMaxCapacity, cityState.MetalProductionPerHour, cityState.MetalProductionPerHour, playerState?.TotalMetalAmount ?? 0d, true),
                StandardResourceType.Coins => ("GOLD", "icon-coins", 0d, 0d, cityState.CoinsProductionPerHour, playerState?.CoinsProductionPerHour ?? 0d, playerState?.CoinsAmount ?? 0d, false),
                StandardResourceType.Research => ("RESEARCH", "icon-research", 0d, 0d, cityState.ResearchPointsPerHour, playerState?.ResearchPointsProductionPerHour ?? 0d, playerState?.ResearchPointsAmount ?? 0d, false),
                _ => ("IDEOLOGY", "icon-ideology", 0d, 0d, cityState.IdeologyFocusPointsPerHour, playerState?.IdeologyFocusPointsProductionPerHour ?? 0d, playerState?.IdeologyFocusPointsAmount ?? 0d, false)
            };

            if (_standardResourceCityRow != null) _standardResourceCityRow.style.display = DisplayStyle.Flex;
            if (_standardResourceCapacityRow != null) _standardResourceCapacityRow.style.display = isStoredResource ? DisplayStyle.Flex : DisplayStyle.None;
            if (_standardResourceTooltipTitle != null) _standardResourceTooltipTitle.text = name;
            if (_standardResourceCityKeyLabel != null) _standardResourceCityKeyLabel.text = isStoredResource ? "IN THIS CITY" : "IN THIS CITY / HOUR";
            if (_standardResourceProductionKeyLabel != null) _standardResourceProductionKeyLabel.text = isStoredResource ? "PRODUCTION / HOUR IN THIS CITY" : "TOTAL PRODUCTION / HOUR";
            SetStandardResourceTooltipIcon(iconClass);
            if (_standardResourceCityAmountLabel != null) _standardResourceCityAmountLabel.text = isStoredResource
                ? $"{Math.Floor(amount):N0} / {Math.Floor(capacity):N0}"
                : $"{FormatProduction(cityProduction)}/hr";
            if (_standardResourceCityAmountLabel != null)
            {
                _standardResourceCityAmountLabel.EnableInClassList("city-standard-resource-tooltip-value--positive", !isStoredResource && cityProduction >= 0d);
                _standardResourceCityAmountLabel.EnableInClassList("city-standard-resource-tooltip-value--negative", !isStoredResource && cityProduction < 0d);
            }
            if (_standardResourceProductionLabel != null)
            {
                _standardResourceProductionLabel.text = $"{FormatProduction(totalProduction)}/hr";
                _standardResourceProductionLabel.EnableInClassList("city-standard-resource-tooltip-value--positive", totalProduction >= 0d);
                _standardResourceProductionLabel.EnableInClassList("city-standard-resource-tooltip-value--negative", totalProduction < 0d);
            }
            if (_standardResourceTimeToCapacityLabel != null) _standardResourceTimeToCapacityLabel.text = FormatTimeToCapacity(amount, capacity, cityProduction);
            if (_standardResourcePlayerTotalLabel != null) _standardResourcePlayerTotalLabel.text = Math.Floor(playerTotal).ToString("N0");
        }

        private void SetStandardResourceTooltipIcon(string iconClass)
        {
            if (_standardResourceTooltipIcon == null) return;

            _standardResourceTooltipIcon.RemoveFromClassList("icon-wood");
            _standardResourceTooltipIcon.RemoveFromClassList("icon-stone");
            _standardResourceTooltipIcon.RemoveFromClassList("icon-metal");
            _standardResourceTooltipIcon.RemoveFromClassList("icon-population");
            _standardResourceTooltipIcon.RemoveFromClassList("icon-coins");
            _standardResourceTooltipIcon.RemoveFromClassList("icon-research");
            _standardResourceTooltipIcon.RemoveFromClassList("icon-ideology");
            _standardResourceTooltipIcon.AddToClassList(iconClass);
        }

        private static string FormatProduction(double productionPerHour)
        {
            return productionPerHour > 0d ? $"+{productionPerHour:N1}" : productionPerHour.ToString("N1");
        }

        private static string FormatTimeToCapacity(double amount, double capacity, double productionPerHour)
        {
            if (capacity <= 0d) return "Unavailable";
            if (amount >= capacity) return "Warehouse full";
            if (productionPerHour <= 0d) return "Not increasing";

            TimeSpan remaining = TimeSpan.FromHours((capacity - amount) / productionPerHour);
            if (remaining.TotalMinutes < 1d) return "< 1 min";
            if (remaining.TotalDays >= 1d) return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
            if (remaining.TotalHours >= 1d) return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            return $"{remaining.Minutes}m";
        }

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
            var productionLookup = (CityStateManager.Instance?.CurrentIslandExoticResources ?? new List<WorldIslandResourceDTO>())
                .GroupBy(resource => resource.ResourceType)
                .ToDictionary(group => group.Key, group => group.Sum(resource => resource.OutputPerHour));

            if (_exoticResourcesTotalLabel != null)
            {
                _exoticResourcesTotalLabel.text = Math.Floor(exoticResourceLookup.Values.Sum()).ToString("N0");
            }

            if (_exoticResourcesTooltipTitle != null)
            {
                _exoticResourcesTooltipTitle.text = "EXOTIC STOCKPILE";
            }

            foreach (ExoticResourceTypeEnum resourceType in _exoticResourceStockLabels.Keys)
            {
                double amount = exoticResourceLookup.TryGetValue(resourceType, out double currentAmount) ? currentAmount : 0d;
                double production = productionLookup.TryGetValue(resourceType, out double currentProduction) ? currentProduction : 0d;
                _exoticResourceStockLabels[resourceType].text = amount.ToString("N0");
                _exoticResourceRateLabels[resourceType].text = $"({FormatProduction(production)}/hr)";
                _exoticResourceRateLabels[resourceType].EnableInClassList("city-exotic-tooltip-item-rate--negative", production < 0d);
            }
        }

        private void BuildExoticResourceTooltipRows()
        {
            if (_exoticResourcesTooltipGrid == null)
            {
                return;
            }

            _exoticResourcesTooltipGrid.Clear();
            _exoticResourceStockLabels.Clear();
            _exoticResourceRateLabels.Clear();

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

            var stock = new Label("0");
            stock.AddToClassList("city-exotic-tooltip-item-stock");
            _exoticResourceStockLabels[resourceType] = stock;

            var rate = new Label("(+0.0/hr)");
            rate.AddToClassList("city-exotic-tooltip-item-rate");
            _exoticResourceRateLabels[resourceType] = rate;

            item.Add(icon);
            item.Add(name);
            item.Add(stock);
            item.Add(rate);

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
            UpdateTooltipPosition(_exoticResourcesTooltip, mouseEvent, 540f, 260f);
        }

        private void UpdateTooltipPosition(VisualElement tooltip, IMouseEvent mouseEvent, float fallbackWidth, float fallbackHeight)
        {
            if (tooltip == null || tooltip.style.display == DisplayStyle.None || tooltip.parent == null)
            {
                return;
            }

            Vector2 screenPosition = mouseEvent.mousePosition;
            Vector2 localPosition = tooltip.parent.WorldToLocal(screenPosition);

            float availableWidth = _rootVisualElement != null ? _rootVisualElement.resolvedStyle.width : 0f;
            float availableHeight = _rootVisualElement != null ? _rootVisualElement.resolvedStyle.height : 0f;
            float tooltipWidth = GetResolvedDimension(tooltip.resolvedStyle.width, fallbackWidth);
            float tooltipHeight = GetResolvedDimension(tooltip.resolvedStyle.height, fallbackHeight);
            const float viewportMargin = 10f;
            const float cursorOffset = 18f;

            float preferredLeft = localPosition.x + cursorOffset;
            if (preferredLeft + tooltipWidth > availableWidth - viewportMargin)
            {
                preferredLeft = localPosition.x - tooltipWidth - cursorOffset;
            }

            float maxLeft = Mathf.Max(viewportMargin, availableWidth - tooltipWidth - viewportMargin);
            float maxTop = Mathf.Max(48f, availableHeight - tooltipHeight - viewportMargin);

            tooltip.style.left = Mathf.Clamp(preferredLeft, viewportMargin, maxLeft);
            tooltip.style.top = Mathf.Clamp(localPosition.y + cursorOffset, 48f, maxTop);
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
