using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
using Project.Network.Manager;
using Project.Network.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public readonly struct ExoticResourceWindowPayload
    {
        public ExoticResourceWindowPayload(Guid islandId, int slotIndex)
        {
            IslandId = islandId;
            SlotIndex = slotIndex;
        }

        public Guid IslandId { get; }
        public int SlotIndex { get; }
    }

    public class ExoticResourceWindowController : BaseWindow
    {
        protected override string WindowName => "ExoticResourceWindow";
        protected override string VisualContainerName => "Exotic-Resource-Window-MainContainer";
        protected override string HeaderName => "Exotic-Resource-Window-Header";

        private readonly List<Button> _tabs = new();
        private readonly List<TextField> _investmentInputs = new();
        private VisualElement _tabsContainer;
        private VisualElement _content;
        private VisualElement _resourceIcon;
        private VisualElement _investmentSection;
        private Label _resourceName;
        private Label _tier;
        private Label _output;
        private Label _progressLabel;
        private Label _availabilityMessage;
        private Label _statusMessage;
        private ProgressBar _progress;
        private Button _investButton;
        private Button _retryButton;
        private Guid _islandId;
        private int _selectedSlotIndex;
        private int _openVersion;
        private int _loadVersion;
        private int _investmentVersion;
        private bool _requestInProgress;
        private bool _activeCityIsOnIsland;
        private WorldIslandDetailsDTO _details;

        public override void OnOpen(object dataPayload)
        {
            _openVersion = BeginDeferredOpen();
            _investmentVersion++;
            _requestInProgress = false;
            BindReferences();
            if (dataPayload is not ExoticResourceWindowPayload payload || payload.IslandId == Guid.Empty)
            {
                ShowError("Invalid exotic resource.");
                CompleteDeferredOpen(_openVersion);
                return;
            }

            _islandId = payload.IslandId;
            _selectedSlotIndex = payload.SlotIndex;
            ClearInputs();
            LoadIsland(_openVersion);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            _loadVersion++;
            _investmentVersion++;
            StopAllCoroutines();
            UnbindControls();
        }

        private void BindReferences()
        {
            UnbindControls();
            _tabsContainer = Root.Q<VisualElement>("Exotic-Resource-Tabs");
            _content = Root.Q<VisualElement>("Exotic-Resource-Content");
            _resourceIcon = Root.Q<VisualElement>("Exotic-Resource-Icon");
            _investmentSection = Root.Q<VisualElement>("Exotic-Resource-Investment");
            _resourceName = Root.Q<Label>("Exotic-Resource-Name");
            _tier = Root.Q<Label>("Exotic-Resource-Tier");
            _output = Root.Q<Label>("Exotic-Resource-Output");
            _progressLabel = Root.Q<Label>("Exotic-Resource-Progress-Label");
            _availabilityMessage = Root.Q<Label>("Exotic-Resource-Availability");
            _statusMessage = Root.Q<Label>("Exotic-Resource-Status");
            _progress = Root.Q<ProgressBar>("Exotic-Resource-Progress");
            _investButton = Root.Q<Button>("Exotic-Resource-Invest-Button");
            _retryButton = Root.Q<Button>("Exotic-Resource-Retry-Button");
            _investmentInputs.Clear();
            _investmentInputs.Add(Root.Q<TextField>("Exotic-Resource-Wood-Input"));
            _investmentInputs.Add(Root.Q<TextField>("Exotic-Resource-Stone-Input"));
            _investmentInputs.Add(Root.Q<TextField>("Exotic-Resource-Metal-Input"));
            _investmentInputs.Add(Root.Q<TextField>("Exotic-Resource-Coin-Input"));
            _investButton.clicked += Invest;
            _retryButton.clicked += OnRetryClicked;
        }

        private void UnbindControls()
        {
            if (_investButton != null)
            {
                _investButton.clicked -= Invest;
            }
            if (_retryButton != null)
            {
                _retryButton.clicked -= OnRetryClicked;
            }

            _tabs.Clear();
        }

        private void LoadIsland(int openVersion)
        {
            if (NetworkManager.Instance == null)
            {
                ShowError("Network is unavailable.");
                CompleteDeferredOpen(openVersion);
                return;
            }

            int requestVersion = ++_loadVersion;
            _content.style.display = DisplayStyle.None;
            _retryButton.style.display = DisplayStyle.None;
            SetStatus("Loading exotic resources...", false);
            StartCoroutine(NetworkManager.Instance.World.GetIslandDetails(
                _islandId,
                NetworkManager.Instance.JwtToken,
                details =>
                {
                    if (!isActiveAndEnabled || requestVersion != _loadVersion)
                    {
                        return;
                    }

                    if (details == null || details.ExoticResources == null || details.ExoticResources.Count == 0)
                    {
                        ShowError("Could not load this island's exotic resources.");
                        CompleteDeferredOpen(openVersion);
                        return;
                    }

                    _details = details;
                    _activeCityIsOnIsland = NetworkManager.Instance.ActiveCityId.HasValue
                        && details.Cities != null
                        && details.Cities.Any(city => city.Id == NetworkManager.Instance.ActiveCityId.Value);
                    RenderTabs();
                    SelectResource(_selectedSlotIndex);
                    _content.style.display = DisplayStyle.Flex;
                    SetStatus(string.Empty, false);
                    CompleteDeferredOpen(openVersion);
                }));
        }

        private void OnRetryClicked()
        {
            LoadIsland(_openVersion);
        }

        private void RenderTabs()
        {
            _tabsContainer.Clear();
            _tabs.Clear();

            foreach (WorldIslandResourceDTO resource in _details.ExoticResources.OrderBy(item => item.SlotIndex))
            {
                int slotIndex = resource.SlotIndex;
                var tab = new Button(() => SelectResource(slotIndex));
                tab.AddToClassList("exotic-resource-tab");

                var icon = new VisualElement();
                icon.AddToClassList("exotic-resource-tab-icon");
                icon.AddToClassList(GetIconClass(resource.ResourceType));
                tab.Add(icon);

                var label = new Label(resource.ResourceType.ToString().ToUpperInvariant());
                label.AddToClassList("exotic-resource-tab-label");
                tab.Add(label);

                _tabs.Add(tab);
                _tabsContainer.Add(tab);
            }
        }

        private void SelectResource(int slotIndex)
        {
            WorldIslandResourceDTO resource = _details?.ExoticResources?.FirstOrDefault(item => item.SlotIndex == slotIndex);
            if (resource == null)
            {
                ShowError("The selected resource slot is unavailable.");
                return;
            }

            _selectedSlotIndex = slotIndex;
            for (int index = 0; index < _tabs.Count; index++)
            {
                _tabs[index].EnableInClassList("exotic-resource-tab--selected", index == slotIndex);
            }

            SetResourceIcon(resource.ResourceType);
            _resourceName.text = resource.ResourceType.ToString().ToUpperInvariant();
            _output.text = $"{resource.OutputPerHour:N2} per hour for every city on this island";
            if (_details.HasOwnedCity)
            {
                _tier.text = $"TIER {resource.Tier} / 10";
                _progress.value = (float)resource.ProgressPercent;
                _progress.title = $"{resource.ProgressPercent:N1}%";
                _progressLabel.text = resource.Tier >= 10 ? "MAXIMUM TIER" : $"{resource.ProgressPercent:N1}% TO TIER {resource.Tier + 1}";
            }
            else
            {
                _tier.text = "Unknown tier";
                _progress.value = 0;
                _progress.title = "Unknown progress";
                _progressLabel.text = "Unknown progress";
            }
            RenderInvestment(resource);
            ClearInputs();
        }

        private void RenderInvestment(WorldIslandResourceDTO resource)
        {
            bool canInvest = _activeCityIsOnIsland && resource.Tier < 10;
            _investmentSection.style.display = canInvest ? DisplayStyle.Flex : DisplayStyle.None;
            _availabilityMessage.style.display = canInvest ? DisplayStyle.None : DisplayStyle.Flex;
            _availabilityMessage.text = resource.Tier >= 10
                ? "This resource has reached its maximum tier."
                : "Select a city on this island to contribute resources.";

            SetCost("Exotic-Resource-Wood-Cost", resource.WoodInvestment, resource.NextTierWoodCost);
            SetCost("Exotic-Resource-Stone-Cost", resource.StoneInvestment, resource.NextTierStoneCost);
            SetCost("Exotic-Resource-Metal-Cost", resource.MetalInvestment, resource.NextTierMetalCost);
            SetCost("Exotic-Resource-Coin-Cost", resource.CoinInvestment, resource.NextTierCoinCost);
            SetControlsEnabled(canInvest && !_requestInProgress);
        }

        private void Invest()
        {
            if (_requestInProgress || !_activeCityIsOnIsland || !NetworkManager.Instance.ActiveCityId.HasValue)
            {
                return;
            }

            if (!TryReadAmount(_investmentInputs[0], out double wood)
                || !TryReadAmount(_investmentInputs[1], out double stone)
                || !TryReadAmount(_investmentInputs[2], out double metal)
                || !TryReadAmount(_investmentInputs[3], out double coins))
            {
                SetStatus("Enter non-negative numeric amounts.", true);
                return;
            }

            if (wood + stone + metal + coins <= 0)
            {
                SetStatus("Enter at least one contribution.", true);
                return;
            }

            if (!HasAvailableResources(wood, stone, metal, coins))
            {
                SetStatus("The active city does not have the entered resources.", true);
                return;
            }

            var request = new ExoticResourceInvestmentRequestDTO
            {
                SlotIndex = _selectedSlotIndex,
                WoodAmount = wood,
                StoneAmount = stone,
                MetalAmount = metal,
                CoinAmount = coins
            };

            _requestInProgress = true;
            int investmentVersion = ++_investmentVersion;
            Guid investmentIslandId = _islandId;
            SetControlsEnabled(false);
            SetStatus("Investing resources...", false);
            StartCoroutine(NetworkManager.Instance.City.InvestInExoticResource(
                NetworkManager.Instance.ActiveCityId.Value,
                request,
                NetworkManager.Instance.JwtToken,
                response => HandleInvestmentResponse(response, investmentVersion, investmentIslandId, wood, stone, metal, coins)));
        }

        private void HandleInvestmentResponse(
            ExoticResourceInvestmentResponseDTO response,
            int investmentVersion,
            Guid investmentIslandId,
            double wood,
            double stone,
            double metal,
            double coins)
        {
            if (!isActiveAndEnabled || investmentVersion != _investmentVersion || investmentIslandId != _islandId)
            {
                return;
            }

            _requestInProgress = false;
            if (response == null || response.NewTier <= 0 || response.IslandExoticResources == null || response.IslandExoticResources.Count == 0)
            {
                SetControlsEnabled(true);
                SetStatus("Investment failed. Check your resources and try again.", true);
                return;
            }

            _details.ExoticResources = response.IslandExoticResources;
            CityStateManager.Instance?.DeductResourcesLocally(wood, stone, metal);
            WorldPlayerStateManager.Instance?.DeductResourcesLocally(coins, 0);
            if (NetworkManager.Instance.ActiveCityId.HasValue)
            {
                CityStateManager.Instance?.RequestImmediateRefresh(NetworkManager.Instance.ActiveCityId.Value);
            }
            if (Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid worldPlayerId))
            {
                WorldPlayerStateManager.Instance?.InitiateEconomyRefresh(worldPlayerId);
            }

            RenderTabs();
            SelectResource(_selectedSlotIndex);
            SetStatus("Investment completed.", false);
        }

        private bool HasAvailableResources(double wood, double stone, double metal, double coins)
        {
            var city = CityStateManager.Instance;
            var player = WorldPlayerStateManager.Instance;
            return city != null
                && player != null
                && wood <= city.CurrentResources.WoodAmount
                && stone <= city.CurrentResources.StoneAmount
                && metal <= city.CurrentResources.MetalAmount
                && coins <= player.CurrentEconomy.CoinsAmount;
        }

        private static bool TryReadAmount(TextField field, out double amount)
        {
            string value = string.IsNullOrWhiteSpace(field.value) ? "0" : field.value.Trim();
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount)
                && double.IsFinite(amount)
                && amount >= 0;
        }

        private void SetCost(string elementName, double invested, double cost)
        {
            Label label = Root.Q<Label>(elementName);
            if (label != null)
            {
                label.text = $"{invested:N0} / {cost:N0}";
            }
        }

        private void SetResourceIcon(ExoticResourceTypeEnum resourceType)
        {
            foreach (ExoticResourceTypeEnum type in Enum.GetValues(typeof(ExoticResourceTypeEnum)))
            {
                _resourceIcon.RemoveFromClassList(GetIconClass(type));
            }
            _resourceIcon.AddToClassList(GetIconClass(resourceType));
        }

        private void SetControlsEnabled(bool enabled)
        {
            foreach (TextField input in _investmentInputs.Where(input => input != null))
            {
                input.SetEnabled(enabled);
            }
            _investButton?.SetEnabled(enabled);
            foreach (Button tab in _tabs)
            {
                tab.SetEnabled(!_requestInProgress);
            }
        }

        private void ClearInputs()
        {
            foreach (TextField input in _investmentInputs.Where(input => input != null))
            {
                input.value = string.Empty;
            }
        }

        private void ShowError(string message)
        {
            if (_content != null)
            {
                _content.style.display = DisplayStyle.None;
            }
            if (_retryButton != null)
            {
                _retryButton.style.display = DisplayStyle.Flex;
            }
            SetStatus(message, true);
        }

        private void SetStatus(string message, bool isError)
        {
            if (_statusMessage == null)
            {
                return;
            }
            _statusMessage.style.display = string.IsNullOrEmpty(message)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _statusMessage.text = message;
            _statusMessage.EnableInClassList("exotic-resource-status--error", isError);
        }

        private static string GetIconClass(ExoticResourceTypeEnum resourceType) => $"icon-{resourceType.ToString().ToLowerInvariant()}";
    }
}
