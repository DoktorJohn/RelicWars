using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Modules.UI.Windows;
using System.Collections.Generic;

namespace Project.Modules.UI.Windows.Implementations
{
    public class MarketPlaceWindowController : BaseWindow
    {
        protected override string WindowName => "MarketPlace";
        protected override string VisualContainerName => "Market-Window-MainContainer";
        protected override string HeaderName => "Market-Window-Header";

        private Label _currentLevelDisplayLabel;
        private ScrollView _marketStatisticsScrollView;

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceComponentReferences();

            Guid activeCityIdentifier = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (activeCityIdentifier == Guid.Empty)
            {
                Debug.LogWarning("[MarketPlaceWindowController] Open failed: No valid City ID found.");
                return;
            }

            RequestAndRenderMarketPlaceProjectionData(activeCityIdentifier);
        }

        private void InitializeUserInterfaceComponentReferences()
        {
            var headerCloseButton = Root.Q<Button>("Header-Close-Button");
            if (headerCloseButton != null)
            {
                headerCloseButton.clicked -= Close;
                headerCloseButton.clicked += Close;
            }

            _currentLevelDisplayLabel = Root.Q<Label>("Lbl-Level");
            _marketStatisticsScrollView = Root.Q<ScrollView>("Market-Stats-List");
        }

        private void RequestAndRenderMarketPlaceProjectionData(Guid cityIdentifier)
        {
            if (_marketStatisticsScrollView != null)
            {
                _marketStatisticsScrollView.Clear();
            }

            string authenticationToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.MarketPlace.GetMarketPlaceInfo(cityIdentifier, authenticationToken, (projectionDataList) =>
            {
                if (projectionDataList != null && projectionDataList.Count > 0)
                {
                    UpdateMarketPlaceHeaderInformation(projectionDataList);
                    PopulateMarketPlaceStatisticsTable(projectionDataList);
                }
            }));
        }

        private void UpdateMarketPlaceHeaderInformation(List<MarketPlaceInfoDTO> projectionDataList)
        {
            MarketPlaceInfoDTO currentLevelEntry = projectionDataList.Find(projection => projection.IsCurrentLevel);

            if (_currentLevelDisplayLabel != null)
            {
                _currentLevelDisplayLabel.text = currentLevelEntry != null
                    ? $"Level {currentLevelEntry.Level}"
                    : "Not Constructed";
            }
        }

        private void PopulateMarketPlaceStatisticsTable(List<MarketPlaceInfoDTO> projectionDataList)
        {
            if (_marketStatisticsScrollView == null) return;

            _marketStatisticsScrollView.Clear();

            foreach (MarketPlaceInfoDTO marketProjection in projectionDataList)
            {
                CreateAndAddMarketPlaceStatisticRow(marketProjection);
            }
        }

        private void CreateAndAddMarketPlaceStatisticRow(MarketPlaceInfoDTO marketPlaceProjectionData)
        {
            VisualElement tableRowContainer = new VisualElement();
            tableRowContainer.AddToClassList("table-row");

            if (marketPlaceProjectionData.IsCurrentLevel)
            {
                tableRowContainer.AddToClassList("table-row-current");
            }

            // 1. Level Cell
            Label levelValueLabel = new Label(marketPlaceProjectionData.Level.ToString());
            levelValueLabel.AddToClassList("row-label");
            if (marketPlaceProjectionData.IsCurrentLevel) levelValueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            tableRowContainer.Add(levelValueLabel);

            // 2. Production Cell (Silver bonus)
            // Vi formaterer det som en procentvis bonus jf. din database logik (+0.10 = +10%)
            string percentageText = $"+{(marketPlaceProjectionData.ModifierIncrease * 100):N0}%";
            Label silverBonusLabel = new Label(percentageText);
            silverBonusLabel.AddToClassList("row-label");

            // COLOR: Silver/Success Green
            silverBonusLabel.style.color = new StyleColor(new Color(0.2f, 0.6f, 0.2f));
            if (marketPlaceProjectionData.IsCurrentLevel) silverBonusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            tableRowContainer.Add(silverBonusLabel);

            _marketStatisticsScrollView.Add(tableRowContainer);
        }
    }
}