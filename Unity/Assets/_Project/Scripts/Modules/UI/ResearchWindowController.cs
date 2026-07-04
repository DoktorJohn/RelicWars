using Project.Modules.UI;
using System;
using System.Collections;
using UnityEngine;
using Project.Network.Manager;
using UnityEngine.UIElements;
using Project.Scripts.Domain.DTOs;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Scripts.Domain.Enums;

namespace Project.Scripts.Modules.UI
{
    public partial class ResearchWindowController : BaseWindow
    {
        protected override string WindowName => "Research";
        protected override string VisualContainerName => "Research-Window-MainContainer";
        protected override string HeaderName => "Research-Window-Header";

        private Label _researchPointsLabel;
        private VisualElement _researchTreeContainer;
        private VisualElement _activeJobPanel;
        private Label _activeResearchNameLabel;
        private Label _activeResearchTimerLabel;
        private Button _cancelResearchButton;
        private VisualElement _lockedResearchTooltip;
        private Label _lockedResearchTooltipBodyLabel;

        private Button _tabButtonEconomy;
        private Button _tabButtonWar;
        private Button _tabButtonUtility;
        private Button _closeButton;
        private ResearchTypeEnum _currentSelectedCategory = ResearchTypeEnum.Economy;

        private Guid _worldPlayerId;
        private int _requestVersion;
        private List<ResearchNodeDTO> _cachedResearchNodes = new List<ResearchNodeDTO>();
        private ActiveResearchJobDTO _activeResearchJob;
        private Guid _currentCancelResearchJobId;
        private Coroutine _activeTimerCoroutine;
        private bool _isCommandInFlight;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeUserInterfaceReferences();

            if (Root != null) Root.pickingMode = PickingMode.Ignore;

            if (NetworkManager.Instance == null)
            {
                WindowAsyncStateHelper.ShowError(_researchTreeContainer, "Network unavailable.");
                CompleteDeferredOpen(version);
                return;
            }

            if (Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid parsedWorldPlayerId))
            {
                _worldPlayerId = parsedWorldPlayerId;
            }
            else
            {
                Debug.LogError($"[ResearchWindow] Ugyldig WorldPlayerId.");
                WindowAsyncStateHelper.ShowError(_researchTreeContainer, "Invalid world player.");
                CompleteDeferredOpen(version);
                return;
            }

            InitializeTabNavigation();
            RefreshResearchWindowState(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            _activeTimerCoroutine = null;
            _isCommandInFlight = false;
            HideLockedResearchTooltip();

            if (_cancelResearchButton != null)
            {
                _cancelResearchButton.clicked -= OnCancelResearchClicked;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked -= Close;
            }

            if (_tabButtonEconomy != null)
            {
                _tabButtonEconomy.clicked -= HandleEconomyTabClicked;
            }

            if (_tabButtonWar != null)
            {
                _tabButtonWar.clicked -= HandleWarTabClicked;
            }

            if (_tabButtonUtility != null)
            {
                _tabButtonUtility.clicked -= HandleUtilityTabClicked;
            }
        }

        private void InitializeUserInterfaceReferences()
        {
            _researchPointsLabel = Root.Q<Label>("Research-Points-Amount");
            _researchTreeContainer = Root.Q<VisualElement>("Research-Tree-Container");
            _activeJobPanel = Root.Q<VisualElement>("Active-Research-Panel");
            _activeResearchNameLabel = Root.Q<Label>("Active-Research-Name");
            _activeResearchTimerLabel = Root.Q<Label>("Active-Research-Timer");
            _cancelResearchButton = Root.Q<Button>("Button-Cancel-Research");
            _lockedResearchTooltip = Root.Q<VisualElement>("Research-Lock-Tooltip");
            _lockedResearchTooltipBodyLabel = Root.Q<Label>("Research-Lock-Tooltip-Body");

            if (_cancelResearchButton != null)
            {
                _cancelResearchButton.style.display = DisplayStyle.None;
                _cancelResearchButton.SetEnabled(false);
            }

            if (_lockedResearchTooltip != null)
            {
                _lockedResearchTooltip.style.display = DisplayStyle.None;
            }

            if (_cancelResearchButton != null)
            {
                _cancelResearchButton.clicked -= OnCancelResearchClicked;
                _cancelResearchButton.clicked += OnCancelResearchClicked;
            }

            var closeButton = Root.Q<Button>("Header-Close-Button");
            if (closeButton != null)
            {
                _closeButton = closeButton;
                closeButton.clicked -= Close;
                closeButton.clicked += Close;
            }
        }

        private void InitializeTabNavigation()
        {
            _tabButtonEconomy = Root.Q<Button>("Tab-Economy");
            _tabButtonWar = Root.Q<Button>("Tab-War");
            _tabButtonUtility = Root.Q<Button>("Tab-Utility");

            if (_tabButtonEconomy != null)
            {
                _tabButtonEconomy.clicked -= HandleEconomyTabClicked;
                _tabButtonEconomy.clicked += HandleEconomyTabClicked;
            }

            if (_tabButtonWar != null)
            {
                _tabButtonWar.clicked -= HandleWarTabClicked;
                _tabButtonWar.clicked += HandleWarTabClicked;
            }

            if (_tabButtonUtility != null)
            {
                _tabButtonUtility.clicked -= HandleUtilityTabClicked;
                _tabButtonUtility.clicked += HandleUtilityTabClicked;
            }

            UpdateTabButtonVisualStates();
        }

