using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Stable
{
    public class StableWindowController : BaseWindow
    {
        protected override string WindowName => "Stable";
        protected override string VisualContainerName => "Stable-Window-MainContainer";
        protected override string HeaderName => "Stable-Window-Header";

        private Label _levelLabel;
        private ScrollView _contentContainer;
        private Guid _currentCityId;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _contentContainer = Root.Q<ScrollView>("Stable-Stats-List");

            _currentCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentCityId == Guid.Empty) return;

            RefreshStableData();
        }

        private void RefreshStableData()
        {
            if (_contentContainer != null) _contentContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Stable.GetStableOverviewInformation(_currentCityId, token, (stableData) =>
            {
                if (stableData != null)
                {
                    UpdateStableUI(stableData);
                }
            }));
        }

        private void UpdateStableUI(StableFullViewDTO data)
        {
            if (_levelLabel != null)
                _levelLabel.text = data.BuildingLevel > 0 ? $"Level {data.BuildingLevel}" : "Not Constructed";

            if (_contentContainer == null) return;
            _contentContainer.Clear();

            // 1. Rekrutterings-kø
            if (data.RecruitmentQueue != null && data.RecruitmentQueue.Count > 0)
            {
                Label queueHeader = new Label("BREEDING QUEUE");
                queueHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                queueHeader.style.color = Color.yellow;
                _contentContainer.Add(queueHeader);

                foreach (var queueItem in data.RecruitmentQueue)
                {
                    CreateQueueRow(queueItem);
                }

                VisualElement spacer = new VisualElement { style = { height = 20 } };
                _contentContainer.Add(spacer);
            }

            // 2. Tilgængelige enheder
            Label recruitHeader = new Label("RECRUIT CAVALRY");
            recruitHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            _contentContainer.Add(recruitHeader);

            if (data.AvailableUnits != null)
            {
                foreach (var unit in data.AvailableUnits)
                {
                    CreateRecruitRow(unit);
                }
            }
        }

        private void CreateQueueRow(RecruitmentQueueItemDTO item)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.AddToClassList("table-row");

            Label infoLabel = new Label($"{item.Amount}x {item.UnitType}");
            infoLabel.style.color = Color.white;
            row.Add(infoLabel);

            TimeSpan t = TimeSpan.FromSeconds(item.TimeRemainingSeconds);
            Label timeLabel = new Label($"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}");
            timeLabel.style.color = Color.yellow;
            row.Add(timeLabel);

            _contentContainer.Add(row);
        }

        private void CreateRecruitRow(StableUnitInfoDTO unit)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("table-row");

            Label nameLabel = new Label($"{unit.UnitName} (Owned: {unit.CurrentInventoryCount})");
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(nameLabel);

            Label costLabel = new Label($"W: {unit.CostWood} | S: {unit.CostStone} | M: {unit.CostMetal} | {unit.RecruitmentTimeInSeconds}s");
            costLabel.style.fontSize = 12;
            costLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
            row.Add(costLabel);

            if (unit.IsUnlocked)
            {
                VisualElement actionContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };

                Button breedOneBtn = new Button();
                breedOneBtn.text = "Train 1";
                breedOneBtn.style.flexGrow = 1;
                breedOneBtn.clicked += () => PerformCavalryRecruitment(unit.UnitType, 1, breedOneBtn);

                Button breedFiveBtn = new Button();
                breedFiveBtn.text = "Train 5";
                breedFiveBtn.style.flexGrow = 1;
                breedFiveBtn.clicked += () => PerformCavalryRecruitment(unit.UnitType, 5, breedFiveBtn);

                actionContainer.Add(breedOneBtn);
                actionContainer.Add(breedFiveBtn);
                row.Add(actionContainer);
            }
            else
            {
                Label lockLabel = new Label("LOCKED (Requires Stable Level)");
                lockLabel.style.color = Color.red;
                row.Add(lockLabel);
            }

            _contentContainer.Add(row);
        }

        private void PerformCavalryRecruitment(UnitTypeEnum type, int amount, Button clickedButton)
        {
            clickedButton.SetEnabled(false);
            string originalText = clickedButton.text;
            clickedButton.text = "Processing...";

            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Stable.RecruitUnits(_currentCityId, type, amount, token, (success, message) =>
            {
                if (clickedButton != null)
                {
                    clickedButton.SetEnabled(true);
                    clickedButton.text = originalText;
                }

                if (success)
                {
                    Debug.Log($"<color=orange>[Stable]</color> {message}");
                    RefreshStableData();
                    if (Project.Modules.City.CityResourceService.Instance != null)
                        Project.Modules.City.CityResourceService.Instance.InitiateResourceRefresh(_currentCityId);
                }
                else
                {
                    Debug.LogError($"<color=red>[Stable Error]</color> {message}");
                }
            }));
        }
    }
}