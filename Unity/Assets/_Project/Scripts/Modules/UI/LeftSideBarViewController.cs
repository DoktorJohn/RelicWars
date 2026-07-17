using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;
using Project.Modules.Messaging;
using Project.Modules.Reports;

namespace Project.Modules.Messaging
{
    public static class MessagingStateEvents
    {
        public static event Action UnreadStateChanged;

        public static void RaiseUnreadStateChanged()
        {
            UnreadStateChanged?.Invoke();
        }
    }
}

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LeftSideBarViewController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

        private VisualElement _dailiesButton;
        private VisualElement _overviewButton;
        private VisualElement _researchButton;
        private VisualElement _playerProfileButton;
        private VisualElement _alliancePanelButton;
        private VisualElement _globalRankingsButton;
        private VisualElement _messageButton;
        private VisualElement _reportsButton;
        private Button _bugReportButton;
        private Label _inboxNotificationBadge;
        private Label _reportsNotificationBadge;
        
        private void OnEnable()
        {
            InitializeUserInterfaceRoots();
            RegisterNavigationButtonCallbacks();
            MessagingStateEvents.UnreadStateChanged += CheckUnreadMessages;
            BattleReportStateEvents.UnreadStateChanged += CheckUnreadBattleReports;
            CheckUnreadMessages();
            CheckUnreadBattleReports();
            InvokeRepeating(nameof(CheckUnreadMessages), 10f, 10f);
            InvokeRepeating(nameof(CheckUnreadBattleReports), 10f, 10f);
        }

        private void CheckUnreadMessages()
        {
            if (Project.Network.Manager.NetworkManager.Instance == null || string.IsNullOrEmpty(Project.Network.Manager.NetworkManager.Instance.WorldPlayerId)) return;

            if (Guid.TryParse(Project.Network.Manager.NetworkManager.Instance.WorldPlayerId, out Guid wpId))
            {
                StartCoroutine(Project.Network.Manager.NetworkManager.Instance.Messaging.GetUnreadMessageCount(wpId, Project.Network.Manager.NetworkManager.Instance.JwtToken, (unreadCount) =>
                {
                    if (_inboxNotificationBadge != null)
                    {
                        _inboxNotificationBadge.style.display = unreadCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                        _inboxNotificationBadge.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                    }
                }));
            }
        }

        private void CheckUnreadBattleReports()
        {
            if (Project.Network.Manager.NetworkManager.Instance == null || string.IsNullOrEmpty(Project.Network.Manager.NetworkManager.Instance.WorldPlayerId)) return;

            if (Guid.TryParse(Project.Network.Manager.NetworkManager.Instance.WorldPlayerId, out Guid wpId))
            {
                StartCoroutine(Project.Network.Manager.NetworkManager.Instance.BattleReports.GetUnreadBattleReportCount(wpId, Project.Network.Manager.NetworkManager.Instance.JwtToken, (unreadCount) =>
                {
                    if (_reportsNotificationBadge != null)
                    {
                        _reportsNotificationBadge.style.display = unreadCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                        _reportsNotificationBadge.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                    }
                }));
            }
        }

        private void OnDisable()
        {
            ResponsiveUiStateManager.UnregisterRoot(_rootVisualElement);
            UnregisterNavigationButtonCallbacks();
            MessagingStateEvents.UnreadStateChanged -= CheckUnreadMessages;
            BattleReportStateEvents.UnreadStateChanged -= CheckUnreadBattleReports;
            CancelInvoke(nameof(CheckUnreadMessages));
            CancelInvoke(nameof(CheckUnreadBattleReports));
        }

        private void InitializeUserInterfaceRoots()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent != null)
            {
                _rootVisualElement = uiDocumentComponent.rootVisualElement;
                ResponsiveUiStateManager.RegisterRoot(_rootVisualElement);

                _dailiesButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Dailies");
                _overviewButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Overview");
                _playerProfileButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Profile");
                _alliancePanelButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Alliance");
                _globalRankingsButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Rankings");
                _messageButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Inbox");
                _reportsButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Reports");
                _researchButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Research");
                _bugReportButton = _rootVisualElement.Q<Button>("SideBar-Button-BugReport");
                
                if (_messageButton != null)
                {
                    _inboxNotificationBadge = _messageButton.Q<Label>("Inbox-Notification-Dot");
                }

                if (_reportsButton != null)
                {
                    _reportsNotificationBadge = _reportsButton.Q<Label>("Reports-Notification-Dot");
                }

                ValidateButtonReferences();
            }
        }

        private void ValidateButtonReferences()
        {
            if (_dailiesButton == null) Debug.LogError("[LeftSideBarViewController] Dailies Button reference missing.");
            if (_overviewButton == null) Debug.LogError("[LeftSideBarViewController] Overview Button reference missing.");
            if (_playerProfileButton == null) Debug.LogError("[LeftSideBarViewController] Profile Button reference missing.");
            if (_alliancePanelButton == null) Debug.LogError("[LeftSideBarViewController] Alliance Button reference missing.");
            if (_globalRankingsButton == null) Debug.LogError("[LeftSideBarViewController] Rankings Button reference missing.");
            if (_messageButton == null) Debug.LogError("[LeftSideBarViewController] Message Button reference missing.");
            if (_reportsButton == null) Debug.LogError("[LeftSideBarViewController] Reports Button reference missing.");
            if (_researchButton == null) Debug.LogError("[LeftSideBarViewController] Research Button reference missing.");
            if (_bugReportButton == null) Debug.LogError("[LeftSideBarViewController] Bug report Button reference missing.");
        }

        private void RegisterNavigationButtonCallbacks()
        {
            _dailiesButton?.RegisterCallback<ClickEvent>(OnDailiesButtonClicked);
            _overviewButton?.RegisterCallback<ClickEvent>(OnOverviewButtonClicked);
            _playerProfileButton?.RegisterCallback<ClickEvent>(OnProfileButtonClicked);
            _alliancePanelButton?.RegisterCallback<ClickEvent>(OnAllianceButtonClicked);
            _globalRankingsButton?.RegisterCallback<ClickEvent>(OnRankingsButtonClicked);
            _messageButton?.RegisterCallback<ClickEvent>(OnMessageButtonClicked);
            _reportsButton?.RegisterCallback<ClickEvent>(OnReportsButtonClicked);
            _researchButton?.RegisterCallback<ClickEvent>(OnResearchButtonClicked);
            if (_bugReportButton != null)
            {
                _bugReportButton.clicked -= OnBugReportButtonClicked;
                _bugReportButton.clicked += OnBugReportButtonClicked;
            }
        }

        private void UnregisterNavigationButtonCallbacks()
        {
            _dailiesButton?.UnregisterCallback<ClickEvent>(OnDailiesButtonClicked);
            _overviewButton?.UnregisterCallback<ClickEvent>(OnOverviewButtonClicked);
            _playerProfileButton?.UnregisterCallback<ClickEvent>(OnProfileButtonClicked);
            _alliancePanelButton?.UnregisterCallback<ClickEvent>(OnAllianceButtonClicked);
            _globalRankingsButton?.UnregisterCallback<ClickEvent>(OnRankingsButtonClicked);
            _messageButton?.UnregisterCallback<ClickEvent>(OnMessageButtonClicked);
            _reportsButton?.UnregisterCallback<ClickEvent>(OnReportsButtonClicked);
            _researchButton?.UnregisterCallback<ClickEvent>(OnResearchButtonClicked);
            if (_bugReportButton != null)
            {
                _bugReportButton.clicked -= OnBugReportButtonClicked;
            }
        }

        private void OnDailiesButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Dailies);
        }

        private void OnOverviewButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Overview);
        }

        private void OnProfileButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Profile);
        }

        private void OnResearchButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Research);
        }

        private void OnAllianceButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Alliance);
        }

        private void OnRankingsButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Rankings);
        }

        private void OnMessageButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Message);
        }

        private void OnReportsButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Reports);
        }

        private void OnBugReportButtonClicked()
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.BugReport);
        }

        private void ExecuteOpenWindowRequest(WindowTypeEnum windowType)
        {
            if (GlobalWindowManager.Instance != null)
            {
                GlobalWindowManager.Instance.OpenWindow(windowType);
            }
            else
            {
                Debug.LogError("[LeftSideBarViewController] Failed to open window: GlobalWindowManager Instance is null.");
            }
        }
    }
}
