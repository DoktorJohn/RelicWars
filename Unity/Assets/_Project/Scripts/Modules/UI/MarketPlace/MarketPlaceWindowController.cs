using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Modules.UI;
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
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceComponentReferences();

            if (NetworkManager.Instance == null)
            {
                SetEmptyState();
                CompleteDeferredOpen(version);
                return;
            }

            Guid activeCityIdentifier = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (activeCityIdentifier == Guid.Empty)
            {
                Debug.LogWarning("[MarketPlaceWindowController] Open failed: No valid City ID found.");
                SetEmptyState();
                CompleteDeferredOpen(version);
                return;
            }

            RequestAndRenderMarketPlaceProjectionData(activeCityIdentifier, version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
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

        private void RequestAndRenderMarketPlaceProjectionData(Guid cityIdentifier, int version)
        {
            if (_marketStatisticsScrollView != null)
            {
                _marketStatisticsScrollView.Clear();
            }

            string authenticationToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.MarketPlace.GetMarketPlaceInfo(cityIdentifier, authenticationToken, (projectionDataList) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (projectionDataList != null && projectionDataList.Count > 0)
                {
                    UpdateMarketPlaceHeaderInformation(projectionDataList);
                    PopulateMarketPlaceStatisticsTable(projectionDataList);
                }
                else
                {
                    SetEmptyState();
                }

                CompleteDeferredOpen(version);
            }));
        }

        private void UpdateMarketPlaceHeaderInformation(List<MarketPlaceInfoDTO> projectionDataList)
        {
            MarketPlaceInfoDTO currentLevelEntry = projectionDataList.Find(projection => projection.IsCurrentLevel);

            if (_currentLevelDisplayLabel != null)
            {
                _currentLevelDisplayLabel.text = currentLevelEntry != null
                    ? $"Level {currentLevelEntry.Level}"
                    : "-";
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

            // 2. Production Cell (Coins bonus)
            // Vi formaterer det som en procentvis bonus jf. din database logik (+0.10 = +10%)
            string percentageText = $"+{(marketPlaceProjectionData.ModifierIncrease * 100):N0}%";
            Label coinsBonusLabel = new Label(percentageText);
            coinsBonusLabel.AddToClassList("row-label");

            // COLOR: Coins/Success Green
            coinsBonusLabel.style.color = new StyleColor(new Color(0.2f, 0.6f, 0.2f));
            if (marketPlaceProjectionData.IsCurrentLevel) coinsBonusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            tableRowContainer.Add(coinsBonusLabel);

            _marketStatisticsScrollView.Add(tableRowContainer);
        }

        private void SetEmptyState()
        {
            if (_currentLevelDisplayLabel != null)
            {
                _currentLevelDisplayLabel.text = "-";
            }

            if (_marketStatisticsScrollView != null)
            {
                WindowAsyncStateHelper.ShowEmpty(_marketStatisticsScrollView, "No marketplace data available.");
            }
        }
    }
}
