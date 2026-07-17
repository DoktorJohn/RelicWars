using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Project.Modules.UI;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;
using Project.Modules.City;
using System.Collections;

namespace Project.Modules.UI.Windows.Implementations
{
    public partial class BarracksWindowController : BaseWindow
    {
        protected override string WindowName => "Barracks";
        protected override string VisualContainerName => "Barracks-Window-MainContainer";
        protected override string HeaderName => "Barracks-Window-Header";

        // UI Referencer - Containers
        private ScrollView _unitTabsScrollContainer;
        private VisualElement _recruitmentQueueListContainer;

        // UI Referencer - Detail View
        private Label _labelUnitName;
        private Label _labelOwnedCountBadge;
        private Label _labelLockRequirements;
        private Label _labelCostWoodValue;
        private Label _labelCostStoneValue;
        private Label _labelCostMetalValue;
        private Label _labelCostPopulationValue;
        private Label _labelTotalRecruitmentTimeValue;
        private Label _labelRecruitmentEtaValue;

        // UI Referencer - Stats Grid
        private Label _labelStatPowerValue;
        private Label _labelStatArmorValue;
        private Label _labelStatDisciplineValue;
        private Label _labelStatMobilityValue;
        private Label _labelStatReachValue;
        private Label _labelStatLootValue;
        private Label _labelStatPopulationValue;
        private Label _labelStatUnitCapacityValue;
        private Label _labelStatRecruitmentTimeValue;

        // UI Referencer - Controls
        private SliderInt _quantityAdjustmentSlider;
        private IntegerField _quantityAdjustmentInput;
        private Button _executeRecruitButton;
        private Label _queueHeaderSummaryLabel;

        // State Data
        private Guid _currentActiveCityId;
        private BarracksUnitInfoDTO _currentlySelectedUnitData;
        private List<Button> _activeTabButtons = new List<Button>();
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceReferences();

            if (NetworkManager.Instance == null)
            {
                WindowAsyncStateHelper.ShowError(_unitTabsScrollContainer, "Barracks unavailable.");
                CompleteDeferredOpen(version);
                return;
            }

            _currentActiveCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentActiveCityId == Guid.Empty)
            {
                WindowAsyncStateHelper.ShowError(_unitTabsScrollContainer, "Invalid city.");
                CompleteDeferredOpen(version);
                return;
            }

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBarracksQueueChanged += HandleRecruitmentQueueUpdated;

                HandleRecruitmentQueueUpdated(CityStateManager.Instance.CurrentBarracksQueue);

