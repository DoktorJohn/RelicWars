using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiRankingWindowController : MonoBehaviour
    {
        private enum RankingView { Players, Alliances }

        private FramedSpriteTabButton _playersTab;
        private FramedSpriteTabButton _alliancesTab;
        private RectTransform _rowsRoot;
        private GameObject _playerHeader;
        private GameObject _allianceHeader;
        private GameObject _playerTemplate;
        private GameObject _allianceTemplate;
        private readonly List<GameObject> _runtimeRows = new();
        private static readonly string[] UnsupportedAuthoredRows =
        {
            "PlayerMilitaryPowerHeader", "PlayerMilitaryPowerDataRow",
            "PlayerFightersHeader", "PlayerFightersDataRow",
            "PlayerAttackersHeader", "PlayerAttackersDataRow",
            "PlayerDefendersHeader", "PlayerDefendersDataRow",
            "AllianceAttackersHeader", "AllianceAttackersDataRow",
            "AllianceDefendersHeader", "AllianceDefendersDataRow"
        };
        private List<RankingEntryDataDTO> _rankings = new();
        private RankingView _currentView = RankingView.Players;
        private int _requestVersion;

        private void Awake()
        {
            _playersTab = FindComponent<FramedSpriteTabButton>(transform, "PlayerPointsTab");
            _alliancesTab = FindComponent<FramedSpriteTabButton>(transform, "AlliancePointsTab");
            _rowsRoot = Find(transform, "Vertical Layout Box") as RectTransform;
            _playerHeader = Find(transform, "PlayerRankingHeader")?.gameObject;
            _allianceHeader = Find(transform, "AllianceRankingHeader")?.gameObject;
            _playerTemplate = Find(transform, "PlayerRankingDataRow")?.gameObject;
            _allianceTemplate = Find(transform, "AllianceRankingDataRow")?.gameObject;

            SetActive(_playerTemplate, false);
            SetActive(_allianceTemplate, false);
            foreach (string authoredRowName in UnsupportedAuthoredRows)
                SetActive(Find(transform, authoredRowName)?.gameObject, false);
        }

        private void OnEnable()
        {
            if (_playersTab != null) _playersTab.OnButtonActivatedClicked += ShowPlayers;
            if (_alliancesTab != null) _alliancesTab.OnButtonActivatedClicked += ShowAlliances;
            LoadRankings();
            SelectView(RankingView.Players);
        }

        private void OnDisable()
        {
            if (_playersTab != null) _playersTab.OnButtonActivatedClicked -= ShowPlayers;
            if (_alliancesTab != null) _alliancesTab.OnButtonActivatedClicked -= ShowAlliances;
            _requestVersion++;
            StopAllCoroutines();
            ClearRows();
        }

        private void ShowPlayers(FramedSpriteTabButton _) => SelectView(RankingView.Players);
        private void ShowAlliances(FramedSpriteTabButton _) => SelectView(RankingView.Alliances);

        private void SelectView(RankingView view)
        {
            _currentView = view;
            SetActive(_playerHeader, view == RankingView.Players);
            SetActive(_allianceHeader, view == RankingView.Alliances);
            if (_playersTab != null) _playersTab.SetSelected(view == RankingView.Players, false);
            if (_alliancesTab != null) _alliancesTab.SetSelected(view == RankingView.Alliances, false);
            RenderCurrentView();
        }

        private void LoadRankings()
        {
            int version = ++_requestVersion;
            NetworkManager network = NetworkManager.Instance;
            if (network == null) return;

            StartCoroutine(network.Ranking.GetGlobalRankings(network.JwtToken, rankings =>
            {
                if (!this || !isActiveAndEnabled || version != _requestVersion) return;
                _rankings = rankings ?? new List<RankingEntryDataDTO>();
                RenderCurrentView();
            }));
        }

        private void RenderCurrentView()
        {
            ClearRows();
            if (_rowsRoot == null) return;
            if (_currentView == RankingView.Players) RenderPlayers();
            else RenderAlliances();
        }

        private void RenderPlayers()
        {
            if (_playerTemplate == null) return;
            foreach (RankingEntryDataDTO entry in _rankings.OrderBy(item => item.Rank))
            {
                GameObject row = CloneTemplate(_playerTemplate);
                SetText(row.transform, "RankText", entry.Rank.ToString("N0"));
                SetText(row.transform, "NameText", entry.PlayerName);
                SetText(row.transform, "AllianceText", string.IsNullOrWhiteSpace(entry.AllianceName) ? string.Empty : entry.AllianceName);
                SetText(row.transform, "IdeologyText", string.IsNullOrWhiteSpace(entry.Ideology) ? "None" : entry.Ideology);
                SetText(row.transform, "PointsText", entry.TotalPoints.ToString("N0"));
                SetText(row.transform, "CitiesText", entry.CityCount.ToString("N0"));
            }
        }

        private void RenderAlliances()
        {
            if (_allianceTemplate == null) return;

            var alliances = _rankings
                .Where(item => Guid.TryParse(item.AllianceId, out Guid id) && id != Guid.Empty)
                .GroupBy(item => item.AllianceId)
                .Select(group => new
                {
                    Name = group.Select(item => item.AllianceName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty,
                    Points = group.Sum(item => (long)item.TotalPoints),
                    Members = group.Count(),
                    Cities = group.Sum(item => item.CityCount)
                })
                .OrderByDescending(item => item.Points)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int index = 0; index < alliances.Count; index++)
            {
                var entry = alliances[index];
                GameObject row = CloneTemplate(_allianceTemplate);
                SetText(row.transform, "RankText", (index + 1).ToString("N0"));
                SetText(row.transform, "NameText", entry.Name);
                SetText(row.transform, "PointsText", entry.Points.ToString("N0"));
                SetText(row.transform, "MembersText", entry.Members.ToString("N0"));
                SetText(row.transform, "CitiesText", entry.Cities.ToString("N0"));
            }
        }

        private GameObject CloneTemplate(GameObject template)
        {
            GameObject row = Instantiate(template, _rowsRoot, false);
            row.name = template.name;
            row.SetActive(true);
            _runtimeRows.Add(row);
            return row;
        }

        private void ClearRows()
        {
            foreach (GameObject row in _runtimeRows)
                if (row != null) Destroy(row);
            _runtimeRows.Clear();
        }

        private static void SetText(Transform row, string containerName, string value)
        {
            Transform container = Find(row, containerName);
            if (container == null) return;
            TMP_Text text = container.GetComponent<TMP_Text>();
            if (text == null) text = container.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = value ?? string.Empty;
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            Transform item = Find(root, name);
            return item != null ? item.GetComponent<T>() : null;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
            {
                if (child.name.Equals(name, StringComparison.Ordinal)) return child;
                Transform nested = Find(child, name);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }
    }
}
