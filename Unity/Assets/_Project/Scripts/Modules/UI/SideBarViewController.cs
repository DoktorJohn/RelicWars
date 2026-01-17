using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SideBarViewController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

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

                _playerProfileButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Profile");
                _alliancePanelButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Alliance");
                _globalRankingsButton = _rootVisualElement.Q<VisualElement>("SideBar-Button-Rankings");

                ValidateButtonReferences();
            }
        }

        private void ValidateButtonReferences()
        {
            if (_playerProfileButton == null) Debug.LogError("[SideBar] Profile Button ikke fundet i UXML.");
            if (_alliancePanelButton == null) Debug.LogError("[SideBar] Alliance Button ikke fundet i UXML.");
            if (_globalRankingsButton == null) Debug.LogError("[SideBar] Rankings Button ikke fundet i UXML.");
        }

        private void RegisterNavigationButtonCallbacks()
        {
            _playerProfileButton?.RegisterCallback<ClickEvent>(OnProfileButtonClicked);
            _alliancePanelButton?.RegisterCallback<ClickEvent>(OnAllianceButtonClicked);
            _globalRankingsButton?.RegisterCallback<ClickEvent>(OnRankingsButtonClicked);
        }

        private void UnregisterNavigationButtonCallbacks()
        {
            _playerProfileButton?.UnregisterCallback<ClickEvent>(OnProfileButtonClicked);
            _alliancePanelButton?.UnregisterCallback<ClickEvent>(OnAllianceButtonClicked);
            _globalRankingsButton?.UnregisterCallback<ClickEvent>(OnRankingsButtonClicked);
        }

        private void OnProfileButtonClicked(ClickEvent clickEvent)
        {
            ExecuteOpenWindowRequest(WindowTypeEnum.Profile);
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
                Debug.LogError("[SideBar] Kunne ikke åbne vindue: GlobalWindowManager.Instance er NULL.");
            }
        }
    }
}