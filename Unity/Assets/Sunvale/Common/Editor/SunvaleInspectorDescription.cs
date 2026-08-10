using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.Editor
{
    internal static class SunvaleInspectorDescription
    {
        private const string PackIconPath = "Assets/Sunvale/Utils/Editor/S-64.png";

        public static Texture2D LoadPackIcon()
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(PackIconPath);
        }

        public static void DrawHeaderIcon(Texture2D packIcon)
        {
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

        public static void DrawBox(Texture2D packIcon, string description)
        {
            GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox);

            GUIStyle textStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                richText = false
            };

            float viewWidth = EditorGUIUtility.currentViewWidth;
            float textWidth = viewWidth - 86f;
            float textHeight = textStyle.CalcHeight(new GUIContent(description), textWidth);

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

            EditorGUI.LabelField(textRect, description, textStyle);
        }
    }

}
