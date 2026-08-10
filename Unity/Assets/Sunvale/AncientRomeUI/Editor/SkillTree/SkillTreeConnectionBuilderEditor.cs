using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.SkillTree;


namespace Sunvale.AncientRomeUI.Editor.SkillTree
{
    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(SkillTreeConnectionBuilder))]
    public class SkillTreeConnectionBuilderEditor : Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Editor builder for visual skill-tree connections. Collects SkillTreeNode children and creates styled Bezier, orthogonal, or diagonal SkillTreeConnectionGraphic graphics between prerequisites.";

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
            SkillTreeConnectionBuilder manager = (SkillTreeConnectionBuilder)target;

            EditorGUILayout.Space(4);
            SunvaleInspectorDescription.DrawBox(packIcon, Description);
            EditorGUILayout.Space(6);

            EditorGUILayout.Space(5);
            GUI.backgroundColor = manager.liveUpdateInEditor ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.3f);
            if (GUILayout.Button(manager.liveUpdateInEditor ? "🔴 LIVE UPDATE: ON" : "🟢 LIVE UPDATE: OFF", GUILayout.Height(35)))
            {
                Undo.RecordObject(manager, "Toggle Live Update");
                manager.liveUpdateInEditor = !manager.liveUpdateInEditor;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox(manager.liveUpdateInEditor 
                ? "Lines will instantly redraw when tweaking sliders or moving nodes. (Turn OFF before playing to save performance)" 
                : "Turn ON to instantly preview slider and transform changes.", MessageType.Info);
            
            EditorGUILayout.Space(10);

            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Collect All Nodes", GUILayout.Height(30)))
            {
                Undo.RecordObject(manager, "Collect Skill Tree Nodes");
                manager.CollectNodes();
                Debug.Log($"Collected {manager.nodes.Count} nodes.");
            }

            if (GUILayout.Button("2. Build Lines", GUILayout.Height(30)))
            {
                manager.BuildLines();
                Debug.Log("Skill tree lines built successfully.");
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear All Lines", GUILayout.Height(25)))
            {
                manager.ClearLines();
                Debug.Log("Skill tree lines cleared.");
            }
            GUI.backgroundColor = Color.white;
        }
    }

    #endif

}
