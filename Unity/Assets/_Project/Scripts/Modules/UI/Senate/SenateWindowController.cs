using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Assets._Project.Scripts.Domain.DTOs;
using Project.Network.Manager;

namespace Project.Modules.UI.Senate
{
    public class SenateWindowController : BaseWindow
    {
        protected override string WindowName => "Senate";
        protected override string VisualContainerName => "Senate-Window-MainContainer";
        protected override string HeaderName => "Senate-Window-Header";

        [Header("UI Templates")]
        [SerializeField] private VisualTreeAsset _buildingRowTemplateAsset;

        private ScrollView _gridContainer;

        // Tooltip Elements
        private VisualElement _tooltipContainer;
        private Label _tipWood, _tipStone, _tipMetal, _tipTime;

        public override void OnOpen(object dataPayload)
        {
            // 1. Initialiser Tooltip & Referencer
            _tooltipContainer = Root.Q<VisualElement>("Resource-Tooltip");
            _tipWood = Root.Q<Label>("Tip-Wood");
            _tipStone = Root.Q<Label>("Tip-Stone");
            _tipMetal = Root.Q<Label>("Tip-Metal");
            _tipTime = Root.Q<Label>("Tip-Time");

            if (_tooltipContainer != null) _tooltipContainer.style.display = DisplayStyle.None;

            // 2. Luk Knap
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            // 3. Data ID
            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            _gridContainer = Root.Q<ScrollView>("Senate-Building-List");
            RefreshContent(cityId);
        }

        private void RefreshContent(Guid cityId)
        {
            if (_gridContainer != null) _gridContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.City.GetSenateAvailableBuildings(cityId, token, (buildings) =>
            {
                PopulateGrid(buildings, cityId);
            }));
        }

        private void PopulateGrid(List<AvailableBuildingDTO> buildings, Guid cityId)
        {
            if (_gridContainer == null || buildings == null) return;
            _gridContainer.Clear();

            foreach (var building in buildings)
            {
                if (_buildingRowTemplateAsset == null) continue;

                VisualElement card = _buildingRowTemplateAsset.Instantiate();

                // --- SÆT INFO ---
                card.Q<Label>("Building-Name").text = building.BuildingName;
                card.Q<Label>("Building-Level").text = $"Lvl {building.CurrentLevel}";

                // --- LOAD IKON (FIX) ---
                // Stien bliver: Assets/Resources/UI/Icons/Buildings/Academy
                string iconPath = $"UI/Icons/Buildings/{building.BuildingType}";
                Sprite icon = Resources.Load<Sprite>(iconPath);

                if (icon != null)
                {
                    card.Q<Image>("Building-Icon").sprite = icon;
                }
                else
                {
                    // Debug hjælp: Tjek om filnavnet matcher Enum 100%
                    Debug.LogWarning($"[Senate] Kunne ikke loade ikon på sti: '{iconPath}'. Tjek filnavn og Texture Type!");
                }

                // --- KNAP ---
                Button upgradeBtn = card.Q<Button>("Upgrade-Button");

                if (building.IsCurrentlyUpgrading)
                {
                    upgradeBtn.text = "BUILDING...";
                    upgradeBtn.SetEnabled(false);
                }
                else
                {
                    bool canBuild = building.CanAfford && building.HasPopulationRoom;
                    upgradeBtn.SetEnabled(canBuild);
                    upgradeBtn.text = canBuild ? "UPGRADE" : "LOCKED";
                }

                upgradeBtn.clicked += () => ExecuteUpgrade(cityId, building.BuildingType);

                // --- TOOLTIP ---
                upgradeBtn.RegisterCallback<MouseEnterEvent>(evt => ShowTooltip(evt, building));
                upgradeBtn.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
                // Vigtigt: Opdater position mens musen bevæger sig
                upgradeBtn.RegisterCallback<MouseMoveEvent>(evt => UpdateTooltipPosition(evt));

                _gridContainer.Add(card);
            }
        }

        private void ShowTooltip(MouseEnterEvent evt, AvailableBuildingDTO data)
        {
            if (_tooltipContainer == null) return;

            // Sæt Data
            _tipWood.text = $"{data.WoodCost:N0}";
            _tipStone.text = $"{data.StoneCost:N0}";
            _tipMetal.text = $"{data.MetalCost:N0}";

            TimeSpan t = TimeSpan.FromSeconds(data.ConstructionTimeInSeconds);
            _tipTime.text = t.ToString(@"hh\:mm\:ss");

            _tooltipContainer.style.display = DisplayStyle.Flex;
            _tooltipContainer.BringToFront();

            // Opdater position med det samme
            UpdateTooltipPosition(evt);
        }

        private void UpdateTooltipPosition(IMouseEvent evt)
        {
            if (_tooltipContainer == null) return;

            // FIX: Vi konverterer musens position til containerens lokale koordinater.
            // Dette fikser "300px off" problemet, fordi tooltippet er inde i containeren.
            Vector2 localPos = _tooltipContainer.parent.WorldToLocal(evt.mousePosition);

            // Offset så musen ikke dækker teksten
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
                if (success) RefreshContent(cityId);
            }));
        }
    }
}