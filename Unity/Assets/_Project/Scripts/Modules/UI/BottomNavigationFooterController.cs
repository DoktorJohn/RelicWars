using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Sunvale.AncientRomeUI.Buttons;
using System;
using UnityEngine;

namespace Project.Modules.UI
{
    public class BottomNavigationFooterController : MonoBehaviour
    {
        [Header("Navigation buttons")]
        [SerializeField] private CircularIconTabButton dailiesButton;
        [SerializeField] private CircularIconTabButton messageButton;
        [SerializeField] private CircularIconTabButton reportsButton;
        [SerializeField] private CircularIconTabButton overviewButton;
        [SerializeField] private CircularIconTabButton profileButton;
        [SerializeField] private CircularIconTabButton allianceButton;
        [SerializeField] private CircularIconTabButton rankingsButton;
        [SerializeField] private CircularIconTabButton researchButton;
        [SerializeField] private CircularIconTabButton bugReportButton;

        [Header("uGUI windows")]
        [SerializeField] private GameObject dailiesWindowPrefab;
        [SerializeField] private GameObject messageWindowPrefab;
        [SerializeField] private GameObject reportsWindowPrefab;
        [SerializeField] private GameObject overviewWindowPrefab;
        [SerializeField] private GameObject profileWindowPrefab;
        [SerializeField] private GameObject allianceWindowPrefab;
        [SerializeField] private GameObject createAllianceWindowPrefab;
        [SerializeField] private GameObject rankingsWindowPrefab;
        [SerializeField] private GameObject researchWindowPrefab;
        [SerializeField] private GameObject bugReportWindowPrefab;

        public static BottomNavigationFooterController Instance { get; private set; }

        private bool _allianceProfileRequestInFlight;

        private void OnEnable()
        {
            Instance = this;
            if (dailiesButton != null) dailiesButton.OnButtonActivatedClicked += OnDailiesClicked;
            if (messageButton != null) messageButton.OnButtonActivatedClicked += OnMessageClicked;
            if (reportsButton != null) reportsButton.OnButtonActivatedClicked += OnReportsClicked;
            if (overviewButton != null) overviewButton.OnButtonActivatedClicked += OnOverviewClicked;
            if (profileButton != null) profileButton.OnButtonActivatedClicked += OnProfileClicked;
            if (allianceButton != null) allianceButton.OnButtonActivatedClicked += OnAllianceClicked;
            if (rankingsButton != null) rankingsButton.OnButtonActivatedClicked += OnRankingsClicked;
            if (researchButton != null) researchButton.OnButtonActivatedClicked += OnResearchClicked;
            if (bugReportButton != null) bugReportButton.OnButtonActivatedClicked += OnBugReportClicked;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (dailiesButton != null) dailiesButton.OnButtonActivatedClicked -= OnDailiesClicked;
            if (messageButton != null) messageButton.OnButtonActivatedClicked -= OnMessageClicked;
            if (reportsButton != null) reportsButton.OnButtonActivatedClicked -= OnReportsClicked;
            if (overviewButton != null) overviewButton.OnButtonActivatedClicked -= OnOverviewClicked;
            if (profileButton != null) profileButton.OnButtonActivatedClicked -= OnProfileClicked;
            if (allianceButton != null) allianceButton.OnButtonActivatedClicked -= OnAllianceClicked;
            if (rankingsButton != null) rankingsButton.OnButtonActivatedClicked -= OnRankingsClicked;
            if (researchButton != null) researchButton.OnButtonActivatedClicked -= OnResearchClicked;
            if (bugReportButton != null) bugReportButton.OnButtonActivatedClicked -= OnBugReportClicked;

            UguiWindowHostController.Instance?.CloseActiveWindow();
            _allianceProfileRequestInFlight = false;
        }

        private void OnDailiesClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Dailies, dailiesWindowPrefab);
        private void OnMessageClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Message, messageWindowPrefab);
        private void OnReportsClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Reports, reportsWindowPrefab);
        private void OnOverviewClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Overview, overviewWindowPrefab);
        private void OnProfileClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Profile, profileWindowPrefab);
        private void OnAllianceClicked(CircularIconTabButton _)
        {
            if (_allianceProfileRequestInFlight) return;

            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.WorldPlayer == null ||
                !Guid.TryParse(network.WorldPlayerId, out Guid worldPlayerId))
            {
                Debug.LogError("[BottomNavigationFooterController] Cannot resolve alliance destination without an active world player.");
                return;
            }

            _allianceProfileRequestInFlight = true;
            StartCoroutine(network.WorldPlayer.GetPlayerProfile(worldPlayerId, network.JwtToken, profile =>
            {
                _allianceProfileRequestInFlight = false;
                if (!isActiveAndEnabled || profile == null)
                {
                    if (profile == null)
                        Debug.LogError("[BottomNavigationFooterController] Could not load alliance membership.");
                    return;
                }

                OpenAllianceDestination(profile.AllianceId != Guid.Empty);
            }));
        }

        private void OpenAllianceDestination(bool hasAlliance)
        {
            OpenWindow(
                WindowTypeEnum.Alliance,
                hasAlliance ? allianceWindowPrefab : createAllianceWindowPrefab);
        }
        private void OnRankingsClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Rankings, rankingsWindowPrefab);
        private void OnResearchClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Research, researchWindowPrefab);
        private void OnBugReportClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.BugReport, bugReportWindowPrefab);

        private void OpenWindow(WindowTypeEnum windowType, GameObject windowPrefab)
        {
            UguiWindowHostController.Instance?.OpenWindow(windowType, windowPrefab);
        }

        public void ReplaceActiveAllianceWindow(GameObject replacementPrefab)
        {
            UguiWindowHostController.Instance?.ReplaceActiveWindow(WindowTypeEnum.Alliance, replacementPrefab);
        }
    }
}
