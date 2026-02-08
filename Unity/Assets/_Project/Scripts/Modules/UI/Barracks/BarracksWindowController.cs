using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;
using Project.Modules.City;
using System.Collections; // Husk at inkludere denne hvis CityStateManager ligger her

namespace Project.Modules.UI.Windows.Implementations
{
    public class BarracksWindowController : BaseWindow
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
        private Label _labelUnitFlavorText;
        private Label _labelTotalCostString;

        // UI Referencer - Stats Grid
        private Label _labelStatPowerValue;
        private Label _labelStatArmorValue;
        private Label _labelStatDisciplineValue;
        private Label _labelStatMobilityValue;
        private Label _labelStatReachValue;
        private Label _labelStatLootValue;
        private Label _labelStatPopulationValue;
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

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceReferences();

            _currentActiveCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentActiveCityId == Guid.Empty) return;

            // 1. Abonner på CityStateManager for at holde køen synkroniseret på tværs af HUD og vinduer
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBarracksQueueChanged += HandleRecruitmentQueueUpdated;

                // Tegn den nuværende kø fra statemanageren med det samme
                HandleRecruitmentQueueUpdated(CityStateManager.Instance.CurrentBarracksQueue);

                // Trigger en overordnet refresh af byens tilstand (DetailedInfo + Køer)
                CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
            }

            // 2. Hent bygningsspecifik data (Available Units / Tabs)
            ExecuteRefreshBarracksBuildingData();
        }

        private void OnDisable()
        {
            // VIGTIGT: Fjern abonnement for at undgå memory leaks og utilsigtede UI opdateringer
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
            // Vi bruger data direkte fra CityStateManager til at tegne køen
            PopulateActiveRecruitmentQueue(updatedQueue);

            // Da køen har ændret sig, genberegner vi hvad spilleren har råd til (Population caps)
            if (_currentlySelectedUnitData != null)
            {
                ApplyUnitSelection(_currentlySelectedUnitData);
            }
        }

        private void ExecuteRefreshBarracksBuildingData()
        {
            string token = NetworkManager.Instance.JwtToken;
            StartCoroutine(NetworkManager.Instance.Barracks.GetBarracksOverviewInformation(_currentActiveCityId, token, (data) => {
                if (data != null) SynchronizeAvailableUnitsTabs(data.AvailableUnits);
            }));
        }

        private void SynchronizeAvailableUnitsTabs(List<BarracksUnitInfoDTO> availableUnits)
        {
            _unitTabsScrollContainer.Clear();
            _activeTabButtons.Clear();

            if (availableUnits == null || availableUnits.Count == 0) return;

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
                // Når en enhed er færdig, trigger vi manageren til at rydde op i staten for alle vinduer
                CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
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
            _executeRecruitButton.SetEnabled(false);
            int amountToRecruit = _quantityAdjustmentInput.value;

            StartCoroutine(NetworkManager.Instance.Barracks.RecruitUnits(
                _currentActiveCityId,
                _currentlySelectedUnitData.UnitType,
                amountToRecruit,
                NetworkManager.Instance.JwtToken,
                (recruitmentResult) => {

                    _executeRecruitButton.SetEnabled(true);

                    if (recruitmentResult.Success)
                    {
                        if (CityStateManager.Instance != null)
                        {
                            CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
                        }
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