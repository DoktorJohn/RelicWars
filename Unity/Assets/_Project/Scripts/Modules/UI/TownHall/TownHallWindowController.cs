using UnityEngine;
using Project.Modules.UI;
using System;
using System.Collections;
using Project.Network.Manager;
using UnityEngine.UIElements;
using Project.Scripts.Domain.DTOs;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;

namespace Project.Scripts.Modules.UI
{
    public class TownHallWindowController : BaseWindow
    {
        protected override string WindowName => "TownHall";
        protected override string VisualContainerName => "TownHall-Window-MainContainer";
        protected override string HeaderName => "TownHall-Window-Header";

        [Header("UI Templates")]
        [SerializeField] private VisualTreeAsset _buildingRowTemplateAsset;

        private VisualElement _mainWindowContainer;
        private ScrollView _buildingGridScrollView;
        private VisualElement _constructionQueueContainer;
        private Label _queueHeaderLabel;

        // Tooltip Elements
        private VisualElement _resourceTooltipContainer;
        private Label _tooltipWoodAmountLabel;
        private Label _tooltipStoneAmountLabel;
        private Label _tooltipMetalAmountLabel;
        private Label _tooltipConstructionTimeLabel;

        private Guid _activeCityId;
        private int _currentQueueCount = 0;

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceReferences();

            if (_mainWindowContainer != null) _mainWindowContainer.style.display = DisplayStyle.None;

            _activeCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_activeCityId == Guid.Empty) return;

