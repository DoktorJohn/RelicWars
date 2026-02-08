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
    public class StableWindowController : BaseWindow
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

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceReferences();

            _currentActiveCityId = dataPayload is Guid id
                ? id
                : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (_currentActiveCityId == Guid.Empty)
                return;

            // 1. Abonner på CityStateManager for synkroniseret kø-tilstand
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnStableQueueChanged += HandleRecruitmentQueueUpdated;

                // Render den eksisterende tilstand fra manageren med det samme
                HandleRecruitmentQueueUpdated(CityStateManager.Instance.CurrentStableQueue);

                // Start en overordnet synkronisering (DetailedInfo + Alle Køer)
                CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
            }

            // 2. Hent bygningsspecifikke data (Tilgængelige Cavalry enheder)
            ExecuteRefreshStableBuildingData();
        }

        private void OnDisable()
        {
            // Vigtigt: Ryd op i abonnementer
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

        private void ExecuteRefreshStableBuildingData()
        {
            string token = NetworkManager.Instance.JwtToken;
            StartCoroutine(NetworkManager.Instance.Stable.GetStableOverviewInformation(_currentActiveCityId, token, (data) =>
            {
                if (data != null) SynchronizeAvailableUnitsTabs(data.AvailableUnits);
            }));
        }

        private void SynchronizeAvailableUnitsTabs(List<StableUnitInfoDTO> availableUnits)
        {
            _unitTabsScrollContainer.Clear();
            _activeTabButtons.Clear();

            if (availableUnits == null || availableUnits.Count == 0) return;

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
                // Når en enhed er færdig, trigger vi manageren til at opdatere byens tilstand (population/antal)
                CityStateManager.Instance.InitiateResourceRefresh(_currentActiveCityId);
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
            _executeRecruitButton.SetEnabled(false);
            int amountToRecruit = _quantityAdjustmentInput.value;

            StartCoroutine(NetworkManager.Instance.Stable.RecruitUnits(
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
    }
}