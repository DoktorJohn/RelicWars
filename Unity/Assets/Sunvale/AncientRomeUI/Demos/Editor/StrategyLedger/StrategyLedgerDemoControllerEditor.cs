using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.Demos.StrategyLedger;


namespace Sunvale.AncientRomeUI.Demos.Editor.StrategyLedger
{
    [CustomEditor(typeof(StrategyLedgerDemoController))]
    public class StrategyLedgerDemoControllerEditor : UnityEditor.Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Demo ledger data source for the strategy table scene. Generates fictional Roman city, government, empire, and military data used by the demo panels.";

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

            StrategyLedgerDemoController viewer = (StrategyLedgerDemoController)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Make Fake Cities Data Set"))
            {
                Undo.RecordObject(viewer, "Make Fake Cities Data Set");
                viewer.MakeFakeCitiesDataSet();
                EditorUtility.SetDirty(viewer);
            }
        }
    }

}
