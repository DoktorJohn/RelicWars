using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Modules.City;

namespace Project.Modules.UI.Windows.Implementations
{
    public partial class StableWindowController : BaseWindow
    {
        protected override string WindowName => "Stable";
        protected override string VisualContainerName => "Stable-Window-MainContainer";
        protected override string HeaderName => "Stable-Window-Header";

        // UI – Containers
        private ScrollView _unitTabsScrollContainer;
        private VisualElement _recruitmentQueueListContainer;

        // UI – Detail View
        private Label _labelUnitName;
        private Label _labelOwnedCountBadge;
        private Label _labelUnitFlavorText;
        private Label _labelTotalCostString;

        // UI – Stats
        private Label _labelStatPowerValue;
        private Label _labelStatArmorValue;
        private Label _labelStatDisciplineValue;
        private Label _labelStatMobilityValue;
        private Label _labelStatReachValue;
        private Label _labelStatLootValue;
        private Label _labelStatPopulationValue;
        private Label _labelStatRecruitmentTimeValue;

        // UI – Controls
        private SliderInt _quantityAdjustmentSlider;
        private IntegerField _quantityAdjustmentInput;
        private Button _executeRecruitButton;
        private Label _queueHeaderSummaryLabel;

        // State
        private Guid _currentActiveCityId;
        private StableUnitInfoDTO _currentlySelectedUnitData;
        private readonly List<Button> _activeTabButtons = new();
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceReferences();

            if (NetworkManager.Instance == null)
            {
                WindowAsyncStateHelper.ShowError(_unitTabsScrollContainer, "Stable unavailable.");
                CompleteDeferredOpen(version);
                return;
            }

            _currentActiveCityId = dataPayload is Guid id
                ? id
                : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (_currentActiveCityId == Guid.Empty)
            {
                WindowAsyncStateHelper.ShowError(_unitTabsScrollContainer, "Invalid city.");
                CompleteDeferredOpen(version);
                return;
            }

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnStableQueueChanged += HandleRecruitmentQueueUpdated;

                HandleRecruitmentQueueUpdated(CityStateManager.Instance.CurrentStableQueue);

                CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
            }

            ExecuteRefreshStableBuildingData(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnStableQueueChanged -= HandleRecruitmentQueueUpdated;
            }
        }

        private void InitializeUserInterfaceReferences()
        {
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null)
            {
                closeBtn.clicked -= Close;
                closeBtn.clicked += Close;
            }

            _unitTabsScrollContainer = Root.Q<ScrollView>("Tabs-Scroll-Container");
            _labelUnitName = Root.Q<Label>("Lbl-UnitName");
            _labelOwnedCountBadge = Root.Q<Label>("Lbl-OwnedCount");
            _labelUnitFlavorText = Root.Q<Label>("Lbl-Flavor");
            _labelTotalCostString = Root.Q<Label>("Lbl-CostString");

            _labelStatPowerValue = Root.Q<Label>("Stat-Power");
            _labelStatArmorValue = Root.Q<Label>("Stat-Armor");
            _labelStatDisciplineValue = Root.Q<Label>("Stat-Discipline");
            _labelStatMobilityValue = Root.Q<Label>("Stat-Mobility");
            _labelStatReachValue = Root.Q<Label>("Stat-Reach");
            _labelStatLootValue = Root.Q<Label>("Stat-Loot");
            _labelStatPopulationValue = Root.Q<Label>("Stat-Pop");
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
            _quantityAdjustmentSlider?.RegisterValueChangedCallback(evt =>
            {
                if (_quantityAdjustmentInput.value != evt.newValue)
                    _quantityAdjustmentInput.value = evt.newValue;

                UpdateExecuteButtonDynamicText(evt.newValue);
                UpdateCalculatedCostDisplay(evt.newValue);
            });

            _quantityAdjustmentInput?.RegisterValueChangedCallback(evt =>
            {
                int clamped = Mathf.Clamp(evt.newValue, _quantityAdjustmentSlider.lowValue, _quantityAdjustmentSlider.highValue);
                if (clamped != evt.newValue) _quantityAdjustmentInput.SetValueWithoutNotify(clamped);
                if (_quantityAdjustmentSlider.value != clamped) _quantityAdjustmentSlider.value = clamped;
            });

            if (_executeRecruitButton != null)
            {
                _executeRecruitButton.clicked -= OnRecruitExecutionRequested;
                _executeRecruitButton.clicked += OnRecruitExecutionRequested;
            }
        }

        private void HandleRecruitmentQueueUpdated(List<RecruitmentQueueItemDTO> updatedQueue)
        {
            // Vi bruger data direkte fra CityStateManager til at tegne køen
            PopulateActiveRecruitmentQueue(updatedQueue);

            // Genberegn hvad der er råd til (Population/Guld kan have ændret sig)
            if (_currentlySelectedUnitData != null)
            {
                ApplyUnitSelection(_currentlySelectedUnitData);
            }
        }

        private void ExecuteRefreshStableBuildingData(int version)
        {
            string token = NetworkManager.Instance.JwtToken;
            WindowAsyncStateHelper.ShowLoading(_unitTabsScrollContainer, "Loading stable...");
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, false);

            StartCoroutine(NetworkManager.Instance.Stable.GetStableOverviewInformation(_currentActiveCityId, token, (data) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (data == null)
                {
                    WindowAsyncStateHelper.ShowError(
                        _unitTabsScrollContainer,
                        "Could not load stable data.",
                        () => ExecuteRefreshStableBuildingData(version));
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, true);
                    CompleteDeferredOpen(version);
                    return;
                }

                if (data.AvailableUnits == null || data.AvailableUnits.Count == 0)
                {
                    WindowAsyncStateHelper.ShowEmpty(_unitTabsScrollContainer, "No stable units available.");
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
