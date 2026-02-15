using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class HousingWindowController : BaseWindow
    {
        protected override string WindowName => "Housing";
        protected override string VisualContainerName => "Housing-Window-MainContainer";
        protected override string HeaderName => "Housing-Window-Header";

        private Label _currentHousingLevelDisplayLabel;
        private ScrollView _housingStatisticsScrollView;

        public override void OnOpen(object dataPayload)
        {
            InitializeUserInterfaceComponentReferences();

            Guid activeCityIdentifier = (dataPayload is Guid cityGuid) ? cityGuid : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (activeCityIdentifier == Guid.Empty)
            {
                Debug.LogWarning("[HousingWindowController] Open failed: No valid City ID found.");
                return;
            }

            RequestAndRenderHousingProjectionData(activeCityIdentifier);
        }

        private void InitializeUserInterfaceComponentReferences()
        {
            var headerCloseButton = Root.Q<Button>("Header-Close-Button");
            if (headerCloseButton != null)
            {
                headerCloseButton.clicked -= Close;
                headerCloseButton.clicked += Close;
            }

            _currentHousingLevelDisplayLabel = Root.Q<Label>("Label-Level-Display");
            _housingStatisticsScrollView = Root.Q<ScrollView>("Housing-Stats-List");
        }

        private void RequestAndRenderHousingProjectionData(Guid cityIdentifier)
        {
            if (_housingStatisticsScrollView != null)
            {
                _housingStatisticsScrollView.Clear();
            }

            string authenticationToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Building.GetHousingProjection(cityIdentifier, authenticationToken, (projectionDataList) =>
            {
                if (projectionDataList != null && projectionDataList.Count > 0)
                {
                    UpdateHousingHeaderInformation(projectionDataList);
                    PopulateHousingStatisticsTable(projectionDataList);
                }
            }));
        }

        private void UpdateHousingHeaderInformation(List<HousingInfoDTO> projectionDataList)
        {
            HousingInfoDTO currentLevelEntry = projectionDataList.Find(projection => projection.IsCurrentLevel);

            if (_currentHousingLevelDisplayLabel != null)
            {
                _currentHousingLevelDisplayLabel.text = currentLevelEntry != null
                    ? $"Level {currentLevelEntry.Level}"
                    : "Not Constructed";
            }
        }

        private void PopulateHousingStatisticsTable(List<HousingInfoDTO> projectionDataList)
        {
            if (_housingStatisticsScrollView == null) return;

            _housingStatisticsScrollView.Clear();

            foreach (HousingInfoDTO housingProjection in projectionDataList)
            {
                CreateAndAddHousingStatisticRow(housingProjection);
            }
        }

        private void CreateAndAddHousingStatisticRow(HousingInfoDTO housingProjectionData)
        {
            VisualElement tableRowContainer = new VisualElement();
            tableRowContainer.AddToClassList("table-row");

            if (housingProjectionData.IsCurrentLevel)
            {
                tableRowContainer.AddToClassList("table-row-current");
            }

            // Level Label
            Label levelValueLabel = new Label(housingProjectionData.Level.ToString());
            levelValueLabel.AddToClassList("row-label");
            tableRowContainer.Add(levelValueLabel);

            // Production Cell
            Label prodLabel = new Label($"{housingProjectionData.Population:N0} total");
            prodLabel.AddToClassList("row-label");

            prodLabel.style.color = new StyleColor(new Color(0.2f, 0.6f, 0.2f));
            if (housingProjectionData.IsCurrentLevel) prodLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            tableRowContainer.Add(prodLabel);

            _housingStatisticsScrollView.Add(tableRowContainer);
        }
    }
}