                CityStateManager.Instance.RequestImmediateRefresh(_currentActiveCityId);
            }

            ExecuteRefreshBarracksBuildingData(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopQueueCountdown();
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBarracksQueueChanged -= HandleRecruitmentQueueUpdated;
            }
        }

        private void InitializeUserInterfaceReferences()
        {
            var closeWindowButton = Root.Q<Button>("Header-Close-Button");
            if (closeWindowButton != null) { closeWindowButton.clicked -= Close; closeWindowButton.clicked += Close; }

            _unitTabsScrollContainer = Root.Q<ScrollView>("Tabs-Scroll-Container");
            _labelUnitName = Root.Q<Label>("Lbl-UnitName");
            _labelOwnedCountBadge = Root.Q<Label>("Lbl-OwnedCount");
            _labelLockRequirements = Root.Q<Label>("Lbl-LockRequirements");
            _labelCostWoodValue = Root.Q<Label>("Lbl-CostWood");
            _labelCostStoneValue = Root.Q<Label>("Lbl-CostStone");
            _labelCostMetalValue = Root.Q<Label>("Lbl-CostMetal");
            _labelCostPopulationValue = Root.Q<Label>("Lbl-CostPopulation");
            _labelTotalRecruitmentTimeValue = Root.Q<Label>("Lbl-TotalRecruitmentTime");
            _labelRecruitmentEtaValue = Root.Q<Label>("Lbl-RecruitmentEta");

            _labelStatPowerValue = Root.Q<Label>("Stat-Power");
            _labelStatArmorValue = Root.Q<Label>("Stat-Armor");
            _labelStatDisciplineValue = Root.Q<Label>("Stat-Discipline");
            _labelStatMobilityValue = Root.Q<Label>("Stat-Mobility");
            _labelStatReachValue = Root.Q<Label>("Stat-Reach");
            _labelStatLootValue = Root.Q<Label>("Stat-Loot");
            _labelStatPopulationValue = Root.Q<Label>("Stat-Pop");
            _labelStatUnitCapacityValue = Root.Q<Label>("Stat-Capacity");
            _labelStatRecruitmentTimeValue = Root.Q<Label>("Stat-Time");

            _quantityAdjustmentSlider = Root.Q<SliderInt>("Slider-Quantity");
            _quantityAdjustmentInput = Root.Q<IntegerField>("Input-Quantity");
            _executeRecruitButton = Root.Q<Button>("Btn-Recruit");

            _recruitmentQueueListContainer = Root.Q<VisualElement>("Recruitment-Queue-List");
            _queueHeaderSummaryLabel = Root.Q<Label>("Queue-Header-Label");

            SetupInteractionEventHandlers();
        }

        private void SetupInteractionEventHandlers()
        {
            _quantityAdjustmentSlider?.RegisterValueChangedCallback(evt => {
                if (_quantityAdjustmentInput.value != evt.newValue)
                    _quantityAdjustmentInput.value = evt.newValue;

                UpdateExecuteButtonDynamicText(evt.newValue);
                UpdateCalculatedCostDisplay(evt.newValue);
            });

            _quantityAdjustmentInput?.RegisterValueChangedCallback(evt => {
                int clampedValue = Mathf.Clamp(evt.newValue, _quantityAdjustmentSlider.lowValue, _quantityAdjustmentSlider.highValue);
                if (clampedValue != evt.newValue) _quantityAdjustmentInput.SetValueWithoutNotify(clampedValue);
                if (_quantityAdjustmentSlider.value != clampedValue) _quantityAdjustmentSlider.value = clampedValue;
            });

            if (_executeRecruitButton != null)
            {
                _executeRecruitButton.clicked -= OnRecruitExecutionRequested;
                _executeRecruitButton.clicked += OnRecruitExecutionRequested;
            }
        }

        private void HandleRecruitmentQueueUpdated(List<RecruitmentQueueItemDTO> updatedQueue)
        {
            PopulateActiveRecruitmentQueue(updatedQueue);

            if (_currentlySelectedUnitData != null)
            {
                ApplyUnitSelection(_currentlySelectedUnitData);
            }
        }

        private void ExecuteRefreshBarracksBuildingData(int version)
        {
            string token = NetworkManager.Instance.JwtToken;
            WindowAsyncStateHelper.ShowLoading(_unitTabsScrollContainer, "Loading barracks...");
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, false);

            StartCoroutine(NetworkManager.Instance.Barracks.GetBarracksOverviewInformation(_currentActiveCityId, token, (data) => {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (data == null)
                {
                    WindowAsyncStateHelper.ShowError(
                        _unitTabsScrollContainer,
                        "Could not load barracks data.",
                        () => ExecuteRefreshBarracksBuildingData(version));
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, true);
                    CompleteDeferredOpen(version);
                    return;
                }

                if (data.AvailableUnits == null || data.AvailableUnits.Count == 0)
                {
                    WindowAsyncStateHelper.ShowEmpty(_unitTabsScrollContainer, "No barracks units available.");
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, true);
                    CompleteDeferredOpen(version);
                    return;
                }

                SynchronizeAvailableUnitsTabs(data.AvailableUnits);
                WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, true);
                CompleteDeferredOpen(version);
            }));
        }
    }
}
