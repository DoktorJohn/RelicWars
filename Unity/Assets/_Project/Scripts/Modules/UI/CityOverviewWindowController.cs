using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Network;
using Project.Network.Models;

namespace Assets._Project.Scripts.Modules.UI
{
    public class CityOverviewWindowController : BaseWindow
    {
        protected override string WindowName => "Overview";
        protected override string VisualContainerName => "Overview-Window-MainContainer";
        protected override string HeaderName => "Overview-Window-Header";

        private readonly Color _darkTextColor = new Color(0.17f, 0.11f, 0.06f, 1.0f);

        private Label _labelGlobalSilverAmount, _labelGlobalResearchAmount, _labelGlobalIdeologyAmount;
        private VisualElement _economyResourceGridContainer;

        private VisualElement _populationUsageBarFill;
        private Label _labelPopulationStatisticalDetails;

        private Label _labelStatusTownHall, _labelStatusBarracks;

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceComponentReferences();

            // 1) Tillad interaktion med spillet bag vinduet
            if (Root != null)
            {
                Root.pickingMode = PickingMode.Ignore;
            }

            Guid activeCityIdentifier = (dataPayload is Guid cityGuid) ? cityGuid : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (activeCityIdentifier == Guid.Empty) return;

            ExecuteCityOverviewDataRequest(activeCityIdentifier);
        }

        private void InitializeUserInterfaceComponentReferences()
        {
            var headerCloseButton = Root.Q<Button>("Header-Close-Button");
            if (headerCloseButton != null) { headerCloseButton.clicked -= Close; headerCloseButton.clicked += Close; }

            _labelGlobalSilverAmount = Root.Q<Label>("Label-Global-Silver");
            _labelGlobalResearchAmount = Root.Q<Label>("Label-Global-Research");
            _labelGlobalIdeologyAmount = Root.Q<Label>("Label-Global-Ideology");

            _economyResourceGridContainer = Root.Q<VisualElement>("Economy-Grid-Container");

            _populationUsageBarFill = Root.Q<VisualElement>("Population-Bar-Used");
            _labelPopulationStatisticalDetails = Root.Q<Label>("Label-Pop-Details");

            _labelStatusTownHall = Root.Q<Label>("Status-TownHall");
            _labelStatusBarracks = Root.Q<Label>("Status-Barracks");
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

        private void PopulateUserInterfaceWithDataModel(CityOverviewHUDDTO dataModel)
        {
            // 1. Update Global Wallets
            _labelGlobalSilverAmount.text = dataModel.GlobalSilverAmount.ToString("N0");
            _labelGlobalResearchAmount.text = dataModel.GlobalResearchPointsAmount.ToString("N0");
            _labelGlobalIdeologyAmount.text = dataModel.GlobalIdeologyFocusPointsAmount.ToString("N0");

            // 2. Build Economy Grid
            _economyResourceGridContainer.Clear();
            AddEconomyResourceCard("WOOD", "icon-wood", dataModel.Wood.Production);
            AddEconomyResourceCard("STONE", "icon-stone", dataModel.Stone.Production);
            AddEconomyResourceCard("METAL", "icon-metal", dataModel.Metal.Production);
            AddEconomyResourceCard("SILVER", "icon-silver", dataModel.SilverProduction);
            AddEconomyResourceCard("RESEARCH", "icon-research", dataModel.ResearchProduction);
            AddEconomyResourceCard("IDEOLOGY", "icon-ideology", dataModel.IdeologyProduction);

            // 5) Update Population Bar (Fixet logik)
            if (dataModel.Population.MaxCapacity > 0)
            {
                float totalPopulationUsage = (float)dataModel.Population.UsedByBuildings + dataModel.Population.UsedByUnits;
                float usagePercentageCalculated = (totalPopulationUsage / (float)dataModel.Population.MaxCapacity) * 100f;

                // Vi tvinger bredden via Length.Percent
                _populationUsageBarFill.style.width = new StyleLength(new Length(Mathf.Clamp(usagePercentageCalculated, 0, 100), LengthUnit.Percent));
            }
            else
            {
                _populationUsageBarFill.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
            }

            _labelPopulationStatisticalDetails.text = $"Buildings: {dataModel.Population.UsedByBuildings} | Units: {dataModel.Population.UsedByUnits} | Free: {dataModel.Population.FreePopulation}";

            // 4. Update Queue Activity Status
            ApplyStatusLabelConfiguration(_labelStatusTownHall, dataModel.TownHallStatus.IsBusy, dataModel.TownHallStatus.CurrentBuildingName, dataModel.TownHallStatus.JobsInQueue);
            ApplyStatusLabelConfiguration(_labelStatusBarracks, dataModel.BarracksStatus.IsBusy, dataModel.BarracksStatus.CurrentUnitType, dataModel.BarracksStatus.TotalUnitsInQueue);
        }

        private void AddEconomyResourceCard(string resourceTitle, string iconCssClass, ProductionBreakdownDTO productionData)
        {
            VisualElement cardContainer = new VisualElement();
            cardContainer.AddToClassList("economy-card");

            // Card Header
            VisualElement headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 5;

            VisualElement resourceIcon = new VisualElement();
            resourceIcon.AddToClassList("side-bar-icon-base");
            resourceIcon.AddToClassList(iconCssClass);
            resourceIcon.style.width = 22; // Hak større
            resourceIcon.style.height = 22;

            Label resourceTitleLabel = new Label(resourceTitle);
            resourceTitleLabel.style.marginLeft = 10;
            resourceTitleLabel.style.color = _darkTextColor;
            resourceTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            resourceTitleLabel.style.fontSize = 13;

            headerRow.Add(resourceIcon);
            headerRow.Add(resourceTitleLabel);

            // Add Breakdown Data
            cardContainer.Add(headerRow);
            cardContainer.Add(CreateStatisticalBreakdownRow("Base Production:", productionData.BaseValue.ToString("N1")));
            cardContainer.Add(CreateStatisticalBreakdownRow("Flat Bonuses:", $"+{productionData.BuildingBonus:N1}"));
            cardContainer.Add(CreateStatisticalBreakdownRow("Multipliers:", $"x{productionData.GlobalModifierMultiplier:F2}"));

            Label hourlyTotalLabel = new Label($"Total: {productionData.FinalValuePerHour:N1} / h");
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

        private void ApplyStatusLabelConfiguration(Label targetStatusLabel, bool isQueueBusy, string activeItemName, int remainingQueueSize)
        {
            if (isQueueBusy)
            {
                targetStatusLabel.text = $"{activeItemName} (+{remainingQueueSize - 1})";
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