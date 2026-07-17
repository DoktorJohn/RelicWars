using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public class HarborWindowController : BaseWindow
    {
        protected override string WindowName => "Harbor";
        protected override string VisualContainerName => "Harbor-Window-MainContainer";
        protected override string HeaderName => "Harbor-Window-Header";

        private ScrollView _unitTabsScrollContainer;
        private VisualElement _recruitmentQueueListContainer;

        private Label _labelUnitName;
        private Label _labelOwnedCountBadge;
        private Label _labelLockRequirements;
        private Label _labelCostWoodValue;
        private Label _labelCostStoneValue;
        private Label _labelCostMetalValue;
        private Label _labelCostPopulationValue;
        private Label _labelTotalRecruitmentTimeValue;
        private Label _labelRecruitmentEtaValue;

        private Label _labelStatPowerValue;
        private Label _labelStatArmorValue;
        private Label _labelStatDisciplineValue;
        private Label _labelStatMobilityValue;
        private Label _labelStatReachValue;
        private Label _labelStatLootValue;
        private Label _labelStatPopulationValue;
        private Label _labelStatCapacityValue;
        private Label _labelStatRecruitmentTimeValue;

        private SliderInt _quantityAdjustmentSlider;
        private IntegerField _quantityAdjustmentInput;
        private Button _executeRecruitButton;
        private Label _queueHeaderSummaryLabel;

        private Guid _currentActiveCityId;
        private HarborUnitInfoDTO _currentlySelectedUnitData;
        private readonly List<Button> _activeTabButtons = new();
        private int _requestVersion;
        private readonly List<QueueCountdownDisplay> _queueCountdownDisplays = new();
        private Coroutine _queueCountdownCoroutine;
        private int _queueCountdownVersion;
        private long _previewRecruitmentDurationSeconds = -1;
        private float _nextRecruitmentEtaUpdateAt;

        private sealed class QueueCountdownDisplay
        {
            public Label Label;
            public VisualElement ProgressFill;
            public float RemainingSeconds;
            public float TotalDurationSeconds;
        }

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceReferences();

            if (NetworkManager.Instance == null)
            {
                WindowAsyncStateHelper.ShowError(_unitTabsScrollContainer, "Harbor unavailable.");
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
                CityStateManager.Instance.OnHarborQueueChanged += HandleRecruitmentQueueUpdated;
                HandleRecruitmentQueueUpdated(CityStateManager.Instance.CurrentHarborQueue);
                CityStateManager.Instance.RequestImmediateRefresh(_currentActiveCityId);
            }

            ExecuteRefreshHarborBuildingData(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopQueueCountdown();
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnHarborQueueChanged -= HandleRecruitmentQueueUpdated;
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
            _labelStatCapacityValue = Root.Q<Label>("Stat-Capacity");
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
                {
                    _quantityAdjustmentInput.value = evt.newValue;
                }

                UpdateExecuteButtonDynamicText(evt.newValue);
                UpdateCalculatedCostDisplay(evt.newValue);
            });

            _quantityAdjustmentInput?.RegisterValueChangedCallback(evt =>
            {
                int clamped = Mathf.Clamp(evt.newValue, _quantityAdjustmentSlider.lowValue, _quantityAdjustmentSlider.highValue);
                if (clamped != evt.newValue)
                {
                    _quantityAdjustmentInput.SetValueWithoutNotify(clamped);
                }

                if (_quantityAdjustmentSlider.value != clamped)
                {
                    _quantityAdjustmentSlider.value = clamped;
                }
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

        private void ExecuteRefreshHarborBuildingData(int version)
        {
            string token = NetworkManager.Instance.JwtToken;
            WindowAsyncStateHelper.ShowLoading(_unitTabsScrollContainer, "Loading harbor...");
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, false);

            StartCoroutine(NetworkManager.Instance.Harbor.GetHarborOverviewInformation(_currentActiveCityId, token, data =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (data == null)
                {
                    WindowAsyncStateHelper.ShowError(
                        _unitTabsScrollContainer,
                        "Could not load harbor data.",
                        () => ExecuteRefreshHarborBuildingData(version));
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, true);
                    CompleteDeferredOpen(version);
                    return;
                }

                if (data.AvailableUnits == null || data.AvailableUnits.Count == 0)
                {
                    WindowAsyncStateHelper.ShowEmpty(_unitTabsScrollContainer, "No harbor units available.");
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, true);
                    CompleteDeferredOpen(version);
                    return;
                }

                SynchronizeAvailableUnitsTabs(data.AvailableUnits);
                WindowAsyncStateHelper.SetButtonsEnabled(new[] { _executeRecruitButton }, true);
                CompleteDeferredOpen(version);
            }));
        }

        private void SynchronizeAvailableUnitsTabs(List<HarborUnitInfoDTO> availableUnits)
        {
            _unitTabsScrollContainer.Clear();
            _activeTabButtons.Clear();

            if (availableUnits == null || availableUnits.Count == 0)
            {
                return;
            }

            foreach (var unit in availableUnits)
            {
                var tab = new Button { text = unit.UnitName.ToUpperInvariant() };
                tab.AddToClassList("window-tab");
                tab.EnableInClassList("unit-tab-locked", !unit.IsUnlocked);
                tab.clicked += () => ApplyUnitSelection(unit);
                _unitTabsScrollContainer.Add(tab);
                _activeTabButtons.Add(tab);
            }

            if (_currentlySelectedUnitData == null)
            {
                ApplyUnitSelection(availableUnits[0]);
            }
            else
            {
                ApplyUnitSelection(availableUnits.FirstOrDefault(u => u.UnitType == _currentlySelectedUnitData.UnitType) ?? availableUnits[0]);
            }
        }

        private void ApplyUnitSelection(HarborUnitInfoDTO unitData)
        {
            _currentlySelectedUnitData = unitData;

            foreach (var btn in _activeTabButtons)
            {
                if (btn.text == unitData.UnitName.ToUpperInvariant())
                {
                    btn.AddToClassList("window-tab-active");
                }
                else
                {
                    btn.RemoveFromClassList("window-tab-active");
                }
            }

            _labelUnitName.text = unitData.UnitName.ToUpperInvariant();
            _labelOwnedCountBadge.text = $"OWNED: {unitData.AlreadyOwnedCount}";
            _labelLockRequirements.text = string.Join("\n", unitData.UnmetRequirements ?? new List<string> { "Recruitment is locked." });
            _labelLockRequirements.style.display = unitData.IsUnlocked ? DisplayStyle.None : DisplayStyle.Flex;
            _labelStatPowerValue.text = unitData.Power.ToString();
            _labelStatArmorValue.text = unitData.Armor.ToString();
            _labelStatDisciplineValue.text = unitData.Discipline.ToString();
            _labelStatMobilityValue.text = unitData.Mobility.ToString();
            _labelStatReachValue.text = unitData.Reach.ToString();
            _labelStatLootValue.text = unitData.LootCapacity.ToString();
            _labelStatPopulationValue.text = unitData.PopulationCost.ToString();
            _labelStatCapacityValue.text = unitData.UnitCapacity.ToString();
            _labelStatRecruitmentTimeValue.text =
                TimeSpan.FromSeconds(unitData.RecruitmentTimeInSeconds).ToString(@"hh\:mm\:ss");

            int maxPossible = CalculateMaximumAffordableUnitQuantity(unitData);
            _quantityAdjustmentSlider.lowValue = 1;
            _quantityAdjustmentSlider.highValue = Mathf.Max(1, maxPossible);

            int startValue = maxPossible > 0 ? 1 : 0;
            _quantityAdjustmentSlider.SetValueWithoutNotify(startValue);
            _quantityAdjustmentInput.SetValueWithoutNotify(startValue);

            bool canConstruct = unitData.IsUnlocked && maxPossible > 0;
            _quantityAdjustmentSlider.SetEnabled(canConstruct);
            _quantityAdjustmentInput.SetEnabled(canConstruct);
            _executeRecruitButton.SetEnabled(canConstruct);

            UpdateCalculatedCostDisplay(startValue);
            UpdateExecuteButtonDynamicText(startValue);
            if (!unitData.IsUnlocked) _executeRecruitButton.text = "LOCKED";
        }

        private void PopulateActiveRecruitmentQueue(List<RecruitmentQueueItemDTO> queueItems)
        {
            StopQueueCountdown();
            _recruitmentQueueListContainer.Clear();
            int count = queueItems?.Count ?? 0;
            _queueHeaderSummaryLabel.text = $"RECRUITMENT QUEUE ({count}/5)";

            int activeSlotCount = Mathf.Min(count, 5);
            for (int index = 0; index < activeSlotCount; index++)
            {
                RecruitmentQueueItemDTO item = queueItems[index];
                VisualElement card = CreateQueueSlot();

                Label title = new Label(item.UnitType.ToString().ToUpperInvariant());
                title.AddToClassList("queue-slot-title");
                card.Add(title);

                var timer = new Label("--:--:--");
                timer.AddToClassList("queue-item-timer");
                Label amount = new Label($"AMOUNT {item.Amount}");
                amount.AddToClassList("queue-slot-amount");
                VisualElement meta = new VisualElement();
                meta.AddToClassList("queue-slot-meta");
                meta.Add(amount);
                meta.Add(timer);
                card.Add(meta);

                VisualElement progressTrack = new VisualElement();
                progressTrack.AddToClassList("queue-progress-track");
                VisualElement progressFill = new VisualElement();
                progressFill.AddToClassList("queue-progress-fill");
                progressTrack.Add(progressFill);
                card.Add(progressTrack);

                _recruitmentQueueListContainer.Add(card);
                _queueCountdownDisplays.Add(new QueueCountdownDisplay
                {
                    Label = timer,
                    ProgressFill = progressFill,
                    RemainingSeconds = Mathf.Max(0f, (float)item.TimeRemainingSeconds),
                    TotalDurationSeconds = Mathf.Max(0f, item.TotalDurationSeconds)
                });
            }

            if (_queueCountdownDisplays.Count > 0)
            {
                int countdownVersion = _queueCountdownVersion;
                _queueCountdownCoroutine = StartCoroutine(ExecuteQueueCountdown(countdownVersion));
            }
        }

        private static VisualElement CreateQueueSlot()
        {
            VisualElement slot = new VisualElement();
            slot.AddToClassList("recruitment-queue-slot");
            return slot;
        }

        private void Update()
        {
            if (_previewRecruitmentDurationSeconds < 0 ||
                _labelRecruitmentEtaValue == null ||
                Time.unscaledTime < _nextRecruitmentEtaUpdateAt)
            {
                return;
            }

            _nextRecruitmentEtaUpdateAt = Time.unscaledTime + 1f;
            UpdateRecruitmentEtaDisplay();
        }

        private IEnumerator ExecuteQueueCountdown(int countdownVersion)
        {
            while (isActiveAndEnabled && countdownVersion == _queueCountdownVersion)
            {
                bool queueItemIsReady = false;
                foreach (QueueCountdownDisplay countdown in _queueCountdownDisplays)
                {
                    countdown.RemainingSeconds = Mathf.Max(
                        0f,
                        countdown.RemainingSeconds - Time.unscaledDeltaTime);
                    countdown.Label.text = countdown.RemainingSeconds > 0f
                        ? TimeSpan.FromSeconds(Math.Ceiling(countdown.RemainingSeconds)).ToString(@"hh\:mm\:ss")
                        : "READY";
                    float progress = countdown.RemainingSeconds <= 0f
                        ? 1f
                        : countdown.TotalDurationSeconds > 0f
                            ? Mathf.Clamp01(1f - countdown.RemainingSeconds / countdown.TotalDurationSeconds)
                            : 0f;
                    countdown.ProgressFill.style.width = Length.Percent(progress * 100f);
                    queueItemIsReady |= countdown.RemainingSeconds <= 0f;
                }

                if (queueItemIsReady)
                {
                    _queueCountdownCoroutine = null;
                    if (CityStateManager.Instance != null &&
                        CityStateManager.Instance.IsPollingCity(_currentActiveCityId))
                    {
                        CityStateManager.Instance.RequestImmediateRefresh(_currentActiveCityId);
                    }

                    yield break;
                }

                yield return null;
            }

            _queueCountdownCoroutine = null;
        }

        private void StopQueueCountdown()
        {
            _queueCountdownVersion++;
            if (_queueCountdownCoroutine != null)
            {
                StopCoroutine(_queueCountdownCoroutine);
                _queueCountdownCoroutine = null;
            }

            _queueCountdownDisplays.Clear();
        }

        private int CalculateMaximumAffordableUnitQuantity(HarborUnitInfoDTO unit)
        {
            if (CityStateManager.Instance == null)
            {
                return 0;
            }

            var resources = CityStateManager.Instance.CurrentResources;

            int woodCap = unit.CostWood > 0 ? (int)(resources.WoodAmount / unit.CostWood) : int.MaxValue;
            int stoneCap = unit.CostStone > 0 ? (int)(resources.StoneAmount / unit.CostStone) : int.MaxValue;
            int metalCap = unit.CostMetal > 0 ? (int)(resources.MetalAmount / unit.CostMetal) : int.MaxValue;
            int populationCap = unit.PopulationCost > 0 ? resources.FreePopulation / unit.PopulationCost : int.MaxValue;

            return Mathf.Max(0, Mathf.Min(Mathf.Min(woodCap, Mathf.Min(stoneCap, metalCap)), Mathf.Min(populationCap, 100)));
        }

        private void UpdateCalculatedCostDisplay(int amount)
        {
            if (_currentlySelectedUnitData == null)
            {
                return;
            }

            _labelCostWoodValue.text = ((long)_currentlySelectedUnitData.CostWood * amount).ToString();
            _labelCostStoneValue.text = ((long)_currentlySelectedUnitData.CostStone * amount).ToString();
            _labelCostMetalValue.text = ((long)_currentlySelectedUnitData.CostMetal * amount).ToString();
            _labelCostPopulationValue.text = ((long)_currentlySelectedUnitData.PopulationCost * amount).ToString();

            _previewRecruitmentDurationSeconds = amount > 0
                ? (long)_currentlySelectedUnitData.RecruitmentTimeInSeconds * amount
                : -1;
            _labelTotalRecruitmentTimeValue.text = _previewRecruitmentDurationSeconds >= 0
                ? FormatRecruitmentDuration(_previewRecruitmentDurationSeconds)
                : "--";
            _nextRecruitmentEtaUpdateAt = 0f;
            UpdateRecruitmentEtaDisplay();
        }

        private void UpdateRecruitmentEtaDisplay()
        {
            _labelRecruitmentEtaValue.text = _previewRecruitmentDurationSeconds >= 0
                ? DateTime.UtcNow.AddSeconds(_previewRecruitmentDurationSeconds).ToString("dd/MM HH:mm:ss 'UTC'")
                : "--";
        }

        private static string FormatRecruitmentDuration(long totalSeconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0L, totalSeconds));
            return duration.TotalDays >= 1d
                ? $"{(int)duration.TotalDays}d {duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
                : duration.ToString(@"hh\:mm\:ss");
        }

        private void UpdateExecuteButtonDynamicText(int amount)
        {
            if (_currentlySelectedUnitData == null)
            {
                return;
            }

            _executeRecruitButton.text = $"RECRUIT {amount} {_currentlySelectedUnitData.UnitName.ToUpperInvariant()}";
        }

        private void OnRecruitExecutionRequested()
        {
            if (_currentlySelectedUnitData == null || !_currentlySelectedUnitData.IsUnlocked)
            {
                return;
            }

            _executeRecruitButton.SetEnabled(false);
            int amountToRecruit = _quantityAdjustmentInput.value;

            StartCoroutine(NetworkManager.Instance.Harbor.RecruitUnits(
                _currentActiveCityId,
                _currentlySelectedUnitData.UnitType,
                amountToRecruit,
                NetworkManager.Instance.JwtToken,
                recruitmentResult =>
                {
                    _executeRecruitButton.SetEnabled(true);

                    if (recruitmentResult.Success)
                    {
                        CityStateManager.Instance?.RequestImmediateRefresh(_currentActiveCityId);
                    }
                    else
                    {
                        Debug.LogError($"[HarborWindow] Recruitment failed: {recruitmentResult.Message}");
                    }
                }));
        }

    }
}
