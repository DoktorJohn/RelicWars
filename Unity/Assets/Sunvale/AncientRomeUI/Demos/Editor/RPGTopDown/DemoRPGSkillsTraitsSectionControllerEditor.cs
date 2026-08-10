using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.SkillTree;
using Sunvale.AncientRomeUI.Demos.RPGTopDown;


namespace Sunvale.AncientRomeUI.Demos.Editor.RPGTopDown
{
    [CustomEditor(typeof(DemoRPGSkillsTraitsSectionController))]
    public class DemoRPGSkillsTraitsSectionControllerEditor : UnityEditor.Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Demo controller for per-character skill and trait pages. Switches between skill tree versions, collects nodes, and updates unlock states from the selected character.";

        private void OnEnable()
        {
            packIcon = SunvaleInspectorDescription.LoadPackIcon();
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            SunvaleInspectorDescription.DrawHeaderIcon(packIcon);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(4);
            SunvaleInspectorDescription.DrawBox(packIcon, Description);
            EditorGUILayout.Space(6);

            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            DemoRPGSkillsTraitsSectionController section = (DemoRPGSkillsTraitsSectionController)target;

            if (GUILayout.Button("Ensure 3 Version Root Slots", GUILayout.Height(28)))
            {
                Undo.RecordObject(section, "Ensure Skill Tree Version Root Slots");
                section.EnsureDefaultVersionSlots();
                EditorUtility.SetDirty(section);
            }

            if (GUILayout.Button("Collect Nodes From Version Roots", GUILayout.Height(28)))
            {
                Undo.RecordObject(section, "Collect Skill Tree Nodes From Roots");

                section.CollectNodesFromVersionRoots();

                foreach (SkillTreeNode node in section.GetAllCollectedNodes())
                {
                    if (node == null)
                    {
                        continue;
                    }

                    Undo.RecordObject(node, "Assign Skill Tree Button");
                    node.AutoAssignButton();
                    EditorUtility.SetDirty(node);
                }

                EditorUtility.SetDirty(section);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);

                if (GUILayout.Button("Refresh Runtime Visual States", GUILayout.Height(24)))
                {
                    section.RefreshAllButtonStates(false);
                }

                if (GUILayout.Button("+1 Current Character Skill Point", GUILayout.Height(24)))
                {
                    section.AddSkillPoints(1);
                }

                if (GUILayout.Button("-1 Current Character Skill Point", GUILayout.Height(24)))
                {
                    section.ModifySkillPoints(-1);
                }

                if (GUILayout.Button("Reset All Tree Runtime States", GUILayout.Height(24)))
                {
                    section.ResetAllTreeRuntimeStatesFromEditorState();
                }
            }
        }
    }

}
