using UnityEngine.Scripting.APIUpdating;
using Sunvale.AncientRomeUI.Graphics;


namespace Sunvale.AncientRomeUI.Editor.Graphics
{
    #if UNITY_EDITOR
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(DrapeGraphic))]
    public class DrapeGraphicEditor : Editor
    {
        private Texture2D packIcon;

        private const string Description =
            "Generates a deformable UI mesh for drapes and banners, then animates it with lightweight wind movement.\n\n" +
            "Keep animated drapes in their own nested Canvas because the mesh is dirtied every frame.";

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

            DrapeGraphic drape = (DrapeGraphic)target;

            EditorGUILayout.Space(10);

            Color oldBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button("SPAWN WIND GUST", GUILayout.Height(40)))
            {
                drape.TriggerManualGust();
            }

            GUI.backgroundColor = oldBackgroundColor;
        }

        private void OnSceneGUI()
        {
            DrapeGraphic drape = (DrapeGraphic)target;

            if (drape.boneOffsets == null || drape.boneOffsets.Length == 0)
                return;

            if (drape.rectTransform == null)
                return;

            Rect r = drape.rectTransform.rect;
            float centerX = r.center.x;
            Vector3 previousWorldPos = Vector3.zero;

            Handles.color = Color.yellow;

            for (int i = 0; i < drape.boneOffsets.Length; i++)
            {
                float t = drape.boneOffsets.Length == 1
                    ? 0f
                    : (float)i / (drape.boneOffsets.Length - 1);

                float localY = Mathf.Lerp(r.yMax, r.yMin, t);
                float localX = centerX + drape.boneOffsets[i];

                Vector3 localPos = new Vector3(localX, localY, 0f);
                Vector3 worldPos = drape.transform.TransformPoint(localPos);

                if (i > 0)
                {
                    Handles.DrawLine(previousWorldPos, worldPos);
                }

                previousWorldPos = worldPos;

                float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.15f;

                EditorGUI.BeginChangeCheck();

                Vector3 newWorldPos = Handles.Slider(
                    worldPos,
                    drape.transform.right,
                    handleSize,
                    Handles.SphereHandleCap,
                    0.1f
                );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(drape, "Move Drape Bone");

                    Vector3 newLocalPos = drape.transform.InverseTransformPoint(newWorldPos);
                    drape.boneOffsets[i] = newLocalPos.x - centerX;

                    drape.SetVerticesDirty();
                    EditorUtility.SetDirty(drape);
                }
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
    #endif

}
