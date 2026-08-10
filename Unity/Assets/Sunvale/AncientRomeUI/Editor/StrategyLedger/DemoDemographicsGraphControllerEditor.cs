using UnityEditor;
using UnityEngine;
using Sunvale.AncientRomeUI.Demos.StrategyLedger;

namespace Sunvale.AncientRomeUI.Editor.StrategyLedger
{
    [CustomEditor(typeof(DemoDemographicsGraphController))]
    public class DemoDemographicsGraphControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DemoDemographicsGraphController controller = (DemoDemographicsGraphController)target;

            EditorGUI.BeginChangeCheck();
            
            // Draw all the default serialized fields (including autoUpdatePreview)
            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                if (controller.autoUpdatePreview)
                {
                    controller.PreviewMockData();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Mock Data Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use these buttons and the arrays above to preview the graph in the Editor without entering Play Mode. The arrays map dynamically, meaning if you provide just 5 points, they will distribute smoothly across the entire graph.", MessageType.Info);

            GUILayout.BeginHorizontal();
            
            // Button to explicitly generate the data
            if (GUILayout.Button("Generate Mock Data", GUILayout.Height(30)))
            {
                controller.PreviewMockData();
                EditorUtility.SetDirty(controller); // Force Unity to recognize the changes
            }
            
            // Button to clear the data to save cleanly
            if (GUILayout.Button("Clear Preview", GUILayout.Height(30)))
            {
                controller.ClearPreview();
                EditorUtility.SetDirty(controller);
            }
            
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
    }
}
