using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.UI;


namespace Sunvale.Common.Editor.UI
{
    [CustomEditor(typeof(SafeZonePanelScaler))]
    public class SafeZonePanelScalerEditor : UnityEditor.Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "When resolutions and aspect ratios change, some UI elements can stretch, shrink, or follow their anchors in unwanted ways.\n\n" +
            "This component solves that problem for large important panels and windows. The panel scales up or down while remaining inside the designated safe zone.";

        private void OnEnable()
        {
            packIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Sunvale/Utils/Editor/S-64.png"
            );
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();

            if (packIcon == null)
                return;

            Rect headerRect = GUILayoutUtility.GetLastRect();

            const float iconSize = 20f;
            const float rightPadding = 34f;
            const float topPadding = 4f;

            Rect iconRect = new Rect(
                headerRect.xMax - rightPadding - iconSize,
                headerRect.y + topPadding,
                iconSize,
                iconSize
            );

            GUI.DrawTexture(iconRect, packIcon, ScaleMode.ScaleToFit, true);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(4);

            DrawDescriptionBox();

            EditorGUILayout.Space(6);

            DrawDefaultInspector();

            SafeZonePanelScaler scaler = (SafeZonePanelScaler)target;

            EditorGUILayout.Space(15);

            if (!scaler.isSetupValid)
            {
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);

                if (GUILayout.Button("BAKE 1080p SETUP", GUILayout.Height(40)))
                {
                    scaler.BakeSetup();
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.2f);

                if (GUILayout.Button("DISENGAGE / UNBAKE", GUILayout.Height(40)))
                {
                    scaler.UnbakeSetup();
                }
            }

            GUI.backgroundColor = Color.white;

            if (!scaler.isSetupValid)
            {
                EditorGUILayout.HelpBox(
                    "Set your Game View to 1920x1080, design your panel in absolute pixels, then click Bake. The script will reject bad setups.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Panel is locked and scaling. To edit the UI layout, click Disengage first.",
                    MessageType.Warning
                );
            }
        }

        private void DrawDescriptionBox()
        {
            GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox);

            GUIStyle textStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                richText = false
            };

            float viewWidth = EditorGUIUtility.currentViewWidth;
            float textWidth = viewWidth - 86f;
            float textHeight = textStyle.CalcHeight(new GUIContent(Description), textWidth);

            float height = Mathf.Max(66f, textHeight + 18f);

            Rect rect = EditorGUILayout.GetControlRect(
                false,
                height,
                GUILayout.ExpandWidth(true)
            );

            GUI.Box(rect, GUIContent.none, boxStyle);

            const float iconSize = 24f;

            Rect iconRect = new Rect(
                rect.x + 12f,
                rect.y + 12f,
                iconSize,
                iconSize
            );

            if (packIcon != null)
            {
                GUI.DrawTexture(iconRect, packIcon, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUIContent infoIcon = EditorGUIUtility.IconContent("console.infoicon");
                GUI.Label(iconRect, infoIcon);
            }

            Rect textRect = new Rect(
                rect.x + 46f,
                rect.y + 8f,
                rect.width - 56f,
                rect.height - 16f
            );

            EditorGUI.LabelField(textRect, Description, textStyle);
        }
    }

}
