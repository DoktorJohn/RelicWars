using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Modules.UI.Windows; // BaseWindow namespace

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
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Stone-Stats-List");

            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            RefreshData(cityId);
        }

        private void RefreshData(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;
            var buildingType = BuildingTypeEnum.StoneQuarry;

            StartCoroutine(NetworkManager.Instance.Building.GetResourceProductionInfo(cityId, buildingType, token, (dataList) =>
            {
                if (dataList != null && dataList.Count > 0)
                {
                    UpdateUI(dataList);
                }
            }));
        }

        private void UpdateUI(List<ResourceBuildingInfoDTO> dataList)
        {
            var current = dataList.Find(x => x.IsCurrentLevel);
            if (current != null && _levelLabel != null)
            {
                _levelLabel.text = $"Level {current.Level}";
            }
            else if (_levelLabel != null)
            {
                _levelLabel.text = "Not Constructed";
            }

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
            if (item.IsCurrentLevel) lvlLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(lvlLabel);

            // Production Cell
            Label prodLabel = new Label($"+{item.ProductionPrHour:N0}");
            prodLabel.AddToClassList("row-label");

            // Stone Grey Color (#969696 is roughly 0.58f)
            prodLabel.style.color = new StyleColor(new Color(0.2f, 0.6f, 0.2f));
            if (item.IsCurrentLevel) prodLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(prodLabel);

            _statsContainer.Add(row);
        }
    }
}