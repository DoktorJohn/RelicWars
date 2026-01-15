using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class TimberCampWindowController : BaseWindow
    {
        // 1. Konfiguration af BaseWindow
        protected override string WindowName => "TimberCamp";
        protected override string VisualContainerName => "Timber-Window-MainContainer";
        protected override string HeaderName => "Timber-Window-Header";

        // 2. UI Referencer
        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            // Setup Close Button
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            // Find UI elementer
            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Timber-Stats-List");

            // Hent City ID (fra payload eller aktiv by)
            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;

            if (cityId == Guid.Empty)
            {
                Debug.LogError("[TimberCampWindow] Ingen ActiveCityId fundet.");
                return;
            }

            RefreshContent(cityId);
        }

        private void RefreshContent(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();

            string token = NetworkManager.Instance.JwtToken;
            var buildingType = BuildingTypeEnum.TimberCamp;

            // 3. Kald Netværket
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
            // Opdater Header (Nuværende Level)
            var current = dataList.Find(x => x.IsCurrentLevel);
            if (current != null && _levelLabel != null)
            {
                _levelLabel.text = $"Level {current.Level}";
            }

            // Byg Tabellen
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

            // Highlight nuværende level
            if (item.IsCurrentLevel)
            {
                row.AddToClassList("table-row-current");
            }

            // Level Kolonne
            Label lvlLabel = new Label(item.Level.ToString());
            lvlLabel.AddToClassList("row-label");
            row.Add(lvlLabel);

            // Production Kolonne
            // Vi viser "+X" og farver det grønt for at indikere indtægt
            Label prodLabel = new Label($"+{item.ProductionPrHour:N0}");
            prodLabel.AddToClassList("row-label");
            prodLabel.style.color = new StyleColor(new Color(0.6f, 1f, 0.6f)); // Lys grøn
            row.Add(prodLabel);

            _statsContainer.Add(row);
        }
    }
}