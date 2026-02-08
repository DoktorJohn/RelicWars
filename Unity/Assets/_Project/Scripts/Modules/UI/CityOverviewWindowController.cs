using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Network;
using Project.Network.Models;
using Project.Modules.City;
using Assets.Scripts.Domain.State;

namespace Assets._Project.Scripts.Modules.UI
{
    public class CityOverviewWindowController : BaseWindow
    {
        protected override string WindowName => "Overview";
        protected override string VisualContainerName => "Overview-Window-MainContainer";
        protected override string HeaderName => "Overview-Window-Header";

        private readonly Color _darkTextColor = new Color(0.17f, 0.11f, 0.06f, 1.0f);

        // UI Referencer - Globale Beholdninger
        private Label _labelGlobalSilverAmount;
        private Label _labelGlobalResearchAmount;
        private Label _labelGlobalIdeologyAmount;

        // UI Referencer - Økonomi Grid
        private VisualElement _economyResourceGridContainer;

        // UI Referencer - Population
        private VisualElement _populationUsageBarFill;
        private Label _labelPopulationStatisticalDetails;

        // UI Referencer - Kø Status
        private Label _labelStatusTownHall;
        private Label _labelStatusBarracks; // Bruges til den samlede "Recruitment" status

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceComponentReferences();

            if (Root != null)
            {
                Root.pickingMode = PickingMode.Ignore;
            }

            // 1. Abonner på CityStateManager events
            if (CityStateManager.Instance != null)
            {
                // Ressource opdateringer (hvert sekund/frame)
                CityStateManager.Instance.OnResourceStateChanged += HandleGlobalResourceStateCalculated;

                // Kø opdateringer (når data lander fra serveren)
                CityStateManager.Instance.OnBuildingQueueChanged += HandleAnyQueueChanged;
                CityStateManager.Instance.OnBarracksQueueChanged += HandleAnyQueueChanged;
                CityStateManager.Instance.OnStableQueueChanged += HandleAnyQueueChanged;
                CityStateManager.Instance.OnWorkshopQueueChanged += HandleAnyQueueChanged;

                // Initial kørsel for at vise nuværende tilstand
                UpdateDynamicUserInterfaceElements(CityStateManager.Instance.CurrentResources);
                UpdateAllActivityStatuses();
            }

            Guid activeCityIdentifier = (dataPayload is Guid cityGuid)
                ? cityGuid
                : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (activeCityIdentifier == Guid.Empty) return;

            ExecuteCityOverviewDataRequest(activeCityIdentifier);
        }

