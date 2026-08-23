using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.State;
using Project.Modules.WorldPlayer;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.SkillTree;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiResearchWindowController : MonoBehaviour
    {
        [Serializable]
        private sealed class ResearchNodeBinding
        {
            [SerializeField] private string researchId;
            [SerializeField] private SkillTreeNode skillTreeNode;

            public string ResearchId => researchId;
            public SkillTreeNode SkillTreeNode => skillTreeNode;
            public SkillTreeButton Button => skillTreeNode != null ? skillTreeNode.myButton : null;
        }

        private const float CompletionRetryDelaySeconds = 1.5f;
        private const double RateComparisonTolerance = 0.000001d;

        [Header("Tabs")]
        [SerializeField] private FramedSpriteTabButton economyTab;
        [SerializeField] private FramedSpriteTabButton militaryTab;
        [SerializeField] private FramedSpriteTabButton administrationTab;

        [Header("Authored Trees")]
        [SerializeField] private GameObject economyTree;
        [SerializeField] private GameObject militaryTree;
        [SerializeField] private GameObject administrationTree;

        [Header("Authored Node Bindings")]
        [SerializeField] private List<ResearchNodeBinding> nodeBindings = new();

        [Header("Tooltip")]
        [SerializeField] private UguiResearchTooltipView researchTooltip;

        private readonly Dictionary<string, ResearchNodeDTO> _nodesById =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<SkillTreeButton, ResearchNodeBinding> _bindingsByButton = new();

        private int _lifecycleVersion;
        private int _loadRequestVersion;
        private bool _isLoadInFlight;
        private bool _isCommandInFlight;
        private ResearchRateDTO _boundRate;
        private ResearchTreeDTO _currentTree;
        private Coroutine _completionRefreshCoroutine;

        private void OnEnable()
        {
            _lifecycleVersion++;
            SubscribeTabs();
            SubscribeNodes();
            SubscribeResearchRate();
            SelectCategory(ResearchTypeEnum.Utility, false);
            LoadResearchData();
        }

        private void OnDisable()
        {
            _lifecycleVersion++;
            _loadRequestVersion++;
            _isLoadInFlight = false;
            _isCommandInFlight = false;
            _boundRate = null;
            _currentTree = null;
            _nodesById.Clear();
            researchTooltip?.Hide();
            StopCompletionRefresh();
            UnsubscribeResearchRate();
            UnsubscribeNodes();
            UnsubscribeTabs();
        }

        private void SubscribeTabs()
        {
            if (economyTab != null) economyTab.OnButtonActivatedClicked += OnEconomyClicked;
            if (militaryTab != null) militaryTab.OnButtonActivatedClicked += OnMilitaryClicked;
            if (administrationTab != null) administrationTab.OnButtonActivatedClicked += OnAdministrationClicked;
        }

        private void UnsubscribeTabs()
        {
            if (economyTab != null) economyTab.OnButtonActivatedClicked -= OnEconomyClicked;
            if (militaryTab != null) militaryTab.OnButtonActivatedClicked -= OnMilitaryClicked;
            if (administrationTab != null) administrationTab.OnButtonActivatedClicked -= OnAdministrationClicked;
        }

        private void SubscribeNodes()
        {
            _bindingsByButton.Clear();
            foreach (ResearchNodeBinding binding in nodeBindings)
            {
                SkillTreeButton button = binding?.Button;
                if (button == null || string.IsNullOrWhiteSpace(binding.ResearchId))
                {
                    Debug.LogError("[UguiResearchWindowController] An authored node binding is incomplete.", this);
                    continue;
                }

                if (!_bindingsByButton.TryAdd(button, binding))
                {
                    Debug.LogError($"[UguiResearchWindowController] Duplicate button binding for '{binding.ResearchId}'.", button);
                    continue;
                }

                button.OnButtonActivatedClicked += HandleNodeActivated;
                button.OnButtonPointerEnterEvent += HandleNodePointerEntered;
                button.OnButtonPointerExitEvent += HandleNodePointerExited;
            }
        }

        private void UnsubscribeNodes()
        {
            foreach (SkillTreeButton button in _bindingsByButton.Keys)
            {
                if (button == null) continue;
                button.OnButtonActivatedClicked -= HandleNodeActivated;
                button.OnButtonPointerEnterEvent -= HandleNodePointerEntered;
                button.OnButtonPointerExitEvent -= HandleNodePointerExited;
            }

            _bindingsByButton.Clear();
        }

        private void SubscribeResearchRate()
        {
            if (WorldPlayerStateManager.Instance != null)
                WorldPlayerStateManager.Instance.OnEconomyStateChanged += HandleEconomyStateChanged;
        }

        private void UnsubscribeResearchRate()
        {
            if (WorldPlayerStateManager.Instance != null)
                WorldPlayerStateManager.Instance.OnEconomyStateChanged -= HandleEconomyStateChanged;
        }

        private void OnEconomyClicked(FramedSpriteTabButton _) => SelectCategory(ResearchTypeEnum.Economy, true);
        private void OnMilitaryClicked(FramedSpriteTabButton _) => SelectCategory(ResearchTypeEnum.War, true);
        private void OnAdministrationClicked(FramedSpriteTabButton _) => SelectCategory(ResearchTypeEnum.Utility, true);

        private void SelectCategory(ResearchTypeEnum category, bool animate)
        {
            researchTooltip?.Hide();
            if (economyTree != null) economyTree.SetActive(category == ResearchTypeEnum.Economy);
            if (militaryTree != null) militaryTree.SetActive(category == ResearchTypeEnum.War);
            if (administrationTree != null) administrationTree.SetActive(category == ResearchTypeEnum.Utility);

            economyTab?.SetSelected(category == ResearchTypeEnum.Economy, animate);
            militaryTab?.SetSelected(category == ResearchTypeEnum.War, animate);
            administrationTab?.SetSelected(category == ResearchTypeEnum.Utility, animate);
        }

        private void LoadResearchData()
        {
            if (_isLoadInFlight || !TryGetNetworkContext(out NetworkManager network, out Guid worldPlayerId))
                return;

            int lifecycleVersion = _lifecycleVersion;
            int requestVersion = ++_loadRequestVersion;
            _isLoadInFlight = true;

            StartCoroutine(network.Research.GetResearchTreeState(worldPlayerId, network.JwtToken, tree =>
            {
                if (!isActiveAndEnabled || lifecycleVersion != _lifecycleVersion || requestVersion != _loadRequestVersion)
                    return;

                _isLoadInFlight = false;
                if (tree?.Nodes == null)
                {
                    _isCommandInFlight = false;
                    Debug.LogError("[UguiResearchWindowController] Research tree could not be loaded.", this);
                    return;
                }

                ApplyTree(tree);
            }));
        }

        private void ApplyTree(ResearchTreeDTO tree)
        {
            _isCommandInFlight = false;
            _boundRate = tree.ResearchRate;
            _currentTree = tree;
            BindNodes(tree);
            ScheduleCompletionRefresh(tree);
        }

        private void BindNodes(ResearchTreeDTO tree)
        {
            _nodesById.Clear();
            foreach (ResearchNodeDTO node in tree.Nodes.Where(node => node != null &&
                         node.ResearchType is ResearchTypeEnum.Economy or ResearchTypeEnum.War or ResearchTypeEnum.Utility))
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    Debug.LogError("[UguiResearchWindowController] Backend returned a display node without an ID.", this);
                    continue;
                }

                if (!_nodesById.TryAdd(node.Id, node))
                    Debug.LogError($"[UguiResearchWindowController] Backend returned duplicate research ID '{node.Id}'.", this);
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ResearchNodeBinding binding in nodeBindings)
            {
                if (binding == null || binding.Button == null || string.IsNullOrWhiteSpace(binding.ResearchId))
                {
                    Debug.LogError("[UguiResearchWindowController] An authored node binding is incomplete.", this);
                    continue;
                }

                if (!seenIds.Add(binding.ResearchId))
                {
                    Debug.LogError($"[UguiResearchWindowController] Duplicate node binding '{binding.ResearchId}'.", this);
                    continue;
                }

                if (!_nodesById.TryGetValue(binding.ResearchId, out ResearchNodeDTO node))
                {
                    Debug.LogError($"[UguiResearchWindowController] No backend research matches '{binding.ResearchId}'.", this);
                    continue;
                }

                ApplyNodeVisual(binding.Button, node);
            }

            foreach (string missingId in _nodesById.Keys.Where(id => !seenIds.Contains(id)))
                Debug.LogError($"[UguiResearchWindowController] Research '{missingId}' has no authored node binding.", this);

            if (researchTooltip != null &&
                _nodesById.TryGetValue(researchTooltip.CurrentResearchId, out ResearchNodeDTO tooltipNode))
            {
                researchTooltip.Refresh(tooltipNode, tree);
            }
        }

        private static void ApplyNodeVisual(SkillTreeButton button, ResearchNodeDTO node)
        {
            button.SetTextOnLabel(node.Name);
            if (node.IsCompleted)
                button.SetUnlocked();
            else if (node.CanStart || node.IsResearching)
                button.SetAvailableToUnlock();
            else
                button.SetLockedBehindPredecessors();
        }

        private void HandleNodeActivated(SkillTreeButton button)
        {
            if (_bindingsByButton.TryGetValue(button, out ResearchNodeBinding binding))
                HandleStartRequested(binding.ResearchId);
        }

        private void HandleNodePointerEntered(SkillTreeButton button)
        {
            if (researchTooltip == null || _currentTree == null ||
                !_bindingsByButton.TryGetValue(button, out ResearchNodeBinding binding) ||
                !_nodesById.TryGetValue(binding.ResearchId, out ResearchNodeDTO node))
            {
                return;
            }

            researchTooltip.Show(binding.SkillTreeNode.RectTransform, node, _currentTree);
        }

        private void HandleNodePointerExited(SkillTreeButton button)
        {
            if (_bindingsByButton.TryGetValue(button, out ResearchNodeBinding binding))
                researchTooltip?.RequestHide(binding.ResearchId);
        }

        private void HandleStartRequested(string researchId)
        {
            if (_isCommandInFlight ||
                !_nodesById.TryGetValue(researchId, out ResearchNodeDTO node) ||
                !node.CanStart ||
                !TryGetNetworkContext(out NetworkManager network, out Guid worldPlayerId))
            {
                return;
            }

            int lifecycleVersion = _lifecycleVersion;
            _isCommandInFlight = true;

            StartCoroutine(network.Research.StartResearchProcess(
                worldPlayerId,
                researchId,
                network.JwtToken,
                (success, message) =>
                {
                    if (!isActiveAndEnabled || lifecycleVersion != _lifecycleVersion) return;

                    if (success)
                    {
                        LoadResearchData();
                    }
                    else
                    {
                        _isCommandInFlight = false;
                        Debug.LogError($"[UguiResearchWindowController] Could not start '{researchId}': {message}", this);
                    }
                }));
        }

        private void HandleEconomyStateChanged(WorldPlayerState state)
        {
            if (state == null ||
                WorldPlayerStateManager.Instance?.HasEconomyState != true ||
                _boundRate == null ||
                _isLoadInFlight ||
                _isCommandInFlight)
            {
                return;
            }

            bool changed = Math.Abs(state.BaseResearchPower - _boundRate.BaseResearchPower) > RateComparisonTolerance ||
                           Math.Abs(state.EffectiveResearchPower - _boundRate.EffectiveResearchPower) > RateComparisonTolerance ||
                           Math.Abs(state.ResearchSpeedMultiplier - _boundRate.SpeedMultiplier) > RateComparisonTolerance;
            if (changed) LoadResearchData();
        }

        private void ScheduleCompletionRefresh(ResearchTreeDTO tree)
        {
            StopCompletionRefresh();
            if (tree.ActiveJob?.ExpectedCompletionTime == null) return;

            double delaySeconds = (tree.ActiveJob.ExpectedCompletionTime.Value - tree.ServerTimeUtc).TotalSeconds;
            _completionRefreshCoroutine = StartCoroutine(RefreshAtCompletion(Math.Max(
                CompletionRetryDelaySeconds,
                delaySeconds)));
        }

        private IEnumerator RefreshAtCompletion(double delaySeconds)
        {
            yield return new WaitForSecondsRealtime((float)Math.Min(delaySeconds, float.MaxValue));
            _completionRefreshCoroutine = null;
            if (isActiveAndEnabled) LoadResearchData();
        }

        private void StopCompletionRefresh()
        {
            if (_completionRefreshCoroutine == null) return;
            StopCoroutine(_completionRefreshCoroutine);
            _completionRefreshCoroutine = null;
        }

        private static bool TryGetNetworkContext(out NetworkManager network, out Guid worldPlayerId)
        {
            network = NetworkManager.Instance;
            if (network != null && network.Research != null && Guid.TryParse(network.WorldPlayerId, out worldPlayerId))
                return true;

            worldPlayerId = Guid.Empty;
            Debug.LogError("[UguiResearchWindowController] No active world player is available.");
            return false;
        }
    }
}
