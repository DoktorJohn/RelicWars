using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.Demos.RPGTopDown;


namespace Sunvale.AncientRomeUI.Demos.Editor.RPGTopDown
{
    [CustomEditor(typeof(RPGItemDefinitionSO))]
    [CanEditMultipleObjects]
    public class RPGItemDefinitionSOEditor : UnityEditor.Editor
    {
            private Texture2D packIcon;

            private const string Description =
                    "ScriptableObject demo item definition with type, icon, sounds, and stat buffs. The preview helps tune how each sprite sits inside inventory and equipment slots.";

            private const float PreviewSlotSize = 100f;

            private SerializedProperty itemIconSlotScaleProp;
            private SerializedProperty itemIconXOffsetProp;
            private SerializedProperty itemIconYOffsetProp;

            private void OnEnable()
            {
                    packIcon = SunvaleInspectorDescription.LoadPackIcon();

                    itemIconSlotScaleProp = serializedObject.FindProperty(nameof(RPGItemDefinitionSO.itemIconSlotScale));
                    itemIconXOffsetProp = serializedObject.FindProperty(nameof(RPGItemDefinitionSO.itemIconXOffset));
                    itemIconYOffsetProp = serializedObject.FindProperty(nameof(RPGItemDefinitionSO.itemIconYOffset));
            }

            protected override void OnHeaderGUI()
            {
                    base.OnHeaderGUI();
                    SunvaleInspectorDescription.DrawHeaderIcon(packIcon);
            }

            public override void OnInspectorGUI()
            {
                    serializedObject.Update();

                    EditorGUILayout.Space(4);
                    SunvaleInspectorDescription.DrawBox(packIcon, Description);
                    EditorGUILayout.Space(6);

                    DrawPropertiesExcluding(
                            serializedObject,
                            "m_Script",
                            nameof(RPGItemDefinitionSO.itemIconSlotScale),
                            nameof(RPGItemDefinitionSO.itemIconXOffset),
                            nameof(RPGItemDefinitionSO.itemIconYOffset)
                    );

                    GUILayout.Space(10);
                    DrawIconPreviewAndControls();

                    GUILayout.Space(10);

                    if (GUILayout.Button("Setup Icon Offsets / Type Defaults", GUILayout.Height(28)))
                    {
                            foreach (Object selectedTarget in targets)
                            {
                                    RPGItemDefinitionSO item = selectedTarget as RPGItemDefinitionSO;

                                    if (item == null)
                                            continue;

                                    Undo.RecordObject(item, "Setup RPG Item Icon Offsets");

                                    item.SetupIconOffsetsEtc();

                                    EditorUtility.SetDirty(item);
                            }

                            AssetDatabase.SaveAssets();

                            serializedObject.Update();
                    }

                    serializedObject.ApplyModifiedProperties();
            }

            private void DrawIconPreviewAndControls()
            {
                    EditorGUILayout.LabelField("Icon Slot Preview", EditorStyles.boldLabel);

                    EditorGUILayout.HelpBox(
                            "Preview is drawn inside a 100x100 slot. The sprite keeps its aspect ratio and is clipped by the preview slot.",
                            MessageType.Info
                    );

                    DrawPreviewSlot();

                    GUILayout.Space(8);

                    EditorGUILayout.Slider(itemIconSlotScaleProp, 0.1f, 3f, "Icon Scale");
                    EditorGUILayout.Slider(itemIconXOffsetProp, -100f, 100f, "Icon X Offset");
                    EditorGUILayout.Slider(itemIconYOffsetProp, -100f, 100f, "Icon Y Offset");

                    GUILayout.Space(4);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                            if (GUILayout.Button("Center Icon"))
                            {
                                    itemIconXOffsetProp.floatValue = 0f;
                                    itemIconYOffsetProp.floatValue = 0f;
                            }

                            if (GUILayout.Button("Reset Scale"))
                            {
                                    itemIconSlotScaleProp.floatValue = 1f;
                            }
                    }
            }

