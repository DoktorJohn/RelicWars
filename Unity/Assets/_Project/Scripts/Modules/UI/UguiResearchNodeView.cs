using System;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.SkillTree;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiResearchNodeView : MonoBehaviour
    {
        [SerializeField] private string researchId;
        [SerializeField] private SkillTreeNode skillTreeNode;
        [SerializeField] private SkillTreeButton skillTreeButton;

        private bool _canRequestStart;
        private bool _interactionBlocked;

        public string ResearchId => researchId;
        public SkillTreeNode SkillTreeNode => skillTreeNode;
        public event Action<string> StartRequested;

        private void OnEnable()
        {
            ResolveReferences();
            if (skillTreeButton != null)
                skillTreeButton.OnButtonActivatedClicked += HandleButtonActivated;
        }

        private void OnDisable()
        {
            if (skillTreeButton != null)
                skillTreeButton.OnButtonActivatedClicked -= HandleButtonActivated;
        }

        public void Bind(ResearchNodeDTO node)
        {
            ResolveReferences();
            if (node == null || skillTreeButton == null) return;

            skillTreeButton.SetTextOnLabel(node.Name);
            _canRequestStart = node.CanStart;

            if (node.IsCompleted)
            {
                skillTreeButton.SetUnlocked();
            }
            else if (node.CanStart)
            {
                skillTreeButton.SetAvailableToUnlock();
            }
            else if (node.IsResearching)
            {
                skillTreeButton.SetAvailableToUnlock();
            }
            else
            {
                skillTreeButton.SetLockedBehindPredecessors();
            }
        }

        public void SetInteractionBlocked(bool blocked) => _interactionBlocked = blocked;

        private void HandleButtonActivated(SkillTreeButton _)
        {
            if (_interactionBlocked || !_canRequestStart || string.IsNullOrWhiteSpace(researchId)) return;
            StartRequested?.Invoke(researchId);
        }

        private void ResolveReferences()
        {
            skillTreeNode ??= GetComponent<SkillTreeNode>();
            skillTreeButton ??= skillTreeNode != null ? skillTreeNode.myButton : null;
            skillTreeButton ??= GetComponentInChildren<SkillTreeButton>(true);
        }

        public void ConfigureRuntime(string id, SkillTreeNode node)
        {
            if (!string.IsNullOrWhiteSpace(id)) researchId = id;
            skillTreeNode = node;
            skillTreeButton = node != null ? node.myButton : null;
            if (skillTreeButton == null && node != null)
                skillTreeButton = node.GetComponentInChildren<SkillTreeButton>(true);
        }

#if UNITY_EDITOR
        public void Configure(string id, SkillTreeNode node)
        {
            ConfigureRuntime(id, node);
        }
#endif
    }
}
