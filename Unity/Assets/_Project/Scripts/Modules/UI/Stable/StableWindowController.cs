using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;
using Project.Modules.City;

namespace Project.Modules.UI.Windows.Implementations
{
    public class StableWindowController : BaseWindow
    {
        protected override string WindowName => "Stable";
        protected override string VisualContainerName => "Stable-Window-MainContainer";
        protected override string HeaderName => "Stable-Window-Header";

        // UI References
        private Label _levelLabel;
        private ScrollView _tabsContainer;

        // Detail View Elements
        private Label _lblUnitName;
        private Label _lblOwnedCount;
        private Label _lblFlavor;
        private Label _lblCostString;

        private SliderInt _quantitySlider;
        private IntegerField _quantityInput;
        private Button _recruitBtn;

        // Data State
        private Guid _currentCityId;
        private StableUnitInfoDTO _selectedUnit; // Note: Stable DTO
        private List<Button> _tabButtons = new List<Button>();

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _tabsContainer = Root.Q<ScrollView>("Tabs-Scroll-Container");

            _lblUnitName = Root.Q<Label>("Lbl-UnitName");
            _lblOwnedCount = Root.Q<Label>("Lbl-OwnedCount");
            _lblFlavor = Root.Q<Label>("Lbl-Flavor");
            _lblCostString = Root.Q<Label>("Lbl-CostString");

            _quantitySlider = Root.Q<SliderInt>("Slider-Quantity");
            _quantityInput = Root.Q<IntegerField>("Input-Quantity");
            _recruitBtn = Root.Q<Button>("Btn-Recruit");

            if (_quantitySlider != null && _quantityInput != null)
            {
                _quantitySlider.RegisterValueChangedCallback(evt =>
                {
                    if (_quantityInput.value != evt.newValue)
                        _quantityInput.value = evt.newValue;

                    UpdateRecruitButtonText(evt.newValue);
                    UpdateCostLabel(evt.newValue);
                });

                _quantityInput.RegisterValueChangedCallback(evt =>
                {
                    int clamped = Mathf.Clamp(evt.newValue, _quantitySlider.lowValue, _quantitySlider.highValue);
                    if (clamped != evt.newValue) _quantityInput.SetValueWithoutNotify(clamped);

                    if (_quantitySlider.value != clamped)
                        _quantitySlider.value = clamped;
                });
            }

            if (_recruitBtn != null)
            {
                _recruitBtn.clicked -= OnRecruitClicked;
                _recruitBtn.clicked += OnRecruitClicked;
            }

            _currentCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentCityId == Guid.Empty) return;

