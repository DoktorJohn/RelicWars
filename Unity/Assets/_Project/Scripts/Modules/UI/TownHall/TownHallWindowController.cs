using UnityEngine;
using Project.Modules.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Project.Network.Manager;
using UnityEngine.UIElements;
using Project.Scripts.Domain.DTOs;
using Project.Network.Models;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using System.Linq;

namespace Project.Scripts.Modules.UI
{
    public partial class TownHallWindowController : BaseWindow
    {
        protected override string WindowName => "TownHall";
        protected override string VisualContainerName => "TownHall-Window-MainContainer";
        protected override string HeaderName => "TownHall-Window-Header";

        [Header("UI Templates")]
        [SerializeField] private VisualTreeAsset _buildingRowTemplateAsset;

        private VisualElement _mainWindowContainer;
        private ScrollView _buildingGridScrollView;
        private VisualElement _constructionQueueContainer;
        private Label _queueHeaderLabel;

        // Tooltip Elements
        private VisualElement _resourceTooltipContainer;
        private Label _tooltipWoodAmountLabel;
        private Label _tooltipStoneAmountLabel;
        private Label _tooltipMetalAmountLabel;
        private Label _tooltipConstructionTimeLabel;

        private Guid _activeCityId;
        private int _currentQueueCount = 0;
        private List<AvailableBuildingDTO> _availableBuildings = new List<AvailableBuildingDTO>();
        private readonly List<BuildingCardView> _buildingCards = new List<BuildingCardView>();
        private Coroutine _queueTimerCoroutine;
        private bool _isUpgradeInFlight;
        private bool _hasInitialReveal;
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            _hasInitialReveal = false;

            if (NetworkManager.Instance == null)
            {
                CompleteDeferredOpen(version);
                return;
            }

            InitializeUserInterfaceReferences();

            if (_mainWindowContainer != null)
            {
                _mainWindowContainer.style.display = DisplayStyle.None;
            }

            _activeCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_activeCityId == Guid.Empty)
            {
                CompleteDeferredOpen(version);
                return;
            }

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBuildingQueueChanged -= PopulateConstructionQueue;
                CityStateManager.Instance.OnBuildingQueueChanged += PopulateConstructionQueue;

                CityStateManager.Instance.OnBuildingStateReceived -= HandleBuildingStateChanged;
                CityStateManager.Instance.OnBuildingStateReceived += HandleBuildingStateChanged;

                CityStateManager.Instance.OnTownHallAvailableBuildingsChanged -= HandleTownHallAvailableBuildingsChanged;
                CityStateManager.Instance.OnTownHallAvailableBuildingsChanged += HandleTownHallAvailableBuildingsChanged;