        private void HandleEconomyTabClicked() => SwitchResearchCategoryTab(ResearchTypeEnum.Economy);
        private void HandleWarTabClicked() => SwitchResearchCategoryTab(ResearchTypeEnum.War);
        private void HandleUtilityTabClicked() => SwitchResearchCategoryTab(ResearchTypeEnum.Utility);

        private void SwitchResearchCategoryTab(ResearchTypeEnum selectedCategory)
        {
            _currentSelectedCategory = selectedCategory;
            UpdateTabButtonVisualStates();
            PopulateResearchTreeVisuals(_cachedResearchNodes);
        }

        private void UpdateTabButtonVisualStates()
        {
            _tabButtonEconomy.EnableInClassList("research-tab-button-active", _currentSelectedCategory == ResearchTypeEnum.Economy);
            _tabButtonWar.EnableInClassList("research-tab-button-active", _currentSelectedCategory == ResearchTypeEnum.War);
            _tabButtonUtility.EnableInClassList("research-tab-button-active", _currentSelectedCategory == ResearchTypeEnum.Utility);
        }

        private void RefreshResearchWindowState(int version)
        {
            string jwtToken = NetworkManager.Instance.JwtToken;
            WindowAsyncStateHelper.ShowLoading(_researchTreeContainer, "Loading research tree...");

            StartCoroutine(NetworkManager.Instance.Research.GetResearchTreeState(_worldPlayerId, jwtToken, (researchTreeData) =>
            {
                if (!isActiveAndEnabled || version != _requestVersion)
                {
                    return;
                }

                if (researchTreeData == null)
                {
                    WindowAsyncStateHelper.ShowError(
                        _researchTreeContainer,
                        "Could not load research tree.",
                        () => RefreshResearchWindowState(version));
                    _isCommandInFlight = false;
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _cancelResearchButton, _tabButtonEconomy, _tabButtonWar, _tabButtonUtility }, true);
                    CompleteDeferredOpen(version);
                    return;
                }

                _cachedResearchNodes = researchTreeData.Nodes;
                _activeResearchJob = researchTreeData.ActiveJob;
                UpdateResearchPointsDisplay(researchTreeData.CurrentResearchPoints);
                if (_cachedResearchNodes == null || _cachedResearchNodes.Count == 0)
                {
                    WindowAsyncStateHelper.ShowEmpty(_researchTreeContainer, "No research available.");
                }
                else
                {
                    PopulateResearchTreeVisuals(_cachedResearchNodes);
                }
                HandleActiveResearchJobDisplay(_activeResearchJob);
                _isCommandInFlight = false;
                WindowAsyncStateHelper.SetButtonsEnabled(new[] { _cancelResearchButton, _tabButtonEconomy, _tabButtonWar, _tabButtonUtility }, true);
                CompleteDeferredOpen(version);
            }));
        }


        public void RequestStartResearch(string researchId)
        {
            if (_isCommandInFlight) return;

            string jwtToken = NetworkManager.Instance.JwtToken;
            _isCommandInFlight = true;
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _cancelResearchButton, _tabButtonEconomy, _tabButtonWar, _tabButtonUtility }, false);
            StartCoroutine(NetworkManager.Instance.Research.StartResearchProcess(_worldPlayerId, researchId, jwtToken, (success, message) =>
            {
                if (!isActiveAndEnabled) return;
                if (success) RefreshResearchWindowState(_requestVersion);
                else
                {
                    _isCommandInFlight = false;
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _cancelResearchButton, _tabButtonEconomy, _tabButtonWar, _tabButtonUtility }, true);
                }
            }));
        }

        private void OnCancelResearchClicked()
        {
            if (_currentCancelResearchJobId == Guid.Empty) return;
            RequestCancelResearch(_currentCancelResearchJobId);
        }

        private void RequestCancelResearch(Guid jobId)
        {
            if (_isCommandInFlight) return;

            string jwtToken = NetworkManager.Instance.JwtToken;
            _isCommandInFlight = true;
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _cancelResearchButton, _tabButtonEconomy, _tabButtonWar, _tabButtonUtility }, false);
            StartCoroutine(NetworkManager.Instance.Research.CancelActiveResearch(_worldPlayerId, jobId, jwtToken, (success, message) =>
            {
                if (!isActiveAndEnabled) return;
                if (success) RefreshResearchWindowState(_requestVersion);
                else
                {
                    _isCommandInFlight = false;
                    WindowAsyncStateHelper.SetButtonsEnabled(new[] { _cancelResearchButton, _tabButtonEconomy, _tabButtonWar, _tabButtonUtility }, true);
                }
            }));
        }
    }
}
