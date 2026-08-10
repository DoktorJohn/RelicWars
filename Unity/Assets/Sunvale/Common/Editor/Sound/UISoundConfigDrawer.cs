using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;


namespace Sunvale.Common.Editor.Sound
{
    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(UISoundConfig))]
    public class UISoundConfigDrawer : PropertyDrawer
    {
        private static AudioSource previewSource;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Foldout + 5 fields + preview button.
            return (line * 7f) + (spacing * 6f) + 6f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect foldoutRect = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty playSoundProp = property.FindPropertyRelative("playSound");
            SerializedProperty clipProp = property.FindPropertyRelative("soundClip");
            SerializedProperty randomizeProp = property.FindPropertyRelative("randomizePitch");
            SerializedProperty volumeProp = property.FindPropertyRelative("baseVolume");
            SerializedProperty pitchProp = property.FindPropertyRelative("basePitch");

            EditorGUI.indentLevel++;

            float y = foldoutRect.yMax + spacing;

            DrawField(position, playSoundProp, ref y, line, spacing);

            using (new EditorGUI.DisabledScope(!playSoundProp.boolValue))
            {
                DrawField(position, clipProp, ref y, line, spacing);
                DrawField(position, randomizeProp, ref y, line, spacing);
                DrawField(position, volumeProp, ref y, line, spacing);
                DrawField(position, pitchProp, ref y, line, spacing);
            }

            y += 4f;

            Rect buttonRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                y,
                position.width - EditorGUIUtility.labelWidth,
                line
            );

            bool canPreview = playSoundProp.boolValue && clipProp.objectReferenceValue != null;

            using (new EditorGUI.DisabledScope(!canPreview))
            {
                if (GUI.Button(buttonRect, "▶ Preview Sound"))
                {
                    PlayPreview(
                        (AudioClip)clipProp.objectReferenceValue,
                        randomizeProp.boolValue,
                        volumeProp.floatValue,
                        pitchProp.floatValue
                    );
                }
            }

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        private static void DrawField(
            Rect position,
            SerializedProperty prop,
            ref float y,
            float line,
            float spacing)
        {
            Rect rect = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(rect, prop);
            y += line + spacing;
        }

        private static void PlayPreview(AudioClip clip, bool randomize, float volume, float pitch)
        {
            if (clip == null)
                return;

            if (previewSource == null)
            {
                GameObject editorAudioObj = EditorUtility.CreateGameObjectWithHideFlags(
                    "Hidden GUI Sound Preview",
                    HideFlags.HideAndDontSave,
                    typeof(AudioSource)
                );

                previewSource = editorAudioObj.GetComponent<AudioSource>();
            }

            previewSource.Stop();

            float finalPitch = pitch;

            if (randomize)
                finalPitch *= Random.Range(0.92f, 1.08f);

            previewSource.pitch = finalPitch;
            previewSource.PlayOneShot(
                clip,
                volume * SimpleSoundManager.GlobalVolume
            );
        }
    }
    #endif
}
