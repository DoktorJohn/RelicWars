using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;
using Project.Modules.City;
using System.Collections;

namespace Project.Modules.UI.Windows.Implementations
{
    public class WorkshopWindowController : BaseWindow
    {
        protected override string WindowName => "Workshop";
        protected override string VisualContainerName => "Workshop-Window-MainContainer";
        protected override string HeaderName => "Workshop-Window-Header";

        // UI – Containers
        private ScrollView _unitTabsScrollContainer;
        private VisualElement _recruitmentQueueListContainer;

        // UI – Detail View
        private Label _labelUnitName;
        private Label _labelOwnedCountBadge;
        private Label _labelUnitFlavorText;
        private Label _labelTotalCostString;

        // UI – Stats Grid
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

        // State Data
        private Guid _currentActiveCityId;
        private WorkshopUnitInfoDTO _currentlySelectedUnitData;
        private readonly List<Button> _activeTabButtons = new();

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceReferences();

            _currentActiveCityId = dataPayload is Guid id
                ? id
                : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (_currentActiveCityId == Guid.Empty)
                return;

            // 1. Abonner på CityStateManager for centraliseret kø-styring
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnWorkshopQueueChanged += HandleRecruitmentQueueUpdated;

                // Render den nuværende kø fra manageren med det samme
                HandleRecruitmentQueueUpdated(CityStateManager.Instance.CurrentWorkshopQueue);

                // Trigger en fuld synkronisering af byens tilstand (DetailedInfo + Alle militære køer)
                CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
            }

            // 2. Hent bygningsspecifikke data (Tilgængelige Siege enheder og tabs)
            ExecuteRefreshWorkshopBuildingData();
        }

        private void OnDisable()
        {
            // VIGTIGT: Fjern event-abonnement for at undgå fejl når vinduet er lukket
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnWorkshopQueueChanged -= HandleRecruitmentQueueUpdated;
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
            // Opdater den visuelle kø baseret på data fra manageren
            PopulateActiveRecruitmentQueue(updatedQueue);

            // Genberegn økonomiske caps (Ressourcer eller population kan have ændret sig siden sidst)
            if (_currentlySelectedUnitData != null)
            {
                ApplyUnitSelection(_currentlySelectedUnitData);
            }
        }

        private void ExecuteRefreshWorkshopBuildingData()
        {
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Workshop.GetWorkshopOverviewInformation(_currentActiveCityId, token, (data) =>
            {
                if (data != null) SynchronizeAvailableUnitsTabs(data.AvailableUnits);
            }));
        }

        private void SynchronizeAvailableUnitsTabs(List<WorkshopUnitInfoDTO> availableUnits)
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

        private void ApplyUnitSelection(WorkshopUnitInfoDTO unitData)
        {
            _currentlySelectedUnitData = unitData;

            foreach (var btn in _activeTabButtons)
            {
                if (btn.text == unitData.UnitName.ToUpper()) btn.AddToClassList("tab-button-active");
                else btn.RemoveFromClassList("tab-button-active");
            }

            _labelUnitName.text = unitData.UnitName.ToUpper();
            _labelOwnedCountBadge.text = $"OWNED: {unitData.AlreadyOwnedCount}";
            _labelUnitFlavorText.text = GetUnitFlavorText(unitData.UnitType);

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
            int count = queueItems?.Count ?? 0;
            _queueHeaderSummaryLabel.text = $"CONSTRUCTION QUEUE ({count}/5)";

            if (count == 0)
            {
                Label emptyLabel = new Label("WORKSHOP IS CURRENTLY IDLE");
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
                // Når en konstruktion er færdig, beder vi manageren om at refreshe for at opdatere antal owned units overalt
                CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
            }
        }

        private int CalculateMaximumAffordableUnitQuantity(WorkshopUnitInfoDTO unit)
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
            _labelTotalCostString.text = $"Wood: {(long)_currentlySelectedUnitData.CostWood * amount} | Stone: {(long)_currentlySelectedUnitData.CostStone * amount} | Metal: {(long)_currentlySelectedUnitData.CostMetal * amount}";
        }

        private void UpdateExecuteButtonDynamicText(int amount)
        {
            if (_currentlySelectedUnitData == null) return;
            _executeRecruitButton.text = $"CONSTRUCT {amount} {_currentlySelectedUnitData.UnitName.ToUpper()}";
        }

        private void OnRecruitExecutionRequested()
        {
            _executeRecruitButton.SetEnabled(false);
            int amountToRecruit = _quantityAdjustmentInput.value;

            // VIGTIG FIX: Vi kalder nu den korrekte Workshop service
            StartCoroutine(NetworkManager.Instance.Workshop.RecruitUnits(
                _currentActiveCityId,
                _currentlySelectedUnitData.UnitType,
                amountToRecruit,
                NetworkManager.Instance.JwtToken,
                (recruitmentResult) => {

                    _executeRecruitButton.SetEnabled(true);

                    if (recruitmentResult.Success)
                    {
                        // Fortæl manageren at tilstanden skal opdateres (trækker ressourcer og opdaterer køer)
                        // Dette trigger HandleRecruitmentQueueUpdated automatisk.
                        if (CityStateManager.Instance != null)
                        {
                            CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[WorkshopWindow] Construction failed: {recruitmentResult.Message}");
                    }
                }));
        }

        private string GetUnitFlavorText(UnitTypeEnum type)
        {
            return "Imperial siege machinery designed for breaking fortified defenses.";
        }
    }
}