using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Modules.UI; // For BaseWindow
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;

namespace Assets._Project.Scripts.Modules.UI.Resource
{
    public class MetalMineWindowController : BaseWindow
    {
        protected override string WindowName => "MetalMine";
        protected override string VisualContainerName => "Metal-Window-MainContainer";
        protected override string HeaderName => "Metal-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            // 1. Setup Close Button
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            // 2. References
            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Metal-Stats-List");

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

            // Hent data specifikt for Metal Mine
            var buildingType = BuildingTypeEnum.MetalMine;

            StartCoroutine(NetworkManager.Instance.Building.GetResourceProductionInfo(cityId, buildingType, token, (dataList) =>
            {
                if (dataList != null && dataList.Count > 0)
                {
                    // Opdater Header
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

            // Lyseblå / Metal farve (#B4C8DC)
            prodLabel.style.color = new StyleColor(new Color(0.7f, 0.78f, 0.86f));

            row.Add(prodLabel);

            _statsContainer.Add(row);
        }
    }
}