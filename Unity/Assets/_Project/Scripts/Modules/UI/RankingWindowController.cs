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
    public class RankingWindowController : BaseWindow
    {
        // Kontrakten med BaseWindow
        protected override string WindowName => "RankingWindow";
        protected override string VisualContainerName => "RankingWindow-Frame";
        protected override string HeaderName => "RankingWindow-TitleBar";

        [Header("Template Configuration")]
        [SerializeField] private VisualTreeAsset _rankingRowTemplate;

        private ListView _rankingsListView;
        private List<RankingEntryDataDTO> _rankingsDataSource = new List<RankingEntryDataDTO>();

        public override void OnOpen(object dataPayload)
        {
            // BEMÆRK: Vi skal IKKE binde Close-knappen her manuelt. 
            // BaseWindow.Initialize gør det for os, da knappenavnet i UXML nu matcher standarden.

            _rankingsListView = Root.Q<ListView>("Ranking-ListView");

            InitializeListView();
            RefreshRankingData();
        }

        private void InitializeListView()
        {
            if (_rankingsListView == null)
            {
                Debug.LogError("[RankingWindow] ListView 'Ranking-ListView' ikke fundet.");
                return;
            }

            _rankingsListView.makeItem = () => _rankingRowTemplate.Instantiate();

            _rankingsListView.bindItem = (VisualElement element, int index) =>
            {
                if (index < 0 || index >= _rankingsDataSource.Count) return;

                var data = _rankingsDataSource[index];

                var rankLabel = element.Q<Label>("Row-Rank");
                var playerNameLabel = element.Q<Label>("Row-PlayerName");
                var pointsLabel = element.Q<Label>("Row-Points");

                if (rankLabel != null) rankLabel.text = data.Rank.ToString();
                if (playerNameLabel != null) playerNameLabel.text = data.PlayerName;
                if (pointsLabel != null) pointsLabel.text = data.TotalPoints.ToString("N0");
            };

            _rankingsListView.itemsSource = _rankingsDataSource;
        }

        private void RefreshRankingData()
        {
            string jwtToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Ranking.GetGlobalRankings(jwtToken, (rankingsList) =>
            {
                if (rankingsList != null)
                {
                    UpdateRankingUI(rankingsList);
                }
                else
                {
                    Debug.LogError("[RankingWindow] Modtog ingen data fra serveren.");
                }
            }));
        }

        private void UpdateRankingUI(List<RankingEntryDataDTO> data)
        {
            if (_rankingsListView == null) return;

            _rankingsDataSource.Clear();
            _rankingsDataSource.AddRange(data);

            _rankingsListView.RefreshItems();
        }
    }
}