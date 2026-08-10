using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.Demos.RPGTopDown;


namespace Sunvale.AncientRomeUI.Demos.Editor.RPGTopDown
{
    [CustomEditor(typeof(InventoryPointerLineGraphic))]
    public class InventoryPointerLineGraphicEditor : UnityEditor.Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Draws a crisp UI line through child RectTransform nodes for inventory callouts and pointer paths. Use the editor buttons to collect child nodes and refresh the mesh.";

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
            InventoryPointerLineGraphic line = (InventoryPointerLineGraphic)target;
            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Bake Line Mesh", GUILayout.Height(30)))
            {
                line.BakeMesh();
                EditorUtility.SetDirty(line);
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(5);
            if (GUILayout.Button("Collect & Rename Children", GUILayout.Height(30)))
            {
                Undo.RecordObject(line, "Collect Line Nodes");
                line.CollectNodesFromChildren();
                EditorUtility.SetDirty(line);
            }
        }
    }

}
