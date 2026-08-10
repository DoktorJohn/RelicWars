using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.SkillTree;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class DemoRPGSkillsTraitsSectionController : MonoBehaviour
    {
        [Serializable]
        public class SkillTreeVersionConfig
        {
            [Tooltip("Must match RPGCharacterData.skillTreeVersion.")]
            public int versionId;

            [Tooltip(
                "Root GameObject for this skill tree version. This object is activated/deactivated when the character changes.")]
            public GameObject versionRoot;

            [Header("Collected Nodes")]
            [Tooltip(
                "Filled by the editor collect button. Nodes are found by polling all children under versionRoot, including inactive children.")]
            public List<SkillTreeNode> nodes = new List<SkillTreeNode>();
        }

        [Header("Counter")] public TextMeshProUGUI skillTreeCounterTMP;

        [Header("StatSheet")] public DemoRPGCharacterStatSheetView statSheet;

        [Header("Skill Tree Versions")]
        [Tooltip("One entry per character/tree version. The versionId should match RPGCharacterData.skillTreeVersion.")]
        public List<SkillTreeVersionConfig> skillTreeVersions = new List<SkillTreeVersionConfig>(3);

        [Header("Runtime")]
        [Tooltip("If true, nodes only show Available when the current character also has enough points.")]
        public bool requirePointsForAvailableVisual = true;

        [Tooltip("If true, nodes copy startsUnlocked into runtime state the first time that version is opened.")]
        public bool resetNodeRuntimeStateWhenVersionFirstOpens = true;

        private readonly List<SkillTreeNode> allNodes = new List<SkillTreeNode>();
        private readonly List<SkillTreeNode> activeNodes = new List<SkillTreeNode>();

        private readonly Dictionary<SkillTreeButton, SkillTreeNode> nodeByButton =
            new Dictionary<SkillTreeButton, SkillTreeNode>();

        private readonly HashSet<SkillTreeButton> subscribedButtons = new HashSet<SkillTreeButton>();
        private readonly HashSet<int> initializedVersionIds = new HashSet<int>();

        private RPGCharacterData currentCharacter;
        private SkillTreeVersionConfig activeVersion;


        private void OnDisable()
        {
            UnsubscribeFromButtons();
        }

        public void InitializeForCharacter(RPGCharacterData character, RPGDemoController sceneWithData)
        {
            currentCharacter = character;
            statSheet.InitializeForCharacter(character, false);

            CollectNodesFromVersionRoots();

            int versionId = currentCharacter != null ? currentCharacter.skillTreeVersion : -1;
            activeVersion = FindVersionDefinition(versionId);


            SetOnlyActiveVersionRoot(activeVersion);
            BuildActiveRuntimeNodeCache();

            RefreshAllButtonStates(true);
            RefreshCounterText();
        }

        [ContextMenu("Collect Nodes From Version Roots")]
        public void CollectNodesFromVersionRoots()
        {
            if (skillTreeVersions == null)
            {
                return;
            }

            for (int i = 0; i < skillTreeVersions.Count; i++)
            {
                SkillTreeVersionConfig version = skillTreeVersions[i];

                if (version == null)
                {
                    continue;
                }

                version.nodes = CollectNodesFromRoot(version.versionRoot);
                AutoAssignButtons(version.nodes);
            }

            RebuildAllNodesCache();
        }

        public void EnsureDefaultVersionSlots()
        {
            const int versionCount = 3;

            if (skillTreeVersions == null)
            {
                skillTreeVersions = new List<SkillTreeVersionConfig>(versionCount);
            }

            while (skillTreeVersions.Count < versionCount)
            {
                skillTreeVersions.Add(new SkillTreeVersionConfig
                {
                    versionId = skillTreeVersions.Count
                });
            }

            for (int i = 0; i < skillTreeVersions.Count; i++)
            {
                if (skillTreeVersions[i] == null)
                {
                    skillTreeVersions[i] = new SkillTreeVersionConfig
                    {
                        versionId = i
                    };
                }
            }
        }

        private List<SkillTreeNode> CollectNodesFromRoot(GameObject root)
        {
            List<SkillTreeNode> result = new List<SkillTreeNode>();

            if (root == null)
            {
                return result;
            }

            SkillTreeNode[] foundNodes = root.GetComponentsInChildren<SkillTreeNode>(true);

            for (int i = 0; i < foundNodes.Length; i++)
            {
                SkillTreeNode node = foundNodes[i];

                if (node != null && !result.Contains(node))
                {
                    result.Add(node);
                    var name = node.myButton.tmpLabel.text;
                    node.gameObject.name = name;
    #if UNITY_EDITOR
                    EditorUtility.SetDirty(node);
    #endif
                }
            }

            return result;
        }

        private void AutoAssignButtons(List<SkillTreeNode> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                {
                    nodes[i].AutoAssignButton();
                }
            }
        }

        public IEnumerable<SkillTreeNode> GetAllCollectedNodes()
        {
            RebuildAllNodesCache();

            for (int i = 0; i < allNodes.Count; i++)
            {
                yield return allNodes[i];
            }
        }

        private void RebuildAllNodesCache()
        {
            allNodes.Clear();

            if (skillTreeVersions == null)
            {
                return;
            }

            for (int i = 0; i < skillTreeVersions.Count; i++)
            {
                SkillTreeVersionConfig version = skillTreeVersions[i];

                if (version == null)
                {
                    continue;
                }

                AddUniqueNodes(version.nodes, allNodes);
            }
        }

        private void RebuildActiveNodesCache()
        {
            activeNodes.Clear();

            if (activeVersion == null)
            {
                return;
            }

            AddUniqueNodes(activeVersion.nodes, activeNodes);
        }

        private void AddUniqueNodes(List<SkillTreeNode> source, List<SkillTreeNode> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                SkillTreeNode node = source[i];

                if (node != null && !target.Contains(node))
                {
                    target.Add(node);
                }
            }
        }

        private SkillTreeVersionConfig FindVersionDefinition(int versionId)
        {
            for (int i = 0; i < skillTreeVersions.Count; i++)
            {
                SkillTreeVersionConfig version = skillTreeVersions[i];

                if (version != null && version.versionId == versionId)
                {
                    return version;
                }
            }

            // Fallback: lets you use skillTreeVersion as the list index.
            if (versionId >= 0 && versionId < skillTreeVersions.Count)
            {
                return skillTreeVersions[versionId];
            }

            return null;
        }

        private void SetOnlyActiveVersionRoot(SkillTreeVersionConfig versionToActivate)
        {
            if (skillTreeVersions == null)
            {
                return;
            }

            for (int i = 0; i < skillTreeVersions.Count; i++)
            {
                SkillTreeVersionConfig version = skillTreeVersions[i];
                bool shouldBeActive = version != null && version == versionToActivate;

                SetVersionRootActive(version, shouldBeActive);
            }
        }

        private void DeactivateAllVersionRoots()
        {
            if (skillTreeVersions == null)
            {
                return;
            }

            for (int i = 0; i < skillTreeVersions.Count; i++)
            {
                SetVersionRootActive(skillTreeVersions[i], false);
            }
        }

        private void SetVersionRootActive(SkillTreeVersionConfig version, bool active)
        {
            version.versionRoot.SetActive(active);
        }

        private void BuildActiveRuntimeNodeCache()
        {
            UnsubscribeFromButtons();
            nodeByButton.Clear();
            RebuildActiveNodesCache();

            bool shouldResetNodes = activeVersion != null
                                    && resetNodeRuntimeStateWhenVersionFirstOpens
                                    && !initializedVersionIds.Contains(activeVersion.versionId);

            for (int i = 0; i < activeNodes.Count; i++)
            {
                SkillTreeNode node = activeNodes[i];

                if (node == null)
                {
                    continue;
                }

                node.AutoAssignButton();

                if (shouldResetNodes)
                {
                    node.ResetRuntimeStateFromEditorState();
                }

                RegisterNodeButton(node);
            }

            if (activeVersion != null)
            {
                initializedVersionIds.Add(activeVersion.versionId);
            }
        }

        private void RegisterNodeButton(SkillTreeNode node)
        {
            if (node == null || node.myButton == null)
            {
                return;
            }

            nodeByButton[node.myButton] = node;

            if (subscribedButtons.Add(node.myButton))
            {
                node.myButton.OnButtonActivatedClicked += HandleButtonActivatedClicked;
            }
        }

        private void UnsubscribeFromButtons()
        {
            foreach (SkillTreeButton button in subscribedButtons)
            {
                if (button != null)
                {
                    button.OnButtonActivatedClicked -= HandleButtonActivatedClicked;
                }
            }

            subscribedButtons.Clear();
        }

        private void HandleButtonActivatedClicked(SkillTreeButton button)
        {
            if (button == null)
            {
                return;
            }

            if (!nodeByButton.TryGetValue(button, out SkillTreeNode node))
            {
                return;
            }

            TryUnlockNode(node);
        }

        public bool TryUnlockNode(SkillTreeNode node)
        {
            if (currentCharacter == null || node == null)
            {
                return false;
            }

            if (!activeNodes.Contains(node))
            {
                return false;
            }

            if (node.isUnlocked)
            {
                return false;
            }

            if (!node.ArePredecessorsUnlocked())
            {
                return false;
            }

            int cost = Mathf.Max(0, node.skillPointCost);

            if (currentCharacter.skillTreePoints < cost)
            {
                return false;
            }

            currentCharacter.skillTreePoints -= cost;
            node.isUnlocked = true;

            RefreshAllButtonStates(false);
            RefreshCounterText();

            return true;
        }

        public void AddSkillPoints(int amount)
        {
            ModifySkillPoints(Mathf.Max(0, amount));
        }

        public void ModifySkillPoints(int amount)
        {
            if (currentCharacter == null)
            {
                return;
            }

            currentCharacter.skillTreePoints = Mathf.Max(0, currentCharacter.skillTreePoints + amount);
            RefreshAllButtonStates(false);
            RefreshCounterText();
        }

        public void SetSkillPoints(int amount)
        {
            if (currentCharacter == null)
            {
                return;
            }

            currentCharacter.skillTreePoints = Mathf.Max(0, amount);
            RefreshAllButtonStates(false);
            RefreshCounterText();
        }

        public int GetCurrentSkillPoints()
        {
            return currentCharacter != null ? Mathf.Max(0, currentCharacter.skillTreePoints) : 0;
        }

        public void ResetAllTreeRuntimeStatesFromEditorState()
        {
            initializedVersionIds.Clear();
            CollectNodesFromVersionRoots();

            for (int i = 0; i < allNodes.Count; i++)
            {
                SkillTreeNode node = allNodes[i];

                if (node != null)
                {
                    node.ResetRuntimeStateFromEditorState();
                }
            }

            BuildActiveRuntimeNodeCache();
            RefreshAllButtonStates(true);
            RefreshCounterText();
        }

        public void RefreshAllButtonStates(bool instant)
        {
            RebuildActiveNodesCache();

            for (int i = 0; i < activeNodes.Count; i++)
            {
                RefreshButtonState(activeNodes[i], instant);
            }
        }

        private void RefreshButtonState(SkillTreeNode node, bool instant)
        {
            if (node == null || node.myButton == null)
            {
                return;
            }

            if (node.isUnlocked)
            {
                node.myButton.SetVisualState(SkillTreeButton.SkillTreeButtonVisualState.Unlocked, instant);
                return;
            }

            if (IsNodeAvailable(node))
            {
                node.myButton.SetVisualState(SkillTreeButton.SkillTreeButtonVisualState.AvailableToUnlock, instant);
                return;
            }

            node.myButton.SetVisualState(SkillTreeButton.SkillTreeButtonVisualState.LockedBehindPredecessors, instant);
        }

        private bool IsNodeAvailable(SkillTreeNode node)
        {
            if (currentCharacter == null || node == null)
            {
                return false;
            }

            if (node.isUnlocked)
            {
                return false;
            }

            if (!node.ArePredecessorsUnlocked())
            {
                return false;
            }

            if (requirePointsForAvailableVisual)
            {
                int cost = Mathf.Max(0, node.skillPointCost);

                if (currentCharacter.skillTreePoints < cost)
                {
                    return false;
                }
            }

            return true;
        }

        private void RefreshCounterText()
        {
            string s = "Skill points: " + currentCharacter.skillTreePoints.ToString();
            skillTreeCounterTMP.SetText(s);
        }
    }

}
