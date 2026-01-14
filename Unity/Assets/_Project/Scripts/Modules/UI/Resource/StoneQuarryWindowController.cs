using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Assets._Project.Scripts.Domain.DTOs;
using Project.Network.Manager;

namespace Project.Modules.UI.Windows.Implementations
{
    public class StoneQuarryWindowController : BaseWindow
    {
        protected override string WindowName => "StoneQuarry";
        protected override string VisualContainerName => "Stone-Window-MainContainer";
        protected override string HeaderName => "Stone-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            // 1. Setup Close Button
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            // 2. References
            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Stone-Stats-List");

            // 3. Get City ID
            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            // 4. Load Data
            RefreshData(cityId);
        }

        private void RefreshData(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            // VIGTIGT: Vi beder om Stone Quarry data her
            var buildingType = BuildingTypeEnum.StoneQuarry;

            StartCoroutine(NetworkManager.Instance.Building.GetResourceProductionInfo(cityId, buildingType, token, (dataList) =>
            {
                if (dataList != null && dataList.Count > 0)
                {
                    // Opdater header level
                    var current = dataList.Find(x => x.IsCurrentLevel);
                    if (current != null && _levelLabel != null)
                        _levelLabel.text = $"Level {current.Level}";
                    else if (_levelLabel != null)
                        _levelLabel.text = "Not Constructed";

                    PopulateTable(dataList);
                }
            }));
        }

        private void PopulateTable(List<ResourceBuildingInfoDTO> dataList)
        {
            if (_statsContainer == null) return;
            _statsContainer.Clear();

            foreach (var item in dataList)
            {
                CreateTableRow(item);
            }
        }

        private void CreateTableRow(ResourceBuildingInfoDTO item)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("table-row");

            if (item.IsCurrentLevel)
            {
                row.AddToClassList("table-row-current");
            }

            // Level Cell
            Label lvlLabel = new Label(item.Level.ToString());
            lvlLabel.AddToClassList("row-label");
            row.Add(lvlLabel);

            // Production Cell
            Label prodLabel = new Label($"+{item.ProductionPrHour:N0}");
            prodLabel.AddToClassList("row-label");

            // Grå farve for sten (#969696)
            prodLabel.style.color = new StyleColor(new Color(0.58f, 0.58f, 0.58f));

            row.Add(prodLabel);

            _statsContainer.Add(row);
        }
    }
}