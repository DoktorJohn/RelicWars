using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.Graphs;


namespace Sunvale.AncientRomeUI.Editor.Graphs
{
    [CustomEditor(typeof(GraphAxisLabels))]
    public class GraphAxisLabelsEditor : UnityEditor.Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Creates pooled TextMeshPro labels around a GraphGridGraphic for bottom, left, and right axes. Adjust ranges and padding, then regenerate labels after layout changes.";

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

            GraphAxisLabels script = (GraphAxisLabels)target;

            GUILayout.Space(10);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fixedHeight = 35;

            if (GUILayout.Button("Generate Labels", buttonStyle))
            {
                script.GenerateLabels();

                EditorUtility.SetDirty(script);
                if (script.labelsContainer != null)
                {
                    EditorUtility.SetDirty(script.labelsContainer);
                }
            }
        }
    }

}