            ExecuteRefreshTownHallContent(_activeCityId);
        }

        private void InitializeUserInterfaceReferences()
        {
            _mainWindowContainer = Root.Q<VisualElement>("TownHall-Window-MainContainer");

            // Tooltip mapping
            _resourceTooltipContainer = Root.Q<VisualElement>("Resource-Tooltip");
            _tooltipWoodAmountLabel = Root.Q<Label>("Tip-Wood");
            _tooltipStoneAmountLabel = Root.Q<Label>("Tip-Stone");
            _tooltipMetalAmountLabel = Root.Q<Label>("Tip-Metal");
            _tooltipConstructionTimeLabel = Root.Q<Label>("Tip-Time");

            if (_resourceTooltipContainer != null)
                _resourceTooltipContainer.style.display = DisplayStyle.None;

            var closeWindowButton = Root.Q<Button>("Header-Close-Button");
            if (closeWindowButton != null)
            {
                closeWindowButton.clicked -= Close;
                closeWindowButton.clicked += Close;
            }

            _buildingGridScrollView = Root.Q<ScrollView>("TownHall-Building-List");
            _constructionQueueContainer = Root.Q<VisualElement>("Building-Queue-List");
            _queueHeaderLabel = Root.Q<Label>("Queue-Header-Label");
        }

        private void ExecuteRefreshTownHallContent(Guid cityIdentifier)
        {
            string authenticationToken = NetworkManager.Instance.JwtToken;

            // Vi henter køen først for at kende antallet (x/5) før vi tegner knapperne
            StartCoroutine(NetworkManager.Instance.Building.GetBuildingQueue(cityIdentifier, authenticationToken, (currentQueue) =>
            {
                _currentQueueCount = currentQueue?.Count ?? 0;

                if (_queueHeaderLabel != null)
                {
                    _queueHeaderLabel.text = $"CONSTRUCTION QUEUE ({_currentQueueCount}/5)";
                }

                if (_constructionQueueContainer != null && currentQueue != null)
                {
                    PopulateConstructionQueue(currentQueue);
                }

                // Når køen er hentet, henter vi bygningerne
                StartCoroutine(NetworkManager.Instance.City.GetTownHallAvailableBuildings(cityIdentifier, authenticationToken, (availableBuildings) =>
                {
                    if (_buildingGridScrollView != null && availableBuildings != null)
                    {
                        PopulateBuildingGrid(availableBuildings, cityIdentifier);
                    }

                    if (_mainWindowContainer != null)
                        _mainWindowContainer.style.display = DisplayStyle.Flex;
                }));
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

                if (buildingNameLabel != null) buildingNameLabel.text = building.BuildingName.ToUpper();
                if (buildingLevelLabel != null) buildingLevelLabel.text = $"LVL {building.CurrentLevel}";

                if (upgradeExecutionButton != null)
                {
                    upgradeExecutionButton.AddToClassList("btn-global-base");

                    if (building.IsCurrentlyUpgrading)
                    {
                        upgradeExecutionButton.text = "UPGRADING";
                        upgradeExecutionButton.SetEnabled(false);
                        upgradeExecutionButton.AddToClassList("btn-imperial-primary");
                    }
                    else if (_currentQueueCount >= 5)
                    {
                        // Krav A: Vis "QUEUE FULL" hvis der er 5 ting i gang
                        upgradeExecutionButton.text = "QUEUE FULL";
                        upgradeExecutionButton.SetEnabled(false);
                        upgradeExecutionButton.AddToClassList("btn-imperial-danger");
                    }
                    else
                    {
                        bool canAffordUpgrade = building.CanAfford;
                        upgradeExecutionButton.SetEnabled(canAffordUpgrade);
                        upgradeExecutionButton.text = canAffordUpgrade ? "UPGRADE" : "LOCKED";

                        if (canAffordUpgrade)
                            upgradeExecutionButton.AddToClassList("btn-imperial-success");
                        else
                            upgradeExecutionButton.AddToClassList("btn-imperial-danger");
                    }

                    var bType = building.BuildingType;
                    upgradeExecutionButton.clicked += () => ExecuteUpgradeRequest(cityIdentifier, bType);

                    upgradeExecutionButton.RegisterCallback<MouseEnterEvent>(evt => ShowResourceUpgradeTooltip(evt, building));
                    upgradeExecutionButton.RegisterCallback<MouseLeaveEvent>(evt => HideResourceUpgradeTooltip());
                    upgradeExecutionButton.RegisterCallback<MouseMoveEvent>(evt => UpdateResourceUpgradeTooltipPosition(evt));
                }

                _buildingGridScrollView.Add(buildingCardInstance);
            }
        }

        private void PopulateConstructionQueue(List<BuildingDTO> constructionJobs)
        {
            _constructionQueueContainer.Clear();

            if (constructionJobs.Count == 0)
            {
                Label emptyQueueLabel = new Label("NO ACTIVE CONSTRUCTIONS");
                emptyQueueLabel.AddToClassList("queue-empty-label");
                _constructionQueueContainer.Add(emptyQueueLabel);
                return;
            }

            foreach (var job in constructionJobs)
            {
                VisualElement queueItemElement = new VisualElement();
                queueItemElement.AddToClassList("queue-item-card");

                VisualElement infoContainer = new VisualElement();
                infoContainer.AddToClassList("queue-item-info-container");

                Label jobTitleLabel = new Label(job.Type.ToString().ToUpper());
                jobTitleLabel.AddToClassList("queue-item-title");

                VisualElement levelContainer = new VisualElement();
                levelContainer.AddToClassList("queue-item-level");

                int currentLevel = job.Level - 1;
                Label currentLevelLabel = new Label($"LVL {currentLevel}");
                Label arrowLabel = new Label("↑");
                arrowLabel.AddToClassList("queue-level-arrow");
                Label newLevelLabel = new Label($"{job.Level}");
                newLevelLabel.AddToClassList("queue-level-new");

                levelContainer.Add(currentLevelLabel);
                levelContainer.Add(arrowLabel);
                levelContainer.Add(newLevelLabel);

                infoContainer.Add(jobTitleLabel);
                infoContainer.Add(levelContainer);

                VisualElement footerContainer = new VisualElement();
                footerContainer.AddToClassList("queue-item-footer");

                Label timerDisplayLabel = new Label("--:--:--");
                timerDisplayLabel.AddToClassList("queue-item-time");

                footerContainer.Add(timerDisplayLabel);

                queueItemElement.Add(infoContainer);
                queueItemElement.Add(footerContainer);
                _constructionQueueContainer.Add(queueItemElement);

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
                    ExecuteRefreshTownHallContent(_activeCityId);
                    yield break;
                }

                label.text = timeRemaining.ToString(@"hh\:mm\:ss");
                yield return new WaitForSeconds(1);
            }
        }

        private void ExecuteUpgradeRequest(Guid cityId, BuildingTypeEnum buildingType)
        {
            StartCoroutine(NetworkManager.Instance.Building.UpgradeBuilding(cityId, buildingType, NetworkManager.Instance.JwtToken, (success, msg) =>
            {
                if (success)
                {
                    if (Project.Modules.City.CityStateManager.Instance != null)
                        Project.Modules.City.CityStateManager.Instance.InitiateResourceRefresh(cityId);

                    ExecuteRefreshTownHallContent(cityId);
                }
            }));
        }

        private void ShowResourceUpgradeTooltip(MouseEnterEvent mouseEnterEvent, AvailableBuildingDTO buildingData)
        {
            if (_resourceTooltipContainer == null) return;

            _tooltipWoodAmountLabel.text = buildingData.WoodCost.ToString("N0");
            _tooltipStoneAmountLabel.text = buildingData.StoneCost.ToString("N0");
            _tooltipMetalAmountLabel.text = buildingData.MetalCost.ToString("N0");

            TimeSpan duration = TimeSpan.FromSeconds(buildingData.ConstructionTimeInSeconds);
            _tooltipConstructionTimeLabel.text = duration.ToString(@"hh\:mm\:ss");

            _resourceTooltipContainer.style.display = DisplayStyle.Flex;
            UpdateResourceUpgradeTooltipPosition(mouseEnterEvent);
        }

        private void UpdateResourceUpgradeTooltipPosition(IMouseEvent mouseEvent)
        {
            if (_resourceTooltipContainer == null || _resourceTooltipContainer.style.display == DisplayStyle.None) return;
            Vector2 pos = _resourceTooltipContainer.parent.WorldToLocal(mouseEvent.mousePosition);
            _resourceTooltipContainer.style.left = pos.x + 20f;
            _resourceTooltipContainer.style.top = pos.y + 20f;
        }

        private void HideResourceUpgradeTooltip()
        {
            if (_resourceTooltipContainer != null) _resourceTooltipContainer.style.display = DisplayStyle.None;
        }
    }
}