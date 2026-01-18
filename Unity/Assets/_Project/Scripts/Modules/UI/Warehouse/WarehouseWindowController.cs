using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Modules.UI.Windows; // BaseWindow namespace

namespace Project.Modules.UI.Windows.Implementations
{
    public class WarehouseWindowController : BaseWindow
    {
        protected override string WindowName => "Warehouse";
        protected override string VisualContainerName => "Warehouse-Window-MainContainer";
        protected override string HeaderName => "Warehouse-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Warehouse-Stats-List");

            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            RefreshData(cityId);
        }

        private void RefreshData(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Building.GetWarehouseProjection(cityId, token, (dataList) =>
            {
                if (dataList != null && dataList.Count > 0)
                {
                    // Find current level
                    var current = dataList.Find(x => x.IsCurrentLevel);
                    if (current != null && _levelLabel != null)
                        _levelLabel.text = $"Level {current.Level}";

                    PopulateTable(dataList);
                }
            }));
        }

        private void PopulateTable(List<WarehouseProjectionDTO> dataList)
        {
            if (_statsContainer == null) return;
            _statsContainer.Clear();

            foreach (var item in dataList)
            {
                CreateTableRow(item);
            }
        }

        private void CreateTableRow(WarehouseProjectionDTO item)
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

            // Capacity Cell
            Label capLabel = new Label($"{item.Capacity:N0}");
            capLabel.AddToClassList("row-label");
            if (item.IsCurrentLevel) capLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(capLabel);

            _statsContainer.Add(row);
        }
    }
}