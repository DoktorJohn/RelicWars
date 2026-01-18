using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class RankingWindowController : BaseWindow
    {
        protected override string WindowName => "RankingWindow";
        protected override string VisualContainerName => "Ranking-Window-MainContainer";
        protected override string HeaderName => "Ranking-Window-Header";

        [Header("Template Configuration")]
        [SerializeField] private VisualTreeAsset _rankingRowTemplate;

        private ScrollView _listContainer;

        public override void OnOpen(object dataPayload)
        {
            // 1. Close Button
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null)
            {
                closeBtn.clicked -= Close;
                closeBtn.clicked += Close;
            }

            // 2. Container
            _listContainer = Root.Q<ScrollView>("Ranking-List-Container");

            RefreshRankingData();
        }

        private void RefreshRankingData()
        {
            if (_listContainer != null) _listContainer.Clear();
            string jwtToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Ranking.GetGlobalRankings(jwtToken, (rankingsList) =>
            {
                if (rankingsList != null)
                {
                    PopulateList(rankingsList);
                }
                else
                {
                    Debug.LogError("[RankingWindow] No data received from ranking service.");
                }
            }));
        }

        private void PopulateList(List<RankingEntryDataDTO> data)
        {
            if (_listContainer == null) return;
            _listContainer.Clear();

            if (_rankingRowTemplate == null)
            {
                Debug.LogError("[RankingWindow] RankingRowTemplate is not assigned in the Inspector!");
                return;
            }

            foreach (var entry in data)
            {
                // Instantiate nu uden inline var() styles - dette stopper crashet.
                VisualElement row = _rankingRowTemplate.Instantiate();

                // Find labels inde i den nye row
                var rankLbl = row.Q<Label>("Row-Rank");
                var nameLbl = row.Q<Label>("Row-PlayerName");
                var pointsLbl = row.Q<Label>("Row-Points");

                // Map data
                if (rankLbl != null) rankLbl.text = entry.Rank.ToString();
                if (nameLbl != null) nameLbl.text = entry.PlayerName;
                if (pointsLbl != null) pointsLbl.text = entry.TotalPoints.ToString("N0");

                _listContainer.Add(row);
            }
        }
    }
}