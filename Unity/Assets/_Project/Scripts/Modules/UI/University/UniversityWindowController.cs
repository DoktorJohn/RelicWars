using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Modules.UI.Windows;

namespace Project.Modules.UI.Windows.Implementations
{
    public class UniversityWindowController : BaseWindow
    {
        protected override string WindowName => "University";
        protected override string VisualContainerName => "University-Window-MainContainer";
        protected override string HeaderName => "University-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            // FIX: Correct ID for the close button in the header
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("University-Stats-List");

            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            RefreshData(cityId);
        }

        private void RefreshData(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Building.GetUniversityInfo(cityId, token, (dataList) =>
            {
                if (dataList != null && dataList.Count > 0)
                {
                    UpdateUI(dataList);
                }
            }));
        }

        private void UpdateUI(List<UniversityInfoDTO> dataList)
        {
            var current = dataList.Find(x => x.IsCurrentLevel);

            if (current != null && _levelLabel != null)
                _levelLabel.text = $"Level {current.Level}";
            else if (_levelLabel != null)
                _levelLabel.text = "Not Constructed";

            if (_statsContainer == null) return;
            _statsContainer.Clear();

            foreach (var item in dataList)
            {
                CreateTableRow(item);
            }
        }

        private void CreateTableRow(UniversityInfoDTO item)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("table-row");

            if (item.IsCurrentLevel)
            {
                row.AddToClassList("table-row-current");
            }

            // 1. Level Cell
            Label lvlLabel = new Label(item.Level.ToString());
            lvlLabel.AddToClassList("row-label");
            if (item.IsCurrentLevel) lvlLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(lvlLabel);

            // 2. Production Cell (Research Points)
            Label prodLabel = new Label($"+{item.ProductionPerHour:N0}");
            prodLabel.AddToClassList("row-label");

            // COLOR: Science Cyan (#64D2FF)
            prodLabel.style.color = new StyleColor(new Color(0.2f, 0.6f, 0.2f));

            if (item.IsCurrentLevel) prodLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(prodLabel);

            _statsContainer.Add(row);
        }
    }
}