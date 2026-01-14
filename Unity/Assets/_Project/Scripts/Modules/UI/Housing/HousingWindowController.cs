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
    public class HousingWindowController : BaseWindow
    {
        protected override string WindowName => "Housing";
        protected override string VisualContainerName => "Housing-Window-MainContainer";
        protected override string HeaderName => "Housing-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            // 1. Setup Close Button
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            // 2. References
            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Housing-Stats-List");

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

            // Kald den specifikke Housing metode
            StartCoroutine(NetworkManager.Instance.Building.GetHousingProjection(cityId, token, (dataList) =>
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

        private void PopulateTable(List<HousingProjectionDTO> dataList)
        {
            if (_statsContainer == null) return;
            _statsContainer.Clear();

            foreach (var item in dataList)
            {
                CreateTableRow(item);
            }
        }

        private void CreateTableRow(HousingProjectionDTO item)
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

            // Population Cell
            Label popLabel = new Label($"{item.Population:N0}");
            popLabel.AddToClassList("row-label");

            // Brug en hvid/beige farve for befolkning, da det ikke er en ressource man "høster"
            popLabel.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.85f));

            row.Add(popLabel);

            _statsContainer.Add(row);
        }
    }
}