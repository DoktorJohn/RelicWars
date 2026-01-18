using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Modules.UI.Windows; // For BaseWindow

namespace Project.Modules.UI.Windows.Implementations
{
    public class TownHallWindowController : BaseWindow
    {
        protected override string WindowName => "TownHall";
        protected override string VisualContainerName => "TownHall-Window-MainContainer";
        protected override string HeaderName => "TownHall-Window-Header";

        [Header("UI Templates")]
        [SerializeField] private VisualTreeAsset _buildingRowTemplateAsset;

        private ScrollView _gridContainer;

        // Tooltip Elements
        private VisualElement _tooltipContainer;
        private Label _tipWood, _tipStone, _tipMetal, _tipTime;

        public override void OnOpen(object dataPayload)
        {
            // 1. Initialize Tooltip & References
            _tooltipContainer = Root.Q<VisualElement>("Resource-Tooltip");
            _tipWood = Root.Q<Label>("Tip-Wood");
            _tipStone = Root.Q<Label>("Tip-Stone");
            _tipMetal = Root.Q<Label>("Tip-Metal");
            _tipTime = Root.Q<Label>("Tip-Time");

            if (_tooltipContainer != null) _tooltipContainer.style.display = DisplayStyle.None;

            // 2. Close Button
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            // 3. Data ID
            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            _gridContainer = Root.Q<ScrollView>("TownHall-Building-List");

            // Safety Check
            if (_gridContainer == null)
            {
                Debug.LogError("[SenateWindow] Could not find 'TownHall-Building-List' ScrollView in UXML.");
                return;
            }

            RefreshContent(cityId);
        }

        private void RefreshContent(Guid cityId)
        {
            // Clear immediately to show loading state if desired
            if (_gridContainer != null) _gridContainer.Clear();

            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.City.GetTownHallAvailableBuildings(cityId, token, (buildings) =>
            {
                // CRITICAL FIX: Check if this controller/window is still valid before updating UI
                if (_gridContainer == null) return;

                PopulateGrid(buildings, cityId);
            }));
        }

        private void PopulateGrid(List<AvailableBuildingDTO> buildings, Guid cityId)
        {
            // Double check
            if (_gridContainer == null || buildings == null) return;

            _gridContainer.Clear();

            foreach (var building in buildings)
            {
                if (_buildingRowTemplateAsset == null)
                {
                    Debug.LogError("[SenateWindow] Building Row Template Asset is missing in Inspector!");
                    return;
                }

                VisualElement card = _buildingRowTemplateAsset.Instantiate();
                card.AddToClassList("building-card"); // Ensure CSS class is applied

                // --- SET INFO ---
                // Find elements safely
                var nameLbl = card.Q<Label>("Building-Name");
                var levelLbl = card.Q<Label>("Building-Level");
                var upgradeBtn = card.Q<Button>("Upgrade-Button");

                if (nameLbl != null) nameLbl.text = building.BuildingName;
                if (levelLbl != null) levelLbl.text = $"Lvl {building.CurrentLevel}";

                // --- BUTTON ---
                if (upgradeBtn != null)
                {
                    if (building.IsCurrentlyUpgrading)
                    {
                        upgradeBtn.text = "BUILDING...";
                        upgradeBtn.SetEnabled(false);
                    }
                    else
                    {
                        // Logic: Can afford AND meets requirements
                        bool canBuild = building.CanAfford && building.HasPopulationRoom;
                        // Note: You might want to check dependency requirements here too if available in DTO

                        upgradeBtn.SetEnabled(canBuild);
                        upgradeBtn.text = canBuild ? "UPGRADE" : "LOCKED";
                    }

                    // Capture variables for closure
                    var bType = building.BuildingType;
                    upgradeBtn.clicked += () => ExecuteUpgrade(cityId, bType);

                    // --- TOOLTIP EVENTS ---
                    upgradeBtn.RegisterCallback<MouseEnterEvent>(evt => ShowTooltip(evt, building));
                    upgradeBtn.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
                    upgradeBtn.RegisterCallback<MouseMoveEvent>(evt => UpdateTooltipPosition(evt));
                }

                _gridContainer.Add(card);
            }
        }

        private void ShowTooltip(MouseEnterEvent evt, AvailableBuildingDTO data)
        {
            if (_tooltipContainer == null) return;

            // Set Data
            if (_tipWood != null) _tipWood.text = $"{data.WoodCost:N0}";
            if (_tipStone != null) _tipStone.text = $"{data.StoneCost:N0}";
            if (_tipMetal != null) _tipMetal.text = $"{data.MetalCost:N0}";

            if (_tipTime != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(data.ConstructionTimeInSeconds);
                _tipTime.text = t.ToString(@"hh\:mm\:ss");
            }

            _tooltipContainer.style.display = DisplayStyle.Flex;
            _tooltipContainer.BringToFront();

            UpdateTooltipPosition(evt);
        }

        private void UpdateTooltipPosition(IMouseEvent evt)
        {
            if (_tooltipContainer == null || _tooltipContainer.parent == null) return;

            // Convert mouse position to local coordinates
            Vector2 localPos = _tooltipContainer.parent.WorldToLocal(evt.mousePosition);

            // Offset
            _tooltipContainer.style.left = localPos.x + 15;
            _tooltipContainer.style.top = localPos.y + 15;
        }

        private void HideTooltip()
        {
            if (_tooltipContainer != null) _tooltipContainer.style.display = DisplayStyle.None;
        }

        private void ExecuteUpgrade(Guid cityId, BuildingTypeEnum type)
        {
            StartCoroutine(NetworkManager.Instance.Building.UpgradeBuilding(cityId, type, NetworkManager.Instance.JwtToken, (success, msg) =>
            {
                if (success)
                {
                    Debug.Log($"<color=green>[TownHall] Upgrade Started: {type}</color>");

                    if (Project.Modules.City.CityResourceService.Instance != null)
                        Project.Modules.City.CityResourceService.Instance.InitiateResourceRefresh(cityId);

                    RefreshContent(cityId);
                }
                else
                {
                    Debug.LogError($"[TownHall Error] {msg}");
                }
            }));
        }
    }
}