using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Scripts.Domain.State;
using Project.Modules.City;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets._Project.Scripts.Modules.UI
{
    public partial class CityOverviewWindowController
    {
        private void HandleCityResourceStateCalculated(CityResourceState currentState)
        {
            UpdateCityUserInterfaceElements(currentState);
        }

        private void HandleAnyQueueChanged<T>(List<T> ignored)
        {
            UpdateAllActivityStatuses();
        }

        private void UpdateAllActivityStatuses()
        {
            if (CityStateManager.Instance == null) return;

            var buildQueue = CityStateManager.Instance.CurrentBuildingQueue;
            bool isBuilding = buildQueue != null && buildQueue.Count > 0;
            string activeBuilding = isBuilding ? buildQueue[0].Type : "Idle";
            int totalBuilds = buildQueue?.Count ?? 0;

            ApplyStatusLabelConfiguration(_labelStatusTownHall, isBuilding, activeBuilding, totalBuilds);

            int totalUnitsInQueue =
                (CityStateManager.Instance.CurrentBarracksQueue?.Count ?? 0) +
                (CityStateManager.Instance.CurrentStableQueue?.Count ?? 0) +
                (CityStateManager.Instance.CurrentWorkshopQueue?.Count ?? 0);

            bool isRecruiting = totalUnitsInQueue > 0;
            ApplyStatusLabelConfiguration(_labelStatusBarracks, isRecruiting, "Recruiting", totalUnitsInQueue);
        }

        private void PopulateUserInterfaceWithDataModel(CityOverviewHUDDTO productionDataModel)
        {
            UpdateCityUserInterfaceElements(CityStateManager.Instance.CurrentResources);
            _economyResourceGridContainer.Clear();
            VisualElement nativeResourceRow = CreateEconomyResourceRow("economy-card-row--four");
            VisualElement exoticResourceRow = CreateEconomyResourceRow("economy-card-row--three");
            _economyResourceGridContainer.Add(nativeResourceRow);
            _economyResourceGridContainer.Add(exoticResourceRow);

            AddEconomyResourceCard(nativeResourceRow, "WOOD", "icon-wood", productionDataModel.Wood.Production);
            AddEconomyResourceCard(nativeResourceRow, "STONE", "icon-stone", productionDataModel.Stone.Production);
            AddEconomyResourceCard(nativeResourceRow, "METAL", "icon-metal", productionDataModel.Metal.Production);
            AddEconomyResourceCard(nativeResourceRow, "GOLD COINS", "icon-coins", CreateProductionBreakdown(productionDataModel.CoinsProduction));
            foreach (CityExoticResourceProductionDTO exoticResource in (productionDataModel.ExoticResourceProductions ?? new List<CityExoticResourceProductionDTO>())
                         .OrderBy(resource => resource.SlotIndex))
            {
                AddExoticResourceCard(exoticResourceRow, exoticResource);
            }
            if (_labelResistanceDetails != null)
            {
                _labelResistanceDetails.text =
                    $"Resistance: {FormatDecimal(productionDataModel.Resistance)} / " +
                    $"{FormatDecimal(productionDataModel.ResistanceTarget)} " +
                    $"({FormatSignedDecimal(productionDataModel.ResistanceRecoveryPerHour)}/h)";
            }
            UpdateAllActivityStatuses();
        }

        private void UpdateCityUserInterfaceElements(CityResourceState resourceState)
        {
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
                _labelPopulationStatisticalDetails.style.color = resourceState.FreePopulation <= 0
                    ? Color.red
                    : StyleKeyword.Null;
            }
        }

        private static VisualElement CreateEconomyResourceRow(string rowModifierClass)
        {
            var resourceRow = new VisualElement();
            resourceRow.AddToClassList("economy-card-row");
            if (!string.IsNullOrWhiteSpace(rowModifierClass))
            {
                resourceRow.AddToClassList(rowModifierClass);
            }
            return resourceRow;
        }

        private void AddEconomyResourceCard(
            VisualElement resourceRow,
            string resourceTitle,
            string iconCssClass,
            ProductionBreakdownDTO productionBreakdown)
        {
            VisualElement cardContainer = new VisualElement();
            cardContainer.AddToClassList("economy-card");

            VisualElement headerRow = new VisualElement();
            headerRow.AddToClassList("economy-card-header");

            VisualElement resourceIcon = new VisualElement();
            resourceIcon.AddToClassList("economy-card-icon");
            resourceIcon.AddToClassList(iconCssClass);

            Label resourceTitleLabel = new Label(resourceTitle);
            resourceTitleLabel.AddToClassList("economy-card-title");
            resourceTitleLabel.style.color = _darkTextColor;
            resourceTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            resourceTitleLabel.style.fontSize = 13;

            headerRow.Add(resourceIcon);
            headerRow.Add(resourceTitleLabel);

            cardContainer.Add(headerRow);
            cardContainer.Add(CreateStatisticalBreakdownRow("Base Production:", FormatDecimal(productionBreakdown.BaseValue)));
            cardContainer.Add(CreateStatisticalBreakdownRow("Flat Bonus:", FormatSignedDecimal(productionBreakdown.BuildingBonus)));
            cardContainer.Add(CreateStatisticalBreakdownRow("Multipliers:", $"x{FormatDecimal(productionBreakdown.GlobalModifierMultiplier, 2)}"));

            Label hourlyTotalLabel = new Label($"Total: {FormatDecimal(productionBreakdown.FinalValuePerHour)} / h");
            hourlyTotalLabel.AddToClassList("breakdown-total");
            cardContainer.Add(hourlyTotalLabel);

            resourceRow.Add(cardContainer);
        }

        private void AddExoticResourceCard(VisualElement resourceRow, CityExoticResourceProductionDTO resource)
        {
            string resourceTitle = resource.ResourceType.ToString().ToUpperInvariant();
            string iconCssClass = $"icon-{resource.ResourceType.ToString().ToLowerInvariant()}";
            AddEconomyResourceCard(resourceRow, resourceTitle, iconCssClass, resource.Production);
        }

        private static ProductionBreakdownDTO CreateProductionBreakdown(CoinsBreakdownDTO coinsBreakdown)
        {
            if (coinsBreakdown == null)
            {
                return new ProductionBreakdownDTO();
            }

            return new ProductionBreakdownDTO
            {
                BaseValue = coinsBreakdown.BaseValue,
                BuildingBonus = coinsBreakdown.BuildingBonus,
                GlobalModifierMultiplier = coinsBreakdown.GlobalModifierMultiplier,
                FinalValuePerHour = coinsBreakdown.FinalValuePerHour
            };
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

        private static string FormatDecimal(double value, int digits = 1)
        {
            return value.ToString($"F{digits}", CultureInfo.InvariantCulture);
        }

        private static string FormatSignedDecimal(double value, int digits = 1)
        {
            return value >= 0
                ? "+" + FormatDecimal(value, digits)
                : FormatDecimal(value, digits);
        }

        private void ApplyStatusLabelConfiguration(Label targetStatusLabel, bool isQueueBusy, string activeItemName, int totalItemsInQueue)
        {
            if (targetStatusLabel == null) return;

            if (isQueueBusy)
            {
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
