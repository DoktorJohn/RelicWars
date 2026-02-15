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

        [Header("Ranking Row Configuration")]
        [SerializeField] private VisualTreeAsset _rankingRowTemplate;

        private ScrollView _rankingEntriesScrollView;

        public override void OnOpen(object dataPayload)
        {
            // Komponent referencer
            _rankingEntriesScrollView = Root.Q<ScrollView>("Ranking-List-Container");

            RequestGlobalRankingDataFromServer();
        }

        private void RequestGlobalRankingDataFromServer()
        {
            if (_rankingEntriesScrollView != null) _rankingEntriesScrollView.Clear();

            string authenticationToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Ranking.GetGlobalRankings(authenticationToken, (rankingsList) =>
            {
                if (rankingsList != null)
                {
                    PopulateGlobalRankingStatisticsTable(rankingsList);
                }
                else
                {
                    Debug.LogError("[RankingWindow] Failed to retrieve ranking data from the network service.");
                }
            }));
        }

        private void PopulateGlobalRankingStatisticsTable(List<RankingEntryDataDTO> rankingData)
        {
            if (_rankingEntriesScrollView == null || _rankingRowTemplate == null) return;

            _rankingEntriesScrollView.Clear();

            foreach (var entry in rankingData)
            {
                // Vi instantiere templaten
                VisualElement rowInstance = _rankingRowTemplate.Instantiate();

                // Finder labels via de præcise navne fra UXML
                Label rankLabel = rowInstance.Q<Label>("Row-Rank");
                Label nameLabel = rowInstance.Q<Label>("Row-PlayerName");
                Label ideologyLabel = rowInstance.Q<Label>("Row-Ideology");
                Label allianceLabel = rowInstance.Q<Label>("Row-Alliance");
                Label scoreLabel = rowInstance.Q<Label>("Row-Points");

                // Mapper data
                if (rankLabel != null) rankLabel.text = entry.Rank.ToString();
                if (nameLabel != null) nameLabel.text = entry.PlayerName;
                if (ideologyLabel != null) ideologyLabel.text = entry.Ideology ?? "None";
                if (allianceLabel != null) allianceLabel.text = entry.AllianceName ?? "";
                if (scoreLabel != null) scoreLabel.text = entry.TotalPoints.ToString("N0");

                // Tilføjer rækken til listen
                _rankingEntriesScrollView.Add(rowInstance);
            }
        }
    }
}