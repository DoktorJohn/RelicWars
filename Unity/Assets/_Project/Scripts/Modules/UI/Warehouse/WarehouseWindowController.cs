using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq; // Vigtig for at finde Warehouse i listen
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Warehouse
{
    public class WarehouseWindowController : BaseWindow
    {
        protected override string WindowName => "Warehouse";
        protected override string VisualContainerName => "Warehouse-Window-MainContainer";
        protected override string HeaderName => "Warehouse-Window-Header";

        private Label _titleLabel;
        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _titleLabel = Root.Q<Label>("Lbl-Title");
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

            // Kald det nye endpoint via NetworkManager
            // Bemærk: Du skal tilføje 'GetWarehouseProjection' til din BuildingService eller CityService i Unity netværkslaget
            StartCoroutine(NetworkManager.Instance.Building.GetWarehouseProjection(cityId, token, (dataList) =>
            {
                if (dataList != null && dataList.Count > 0)
                {
                    // Find nuværende level for at opdatere headeren
                    var current = dataList.Find(x => x.IsCurrentLevel);
                    if (current != null) _levelLabel.text = $"Level {current.Level}";

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
            row.Add(lvlLabel);

            // Capacity Cell
            Label capLabel = new Label($"{item.Capacity:N0}");
            capLabel.AddToClassList("row-label");
            if (item.IsCurrentLevel) capLabel.style.color = new StyleColor(new Color(0.6f, 1f, 0.6f));
            row.Add(capLabel);

            // Protection Cell (FJERNET - da det ikke findes)
            // Hvis du vil have en tom celle eller noget andet, kan du tilføje det her.
            // Ellers skal du huske at fjerne "Protection" headeren i din UXML også!

            _statsContainer.Add(row);
        }
    }
}