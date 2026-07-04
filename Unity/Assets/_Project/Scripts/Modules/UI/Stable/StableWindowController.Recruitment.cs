using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public partial class StableWindowController
    {
        private void SynchronizeAvailableUnitsTabs(List<StableUnitInfoDTO> availableUnits)
        {
            _unitTabsScrollContainer.Clear();
            _activeTabButtons.Clear();

            if (availableUnits == null || availableUnits.Count == 0)
                return;

            foreach (var unit in availableUnits)
            {
                var tab = new Button { text = unit.UnitName.ToUpper() };
                tab.AddToClassList("tab-button");
                tab.clicked += () => ApplyUnitSelection(unit);

                _unitTabsScrollContainer.Add(tab);
                _activeTabButtons.Add(tab);
            }

            if (_currentlySelectedUnitData == null)
                ApplyUnitSelection(availableUnits[0]);
            else
                ApplyUnitSelection(availableUnits.FirstOrDefault(u => u.UnitType == _currentlySelectedUnitData.UnitType) ?? availableUnits[0]);
        }

        private void ApplyUnitSelection(StableUnitInfoDTO unit)
        {
            _currentlySelectedUnitData = unit;

            foreach (var btn in _activeTabButtons)
            {
                if (btn.text == unit.UnitName.ToUpper()) btn.AddToClassList("tab-button-active");
                else btn.RemoveFromClassList("tab-button-active");
            }

            _labelUnitName.text = unit.UnitName.ToUpper();
            _labelOwnedCountBadge.text = $"OWNED: {unit.AlreadyOwnedCount}";
            _labelUnitFlavorText.text = "Elite cavalry bred for speed and shock impact.";

            _labelStatPowerValue.text = unit.Power.ToString();
            _labelStatArmorValue.text = unit.Armor.ToString();
            _labelStatDisciplineValue.text = unit.Discipline.ToString();
            _labelStatMobilityValue.text = unit.Mobility.ToString();
            _labelStatReachValue.text = unit.Reach.ToString();
            _labelStatLootValue.text = unit.LootCapacity.ToString();
            _labelStatPopulationValue.text = unit.PopulationCost.ToString();
            _labelStatRecruitmentTimeValue.text = TimeSpan.FromSeconds(unit.RecruitmentTimeInSeconds).ToString(@"hh\:mm\:ss");

            int maxPossible = CalculateMaximumAffordableUnitQuantity(unit);
            _quantityAdjustmentSlider.lowValue = 1;
            _quantityAdjustmentSlider.highValue = Mathf.Max(1, maxPossible);

            int startValue = maxPossible > 0 ? 1 : 0;
            _quantityAdjustmentSlider.SetValueWithoutNotify(startValue);
            _quantityAdjustmentInput.SetValueWithoutNotify(startValue);

            bool canConstruct = unit.IsUnlocked && maxPossible > 0;
            _quantityAdjustmentSlider.SetEnabled(canConstruct);
            _quantityAdjustmentInput.SetEnabled(canConstruct);
            _executeRecruitButton.SetEnabled(canConstruct);

            UpdateCalculatedCostDisplay(startValue);
            UpdateExecuteButtonDynamicText(startValue);
        }

        private void PopulateActiveRecruitmentQueue(List<RecruitmentQueueItemDTO> queue)
        {
            _recruitmentQueueListContainer.Clear();
            int count = queue?.Count ?? 0;
            _queueHeaderSummaryLabel.text = $"RECRUITMENT QUEUE ({count}/5)";

            if (count == 0)
            {
                Label empty = new Label("STABLE IS CURRENTLY IDLE");
                empty.AddToClassList("queue-empty-label");
                _recruitmentQueueListContainer.Add(empty);
                return;
            }

            foreach (var item in queue)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("recruitment-item-card");

                card.Add(new Label(item.UnitType.ToString().ToUpper()) { name = "q-title" });
                card.Add(new Label($"QTY: {item.Amount}") { name = "q-amount" });

                Label timer = new Label("--:--:--");
                timer.AddToClassList("queue-item-timer");
                card.Add(timer);

                _recruitmentQueueListContainer.Add(card);

                StartCoroutine(ExecuteUpdateQueueTimerCountdown(timer, item.TimeRemainingSeconds));
            }
        }

        private IEnumerator ExecuteUpdateQueueTimerCountdown(Label label, double seconds)
        {
            float remaining = (float)seconds;
            while (remaining > 0 && label != null)
            {
                label.text = TimeSpan.FromSeconds(remaining).ToString(@"hh\:mm\:ss");
                yield return new WaitForSeconds(1);
                remaining--;
            }

            if (label != null)
            {
                label.text = "READY";
                CityStateManager.Instance?.InitiateResourceRefresh(_currentActiveCityId);
            }
        }

        private int CalculateMaximumAffordableUnitQuantity(StableUnitInfoDTO unit)
        {
            if (CityStateManager.Instance == null) return 0;
            var resources = CityStateManager.Instance.CurrentResources;

            int woodCap = unit.CostWood > 0 ? (int)(resources.WoodAmount / unit.CostWood) : int.MaxValue;
            int stoneCap = unit.CostStone > 0 ? (int)(resources.StoneAmount / unit.CostStone) : int.MaxValue;
            int metalCap = unit.CostMetal > 0 ? (int)(resources.MetalAmount / unit.CostMetal) : int.MaxValue;
            int populationCap = unit.PopulationCost > 0 ? resources.FreePopulation / unit.PopulationCost : int.MaxValue;

            return Mathf.Max(0, Mathf.Min(Mathf.Min(woodCap, Mathf.Min(stoneCap, metalCap)), Mathf.Min(populationCap, 100)));
        }

        private void UpdateCalculatedCostDisplay(int amount)
        {
            if (_currentlySelectedUnitData == null) return;
            _labelTotalCostString.text = $"Wood: {_currentlySelectedUnitData.CostWood * amount} | Stone: {_currentlySelectedUnitData.CostStone * amount} | Metal: {_currentlySelectedUnitData.CostMetal * amount}";
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

            StartCoroutine(NetworkManager.Instance.Stable.RecruitUnits(
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
                        Debug.LogError($"[StableWindow] Recruitment failed: {recruitmentResult.Message}");
                    }
                }));
        }
    }
}
