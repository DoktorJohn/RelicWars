using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class BarracksWindowController : BaseWindow
    {
        protected override string WindowName => "Barracks";
        protected override string VisualContainerName => "Barracks-Window-MainContainer";
        protected override string HeaderName => "Barracks-Window-Header";

        private Label _levelLabel;
        private ScrollView _statsContainer;
        private Guid _currentCityId;

        public override void OnOpen(object dataPayload)
        {
            var closeBtn = Root.Q<Button>("Common-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            _levelLabel = Root.Q<Label>("Lbl-Level");
            _statsContainer = Root.Q<ScrollView>("Barracks-Stats-List");

            _currentCityId = (dataPayload is Guid id) ? id : NetworkManager.Instance.ActiveCityId ?? Guid.Empty;
            if (_currentCityId == Guid.Empty) return;

            RefreshData();
        }

        // Denne metode hedder nu RefreshData men bruger den NYE service-metode
        private void RefreshData()
        {
            if (_statsContainer != null) _statsContainer.Clear();
            string token = NetworkManager.Instance.JwtToken;

            // HER ER ÆNDRINGEN: Vi kalder GetBarracksOverviewInformation i stedet for GetBarracksInfo
            StartCoroutine(NetworkManager.Instance.Barracks.GetBarracksOverviewInformation(_currentCityId, token, (barracksData) =>
            {
                if (barracksData != null)
                {
                    UpdateUI(barracksData);
                }
            }));
        }

        private void UpdateUI(BarracksFullViewDTO data)
        {
            // Opdater Level
            if (_levelLabel != null)
                _levelLabel.text = data.BuildingLevel > 0 ? $"Level {data.BuildingLevel}" : "Not Constructed";

            if (_statsContainer == null) return;
            _statsContainer.Clear();

            // 1. Vis Køen (Hvis der er enheder i træning)
            if (data.RecruitmentQueue != null && data.RecruitmentQueue.Count > 0)
            {
                Label queueHeader = new Label("TRAINING QUEUE");
                queueHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                queueHeader.style.color = Color.yellow;
                _statsContainer.Add(queueHeader);

                foreach (var queueItem in data.RecruitmentQueue)
                {
                    CreateQueueRow(queueItem);
                }

                // Afstand
                VisualElement spacer = new VisualElement();
                spacer.style.height = 20;
                _statsContainer.Add(spacer);
            }

            // 2. Vis Rekrutteringsmuligheder
            Label recruitHeader = new Label("RECRUIT UNITS");
            recruitHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statsContainer.Add(recruitHeader);

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
            row.AddToClassList("table-row"); // Genbruger din CSS klasse

            Label infoLabel = new Label($"{item.Amount}x {item.UnitType}");
            infoLabel.style.color = Color.white;
            row.Add(infoLabel);

            TimeSpan t = TimeSpan.FromSeconds(item.TimeRemainingSeconds);
            Label timeLabel = new Label($"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}");
            timeLabel.style.color = Color.yellow;
            row.Add(timeLabel);

            _statsContainer.Add(row);
        }

        private void CreateRecruitRow(BarracksUnitInfoDTO unit)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("table-row");
            row.style.height = 80; // Gør plads til knapper
            row.style.flexDirection = FlexDirection.Column; // Vi stabler info og knapper

            // Info Linje
            Label nameLabel = new Label($"{unit.UnitName} (Owned: {unit.CurrentInventoryCount})");
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(nameLabel);

            Label costLabel = new Label($"Wood: {unit.CostWood} | Stone: {unit.CostStone} | Metal: {unit.CostMetal} | Time: {unit.RecruitmentTimeInSeconds}s");
            costLabel.style.fontSize = 12;
            row.Add(costLabel);

            if (unit.IsUnlocked)
            {
                // Action Linje
                VisualElement actionContainer = new VisualElement();
                actionContainer.style.flexDirection = FlexDirection.Row;
                actionContainer.style.marginTop = 5;

                // Input felt (Simuleret med knapper for simpelthed i UI Toolkit uden styles)
                Button trainOneBtn = new Button(() => PerformRecruitment(unit.UnitType, 1));
                trainOneBtn.text = "Train 1";
                trainOneBtn.style.flexGrow = 1;

                Button trainFiveBtn = new Button(() => PerformRecruitment(unit.UnitType, 5));
                trainFiveBtn.text = "Train 5";
                trainFiveBtn.style.flexGrow = 1;

                actionContainer.Add(trainOneBtn);
                actionContainer.Add(trainFiveBtn);
                row.Add(actionContainer);
            }
            else
            {
                Label lockLabel = new Label("LOCKED");
                lockLabel.style.color = Color.red;
                row.Add(lockLabel);
            }

            _statsContainer.Add(row);
        }

        // HER ER DEN METODE JEG SNAKKEDE OM (Den manglede i din gamle kode)
        private void PerformRecruitment(UnitTypeEnum type, int amount)
        {
            string token = NetworkManager.Instance.JwtToken;

            // Her bruger vi Action<bool, string> signaturen
            StartCoroutine(NetworkManager.Instance.Barracks.RecruitUnits(_currentCityId, type, amount, token, (success, message) =>
            {
                if (success)
                {
                    Debug.Log($"<color=green>SUCCESS:</color> {message}");
                    // Genindlæs data for at opdatere køen
                    RefreshData();

                    // Opdater ressourcer globalt (hvis du har CityResourceService)
                    if (Project.Modules.City.CityResourceService.Instance != null)
                        Project.Modules.City.CityResourceService.Instance.InitiateResourceRefresh(_currentCityId);
                }
                else
                {
                    Debug.LogError($"<color=red>FAILED:</color> {message}");
                    // Her kan du evt. vise en popup i fremtiden
                }
            }));
        }
    }
}