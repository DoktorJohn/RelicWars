using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.UI;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.UI
{
    public partial class TownHallWindowController
    {
        private void PopulateBuildingGrid(List<AvailableBuildingDTO> buildingDataList, Guid cityIdentifier)
        {
            if (_buildingGridScrollView == null)
            {
                return;
            }

            buildingDataList ??= new List<AvailableBuildingDTO>();
            buildingDataList = buildingDataList
                .OrderBy(building => building.BuildingType == BuildingTypeEnum.TownHall ? 0 : 1)
                .ThenBy(building => (int)building.BuildingType)
                .ToList();
            _availableBuildings = buildingDataList;

            if (buildingDataList.Count == 0)
            {
                _buildingGridScrollView.Clear();
                _buildingCards.Clear();
                WindowAsyncStateHelper.ShowEmpty(_buildingGridScrollView, "No buildings available.");
                RefreshBuildingGridStates();
                return;
            }

            bool needsRebuild = _buildingCards.Count != buildingDataList.Count;
            if (!needsRebuild)
            {
                for (int index = 0; index < buildingDataList.Count; index++)
                {
                    if (_buildingCards[index].BuildingType != buildingDataList[index].BuildingType)
                    {
                        needsRebuild = true;
                        break;
                    }
                }
            }

            if (needsRebuild)
            {
                RebuildBuildingGrid(buildingDataList, cityIdentifier);
            }
            else
            {
                UpdateBuildingCards(buildingDataList);
            }

            RefreshBuildingGridStates();
        }

        private void RebuildBuildingGrid(List<AvailableBuildingDTO> buildingDataList, Guid cityIdentifier)
        {
            _buildingGridScrollView.Clear();
            _buildingCards.Clear();
            VisualElement secondaryBuildingGrid = new VisualElement();
            secondaryBuildingGrid.AddToClassList("building-secondary-grid");

            foreach (var building in buildingDataList)
            {
                var card = new BuildingCardView(_buildingRowTemplateAsset.Instantiate(), building);
                card.Initialize(cityIdentifier, this);
                _buildingCards.Add(card);

                if (building.BuildingType == BuildingTypeEnum.TownHall)
                {
                    card.Root.AddToClassList("building-card-townhall");
                    _buildingGridScrollView.Add(card.Root);
                }
                else
                {
                    secondaryBuildingGrid.Add(card.Root);
                }
            }

            if (secondaryBuildingGrid.childCount > 0)
            {
                _buildingGridScrollView.Add(secondaryBuildingGrid);
            }
        }

        private void UpdateBuildingCards(List<AvailableBuildingDTO> buildingDataList)
        {
            for (int index = 0; index < buildingDataList.Count; index++)
            {
                _buildingCards[index].SetData(buildingDataList[index]);
            }
        }

        private void RefreshBuildingGridStates()
        {
            if (_buildingCards.Count == 0)
            {
                return;
            }

            foreach (var card in _buildingCards)
            {
                card.ApplyState(_currentQueueCount, _isUpgradeInFlight, CityStateManager.Instance);
            }
        }

        private sealed class BuildingCardView
        {
            private readonly VisualElement _root;
            private readonly Label _buildingNameLabel;
            private readonly Label _buildingLevelLabel;
            private readonly Button _upgradeButton;
            private Guid _cityIdentifier;
            private TownHallWindowController _controller;
            private bool _isInitialized;

            public BuildingCardView(VisualElement root, AvailableBuildingDTO initialData)
            {
                _root = root;
                _root.AddToClassList("building-card");

                _buildingNameLabel = _root.Q<Label>("Building-Name");
                _buildingLevelLabel = _root.Q<Label>("Building-Level");
                _upgradeButton = _root.Q<Button>("Upgrade-Button");
                SetData(initialData);
            }

            public VisualElement Root => _root;
            public BuildingTypeEnum BuildingType => CurrentData.BuildingType;
            public AvailableBuildingDTO CurrentData { get; private set; }

            public void Initialize(Guid cityIdentifier, TownHallWindowController controller)
            {
                _cityIdentifier = cityIdentifier;
                _controller = controller;

                if (_upgradeButton != null && !_isInitialized)
                {
                    _upgradeButton.AddToClassList("btn-global-base");
                    _upgradeButton.clicked += OnUpgradeClicked;
                    _upgradeButton.RegisterCallback<MouseEnterEvent>(OnTooltipEnter);
                    _upgradeButton.RegisterCallback<MouseLeaveEvent>(OnTooltipLeave);
                    _upgradeButton.RegisterCallback<MouseMoveEvent>(OnTooltipMove);
                    _isInitialized = true;
                }
            }

            public void SetData(AvailableBuildingDTO data)
            {
                CurrentData = data;

                if (_buildingNameLabel != null)
                {
                    _buildingNameLabel.text = data.BuildingName.ToUpperInvariant();
                }

                if (_buildingLevelLabel != null)
                {
                    _buildingLevelLabel.text = data.IsConstructed
                        ? $"LVL {data.CurrentLevel.Value}"
                        : "NOT BUILT";
                }
            }

            public void ApplyState(int currentQueueCount, bool isUpgradeInFlight, CityStateManager cityStateManager)
            {
                if (_upgradeButton == null)
                {
                    return;
                }

                int queuedUpgradesForThisBuilding = 0;
                bool hasQueueData = cityStateManager?.HasBuildingQueueData == true;
                if (hasQueueData)
                {
                    queuedUpgradesForThisBuilding = cityStateManager.CurrentBuildingQueue.Count(job =>
                        string.Equals(job.Type, CurrentData.BuildingType.ToString(), StringComparison.OrdinalIgnoreCase));
                }

                int targetLevel = (CurrentData.CurrentLevel ?? 0) + queuedUpgradesForThisBuilding;
                int maxLevelAllowed = CurrentData.MaximumLevel > 0 ? CurrentData.MaximumLevel : 20;
                bool canAfford = ResolveCanAfford(cityStateManager);

                if (targetLevel >= maxLevelAllowed)
                {
                    ConfigureButton("MAX LEVEL", false, "btn-imperial-primary");
                }
                else if (isUpgradeInFlight)
                {
                    ConfigureButton("PLEASE WAIT", false, "btn-imperial-primary");
                }
                else if (currentQueueCount >= 7)
                {
                    ConfigureButton("QUEUE FULL", false, "btn-imperial-danger");
                }
                else if (!canAfford)
                {
                    ConfigureButton("INSUFFICIENT RESOURCES", false, "btn-imperial-danger");
                }
                else
                {
                    string buttonText = queuedUpgradesForThisBuilding > 0
                        ? "QUEUE NEXT LEVEL"
                        : CurrentData.IsConstructed ? "UPGRADE" : "BUILD";
                    ConfigureButton(buttonText, true, "btn-imperial-success");
                }
            }

            private bool ResolveCanAfford(CityStateManager cityStateManager)
            {
                if (cityStateManager == null || !cityStateManager.HasDetailedCityState)
                {
                    return CurrentData.CanAfford;
                }

                var resources = cityStateManager.CurrentResources;
                return resources.WoodAmount >= CurrentData.WoodCost
                    && resources.StoneAmount >= CurrentData.StoneCost
                    && resources.MetalAmount >= CurrentData.MetalCost;
            }

            private void ConfigureButton(string text, bool enabled, string styleClass)
            {
                _upgradeButton.text = text;
                _upgradeButton.SetEnabled(enabled);
                _upgradeButton.RemoveFromClassList("btn-imperial-primary");
                _upgradeButton.RemoveFromClassList("btn-imperial-success");
                _upgradeButton.RemoveFromClassList("btn-imperial-danger");
                _upgradeButton.AddToClassList(styleClass);
            }

            private void OnUpgradeClicked()
            {
                _controller?.ExecuteUpgradeRequest(_cityIdentifier, CurrentData.BuildingType);
            }

            private void OnTooltipEnter(MouseEnterEvent evt)
            {
                _controller?.ShowResourceUpgradeTooltip(evt, CurrentData);
            }

            private void OnTooltipLeave(MouseLeaveEvent evt)
            {
                _controller?.HideResourceUpgradeTooltip();
            }

            private void OnTooltipMove(MouseMoveEvent evt)
            {
                _controller?.UpdateResourceUpgradeTooltipPosition(evt);
            }
        }
    }
}
