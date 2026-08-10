#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sunvale.AncientRomeUI.EditorTools
{
    [InitializeOnLoad]
    public static class SunvaleTMPSetupPrompt
    {
        private const string AlreadyCheckedSessionKey =
            "Sunvale_AncientRomeUI_TMPAlreadyChecked";

        private const string PromptShownSessionKey =
            "Sunvale_AncientRomeUI_TMPPromptShown";

        private const string TMPSettingsPath =
            "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static SunvaleTMPSetupPrompt()
        {
            if (SessionState.GetBool(AlreadyCheckedSessionKey, false))
                return;

            EditorApplication.update -= WaitUntilEditorReady;
            EditorApplication.update += WaitUntilEditorReady;
        }

        private static void WaitUntilEditorReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= WaitUntilEditorReady;

            CheckTMPSetupOnce();
        }

        private static void CheckTMPSetupOnce()
        {
            SessionState.SetBool(AlreadyCheckedSessionKey, true);

            Object tmpSettings = AssetDatabase.LoadAssetAtPath<Object>(TMPSettingsPath);

            if (tmpSettings != null)
                return;

            if (SessionState.GetBool(PromptShownSessionKey, false))
                return;

            SessionState.SetBool(PromptShownSessionKey, true);

            bool import = EditorUtility.DisplayDialog(
                "TextMesh Pro setup required",
                "Sunvale Ancient Rome UI uses TextMesh Pro. Please import TMP Essential Resources before opening or playing the demo scenes.",
                "Import TMP Essentials",
                "Later"
            );

            if (import)
            {
                EditorApplication.ExecuteMenuItem(
                    "Window/TextMeshPro/Import TMP Essential Resources"
                );
            }
        }
    }
}
#endif