            RefreshData();
        }

        private void RefreshData()
        {
            string token = NetworkManager.Instance.JwtToken;
            // Uses Stable endpoint
            StartCoroutine(NetworkManager.Instance.Stable.GetStableOverviewInformation(_currentCityId, token, (stableData) =>
            {
                if (stableData != null)
                {
                    UpdateUI(stableData);
                }
            }));
        }

        private void UpdateUI(StableFullViewDTO data)
        {
            if (_levelLabel != null)
                _levelLabel.text = data.BuildingLevel > 0 ? $"Level {data.BuildingLevel}" : "Not Constructed";

            if (_tabsContainer != null)
            {
                _tabsContainer.Clear();
                _tabButtons.Clear();

                if (data.AvailableUnits != null && data.AvailableUnits.Count > 0)
                {
                    foreach (var unit in data.AvailableUnits)
                    {
                        CreateTab(unit);
                    }

                    if (_selectedUnit == null || !data.AvailableUnits.Any(u => u.UnitType == _selectedUnit.UnitType))
                    {
                        SelectUnit(data.AvailableUnits[0]);
                    }
                    else
                    {
                        var refreshedUnit = data.AvailableUnits.First(u => u.UnitType == _selectedUnit.UnitType);
                        SelectUnit(refreshedUnit);
                    }
                }
            }
        }

        private void CreateTab(StableUnitInfoDTO unit)
        {
            Button tab = new Button();
            tab.text = unit.UnitName;
            tab.AddToClassList("tab-button");
            tab.clicked += () => SelectUnit(unit);

            _tabsContainer.Add(tab);
            _tabButtons.Add(tab);
        }

        private void SelectUnit(StableUnitInfoDTO unit)
        {
            _selectedUnit = unit;

            foreach (var btn in _tabButtons)
            {
                if (btn.text == unit.UnitName) btn.AddToClassList("tab-button-active");
                else btn.RemoveFromClassList("tab-button-active");
            }

            if (_lblUnitName != null) _lblUnitName.text = unit.UnitName;
            if (_lblOwnedCount != null) _lblOwnedCount.text = $"In Army: {unit.CurrentInventoryCount}";
            if (_lblFlavor != null) _lblFlavor.text = GetFlavorText(unit.UnitType);

            // BEREGN MAX BASERET PÅ RESSOURCER
            int maxAffordable = CalculateMaxAffordableAmount(unit);

            if (_quantitySlider != null && _quantityInput != null)
            {
                _quantitySlider.lowValue = 1;

                // Max affordable capped at 1000, but minimum 1 to keep UI functional
                _quantitySlider.highValue = Mathf.Max(1, maxAffordable);

                int startValue = maxAffordable > 0 ? 1 : 0;

                _quantitySlider.value = startValue;
                _quantityInput.value = startValue;

                bool canRecruit = unit.IsUnlocked && maxAffordable > 0;

                _quantitySlider.SetEnabled(canRecruit);
                _quantityInput.SetEnabled(canRecruit);
                _recruitBtn.SetEnabled(canRecruit);

                if (!unit.IsUnlocked)
                {
                    _recruitBtn.text = "LOCKED (Low Level)";
                }
                else if (maxAffordable == 0)
                {
                    _recruitBtn.text = "NOT ENOUGH RESOURCES";
                }
                else
                {
                    UpdateRecruitButtonText(startValue);
                }
            }

            UpdateCostLabel(_quantitySlider != null ? _quantitySlider.value : 1);
        }

        private int CalculateMaxAffordableAmount(StableUnitInfoDTO unit)
        {
            if (CityResourceService.Instance == null) return 999;

            var resources = CityResourceService.Instance.CurrentResources;

            int maxWood = unit.CostWood > 0
                ? (int)(resources.WoodAmount / unit.CostWood)
                : 9999;

            int maxStone = unit.CostStone > 0
                ? (int)(resources.StoneAmount / unit.CostStone)
                : 9999;

            int maxMetal = unit.CostMetal > 0
                ? (int)(resources.MetalAmount / unit.CostMetal)
                : 9999;

            int maxAffordable = Mathf.Min(maxWood, Mathf.Min(maxStone, maxMetal));
            return Mathf.Min(maxAffordable, 1000); // Tech cap
        }

        private void UpdateCostLabel(int quantity)
        {
            if (_selectedUnit == null || _lblCostString == null) return;

            long totalWood = (long)_selectedUnit.CostWood * quantity;
            long totalStone = (long)_selectedUnit.CostStone * quantity;
            long totalMetal = (long)_selectedUnit.CostMetal * quantity;
            long totalTime = (long)_selectedUnit.RecruitmentTimeInSeconds * quantity;

            _lblCostString.text = $"Wood: {totalWood}  |  Stone: {totalStone}  |  Metal: {totalMetal}  |  Time: {totalTime}s";

            if (CityResourceService.Instance != null)
            {
                var res = CityResourceService.Instance.CurrentResources;
                bool canAfford = res.WoodAmount >= totalWood && res.StoneAmount >= totalStone && res.MetalAmount >= totalMetal;
                _lblCostString.style.color = canAfford ? new StyleColor(new Color(0.1f, 0.1f, 0.1f)) : new StyleColor(Color.red);
            }
        }

        private void UpdateRecruitButtonText(int amount)
        {
            if (_recruitBtn == null || _selectedUnit == null) return;
            _recruitBtn.text = $"RECRUIT {amount} {(_selectedUnit.UnitName.ToUpper())}";
        }

        private void OnRecruitClicked()
        {
            if (_selectedUnit == null) return;

            int amount = _quantityInput.value;
            if (amount <= 0) return;

            string token = NetworkManager.Instance.JwtToken;
            _recruitBtn.SetEnabled(false);

            StartCoroutine(NetworkManager.Instance.Stable.RecruitUnits(_currentCityId, _selectedUnit.UnitType, amount, token, (success, message) =>
            {
                _recruitBtn.SetEnabled(true);

                if (success)
                {
                    Debug.Log($"<color=green>[Stable] SUCCESS:</color> {message}");
                    if (CityResourceService.Instance != null)
                        CityResourceService.Instance.InitiateResourceRefresh(_currentCityId);

                    RefreshData();
                }
                else
                {
                    Debug.LogError($"<color=red>[Stable] FAILED:</color> {message}");
                }
            }));
        }

        private string GetFlavorText(UnitTypeEnum type)
        {
            switch (type)
            {
                // Note: Ensure your Enum actually has these types, otherwise default to generic
                case UnitTypeEnum.Cavalry: return "Swift riders ideal for raiding resources and flanking enemy lines.";
                default: return "A powerful mounted unit.";
            }
        }
    }
}