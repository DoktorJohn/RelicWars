using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LeftSideBarViewController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

        private VisualElement _overviewButton;
        private VisualElement _researchButton;
        private VisualElement _playerProfileButton;
        private VisualElement _alliancePanelButton;
        private VisualElement _globalRankingsButton;

        private void OnEnable()
        {
            InitializeUserInterfaceRoots();
            RegisterNavigationButtonCallbacks();
        }

        private void OnDisable()
        {
            UnregisterNavigationButtonCallbacks();
        }

        private void InitializeUserInterfaceRoots()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent != null)
            {
                _rootVisualElement = uiDocumentComponent.rootVisualElement;

                _overviewButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Overview");
                _playerProfileButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Profile");
                _alliancePanelButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Alliance");
                _globalRankingsButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Rankings");
                _researchButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Research");

                ValidateButtonReferences();
            }
        }

        private void ValidateButtonReferences()
        {
            if (_overviewButton == null) Debug.LogError("[LeftSideBarViewController] Overview Button reference missing.");
            if (_playerProfileButton == null) Debug.LogError("[LeftSideBarViewController] Profile Button reference missing.");
            if (_alliancePanelButton == null) Debug.LogError("[LeftSideBarViewController] Alliance Button reference missing.");
            if (_globalRankingsButton == null) Debug.LogError("[LeftSideBarViewController] Rankings Button reference missing.");
            if (_researchButton == null) Debug.LogError("[LeftSideBarViewController] Research Button reference missing.");
        }

        private void RegisterNavigationButtonCallbacks()
        {
            _overviewButton?.RegisterCallback<ClickEvent>(OnOverviewButtonClicked);
            _playerProfileButton?.RegisterCallback<ClickEvent>(OnProfileButtonClicked);
            _alliancePanelButton?.RegisterCallback<ClickEvent>(OnAllianceButtonClicked);
            _globalRankingsButton?.RegisterCallback<ClickEvent>(OnRankingsButtonClicked);
            _researchButton?.RegisterCallback<ClickEvent>(OnResearchButtonClicked);
        }

        private void UnregisterNavigationButtonCallbacks()
        {
            _overviewButton?.UnregisterCallback<ClickEvent>(OnOverviewButtonClicked);
            _playerProfileButton?.UnregisterCallback<ClickEvent>(OnProfileButtonClicked);
            _alliancePanelButton?.UnregisterCallback<ClickEvent>(OnAllianceButtonClicked);
            _globalRankingsButton?.UnregisterCallback<ClickEvent>(OnRankingsButtonClicked);
            _researchButton?.UnregisterCallback<ClickEvent>(OnResearchButtonClicked);
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