using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public partial class BarracksWindowController
    {
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

        private void SynchronizeAvailableUnitsTabs(List<BarracksUnitInfoDTO> availableUnits)
        {
            _unitTabsScrollContainer.Clear();
            _activeTabButtons.Clear();

            if (availableUnits == null || availableUnits.Count == 0)
                return;

            foreach (var unit in availableUnits)
            {
                Button tabButton = new Button { text = unit.UnitName.ToUpper() };
                tabButton.AddToClassList("window-tab");
                tabButton.EnableInClassList("unit-tab-locked", !unit.IsUnlocked);
                tabButton.clicked += () => ApplyUnitSelection(unit);
                _unitTabsScrollContainer.Add(tabButton);
                _activeTabButtons.Add(tabButton);
            }

            if (_currentlySelectedUnitData == null)
                ApplyUnitSelection(availableUnits[0]);
            else
                ApplyUnitSelection(availableUnits.FirstOrDefault(u => u.UnitType == _currentlySelectedUnitData.UnitType) ?? availableUnits[0]);
        }

        private void ApplyUnitSelection(BarracksUnitInfoDTO unitData)
        {
            _currentlySelectedUnitData = unitData;

            foreach (var btn in _activeTabButtons)
            {
                if (btn.text == unitData.UnitName.ToUpper()) btn.AddToClassList("window-tab-active");
                else btn.RemoveFromClassList("window-tab-active");
            }

            _labelUnitName.text = unitData.UnitName.ToUpper();
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
            _labelStatUnitCapacityValue.text = unitData.UnitCapacity.ToString();
            _labelStatRecruitmentTimeValue.text = TimeSpan.FromSeconds(unitData.RecruitmentTimeInSeconds).ToString(@"hh\:mm\:ss");

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
            int currentQueueCount = queueItems?.Count ?? 0;
            _queueHeaderSummaryLabel.text = $"RECRUITMENT QUEUE ({currentQueueCount}/5)";

            int activeSlotCount = Mathf.Min(currentQueueCount, 5);
            for (int index = 0; index < activeSlotCount; index++)
            {
                RecruitmentQueueItemDTO item = queueItems[index];
                VisualElement queueCard = CreateQueueSlot();

                Label titleLabel = new Label(item.UnitType.ToString().ToUpper());
                titleLabel.AddToClassList("queue-slot-title");
                queueCard.Add(titleLabel);

                Label timerLabel = new Label("--:--:--");
                timerLabel.AddToClassList("queue-item-timer");
                Label amountLabel = new Label($"AMOUNT {item.Amount}");
                amountLabel.AddToClassList("queue-slot-amount");
                VisualElement metaRow = new VisualElement();
                metaRow.AddToClassList("queue-slot-meta");
                metaRow.Add(amountLabel);
                metaRow.Add(timerLabel);
                queueCard.Add(metaRow);

                VisualElement progressTrack = new VisualElement();
                progressTrack.AddToClassList("queue-progress-track");
                VisualElement progressFill = new VisualElement();
                progressFill.AddToClassList("queue-progress-fill");
                progressTrack.Add(progressFill);
                queueCard.Add(progressTrack);

                _recruitmentQueueListContainer.Add(queueCard);
                _queueCountdownDisplays.Add(new QueueCountdownDisplay
                {
                    Label = timerLabel,
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
                    countdown.RemainingSeconds = Mathf.Max(0f, countdown.RemainingSeconds - Time.unscaledDeltaTime);
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
                    if (CityStateManager.Instance != null && CityStateManager.Instance.IsPollingCity(_currentActiveCityId))
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

        private int CalculateMaximumAffordableUnitQuantity(BarracksUnitInfoDTO unit)
        {
            if (CityStateManager.Instance == null) return 0;
            var resources = CityStateManager.Instance.CurrentResources;

            int woodCap = unit.CostWood > 0 ? (int)(resources.WoodAmount / unit.CostWood) : int.MaxValue;
            int stoneCap = unit.CostStone > 0 ? (int)(resources.StoneAmount / unit.CostStone) : int.MaxValue;
            int metalCap = unit.CostMetal > 0 ? (int)(resources.MetalAmount / unit.CostMetal) : int.MaxValue;
            int populationCap = unit.PopulationCost > 0 ? resources.FreePopulation / unit.PopulationCost : int.MaxValue;

            return Mathf.Max(0, Mathf.Min(Mathf.Min(woodCap, Mathf.Min(stoneCap, metalCap)), Mathf.Min(populationCap, 100)));
        }

        private void UpdateCalculatedCostDisplay(int requestedQuantity)
        {
            if (_currentlySelectedUnitData == null) return;
            _labelCostWoodValue.text = ((long)_currentlySelectedUnitData.CostWood * requestedQuantity).ToString();
            _labelCostStoneValue.text = ((long)_currentlySelectedUnitData.CostStone * requestedQuantity).ToString();
            _labelCostMetalValue.text = ((long)_currentlySelectedUnitData.CostMetal * requestedQuantity).ToString();
            _labelCostPopulationValue.text = ((long)_currentlySelectedUnitData.PopulationCost * requestedQuantity).ToString();

            _previewRecruitmentDurationSeconds = requestedQuantity > 0
                ? (long)_currentlySelectedUnitData.RecruitmentTimeInSeconds * requestedQuantity
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
            if (_currentlySelectedUnitData == null) return;
            _executeRecruitButton.text = $"RECRUIT {amount} {_currentlySelectedUnitData.UnitName.ToUpper()}";
        }

        private void OnRecruitExecutionRequested()
        {
            if (_currentlySelectedUnitData == null || !_currentlySelectedUnitData.IsUnlocked)
                return;

            _executeRecruitButton.SetEnabled(false);
            int amountToRecruit = _quantityAdjustmentInput.value;

            StartCoroutine(NetworkManager.Instance.Barracks.RecruitUnits(
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
                        Debug.LogError($"[BarracksWindow] Recruitment failed: {recruitmentResult.Message}");
                    }
                }));
        }

    }
}