                if (CityStateManager.Instance.HasBuildingQueueData)
                {
                    PopulateConstructionQueue(CityStateManager.Instance.CurrentBuildingQueue);
                }
                else
                {
                    ShowConstructionQueueLoading();
                }
            }

            bool hasCachedBuildings = CityStateManager.Instance?.HasTownHallAvailableBuildingsData == true;
            if (hasCachedBuildings)
            {
                HandleTownHallAvailableBuildingsChanged(CityStateManager.Instance.CurrentTownHallAvailableBuildings);
                RevealTownHall(version);
            }
            else if (_buildingGridScrollView != null)
            {
                WindowAsyncStateHelper.ShowLoading(_buildingGridScrollView, "Loading buildings...");
            }

            RequestTownHallAvailableBuildingsRefresh(_activeCityId, version, showLoadingState: !hasCachedBuildings);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            _queueTimerCoroutine = null;
            _isUpgradeInFlight = false;
            _hasInitialReveal = false;

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBuildingQueueChanged -= PopulateConstructionQueue;
                CityStateManager.Instance.OnBuildingStateReceived -= HandleBuildingStateChanged;
                CityStateManager.Instance.OnTownHallAvailableBuildingsChanged -= HandleTownHallAvailableBuildingsChanged;
            }
        }

        private void InitializeUserInterfaceReferences()
        {
            _mainWindowContainer = Root.Q<VisualElement>("TownHall-Window-MainContainer");

            _resourceTooltipContainer = Root.Q<VisualElement>("Resource-Tooltip");
            _tooltipWoodAmountLabel = Root.Q<Label>("Tip-Wood");
            _tooltipStoneAmountLabel = Root.Q<Label>("Tip-Stone");
            _tooltipMetalAmountLabel = Root.Q<Label>("Tip-Metal");
            _tooltipConstructionTimeLabel = Root.Q<Label>("Tip-Time");

            if (_resourceTooltipContainer != null)
            {
                _resourceTooltipContainer.style.display = DisplayStyle.None;
            }

            var closeWindowButton = Root.Q<Button>("Header-Close-Button");
            if (closeWindowButton != null)
            {
                closeWindowButton.clicked -= Close;
                closeWindowButton.clicked += Close;
            }

            _buildingGridScrollView = Root.Q<ScrollView>("TownHall-Building-List");
            _constructionQueueContainer = Root.Q<VisualElement>("Building-Queue-List");
            _queueHeaderLabel = Root.Q<Label>("Queue-Header-Label");
            InitializeEdictInterface();
        }

        private void RequestTownHallAvailableBuildingsRefresh(Guid cityIdentifier, int version, bool showLoadingState)
        {
            if (NetworkManager.Instance == null || _buildingGridScrollView == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[TownHall] Beder om friske AvailableBuildings fra backend...");
#endif

            if (showLoadingState)
            {
                WindowAsyncStateHelper.ShowLoading(_buildingGridScrollView, "Loading buildings...");
            }

            string authenticationToken = NetworkManager.Instance.JwtToken;
            StartCoroutine(NetworkManager.Instance.City.GetTownHallAvailableBuildings(cityIdentifier, authenticationToken, (availableBuildings) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (availableBuildings == null)
                {
                    if (CityStateManager.Instance?.HasTownHallAvailableBuildingsData == true)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning("[TownHall] Could not refresh available buildings, keeping cached state.");
#endif
                        return;
                    }

                    WindowAsyncStateHelper.ShowError(
                        _buildingGridScrollView,
                        "Could not load available buildings.",
                        () => RequestTownHallAvailableBuildingsRefresh(cityIdentifier, version, true));

                    if (_mainWindowContainer != null)
                    {
                        _mainWindowContainer.style.display = DisplayStyle.Flex;
                    }

                    RevealTownHall(version);
                    return;
                }

                if (CityStateManager.Instance != null)
                {
                    CityStateManager.Instance.UpdateTownHallAvailableBuildings(availableBuildings);
                }
                else
                {
                    HandleTownHallAvailableBuildingsChanged(availableBuildings);
                }

                if (_mainWindowContainer != null)
                {
                    _mainWindowContainer.style.display = DisplayStyle.Flex;
                }

                RevealTownHall(version);
            }));
        }

        private void ExecuteUpgradeRequest(Guid cityId, BuildingTypeEnum buildingType)
        {
            if (_isUpgradeInFlight)
            {
                return;
            }

            _isUpgradeInFlight = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[TownHall] Trykkede Upgrade på {buildingType}. Sender API kald...");
#endif

            RefreshBuildingGridStates();

            StartCoroutine(NetworkManager.Instance.Building.UpgradeBuilding(cityId, buildingType, NetworkManager.Instance.JwtToken, (success, msg) =>
            {
                _isUpgradeInFlight = false;
                if (!isActiveAndEnabled)
                {
                    if (success && CityStateManager.Instance != null)
                    {
                        CityStateManager.Instance.RequestImmediateRefresh(cityId);
                    }

                    return;
                }

                if (success)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log("[TownHall] Upgrade Success! Beder CityManager om frisk data.");
#endif
                    if (CityStateManager.Instance != null)
                    {
                        CityStateManager.Instance.RequestImmediateRefresh(cityId);
                    }

                    RequestTownHallAvailableBuildingsRefresh(cityId, _requestVersion, showLoadingState: false);
                }
                else
                {
                    RefreshBuildingGridStates();
                    Debug.LogError($"[TownHall] Upgrade failed: {msg}");
                }
            }));
        }

        private void HandleTownHallAvailableBuildingsChanged(List<AvailableBuildingDTO> availableBuildings)
        {
            _availableBuildings = availableBuildings ?? new List<AvailableBuildingDTO>();
            PopulateBuildingGrid(_availableBuildings, _activeCityId);
        }

        private void HandleBuildingStateChanged(List<CityControllerGetDetailedCityInformationBuildingDTO> buildingStates)
        {
            if (buildingStates == null || buildingStates.Count == 0)
            {
                return;
            }

            if (_activeCityId == Guid.Empty)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            RequestTownHallAvailableBuildingsRefresh(
                _activeCityId,
                _requestVersion,
                showLoadingState: !_hasInitialReveal && CityStateManager.Instance?.HasTownHallAvailableBuildingsData != true);
        }

        private void RevealTownHall(int version)
        {
            if (_hasInitialReveal || !IsDeferredOpenCurrent(version))
            {
                return;
            }

            _hasInitialReveal = true;
            CompleteDeferredOpen(version);
        }

        private void ShowConstructionQueueLoading()
        {
            if (_constructionQueueContainer == null)
            {
                return;
            }

            WindowAsyncStateHelper.ShowLoading(_constructionQueueContainer, "Loading queue...");

            if (_queueHeaderLabel != null)
            {
                _queueHeaderLabel.text = "CONSTRUCTION QUEUE (...)";
            }
        }

        private void ShowResourceUpgradeTooltip(MouseEnterEvent mouseEnterEvent, AvailableBuildingDTO buildingData)
        {
            if (_resourceTooltipContainer == null) return;

            if (_tooltipWoodAmountLabel != null) _tooltipWoodAmountLabel.text = buildingData.WoodCost.ToString("N0");
            if (_tooltipStoneAmountLabel != null) _tooltipStoneAmountLabel.text = buildingData.StoneCost.ToString("N0");
            if (_tooltipMetalAmountLabel != null) _tooltipMetalAmountLabel.text = buildingData.MetalCost.ToString("N0");

            TimeSpan duration = TimeSpan.FromSeconds(buildingData.ConstructionTimeInSeconds);
            if (_tooltipConstructionTimeLabel != null)
                _tooltipConstructionTimeLabel.text = duration.ToString(@"hh\:mm\:ss");

            _resourceTooltipContainer.BringToFront();
            _resourceTooltipContainer.style.display = DisplayStyle.Flex;

            UpdateResourceUpgradeTooltipPosition(mouseEnterEvent);
        }

        private void UpdateResourceUpgradeTooltipPosition(IMouseEvent mouseEvent)
        {
            if (_resourceTooltipContainer == null || _resourceTooltipContainer.style.display == DisplayStyle.None) return;

            Vector2 screenPosition = mouseEvent.mousePosition;
            Vector2 localPos = _resourceTooltipContainer.parent.WorldToLocal(screenPosition);

            _resourceTooltipContainer.style.left = localPos.x + 20f;
            _resourceTooltipContainer.style.top = localPos.y + 20f;
        }

        private void HideResourceUpgradeTooltip()
        {
            if (_resourceTooltipContainer != null) _resourceTooltipContainer.style.display = DisplayStyle.None;
        }
    }
}
