using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.UI;


namespace Sunvale.Common.Editor.UI
{
    [CustomEditor(typeof(CircularRaycastFilter))]
    public class CircularRaycastFilterEditor : UnityEditor.Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Unity UI raycasts against rectangles, not circles or custom shapes, so circular buttons can click and hover when the mouse is outside their visible bounds.\n\n" +
            "This component solves that problem: it runs custom raycast code to determine if the raycast hits it, and with default settings it acts as a circle.\n\n" +
            "Disable raycast target on everything else on your buttons, for it to work as intended.";

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
