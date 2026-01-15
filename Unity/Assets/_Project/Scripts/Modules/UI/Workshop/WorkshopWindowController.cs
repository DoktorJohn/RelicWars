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
    public class WorkshopWindowController : BaseWindow
    {
        protected override string WindowName => "Workshop";
        protected override string VisualContainerName => "Workshop-Window-MainContainer";
        protected override string HeaderName => "Workshop-Window-Header";

        private Label _levelLabel;
        private ScrollView _contentContainer;
        private Guid _currentCityId;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _contentContainer = Root.Q<ScrollView>("Workshop-Stats-List");

            _currentCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentCityId == Guid.Empty) return;

            RefreshWorkshopData();
        }

        private void RefreshWorkshopData()
        {
            if (_contentContainer != null) _contentContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Workshop.GetWorkshopOverviewInformation(_currentCityId, token, (workshopData) =>
            {
                if (workshopData != null)
                {
                    UpdateWorkshopUI(workshopData);
                }
            }));
        }

        private void UpdateWorkshopUI(WorkshopFullViewDTO data)
        {
            if (_levelLabel != null)
                _levelLabel.text = data.BuildingLevel > 0 ? $"Level {data.BuildingLevel}" : "Under Construction";

            if (_contentContainer == null) return;
            _contentContainer.Clear();

            if (data.RecruitmentQueue != null && data.RecruitmentQueue.Count > 0)
            {
                Label queueHeader = new Label("CONSTRUCTION QUEUE");
                queueHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                queueHeader.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                _contentContainer.Add(queueHeader);

                foreach (var queueItem in data.RecruitmentQueue)
                {
                    CreateQueueRow(queueItem);
                }

                _contentContainer.Add(new VisualElement { style = { height = 20 } });
            }

            Label recruitHeader = new Label("BUILD SIEGE ENGINES");
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

            row.Add(new Label($"{item.Amount}x {item.UnitType}") { style = { color = Color.white } });

            TimeSpan timeSpan = TimeSpan.FromSeconds(item.TimeRemainingSeconds);
            row.Add(new Label($"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}") { style = { color = Color.yellow } });

            _contentContainer.Add(row);
        }

        private void CreateRecruitRow(WorkshopUnitInfoDTO unit)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("table-row");

            row.Add(new Label($"{unit.UnitName} (In Storage: {unit.CurrentInventoryCount})") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            row.Add(new Label($"W: {unit.CostWood} | S: {unit.CostStone} | M: {unit.CostMetal} | {unit.RecruitmentTimeInSeconds}s")
            { style = { fontSize = 12, color = new Color(0.8f, 0.8f, 0.8f) } });

            if (unit.IsUnlocked)
            {
                VisualElement actionContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };

                Button buildOneBtn = new Button(() => PerformSiegeConstruction(unit.UnitType, 1, null)) { text = "Build 1", style = { flexGrow = 1 } };
                buildOneBtn.clicked += () => PerformSiegeConstruction(unit.UnitType, 1, buildOneBtn);

                Button buildFiveBtn = new Button() { text = "Build 5", style = { flexGrow = 1 } };
                buildFiveBtn.clicked += () => PerformSiegeConstruction(unit.UnitType, 5, buildFiveBtn);

                actionContainer.Add(buildOneBtn);
                actionContainer.Add(buildFiveBtn);
                row.Add(actionContainer);
            }
            else
            {
                row.Add(new Label("LOCKED (Higher Workshop Level Required)") { style = { color = Color.red } });
            }

            _contentContainer.Add(row);
        }

        private void PerformSiegeConstruction(UnitTypeEnum type, int amount, Button clickedButton)
        {
            if (clickedButton != null) clickedButton.SetEnabled(false);

            string token = NetworkManager.Instance.JwtToken;
            StartCoroutine(NetworkManager.Instance.Workshop.RecruitUnits(_currentCityId, type, amount, token, (success, message) =>
            {
                if (clickedButton != null) clickedButton.SetEnabled(true);

                if (success)
                {
                    RefreshWorkshopData();
                    if (Project.Modules.City.CityResourceService.Instance != null)
                        Project.Modules.City.CityResourceService.Instance.InitiateResourceRefresh(_currentCityId);
                }
                else
                {
                    Debug.LogError($"[Workshop Error] {message}");
                }
            }));
        }
    }
}