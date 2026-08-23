using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Domain.Enums;
using Assets.Scripts.Domain.State;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
using Project.Network.Manager;
using Project.Network.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public partial class CityTopBarViewController
    {
        [Serializable]
        private struct ExoticTooltipBinding
        {
            public ExoticResourceTypeEnum ResourceType;
            public TMP_Text AmountLabel;
            public TMP_Text ProductionLabel;
        }

        [Header("Standard resource tooltip")]
        [SerializeField] private RectTransform standardResourceTooltip;
        [SerializeField] private Image standardResourceTooltipIcon;
        [SerializeField] private TMP_Text standardResourceTooltipTitle;
        [SerializeField] private TMP_Text standardResourceCityKeyLabel;
        [SerializeField] private TMP_Text standardResourceCityAmountLabel;
        [SerializeField] private TMP_Text standardResourceProductionKeyLabel;
        [SerializeField] private TMP_Text standardResourceProductionLabel;
        [SerializeField] private GameObject standardResourceCapacityRow;
        [SerializeField] private TMP_Text standardResourceTimeToCapacityLabel;
        [SerializeField] private TMP_Text standardResourcePlayerTotalLabel;

        [Header("Wood tooltip")]
        [SerializeField] private TMP_Text woodResourceCityAmountLabel;
        [SerializeField] private TMP_Text woodResourceProductionLabel;
        [SerializeField] private TMP_Text woodResourceTimeToCapacityLabel;
        [SerializeField] private TMP_Text woodResourcePlayerTotalLabel;

        [Header("Stone tooltip")]
        [SerializeField] private TMP_Text stoneResourceCityAmountLabel;
        [SerializeField] private TMP_Text stoneResourceProductionLabel;
        [SerializeField] private TMP_Text stoneResourceTimeToCapacityLabel;
        [SerializeField] private TMP_Text stoneResourcePlayerTotalLabel;

        [Header("Metal tooltip")]
        [SerializeField] private TMP_Text metalResourceCityAmountLabel;
        [SerializeField] private TMP_Text metalResourceProductionLabel;
        [SerializeField] private TMP_Text metalResourceTimeToCapacityLabel;
        [SerializeField] private TMP_Text metalResourcePlayerTotalLabel;

        [Header("Population tooltip")]
        [SerializeField] private TMP_Text populationCapacityLabel;
        [SerializeField] private TMP_Text populationInUseLabel;
        [SerializeField] private TMP_Text populationRemainingLabel;

        [Header("Gold coins tooltip")]
        [SerializeField] private TMP_Text coinsProductionInCityLabel;
        [SerializeField] private TMP_Text coinsExpenditureInCityLabel;
        [SerializeField] private TMP_Text coinsNetInCityLabel;
        [SerializeField] private TMP_Text coinsEmpireNetLabel;
        [SerializeField] private TMP_Text coinsEmpireStockpileLabel;

        [Header("Research tooltip")]
        [SerializeField] private TMP_Text researchProductionInCityLabel;
        [SerializeField] private TMP_Text researchEmpireProductionLabel;
        [SerializeField] private TMP_Text researchEmpireStockpileLabel;

        [Header("Ideology tooltip")]
        [SerializeField] private TMP_Text ideologyProductionInCityLabel;
        [SerializeField] private TMP_Text ideologyEmpireProductionLabel;
        [SerializeField] private TMP_Text ideologyEmpireStockpileLabel;

        [Header("Exotic resource tooltip")]
        [SerializeField] private ExoticTooltipBinding[] exoticTooltipBindings;

        private CityTopBarResourceType? _visibleResourceType;
        private CoinsBreakdownDTO _coinsBreakdown;
        private Guid _coinsBreakdownCityId;
        private Guid _coinsBreakdownRequestCityId;
        private Coroutine _coinsBreakdownRequestCoroutine;
        private int _coinsBreakdownRequestVersion;
        private bool _coinsBreakdownRequestInFlight;

        private void InitializeResourceTooltips()
        {
            if (resourceViews != null)
            {
                foreach (CityTopBarResourceView view in resourceViews)
                {
                    if (view == null) continue;
                    view.PointerEntered += HandleResourcePointerEntered;
                    view.PointerExited += HandleResourcePointerExited;
                    view.PointerMoved += HandleResourcePointerMoved;
                    view.PointerClicked += HandleResourcePointerClicked;
                }
            }

            RefreshExoticResourcesSection();
            HideResourceTooltips();
        }

        private void CleanupResourceTooltips()
        {
            InvalidateCoinsBreakdownRequest();
            if (resourceViews == null) return;

            foreach (CityTopBarResourceView view in resourceViews)
            {
                if (view == null) continue;
                view.PointerEntered -= HandleResourcePointerEntered;
                view.PointerExited -= HandleResourcePointerExited;
                view.PointerMoved -= HandleResourcePointerMoved;
                view.PointerClicked -= HandleResourcePointerClicked;
            }
        }

        private void HandleResourcePointerEntered(CityTopBarResourceView view, PointerEventData eventData)
        {
            if (ResponsiveUiStateManager.IsPhoneLayout) return;

            _visibleResourceType = view.ResourceType;
            if (view.ResourceType == CityTopBarResourceType.Coins)
            {
                RefreshCoinsBreakdownForActiveCity();
            }
            RefreshVisibleResourceTooltip();
        }

        private void HandleResourcePointerExited(CityTopBarResourceView view, PointerEventData eventData)
        {
            if (_visibleResourceType == view.ResourceType) _visibleResourceType = null;
        }

        private void HandleResourcePointerMoved(CityTopBarResourceView view, PointerEventData eventData)
        {
            if (!ResponsiveUiStateManager.IsPhoneLayout && _visibleResourceType == view.ResourceType)
            {
                PositionTooltip(GetVisibleTooltip(), eventData.position);
            }
        }

        private void HandleResourcePointerClicked(CityTopBarResourceView view, PointerEventData eventData)
        {
            if (!ResponsiveUiStateManager.IsPhoneLayout) return;

            bool closeCurrent = _visibleResourceType == view.ResourceType && GetVisibleTooltip()?.gameObject.activeSelf == true;
            CloseAllPopups();
            if (!closeCurrent) ShowResourceTooltip(view, eventData.position, true);
        }

        private void ShowResourceTooltip(CityTopBarResourceView view, Vector2 screenPosition, bool useBackdrop)
        {
            if (view == null) return;

            HideCitySelectorPopup();
            HideResourceTooltips();
            _visibleResourceType = view.ResourceType;
            if (view.ResourceType == CityTopBarResourceType.Coins)
            {
                RefreshCoinsBreakdownForActiveCity();
            }
            RefreshVisibleResourceTooltip();

            RectTransform tooltip = GetVisibleTooltip();
            if (tooltip == null) return;

            if (useBackdrop) SetBackdropVisible(true);
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();
            PositionTooltip(tooltip, screenPosition);
        }

        private RectTransform GetVisibleTooltip()
        {
            return standardResourceTooltip;
        }

        private void HideResourceTooltips()
        {
            _visibleResourceType = null;
            if (standardResourceTooltip != null) standardResourceTooltip.gameObject.SetActive(false);
        }

        private void RefreshVisibleResourceTooltip()
        {
            if (!_visibleResourceType.HasValue) return;
            if (_visibleResourceType == CityTopBarResourceType.Exotic)
            {
                RefreshExoticResourcesSection();
                return;
            }

            CityResourceState cityState = CityStateManager.Instance?.CurrentResources ?? default;
            WorldPlayerState playerState = WorldPlayerStateManager.Instance?.CurrentEconomy;

            if (_visibleResourceType == CityTopBarResourceType.Population)
            {
                SetText(populationCapacityLabel, $"<b>CAPACITY IN CITY</b>\n{cityState.MaxPopulationCapacity:N0}");
                SetText(populationInUseLabel, $"<b>IN USE</b>\n{cityState.CurrentPopulationUsage:N0}");
                SetText(populationRemainingLabel, $"<b>REMAINING FREE POPULATION</b>\n{cityState.RemainingPopulation:N0}");
                return;
            }

            if (_visibleResourceType == CityTopBarResourceType.Research)
            {
                double basePower = playerState?.BaseResearchPower ?? 0d;
                double effectivePower = playerState?.EffectiveResearchPower ?? 0d;
                double speed = playerState?.ResearchSpeedMultiplier ?? 0d;
                SetText(researchProductionInCityLabel, $"<b>RESEARCH POWER FROM CITY</b>\n{cityState.ResearchPower:F2}");
                SetText(researchEmpireProductionLabel, $"<b>BASE EMPIRE RESEARCH POWER</b>\n{basePower:F2}");
                SetText(researchEmpireStockpileLabel, $"<b>EFFECTIVE EMPIRE RESEARCH POWER</b>\n{effectivePower:F2} ({speed:F2}x)");
                return;
            }

            if (_visibleResourceType == CityTopBarResourceType.Coins)
            {
                RenderCoinsTooltip(playerState);
                return;
            }

            (string name, double amount, double capacity, double cityProduction, double totalProduction, double playerTotal, bool stored) =
                _visibleResourceType.Value switch
                {
                    CityTopBarResourceType.Wood => ("WOOD", cityState.WoodAmount, cityState.WoodMaxCapacity, cityState.WoodProductionPerHour, cityState.WoodProductionPerHour, playerState?.TotalWoodAmount ?? 0d, true),
                    CityTopBarResourceType.Stone => ("STONE", cityState.StoneAmount, cityState.StoneMaxCapacity, cityState.StoneProductionPerHour, cityState.StoneProductionPerHour, playerState?.TotalStoneAmount ?? 0d, true),
                    CityTopBarResourceType.Metal => ("METAL", cityState.MetalAmount, cityState.MetalMaxCapacity, cityState.MetalProductionPerHour, cityState.MetalProductionPerHour, playerState?.TotalMetalAmount ?? 0d, true),
                    _ => ("IDEOLOGY", 0d, 0d, cityState.IdeologyFocusPointsPerHour, playerState?.IdeologyFocusPointsProductionPerHour ?? 0d, playerState?.IdeologyFocusPointsAmount ?? 0d, false)
                };

            if (_visibleResourceType == CityTopBarResourceType.Wood)
            {
                SetText(woodResourceCityAmountLabel, $"<b>IN THIS CITY</b>\n{Math.Floor(amount):N0} / {Math.Floor(capacity):N0}");
                SetText(woodResourceProductionLabel, $"<b>PRODUCTION / HOUR</b>\n{FormatProductionValue(cityProduction)}/hr");
                SetText(woodResourceTimeToCapacityLabel, $"<b>TIME TO CAPACITY</b>\n{FormatTimeToCapacity(amount, capacity, cityProduction)}");
                SetText(woodResourcePlayerTotalLabel, $"<b>PLAYER TOTAL</b>\n{Math.Floor(playerTotal):N0}");
                return;
            }

            if (_visibleResourceType == CityTopBarResourceType.Stone)
            {
                SetText(stoneResourceCityAmountLabel, $"<b>IN THIS CITY</b>\n{Math.Floor(amount):N0} / {Math.Floor(capacity):N0}");
                SetText(stoneResourceProductionLabel, $"<b>PRODUCTION / HOUR</b>\n{FormatProductionValue(cityProduction)}/hr");
                SetText(stoneResourceTimeToCapacityLabel, $"<b>TIME TO CAPACITY</b>\n{FormatTimeToCapacity(amount, capacity, cityProduction)}");
                SetText(stoneResourcePlayerTotalLabel, $"<b>PLAYER TOTAL</b>\n{Math.Floor(playerTotal):N0}");
                return;
            }

            if (_visibleResourceType == CityTopBarResourceType.Metal)
            {
                SetText(metalResourceCityAmountLabel, $"<b>IN THIS CITY</b>\n{Math.Floor(amount):N0} / {Math.Floor(capacity):N0}");
                SetText(metalResourceProductionLabel, $"<b>PRODUCTION / HOUR</b>\n{FormatProductionValue(cityProduction)}/hr");
                SetText(metalResourceTimeToCapacityLabel, $"<b>TIME TO CAPACITY</b>\n{FormatTimeToCapacity(amount, capacity, cityProduction)}");
                SetText(metalResourcePlayerTotalLabel, $"<b>PLAYER TOTAL</b>\n{Math.Floor(playerTotal):N0}");
                return;
            }

            if (_visibleResourceType == CityTopBarResourceType.Ideology)
            {
                SetText(ideologyProductionInCityLabel, $"<b>PRODUCTION / HOUR FROM CITY</b>\n{FormatProductionValue(cityProduction)}/hr");
                SetText(ideologyEmpireProductionLabel, $"<b>EMPIRE PRODUCTION / HOUR</b>\n{FormatProductionValue(totalProduction)}/hr");
                SetText(ideologyEmpireStockpileLabel, $"<b>TOTAL EMPIRE STOCKPILE</b>\n{Math.Floor(playerTotal):N0}");
                return;
            }

            if (standardResourceTooltipIcon != null && _resourceViewLookup.TryGetValue(_visibleResourceType.Value, out CityTopBarResourceView view))
            {
                standardResourceTooltipIcon.sprite = view.Icon;
            }
            SetText(standardResourceTooltipTitle, name);
            SetText(standardResourceCityKeyLabel, stored ? "IN THIS CITY" : "IN THIS CITY / HOUR");
            SetText(standardResourceProductionKeyLabel, stored ? "PRODUCTION / HOUR IN THIS CITY" : "TOTAL PRODUCTION / HOUR");
            SetText(standardResourceCityAmountLabel, stored ? $"{Math.Floor(amount):N0} / {Math.Floor(capacity):N0}" : $"{FormatProductionValue(cityProduction)}/hr");
            SetText(standardResourceProductionLabel, $"{FormatProductionValue(totalProduction)}/hr");
            SetText(standardResourceTimeToCapacityLabel, FormatTimeToCapacity(amount, capacity, cityProduction));
            SetText(standardResourcePlayerTotalLabel, Math.Floor(playerTotal).ToString("N0"));
            if (standardResourceCapacityRow != null) standardResourceCapacityRow.SetActive(stored);
        }

        private void RefreshCoinsBreakdownForActiveCity()
        {
            NetworkManager network = NetworkManager.Instance;
            Guid cityId = network?.ActiveCityId ?? Guid.Empty;
            if (network?.City == null || cityId == Guid.Empty)
            {
                InvalidateCoinsBreakdownRequest();
                _coinsBreakdown = null;
                _coinsBreakdownCityId = Guid.Empty;
                return;
            }

            if (_coinsBreakdownCityId != cityId)
            {
                _coinsBreakdown = null;
                _coinsBreakdownCityId = Guid.Empty;
            }

            if (_coinsBreakdownRequestInFlight && _coinsBreakdownRequestCityId == cityId)
            {
                return;
            }

            InvalidateCoinsBreakdownRequest();
            int version = _coinsBreakdownRequestVersion;
            _coinsBreakdownRequestInFlight = true;
            _coinsBreakdownRequestCityId = cityId;
            _coinsBreakdownRequestCoroutine = StartCoroutine(
                ExecuteCoinsBreakdownRequest(network, cityId, version));
        }

        private IEnumerator ExecuteCoinsBreakdownRequest(NetworkManager network, Guid cityId, int version)
        {
            CityOverviewHUDDTO overview = null;
            yield return StartCoroutine(network.City.GetCityOverviewHUD(
                cityId,
                network.JwtToken,
                response => overview = response));

            if (version != _coinsBreakdownRequestVersion)
            {
                yield break;
            }

            _coinsBreakdownRequestCoroutine = null;
            _coinsBreakdownRequestInFlight = false;
            _coinsBreakdownRequestCityId = Guid.Empty;

            if (!isActiveAndEnabled || (NetworkManager.Instance?.ActiveCityId ?? Guid.Empty) != cityId)
            {
                RefreshVisibleResourceTooltip();
                yield break;
            }

            if (overview?.CoinsProduction != null)
            {
                _coinsBreakdown = overview.CoinsProduction;
                _coinsBreakdownCityId = cityId;
            }

            RefreshVisibleResourceTooltip();
        }

        private void InvalidateCoinsBreakdownRequest()
        {
            _coinsBreakdownRequestVersion++;
            if (_coinsBreakdownRequestCoroutine != null)
            {
                StopCoroutine(_coinsBreakdownRequestCoroutine);
                _coinsBreakdownRequestCoroutine = null;
            }

            _coinsBreakdownRequestInFlight = false;
            _coinsBreakdownRequestCityId = Guid.Empty;
        }

        private void RenderCoinsTooltip(WorldPlayerState playerState)
        {
            Guid activeCityId = NetworkManager.Instance?.ActiveCityId ?? Guid.Empty;
            bool hasCurrentBreakdown = activeCityId != Guid.Empty &&
                                       _coinsBreakdownCityId == activeCityId &&
                                       _coinsBreakdown != null;

            if (hasCurrentBreakdown)
            {
                double production = _coinsBreakdown.FinalValuePerHour;
                double expenditure = _coinsBreakdown.Expenditure;
                double net = production - expenditure;
                SetText(coinsProductionInCityLabel, $"<b>PRODUCTION / HOUR IN CITY</b>\n{FormatProductionValue(production)}/hr");
                SetText(coinsExpenditureInCityLabel, $"<b>EXPENDITURE / HOUR IN CITY</b>\n-{Math.Abs(expenditure):N1}/hr");
                SetText(coinsNetInCityLabel, $"<b>NET / HOUR IN CITY</b>\n{FormatProductionValue(net)}/hr");
            }
            else
            {
                string state = _coinsBreakdownRequestInFlight && _coinsBreakdownRequestCityId == activeCityId
                    ? "Loading..."
                    : "Unavailable";
                SetText(coinsProductionInCityLabel, $"<b>PRODUCTION / HOUR IN CITY</b>\n{state}");
                SetText(coinsExpenditureInCityLabel, $"<b>EXPENDITURE / HOUR IN CITY</b>\n{state}");
                SetText(coinsNetInCityLabel, $"<b>NET / HOUR IN CITY</b>\n{state}");
            }

            SetText(
                coinsEmpireNetLabel,
                $"<b>TOTAL EMPIRE NET / HOUR</b>\n{FormatProductionValue(playerState?.CoinsProductionPerHour ?? 0d)}/hr");
            SetText(
                coinsEmpireStockpileLabel,
                $"<b>TOTAL EMPIRE STOCKPILE</b>\n{Math.Floor(playerState?.CoinsAmount ?? 0d):N0}");
        }

        private void RefreshExoticResourcesSection()
        {
            var stocks = (CityStateManager.Instance?.CurrentExoticResources ?? new List<CityExoticResourceDTO>())
                .GroupBy(resource => resource.ResourceType)
                .ToDictionary(group => group.Key, group => group.Sum(resource => resource.Amount));
            var rates = (CityStateManager.Instance?.CurrentIslandExoticResources ?? new List<WorldIslandResourceDTO>())
                .GroupBy(resource => resource.ResourceType)
                .ToDictionary(group => group.Key, group => group.Sum(resource => resource.OutputPerHour));

            SetResourceAmount(CityTopBarResourceType.Exotic, Math.Floor(stocks.Values.Sum()).ToString("N0"));
            SetResourceProduction(CityTopBarResourceType.Exotic, rates.Values.Sum());
            if (exoticTooltipBindings == null) return;

            foreach (ExoticTooltipBinding binding in exoticTooltipBindings)
            {
                double amount = stocks.TryGetValue(binding.ResourceType, out double currentAmount) ? currentAmount : 0d;
                double rate = rates.TryGetValue(binding.ResourceType, out double currentRate) ? currentRate : 0d;
                SetText(binding.AmountLabel, Math.Floor(amount).ToString("N0"));
                SetText(binding.ProductionLabel, $"({FormatProductionValue(rate)}/hr)");
            }
        }

        private void PositionTooltip(RectTransform tooltip, Vector2 screenPosition)
        {
            if (tooltip == null || overlayRoot == null) return;

            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPosition, eventCamera, out Vector2 pointer)) return;

            tooltip.pivot = new Vector2(0f, 1f);
            Vector2 size = tooltip.rect.size;
            Rect bounds = overlayRoot.rect;
            const float margin = 10f;
            const float offset = 18f;
            float x = pointer.x + offset;
            if (x + size.x > bounds.xMax - margin) x = pointer.x - size.x - offset;
            float y = pointer.y - offset;
            x = Mathf.Clamp(x, bounds.xMin + margin, bounds.xMax - size.x - margin);
            y = Mathf.Clamp(y, bounds.yMin + size.y + margin, bounds.yMax - margin);
            tooltip.anchoredPosition = new Vector2(x, y);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        public static string FormatProductionValue(double productionPerHour)
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
    }
}
