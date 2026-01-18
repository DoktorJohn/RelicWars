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
    public class WallWindowController : BaseWindow
    {
        protected override string WindowName => "Wall";
        protected override string VisualContainerName => "Wall-Window-MainContainer";
        protected override string HeaderName => "Wall-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Wall-Stats-List");

            Guid cityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (cityId == Guid.Empty) return;

            RefreshData(cityId);
        }

        private void RefreshData(Guid cityId)
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Building.GetWallInfo(cityId, token, (dataList) =>
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

        private void PopulateTable(List<WallInfoDTO> dataList)
        {
            if (_statsContainer == null) return;
            _statsContainer.Clear();

            foreach (var item in dataList)
            {
                CreateTableRow(item);
            }
        }

        private void CreateTableRow(WallInfoDTO item)
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

            // Defense Bonus Cell
            string bonusText = "0%";

            if (item.DefensiveModifier != null)
            {
                // Antag at Increased = % (f.eks. 0.05 -> 5%)
                if (item.DefensiveModifier.ModifierType == ModifierTypeEnum.Increased)
                {
                    bonusText = $"+{item.DefensiveModifier.Value * 100:0.#}%";
                }
                else
                {
                    bonusText = $"+{item.DefensiveModifier.Value:0.#}";
                }
            }

            Label bonusLabel = new Label(bonusText);
            bonusLabel.AddToClassList("row-label");
            if (item.IsCurrentLevel) bonusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(bonusLabel);

            _statsContainer.Add(row);
        }
    }
}