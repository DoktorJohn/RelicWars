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
    public class ResearchWindowController : BaseWindow
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

        // Tab References
        private Button _tabButtonEconomy;
        private Button _tabButtonWar;
        private Button _tabButtonUtility;
        private ResearchTypeEnum _currentSelectedCategory = ResearchTypeEnum.Economy;

        private Guid _worldPlayerId;
        private List<ResearchNodeDTO> _cachedResearchNodes = new List<ResearchNodeDTO>();
        private Coroutine _activeTimerCoroutine;

        public override void OnOpen(object dataPayload)
        {
            if (Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid parsedWorldPlayerId))
            {
                _worldPlayerId = parsedWorldPlayerId;
            }
            else
            {
                Debug.LogError($"[ResearchWindow] Ugyldig WorldPlayerId: {NetworkManager.Instance.WorldPlayerId}");
                return;
            }

            InitializeUserInterfaceReferences();
            InitializeTabNavigation();
            RefreshResearchWindowState();
        }

        private void InitializeUserInterfaceReferences()
        {
            _researchPointsLabel = Root.Q<Label>("Research-Points-Amount");
            _researchTreeContainer = Root.Q<VisualElement>("Research-Tree-Container");
            _activeJobPanel = Root.Q<VisualElement>("Active-Research-Panel");
            _activeResearchNameLabel = Root.Q<Label>("Active-Research-Name");
            _activeResearchTimerLabel = Root.Q<Label>("Active-Research-Timer");
            _cancelResearchButton = Root.Q<Button>("Button-Cancel-Research");

            var closeButton = Root.Q<Button>("Header-Close-Button");
            if (closeButton != null)
            {
                closeButton.clicked -= Close;
                closeButton.clicked += Close;
            }
        }

        private void InitializeTabNavigation()
        {
            _tabButtonEconomy = Root.Q<Button>("Tab-Economy");
            _tabButtonWar = Root.Q<Button>("Tab-War");
            _tabButtonUtility = Root.Q<Button>("Tab-Utility");

            _tabButtonEconomy.clicked += () => SwitchResearchCategoryTab(ResearchTypeEnum.Economy);
            _tabButtonWar.clicked += () => SwitchResearchCategoryTab(ResearchTypeEnum.War);
            _tabButtonUtility.clicked += () => SwitchResearchCategoryTab(ResearchTypeEnum.Utility);

            UpdateTabButtonVisualStates();
        }

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

        private void RefreshResearchWindowState()
        {
            string jwtToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Research.GetResearchTreeState(_worldPlayerId, jwtToken, (researchTreeData) =>
            {
                if (researchTreeData == null) return;

                _cachedResearchNodes = researchTreeData.Nodes;
                UpdateResearchPointsDisplay(researchTreeData.CurrentResearchPoints);
                PopulateResearchTreeVisuals(_cachedResearchNodes);
                HandleActiveResearchJobDisplay(researchTreeData.ActiveJob);
            }));
        }

        private void UpdateResearchPointsDisplay(double currentPoints)
        {
            if (_researchPointsLabel != null)
            {
                _researchPointsLabel.text = currentPoints.ToString("N0");
            }
        }

        private void PopulateResearchTreeVisuals(List<ResearchNodeDTO> nodes)
        {
            if (_researchTreeContainer == null) return;
            _researchTreeContainer.Clear();

            // Filtrering baseret på den valgte fane
            var filteredNodes = nodes.Where(node => node.ResearchType == _currentSelectedCategory).ToList();

            foreach (var node in filteredNodes)
            {
                AddResearchNodeToUI(node);
            }
        }

        private void AddResearchNodeToUI(ResearchNodeDTO nodeData)
        {
            VisualElement nodeCard = new VisualElement();
            nodeCard.AddToClassList("research-node");

            Label title = new Label(nodeData.Name);
            title.AddToClassList("node-title");
            nodeCard.Add(title);

            Label desc = new Label(nodeData.Description);
            desc.AddToClassList("node-description");
            nodeCard.Add(desc);

            VisualElement costRow = new VisualElement();
            costRow.AddToClassList("node-cost-row");

            Label costLabel = new Label($"{nodeData.ResearchPointCost:N0} RP");
            costLabel.AddToClassList("node-cost-label");
            costRow.Add(costLabel);

            if (nodeData.IsCompleted)
            {
                nodeCard.AddToClassList("node-completed");
                Label completedLabel = new Label("DONE");
                completedLabel.style.color = new Color(0, 0.4f, 0);
                completedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                costRow.Add(completedLabel);
            }
            else if (nodeData.IsLocked)
            {
                nodeCard.AddToClassList("node-locked");
                Label lockedLabel = new Label("LOCKED");
                lockedLabel.style.color = Color.gray;
                costRow.Add(lockedLabel);
            }
            else
            {
                Button researchBtn = new Button(() => RequestStartResearch(nodeData.Id));
                researchBtn.text = "START";
                researchBtn.style.fontSize = 10;
                researchBtn.SetEnabled(nodeData.CanAfford);
                costRow.Add(researchBtn);
            }

            nodeCard.Add(costRow);
            _researchTreeContainer.Add(nodeCard);
        }

        private void HandleActiveResearchJobDisplay(ActiveResearchJobDTO activeJob)
        {
            if (activeJob == null)
            {
                if (_activeJobPanel != null) _activeJobPanel.style.display = DisplayStyle.None;
                return;
            }

            if (_activeJobPanel != null) _activeJobPanel.style.display = DisplayStyle.Flex;

            // Find navnet på den research der er i gang fra cachen
            var researchInfo = _cachedResearchNodes.FirstOrDefault(n => n.Id == activeJob.ResearchId);
            if (_activeResearchNameLabel != null)
                _activeResearchNameLabel.text = researchInfo != null ? researchInfo.Name.ToUpper() : activeJob.ResearchId;

            if (_cancelResearchButton != null)
            {
                _cancelResearchButton.clicked -= () => RequestCancelResearch(activeJob.JobId);
                _cancelResearchButton.clicked += () => RequestCancelResearch(activeJob.JobId);
            }

            if (_activeTimerCoroutine != null) StopCoroutine(_activeTimerCoroutine);
            _activeTimerCoroutine = StartCoroutine(ExecuteActiveResearchCountdownTimer(activeJob.ExpectedCompletionTime));
        }

        private IEnumerator ExecuteActiveResearchCountdownTimer(DateTime completionTime)
        {
            while (true)
            {
                TimeSpan remainingTime = completionTime - DateTime.UtcNow;

                if (remainingTime.TotalSeconds <= 0)
                {
                    if (_activeResearchTimerLabel != null) _activeResearchTimerLabel.text = "00:00:00";
                    RefreshResearchWindowState();
                    yield break;
                }

                if (_activeResearchTimerLabel != null)
                {
                    _activeResearchTimerLabel.text = remainingTime.ToString(@"hh\:mm\:ss");
                }

                yield return new WaitForSeconds(1.0f);
            }
        }

        public void RequestStartResearch(string researchId)
        {
            string jwtToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Research.StartResearchProcess(_worldPlayerId, researchId, jwtToken, (success, message) =>
            {
                if (success)
                {
                    RefreshResearchWindowState();
                }
                else
                {
                    Debug.LogWarning($"[ResearchWindow] Start failed: {message}");
                }
            }));
        }

        private void RequestCancelResearch(Guid jobId)
        {
            string jwtToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Research.CancelActiveResearch(_worldPlayerId, jobId, jwtToken, (success, message) =>
            {
                if (success)
                {
                    RefreshResearchWindowState();
                }
            }));
        }
    }
}