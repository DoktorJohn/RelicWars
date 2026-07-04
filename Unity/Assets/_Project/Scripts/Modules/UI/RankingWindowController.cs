using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            _rankingEntriesScrollView = Root.Q<ScrollView>("Ranking-List-Container");

            if (_rankingEntriesScrollView == null)
            {
                Debug.LogError("[RankingWindow] Missing ranking list container.");
                CompleteDeferredOpen(version);
                return;
            }

            RequestGlobalRankingDataFromServer(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
        }

        private void RequestGlobalRankingDataFromServer(int version)
        {
            if (_rankingEntriesScrollView != null)
            {
                _rankingEntriesScrollView.Clear();
            }

            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[RankingWindow] NetworkManager instance is not available.");
                ShowRankingState("Ranking service unavailable.");
                CompleteDeferredOpen(version);
                return;
            }

            var authenticationToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Ranking.GetGlobalRankings(authenticationToken, rankingsList =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (rankingsList != null)
                {
                    PopulateGlobalRankingStatisticsTable(rankingsList);
                }
                else
                {
                    Debug.LogError("[RankingWindow] Failed to retrieve ranking data from the network service.");
                    ShowRankingState("Failed to load rankings.");
                }

                CompleteDeferredOpen(version);
            }));
        }

        private void PopulateGlobalRankingStatisticsTable(List<RankingEntryDataDTO> rankingData)
        {
            if (_rankingEntriesScrollView == null)
            {
                return;
            }

            _rankingEntriesScrollView.Clear();

            if (_rankingRowTemplate == null)
            {
                Debug.LogError("[RankingWindow] Ranking row template is not assigned.");
                ShowRankingState("Ranking UI is missing.");
                return;
            }

            if (rankingData == null || rankingData.Count == 0)
            {
                ShowRankingState("No rankings available.");
                return;
            }

            foreach (var entry in rankingData)
            {
                var rowInstance = _rankingRowTemplate.Instantiate();

                var rankLabel = rowInstance.Q<Label>("Row-Rank");
                var playerName = rowInstance.Q<Label>("Row-PlayerName");
                var ideologyLabel = rowInstance.Q<Label>("Row-Ideology");
                var allianceName = rowInstance.Q<Label>("Row-Alliance");
                var scoreLabel = rowInstance.Q<Label>("Row-Points");

                if (rankLabel != null)
                {
                    rankLabel.text = entry.Rank.ToString();
                }

                ConfigurePlayerName(playerName, entry);
                ConfigureAllianceName(allianceName, entry);

                if (ideologyLabel != null)
                {
                    ideologyLabel.text = string.IsNullOrWhiteSpace(entry.Ideology) ? "None" : entry.Ideology;
                }

                if (scoreLabel != null)
                {
                    scoreLabel.text = entry.TotalPoints.ToString("N0");
                }

                _rankingEntriesScrollView.Add(rowInstance);
            }
        }

        private static void ConfigurePlayerName(Label label, RankingEntryDataDTO entry)
        {
            if (label == null || entry == null)
            {
                return;
            }

            label.text = string.IsNullOrWhiteSpace(entry.PlayerName) ? "Unknown" : entry.PlayerName;

            if (TryParseGuid(entry.WorldPlayerId, out var worldPlayerId))
            {
                label.RegisterCallback<ClickEvent>(_ => WindowNavigationHelper.OpenProfile(worldPlayerId));
            }
            else
            {
                label.SetEnabled(false);
            }
        }

        private static void ConfigureAllianceName(Label label, RankingEntryDataDTO entry)
        {
            if (label == null || entry == null)
            {
                return;
            }

            var allianceName = string.IsNullOrWhiteSpace(entry.AllianceName) ? "-" : entry.AllianceName;
            label.text = allianceName;

            if (TryParseGuid(entry.AllianceId, out var allianceId))
            {
                label.RegisterCallback<ClickEvent>(_ => WindowNavigationHelper.OpenAlliance(allianceId));
            }
            else
            {
                label.SetEnabled(false);
            }
        }

        private static bool TryParseGuid(string value, out Guid parsedGuid)
        {
            return Guid.TryParse(value, out parsedGuid) && parsedGuid != Guid.Empty;
        }

        private void ShowRankingState(string message)
        {
            if (_rankingEntriesScrollView == null)
            {
                return;
            }

            _rankingEntriesScrollView.Clear();

            var label = new Label(message ?? string.Empty);
            label.AddToClassList("ranking-window-state-label");
            _rankingEntriesScrollView.Add(label);
        }
    }
}