        private void OnDisable()
        {
            // VIGTIGT: Fjern abonnementer
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged -= HandleGlobalResourceStateCalculated;
                CityStateManager.Instance.OnBuildingQueueChanged -= HandleAnyQueueChanged;
                CityStateManager.Instance.OnBarracksQueueChanged -= HandleAnyQueueChanged;
                CityStateManager.Instance.OnStableQueueChanged -= HandleAnyQueueChanged;
                CityStateManager.Instance.OnWorkshopQueueChanged -= HandleAnyQueueChanged;
            }
        }

        private void InitializeUserInterfaceComponentReferences()
        {
            var headerCloseButton = Root.Q<Button>("Header-Close-Button");
            if (headerCloseButton != null)
            {
                headerCloseButton.clicked -= Close;
                headerCloseButton.clicked += Close;
            }

            _labelGlobalSilverAmount = Root.Q<Label>("Label-Global-Silver");
            _labelGlobalResearchAmount = Root.Q<Label>("Label-Global-Research");
            _labelGlobalIdeologyAmount = Root.Q<Label>("Label-Global-Ideology");

            _economyResourceGridContainer = Root.Q<VisualElement>("Economy-Grid-Container");

            _populationUsageBarFill = Root.Q<VisualElement>("Population-Bar-Used");
            _labelPopulationStatisticalDetails = Root.Q<Label>("Label-Pop-Details");

            _labelStatusTownHall = Root.Q<Label>("Status-TownHall");
            _labelStatusBarracks = Root.Q<Label>("Status-Barracks");
        }

        private void HandleGlobalResourceStateCalculated(CityResourceState currentState)
        {
            UpdateDynamicUserInterfaceElements(currentState);
        }

        // Fælles handler for alle kø-ændringer
        private void HandleAnyQueueChanged<T>(List<T> ignored)
        {
            UpdateAllActivityStatuses();
        }

        /// <summary>
        /// Samler og opdaterer alle "Busy/Idle" status labels baseret på managerens nuværende lister.
        /// </summary>
        private void UpdateAllActivityStatuses()
        {
            if (CityStateManager.Instance == null) return;

            // --- Town Hall Logik ---
            var buildQueue = CityStateManager.Instance.CurrentBuildingQueue;
            bool isBuilding = buildQueue != null && buildQueue.Count > 0;
            string activeBuilding = isBuilding ? buildQueue[0].Type : "Idle";
            int totalBuilds = buildQueue?.Count ?? 0;

            ApplyStatusLabelConfiguration(_labelStatusTownHall, isBuilding, activeBuilding, totalBuilds);

            // --- Recruitment Logik (Samlet for alle militære bygninger) ---
            int totalUnitsInQueue =
                (CityStateManager.Instance.CurrentBarracksQueue?.Count ?? 0) +
                (CityStateManager.Instance.CurrentStableQueue?.Count ?? 0) +
                (CityStateManager.Instance.CurrentWorkshopQueue?.Count ?? 0);

            bool isRecruiting = totalUnitsInQueue > 0;

            // Vi skriver "Recruiting" hvis der er noget i gang, ellers "Idle"
            ApplyStatusLabelConfiguration(_labelStatusBarracks, isRecruiting, "Recruiting", totalUnitsInQueue);
        }

        private void ExecuteCityOverviewDataRequest(Guid cityIdentifier)
        {
            string authenticationToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.City.GetCityOverviewHUD(cityIdentifier, authenticationToken, (cityOverviewData) =>
            {
                if (cityOverviewData != null)
                {
                    PopulateUserInterfaceWithDataModel(cityOverviewData);
                }
            }));
        }

        private void PopulateUserInterfaceWithDataModel(CityOverviewHUDDTO productionDataModel)
        {
            UpdateDynamicUserInterfaceElements(CityStateManager.Instance.CurrentResources);

            _economyResourceGridContainer.Clear();
            AddEconomyResourceCard("WOOD", "icon-wood", productionDataModel.Wood.Production);
            AddEconomyResourceCard("STONE", "icon-stone", productionDataModel.Stone.Production);
            AddEconomyResourceCard("METAL", "icon-metal", productionDataModel.Metal.Production);
            AddEconomyResourceCard("SILVER", "icon-silver", productionDataModel.SilverProduction);
            AddEconomyResourceCard("RESEARCH", "icon-research", productionDataModel.ResearchProduction);
            AddEconomyResourceCard("IDEOLOGY", "icon-ideology", productionDataModel.IdeologyProduction);

            // Tving en status-opdatering når vi får nye data
            UpdateAllActivityStatuses();
        }

        private void UpdateDynamicUserInterfaceElements(CityResourceState resourceState)
        {
            if (_labelGlobalSilverAmount != null)
                _labelGlobalSilverAmount.text = Math.Floor(resourceState.SilverAmount).ToString("N0");

            if (_labelGlobalResearchAmount != null)
                _labelGlobalResearchAmount.text = Math.Floor(resourceState.ResearchPointsAmount).ToString("N0");

            if (_labelGlobalIdeologyAmount != null)
                _labelGlobalIdeologyAmount.text = Math.Floor(resourceState.IdeologyFocusPointsAmount).ToString("N0");

            if (resourceState.MaxPopulationCapacity > 0)
            {
                float usagePercentage = ((float)resourceState.CurrentPopulationUsage / (float)resourceState.MaxPopulationCapacity) * 100f;
                _populationUsageBarFill.style.width = new StyleLength(new Length(Mathf.Clamp(usagePercentage, 0, 100), LengthUnit.Percent));
            }
            else
            {
                _populationUsageBarFill.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
            }

            if (_labelPopulationStatisticalDetails != null)
            {
                _labelPopulationStatisticalDetails.text = $"Units: {resourceState.CurrentPopulationUsage} | Free: {resourceState.FreePopulation}";
                _labelPopulationStatisticalDetails.style.color = (resourceState.FreePopulation <= 0) ? Color.red : _darkTextColor;
            }
        }

        private void AddEconomyResourceCard(string resourceTitle, string iconCssClass, ProductionBreakdownDTO productionBreakdown)
        {
            VisualElement cardContainer = new VisualElement();
            cardContainer.AddToClassList("economy-card");

            VisualElement headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 5;

            VisualElement resourceIcon = new VisualElement();
            resourceIcon.AddToClassList("side-bar-icon-base");
            resourceIcon.AddToClassList(iconCssClass);
            resourceIcon.style.width = 22;
            resourceIcon.style.height = 22;

            Label resourceTitleLabel = new Label(resourceTitle);
            resourceTitleLabel.style.marginLeft = 10;
            resourceTitleLabel.style.color = _darkTextColor;
            resourceTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            resourceTitleLabel.style.fontSize = 13;

            headerRow.Add(resourceIcon);
            headerRow.Add(resourceTitleLabel);

            cardContainer.Add(headerRow);
            cardContainer.Add(CreateStatisticalBreakdownRow("Base Production:", productionBreakdown.BaseValue.ToString("N1")));
            cardContainer.Add(CreateStatisticalBreakdownRow("Flat Bonus:", $"+{productionBreakdown.BuildingBonus:N1}"));
            cardContainer.Add(CreateStatisticalBreakdownRow("Multipliers:", $"x{productionBreakdown.GlobalModifierMultiplier:F2}"));

            Label hourlyTotalLabel = new Label($"Total: {productionBreakdown.FinalValuePerHour:N1} / h");
            hourlyTotalLabel.AddToClassList("breakdown-total");
            cardContainer.Add(hourlyTotalLabel);

            _economyResourceGridContainer.Add(cardContainer);
        }

        private VisualElement CreateStatisticalBreakdownRow(string descriptionLabelText, string statisticValueText)
        {
            VisualElement statisticalRowContainer = new VisualElement();
            statisticalRowContainer.AddToClassList("breakdown-row");

            Label descriptionLabel = new Label(descriptionLabelText);
            descriptionLabel.AddToClassList("breakdown-label");

            Label statisticValueLabel = new Label(statisticValueText);
            statisticValueLabel.AddToClassList("breakdown-value");

            statisticalRowContainer.Add(descriptionLabel);
            statisticalRowContainer.Add(statisticValueLabel);

            return statisticalRowContainer;
        }

        private void ApplyStatusLabelConfiguration(Label targetStatusLabel, bool isQueueBusy, string activeItemName, int totalItemsInQueue)
        {
            if (targetStatusLabel == null) return;

            if (isQueueBusy)
            {
                // Format: "BuildingName (+X)" hvis der er mere end 1 emne i køen
                string queueCountText = totalItemsInQueue > 1 ? $" (+{totalItemsInQueue - 1})" : "";
                targetStatusLabel.text = $"{activeItemName}{queueCountText}";
                targetStatusLabel.RemoveFromClassList("status-idle");
            }
            else
            {
                targetStatusLabel.text = "Idle";
                targetStatusLabel.AddToClassList("status-idle");
            }
        }
    }
}