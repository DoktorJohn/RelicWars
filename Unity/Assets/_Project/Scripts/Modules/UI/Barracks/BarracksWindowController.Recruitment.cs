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
        private void SynchronizeAvailableUnitsTabs(List<BarracksUnitInfoDTO> availableUnits)
        {
            _unitTabsScrollContainer.Clear();
            _activeTabButtons.Clear();

            if (availableUnits == null || availableUnits.Count == 0)
                return;

            foreach (var unit in availableUnits)
            {
                Button tabButton = new Button { text = unit.UnitName.ToUpper() };
                tabButton.AddToClassList("tab-button");
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
                if (btn.text == unitData.UnitName.ToUpper()) btn.AddToClassList("tab-button-active");
                else btn.RemoveFromClassList("tab-button-active");
            }

            _labelUnitName.text = unitData.UnitName.ToUpper();
            _labelOwnedCountBadge.text = $"OWNED: {unitData.AlreadyOwnedCount}";
            _labelUnitFlavorText.text = GetUnitHistoricalFlavorText(unitData.UnitType);

            _labelStatPowerValue.text = unitData.Power.ToString();
            _labelStatArmorValue.text = unitData.Armor.ToString();
            _labelStatDisciplineValue.text = unitData.Discipline.ToString();
            _labelStatMobilityValue.text = unitData.Mobility.ToString();
            _labelStatReachValue.text = unitData.Reach.ToString();
            _labelStatLootValue.text = unitData.LootCapacity.ToString();
            _labelStatPopulationValue.text = unitData.PopulationCost.ToString();
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
        }

        private void PopulateActiveRecruitmentQueue(List<RecruitmentQueueItemDTO> queueItems)
        {
            _recruitmentQueueListContainer.Clear();
            int currentQueueCount = queueItems?.Count ?? 0;
            _queueHeaderSummaryLabel.text = $"RECRUITMENT QUEUE ({currentQueueCount}/5)";

            if (currentQueueCount == 0)
            {
                Label emptyLabel = new Label("BARRACKS ARE CURRENTLY IDLE");
                emptyLabel.AddToClassList("queue-empty-label");
                _recruitmentQueueListContainer.Add(emptyLabel);
                return;
            }

            foreach (var item in queueItems)
            {
                VisualElement queueCard = new VisualElement();
                queueCard.AddToClassList("recruitment-item-card");

                queueCard.Add(new Label(item.UnitType.ToString().ToUpper()) { name = "q-title" });
                queueCard.Add(new Label($"QTY: {item.Amount}") { name = "q-amount" });

                Label timerLabel = new Label("--:--:--");
                timerLabel.AddToClassList("queue-item-timer");
                queueCard.Add(timerLabel);

                _recruitmentQueueListContainer.Add(queueCard);
                StartCoroutine(ExecuteUpdateQueueTimerCountdown(timerLabel, item.TimeRemainingSeconds));
            }
        }

        private IEnumerator ExecuteUpdateQueueTimerCountdown(Label displayLabel, double remainingSeconds)
        {
            float countdownValue = (float)remainingSeconds;
            while (countdownValue > 0 && displayLabel != null)
            {
                displayLabel.text = TimeSpan.FromSeconds(countdownValue).ToString(@"hh\:mm\:ss");
                yield return new WaitForSeconds(1);
                countdownValue--;
            }

            if (displayLabel != null)
            {
                displayLabel.text = "READY";
                CityStateManager.Instance?.InitiateResourceRefresh(_currentActiveCityId);
            }
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
            _labelTotalCostString.text = $"Wood: {_currentlySelectedUnitData.CostWood * requestedQuantity} | Stone: {_currentlySelectedUnitData.CostStone * requestedQuantity} | Metal: {_currentlySelectedUnitData.CostMetal * requestedQuantity}";
        }

        private void UpdateExecuteButtonDynamicText(int amount)
        {
            if (_currentlySelectedUnitData == null) return;
            _executeRecruitButton.text = $"RECRUIT {amount} {_currentlySelectedUnitData.UnitName.ToUpper()}";
        }

        private void OnRecruitExecutionRequested()
        {
            if (_currentlySelectedUnitData == null)
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
                        CityStateManager.Instance?.InitiateResourceRefresh(_currentActiveCityId);
                    }
                    else
                    {
                        Debug.LogError($"[BarracksWindow] Recruitment failed: {recruitmentResult.Message}");
                    }
                }));
        }

        private string GetUnitHistoricalFlavorText(UnitTypeEnum type) => "Imperial forces specialized for regional conquest.";
    }
}