            private void DrawPreviewSlot()
            {
                    RPGItemDefinitionSO item = target as RPGItemDefinitionSO;

                    Rect outerRect = GUILayoutUtility.GetRect(
                            PreviewSlotSize,
                            PreviewSlotSize,
                            GUILayout.ExpandWidth(false)
                    );

                    outerRect.x = EditorGUIUtility.currentViewWidth * 0.5f - PreviewSlotSize * 0.5f;

                    DrawSlotBackground(outerRect);

                    if (item == null || item.itemSprite == null)
                    {
                            GUI.Label(outerRect, "No Sprite", EditorStyles.centeredGreyMiniLabel);
                            return;
                    }

                    DrawSpriteInSlot(
                            outerRect,
                            item.itemSprite,
                            item.itemIconSlotScale,
                            item.itemIconXOffset,
                            item.itemIconYOffset
                    );
            }

            private static void DrawSlotBackground(Rect slotRect)
            {
                    EditorGUI.DrawRect(slotRect, new Color(0.09f, 0.025f, 0.02f, 1f));

                    Handles.BeginGUI();

                    Color oldColor = Handles.color;

                    Handles.color = new Color(0.95f, 0.68f, 0.22f, 1f);
                    Handles.DrawAAPolyLine(
                            2f,
                            new Vector3(slotRect.xMin, slotRect.yMin),
                            new Vector3(slotRect.xMax, slotRect.yMin),
                            new Vector3(slotRect.xMax, slotRect.yMax),
                            new Vector3(slotRect.xMin, slotRect.yMax),
                            new Vector3(slotRect.xMin, slotRect.yMin)
                    );

                    Rect innerRect = new Rect(
                            slotRect.x + 5f,
                            slotRect.y + 5f,
                            slotRect.width - 10f,
                            slotRect.height - 10f
                    );

                    Handles.color = new Color(0.25f, 0.11f, 0.035f, 1f);
                    Handles.DrawAAPolyLine(
                            1f,
                            new Vector3(innerRect.xMin, innerRect.yMin),
                            new Vector3(innerRect.xMax, innerRect.yMin),
                            new Vector3(innerRect.xMax, innerRect.yMax),
                            new Vector3(innerRect.xMin, innerRect.yMax),
                            new Vector3(innerRect.xMin, innerRect.yMin)
                    );

                    Handles.color = oldColor;
                    Handles.EndGUI();
            }

            private static void DrawSpriteInSlot(
                    Rect slotRect,
                    Sprite sprite,
                    float scale,
                    float xOffset,
                    float yOffset
            )
            {
                    if (sprite == null || sprite.texture == null)
                            return;

                    Texture2D texture = sprite.texture;
                    Rect textureRect = sprite.textureRect;

                    Rect uvRect = new Rect(
                            textureRect.x / texture.width,
                            textureRect.y / texture.height,
                            textureRect.width / texture.width,
                            textureRect.height / texture.height
                    );

                    float spriteWidth = textureRect.width;
                    float spriteHeight = textureRect.height;

                    if (spriteWidth <= 0f || spriteHeight <= 0f)
                            return;

                    float aspect = spriteWidth / spriteHeight;

                    float drawWidth;
                    float drawHeight;

                    if (aspect >= 1f)
                    {
                            drawWidth = PreviewSlotSize;
                            drawHeight = PreviewSlotSize / aspect;
                    }
                    else
                    {
                            drawHeight = PreviewSlotSize;
                            drawWidth = PreviewSlotSize * aspect;
                    }

                    drawWidth *= Mathf.Max(0.01f, scale);
                    drawHeight *= Mathf.Max(0.01f, scale);

                    Rect localDrawRect = new Rect(
                            PreviewSlotSize * 0.5f - drawWidth * 0.5f + xOffset,
                            PreviewSlotSize * 0.5f - drawHeight * 0.5f - yOffset,
                            drawWidth,
                            drawHeight
                    );

                    GUI.BeginGroup(slotRect);
                    GUI.DrawTextureWithTexCoords(localDrawRect, texture, uvRect, true);
                    GUI.EndGroup();
            }
    }

}
