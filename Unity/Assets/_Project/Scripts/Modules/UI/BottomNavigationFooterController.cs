using Assets.Scripts.Domain.Enums;
using Sunvale.AncientRomeUI.Buttons;
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

        private void OnEnable()
        {
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
            if (dailiesButton != null) dailiesButton.OnButtonActivatedClicked -= OnDailiesClicked;
            if (messageButton != null) messageButton.OnButtonActivatedClicked -= OnMessageClicked;
            if (reportsButton != null) reportsButton.OnButtonActivatedClicked -= OnReportsClicked;
            if (overviewButton != null) overviewButton.OnButtonActivatedClicked -= OnOverviewClicked;
            if (profileButton != null) profileButton.OnButtonActivatedClicked -= OnProfileClicked;
            if (allianceButton != null) allianceButton.OnButtonActivatedClicked -= OnAllianceClicked;
            if (rankingsButton != null) rankingsButton.OnButtonActivatedClicked -= OnRankingsClicked;
            if (researchButton != null) researchButton.OnButtonActivatedClicked -= OnResearchClicked;
            if (bugReportButton != null) bugReportButton.OnButtonActivatedClicked -= OnBugReportClicked;
        }

        private void OnDailiesClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Dailies);
        private void OnMessageClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Message);
        private void OnReportsClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Reports);
        private void OnOverviewClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Overview);
        private void OnProfileClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Profile);
        private void OnAllianceClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Alliance);
        private void OnRankingsClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Rankings);
        private void OnResearchClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.Research);
        private void OnBugReportClicked(CircularIconTabButton _) => OpenWindow(WindowTypeEnum.BugReport);

        private void OpenWindow(WindowTypeEnum windowType)
        {
            if (GlobalWindowManager.Instance != null)
            {
                GlobalWindowManager.Instance.OpenWindow(windowType);
            }
            else
            {
                Debug.LogError($"[BottomNavigationFooterController] Failed to open {windowType}: GlobalWindowManager Instance is null.");
            }
        }
    }
}
