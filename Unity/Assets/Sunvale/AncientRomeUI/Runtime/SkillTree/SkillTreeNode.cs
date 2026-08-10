using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.SkillTree
{
    [RequireComponent(typeof(RectTransform))]
    public class SkillTreeNode : MonoBehaviour
    {
        [Header("Core References")]
        [Tooltip("Assigned automatically when lines are built.")]
        public SkillTreeConnectionBuilder manager;

        public SkillTreeButton myButton;

        [Header("Demo Runtime State")]
        [Tooltip("Initial state for the demo skill tree. This is copied into runtime state during Initialize().")]
        public bool startsUnlocked = false;

        [Min(0)]
        public int skillPointCost = 1;

        [NonSerialized]
        public bool isUnlocked;

        [Header("Tree Layout")]
        [Tooltip("Other bubbles/nodes that unlock this one.")]
        public List<SkillTreeNode> predecessors = new List<SkillTreeNode>();

        [Tooltip("Check if paths to this node should be rendered with dashed exclusive styling.")]
        public bool isExclusiveConnection;

        [Header("Outgoing Line Styling")]
        [Tooltip("If greater than 0, forces all outgoing orthogonal lines to bend exactly this many pixels away, ensuring perfect overlaps for multiple children.")]
        public float fixedShoulderOffset = 0f;

        [Header("Allowed Connection Ports")]
        public bool allowLeft = false;
        public bool allowRight = false;
        public bool allowTop = true;
        public bool allowBottom = true;

        [Header("Connection Insets")]
        [Tooltip("Positive values push the connection point INWARDS. Uses standard Canvas Pixels.")]
        public float insetLeft = 0f;
        public float insetRight = 0f;
        public float insetTop = 0f;
        public float insetBottom = 0f;

        public RectTransform RectTransform => (RectTransform)transform;

        private void Reset()
        {
            AutoAssignButton();
        }

        private void OnValidate()
        {
            AutoAssignButton();
        }

        public void AutoAssignButton()
        {
            if (myButton == null)
            {
                myButton = GetComponentInChildren<SkillTreeButton>(true);
            }
        }

        public void ResetRuntimeStateFromEditorState()
        {
            isUnlocked = startsUnlocked;
        }

        public bool ArePredecessorsUnlocked()
        {
            for (int i = 0; i < predecessors.Count; i++)
            {
                SkillTreeNode predecessor = predecessors[i];

                if (predecessor == null)
                {
                    return false;
                }

                if (!predecessor.isUnlocked)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
