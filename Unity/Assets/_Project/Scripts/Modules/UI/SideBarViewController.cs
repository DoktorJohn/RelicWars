using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Models;
using Assets.Scripts.Domain.Enums; // For WindowTypeEnum hvis nødvendigt

namespace Project.Modules.UI
{
    /// <summary>
    /// Controller der håndterer den venstre side-bar (Profil, Alliance, Rankings).
    /// Benytter Event-baseret interaktion for at åbne globale vinduer.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CitySideBarViewController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

        // Verbøse referencer til knapperne
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

                _playerProfileButton = _rootVisualElement.Q<VisualElement>("City-SideBar-Button-Profile");
                _alliancePanelButton = _rootVisualElement.Q<VisualElement>("City-SideBar-Button-Alliance");
                _globalRankingsButton = _rootVisualElement.Q<VisualElement>("City-SideBar-Button-Rankings");

                ValidateButtonReferences();
            }
        }

        private void ValidateButtonReferences()
        {
            if (_playerProfileButton == null) Debug.LogError("[CitySideBar] Profile Button ikke fundet i UXML.");
            if (_alliancePanelButton == null) Debug.LogError("[CitySideBar] Alliance Button ikke fundet i UXML.");
            if (_globalRankingsButton == null) Debug.LogError("[CitySideBar] Rankings Button ikke fundet i UXML.");
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
            Debug.Log("[CitySideBar] Åbner Spillerprofil.");
            ExecuteOpenWindowRequest(WindowTypeEnum.Profile);
        }

        private void OnAllianceButtonClicked(ClickEvent clickEvent)
        {
            Debug.Log("[CitySideBar] Åbner Alliance-oversigt.");
            ExecuteOpenWindowRequest(WindowTypeEnum.Alliance);
        }

        private void OnRankingsButtonClicked(ClickEvent clickEvent)
        {
            Debug.Log("[CitySideBar] Åbner Rankings.");
            ExecuteOpenWindowRequest(WindowTypeEnum.Rankings);
        }

        private void ExecuteOpenWindowRequest(WindowTypeEnum windowType)
        {
            // Vi benytter den eksisterende GlobalWindowManager til at åbne UI-lagene
            if (GlobalWindowManager.Instance != null)
            {
                GlobalWindowManager.Instance.OpenWindow(windowType);
            }
            else
            {
                Debug.LogError("[CitySideBar] Kunne ikke åbne vindue: GlobalWindowManager.Instance er NULL.");
            }
        }
    }
}