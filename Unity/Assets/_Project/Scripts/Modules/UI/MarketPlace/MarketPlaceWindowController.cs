using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Modules.UI.Windows;

namespace Project.Modules.UI.Windows.Implementations
{
    public class MarketPlaceWindowController : BaseWindow
    {
        protected override string WindowName => "MarketPlace";
        protected override string VisualContainerName => "Market-Window-MainContainer";
        protected override string HeaderName => "Market-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Market-Stats-List");

            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            RefreshData(cityId);
        }

        private void RefreshData(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            // FIX: Callback now accepts a single 'data' object
            StartCoroutine(NetworkManager.Instance.MarketPlace.GetMarketPlaceInfo(cityId, token, (data) =>
            {
                if (data != null)
                {
                    UpdateUI(data);
                }
            }));
        }

        private void UpdateUI(MarketPlaceInfoDTO data)
        {
            // Logic: If data exists and Level > 0, it is constructed
            if (_levelLabel != null)
            {
                _levelLabel.text = data.Level > 0 ? $"Level {data.Level}" : "Not Constructed";
            }

            if (_statsContainer == null) return;
            _statsContainer.Clear();

            // Create a single row for the current state
            CreateTableRow(data);
        }

        private void CreateTableRow(MarketPlaceInfoDTO item)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("table-row");

            // 1. Level Cell
            Label lvlLabel = new Label(item.Level.ToString());
            lvlLabel.AddToClassList("row-label");
            lvlLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(lvlLabel);

            // 2. Silver Production Cell
            string valueText = item.Modifier != null ? $"+{item.Modifier.Value:N0}" : "+0";
            Label prodLabel = new Label(valueText);
            prodLabel.AddToClassList("row-label");

            // COLOR: Bright Silver/White (#E0E0E0)
            prodLabel.style.color = new StyleColor(new Color(0.2f, 0.6f, 0.2f));
            prodLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(prodLabel);

            _statsContainer.Add(row);
        }
    }
}