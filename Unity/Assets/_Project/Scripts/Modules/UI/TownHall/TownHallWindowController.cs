using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class TownHallWindowController : BaseWindow
    {
        protected override string WindowName => "TownHall";
        protected override string VisualContainerName => "TownHall-Window-MainContainer";
        protected override string HeaderName => "TownHall-Window-Header";

        [Header("UI Templates")]
        [SerializeField] private VisualTreeAsset _buildingRowTemplateAsset;

        private ScrollView _buildingGridScrollView;
        private ScrollView _constructionQueueScrollView;

        // Tooltip Elements
        private VisualElement _resourceTooltipContainer;
        private Label _tooltipWoodAmountLabel;
        private Label _tooltipStoneAmountLabel;
        private Label _tooltipMetalAmountLabel;
        private Label _tooltipConstructionTimeLabel;

        private Guid _activeCityId;

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceReferences();

            _activeCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_activeCityId == Guid.Empty) return;

            RefreshTownHallContent(_activeCityId);
        }

        private void InitializeUserInterfaceReferences()
        {
            // Tooltip mapping
            _resourceTooltipContainer = Root.Q<VisualElement>("Resource-Tooltip");
            _tooltipWoodAmountLabel = Root.Q<Label>("Tip-Wood");
            _tooltipStoneAmountLabel = Root.Q<Label>("Tip-Stone");
            _tooltipMetalAmountLabel = Root.Q<Label>("Tip-Metal");
            _tooltipConstructionTimeLabel = Root.Q<Label>("Tip-Time");

            if (_resourceTooltipContainer != null)
            {
                _resourceTooltipContainer.style.display = DisplayStyle.None;
            }

            // Global actions
            var closeWindowButton = Root.Q<Button>("Header-Close-Button");
            if (closeWindowButton != null)
            {
                closeWindowButton.clicked -= Close;
                closeWindowButton.clicked += Close;
            }

            _buildingGridScrollView = Root.Q<ScrollView>("TownHall-Building-List");
            _constructionQueueScrollView = Root.Q<ScrollView>("Building-Queue-List");
        }

        private void RefreshTownHallContent(Guid cityIdentifier)
        {
            string authenticationToken = NetworkManager.Instance.JwtToken;

            // 1. Hent tilgængelige bygninger
            StartCoroutine(NetworkManager.Instance.City.GetTownHallAvailableBuildings(cityIdentifier, authenticationToken, (availableBuildings) =>
            {
                if (_buildingGridScrollView == null || availableBuildings == null) return;
                PopulateBuildingGrid(availableBuildings, cityIdentifier);
            }));

            // 2. Hent byggekøen
            StartCoroutine(NetworkManager.Instance.Building.GetBuildingQueue(cityIdentifier, authenticationToken, (currentQueue) =>
            {
                if (_constructionQueueScrollView == null || currentQueue == null) return;
                PopulateConstructionQueue(currentQueue);
            }));
        }

        private void PopulateBuildingGrid(List<AvailableBuildingDTO> buildingDataList, Guid cityIdentifier)
        {
            _buildingGridScrollView.Clear();

            foreach (var building in buildingDataList)
            {
                VisualElement buildingCardInstance = _buildingRowTemplateAsset.Instantiate();
                buildingCardInstance.AddToClassList("building-card");

                var buildingNameLabel = buildingCardInstance.Q<Label>("Building-Name");
                var buildingLevelLabel = buildingCardInstance.Q<Label>("Building-Level");
                var upgradeExecutionButton = buildingCardInstance.Q<Button>("Upgrade-Button");

                if (buildingNameLabel != null) buildingNameLabel.text = building.BuildingName;
                if (buildingLevelLabel != null) buildingLevelLabel.text = $"Lvl {building.CurrentLevel}";

                if (upgradeExecutionButton != null)
                {
                    if (building.IsCurrentlyUpgrading)
                    {
                        upgradeExecutionButton.text = "BUILDING...";
                        upgradeExecutionButton.SetEnabled(false);
                    }
                    else
                    {
                        bool canAffordUpgrade = building.CanAfford && building.HasPopulationRoom;
                        upgradeExecutionButton.SetEnabled(canAffordUpgrade);
                        upgradeExecutionButton.text = canAffordUpgrade ? "UPGRADE" : "LOCKED";
                    }

                    var buildingType = building.BuildingType;
                    upgradeExecutionButton.clicked += () => ExecuteUpgradeRequest(cityIdentifier, buildingType);

                    // Event Registration for Tooltip
                    upgradeExecutionButton.RegisterCallback<MouseEnterEvent>(mouseEvent => ShowResourceUpgradeTooltip(mouseEvent, building));
                    upgradeExecutionButton.RegisterCallback<MouseLeaveEvent>(mouseEvent => HideResourceUpgradeTooltip());
                    upgradeExecutionButton.RegisterCallback<MouseMoveEvent>(mouseEvent => UpdateResourceUpgradeTooltipPosition(mouseEvent));
                }

                _buildingGridScrollView.Add(buildingCardInstance);
            }
        }

        private void PopulateConstructionQueue(List<BuildingDTO> constructionJobs)
        {
            _constructionQueueScrollView.Clear();

            if (constructionJobs.Count == 0)
            {
                Label emptyQueueLabel = new Label("No active construction.");
                emptyQueueLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                emptyQueueLabel.style.marginTop = 20;
                _constructionQueueScrollView.Add(emptyQueueLabel);
                return;
            }

            foreach (var job in constructionJobs)
            {
                VisualElement queueItemElement = new VisualElement();
                queueItemElement.AddToClassList("queue-item-card");

                Label jobTitleLabel = new Label($"{job.Type} (Lvl {job.Level})");
                jobTitleLabel.AddToClassList("queue-item-title");

                Label timerDisplayLabel = new Label("Calculating...");
                timerDisplayLabel.AddToClassList("queue-item-time");

                queueItemElement.Add(jobTitleLabel);
                queueItemElement.Add(timerDisplayLabel);
                _constructionQueueScrollView.Add(queueItemElement);

                if (job.UpgradeFinished.HasValue)
                {
                    StartCoroutine(UpdateConstructionTimerLabel(timerDisplayLabel, job.UpgradeFinished.Value));
                }
            }
        }

        private IEnumerator UpdateConstructionTimerLabel(Label label, DateTime finishTimestamp)
        {
            while (label != null)
            {
                TimeSpan timeRemaining = finishTimestamp - DateTime.UtcNow;
                if (timeRemaining.TotalSeconds <= 0)
                {
                    label.text = "FINISHED";
                    RefreshTownHallContent(_activeCityId);
                    yield break;
                }

                label.text = timeRemaining.ToString(@"hh\:mm\:ss");
                yield return new WaitForSeconds(1);
            }
        }

        private void ExecuteUpgradeRequest(Guid cityId, BuildingTypeEnum buildingType)
        {
            StartCoroutine(NetworkManager.Instance.Building.UpgradeBuilding(cityId, buildingType, NetworkManager.Instance.JwtToken, (requestSuccess, responseMessage) =>
            {
                if (requestSuccess)
                {
                    if (Project.Modules.City.CityResourceService.Instance != null)
                    {
                        Project.Modules.City.CityResourceService.Instance.InitiateResourceRefresh(cityId);
                    }

                    RefreshTownHallContent(cityId);
                }
            }));
        }

        // ============================================================
        // TOOLTIP IMPLEMENTATION
        // ============================================================

        private void ShowResourceUpgradeTooltip(MouseEnterEvent mouseEnterEvent, AvailableBuildingDTO buildingData)
        {
            if (_resourceTooltipContainer == null) return;

            // Opdater værdier i tooltip baseret på DTO
            if (_tooltipWoodAmountLabel != null) _tooltipWoodAmountLabel.text = buildingData.WoodCost.ToString("N0");
            if (_tooltipStoneAmountLabel != null) _tooltipStoneAmountLabel.text = buildingData.StoneCost.ToString("N0");
            if (_tooltipMetalAmountLabel != null) _tooltipMetalAmountLabel.text = buildingData.MetalCost.ToString("N0");

            // Vi antager at DTO har en måde at vise tiden på (enten sekunder eller formateret streng)
            if (_tooltipConstructionTimeLabel != null)
            {
                TimeSpan constructionDuration = TimeSpan.FromSeconds(buildingData.ConstructionTimeInSeconds);
                _tooltipConstructionTimeLabel.text = constructionDuration.ToString(@"hh\:mm\:ss");
            }

            // Gør tooltip synlig
            _resourceTooltipContainer.style.display = DisplayStyle.Flex;

            // Opdater position med det samme så den ikke "blinker" i 0,0
            UpdateResourceUpgradeTooltipPosition(mouseEnterEvent);
        }

        private void UpdateResourceUpgradeTooltipPosition(IMouseEvent mouseEvent)
        {
            if (_resourceTooltipContainer == null || _resourceTooltipContainer.style.display == DisplayStyle.None) return;

            // Vi transformerer musens globale position til tooltip-containerens forældre-koordinater
            // Dette sikrer at tooltippet følger musen korrekt uanset scroll eller vinduesstørrelse
            Vector2 worldMousePosition = mouseEvent.mousePosition;
            Vector2 localPositionInParent = _resourceTooltipContainer.parent.WorldToLocal(worldMousePosition);

            // Tilføj et lille offset så tooltippet ikke ligger direkte under cursoren
            float horizontalOffset = 15f;
            float verticalOffset = 15f;

            _resourceTooltipContainer.style.left = localPositionInParent.x + horizontalOffset;
            _resourceTooltipContainer.style.top = localPositionInParent.y + verticalOffset;
        }

        private void HideResourceUpgradeTooltip()
        {
            if (_resourceTooltipContainer != null)
            {
                _resourceTooltipContainer.style.display = DisplayStyle.None;
            }
        }
    }
}