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
    public class WorkshopWindowController : BaseWindow
    {
        protected override string WindowName => "Workshop";
        protected override string VisualContainerName => "Workshop-Window-MainContainer";
        protected override string HeaderName => "Workshop-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Workshop-Stats-List");

            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            RefreshData(cityId);
        }

        private void RefreshData(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Building.GetWorkshopInfo(cityId, token, (dataList) =>
            {
                if (dataList != null && dataList.Count > 0)
                {
                    var current = dataList.Find(x => x.IsCurrentLevel);
                    if (current != null && _levelLabel != null)
                        _levelLabel.text = $"Level {current.Level}";
                    else if (_levelLabel != null)
                        _levelLabel.text = "Not Constructed";

                    PopulateTable(dataList);
                }
            }));
        }

        private void PopulateTable(List<WorkshopInfoDTO> dataList)
        {
            if (_statsContainer == null) return;
            _statsContainer.Clear();

            foreach (var item in dataList)
            {
                CreateTableRow(item);
            }
        }

        private void CreateTableRow(WorkshopInfoDTO item)
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

            // Status Cell
            // Da vi ikke har data, skriver vi bare "Active" hvis det er nuværende, ellers "-"
            string statusText = item.IsCurrentLevel ? "Operational" : "Upgrade Available";
            if (item.Level < 1) statusText = "Not Built";

            Label statusLabel = new Label(statusText);
            statusLabel.AddToClassList("row-label");

            if (item.IsCurrentLevel)
            {
                statusLabel.style.color = new StyleColor(new Color(0.6f, 1f, 0.6f));
            }

            row.Add(statusLabel);

            _statsContainer.Add(row);
        }
    }
}