using UnityEngine;
using System.IO;
using UnityEditor;

public class BulkRenamer : EditorWindow
{
    private string _baseName = "NewName";
    private int _startIndex = 1;

    [MenuItem("Tools/World/Bulk Renamer")]
    public static void ShowWindow()
    {
        GetWindow<BulkRenamer>("Bulk Renamer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Rename Selected Assets", EditorStyles.boldLabel);

        _baseName = EditorGUILayout.TextField("Base Name (e.g. Desert)", _baseName);
        _startIndex = EditorGUILayout.IntField("Start Index", _startIndex);

        GUILayout.Space(10);

        if (GUILayout.Button("Rename Selected Files", GUILayout.Height(30)))
        {
            RenameSelected();
        }

        GUILayout.Label("Instruction: Select the files in Project view first.", EditorStyles.helpBox);
    }

    private void RenameSelected()
    {
        // Hent alle valgte objekter i Project-vinduet
        Object[] selectedObjects = Selection.objects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("[BulkRenamer] Ingen filer valgt!");
            return;
        }

        int count = _startIndex;

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            string directory = Path.GetDirectoryName(path);

            // Format: BaseName_Number (f.eks. Plains_1)
            string newName = $"{_baseName}_{count}";

            // Unitys AssetDatabase sørger for at .meta filer følger med korrekt
            string error = AssetDatabase.RenameAsset(path, newName);

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[BulkRenamer] Fejl ved omdøbning af {obj.name}: {error}");
            }

            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BulkRenamer] Færdig! Omdøbte {selectedObjects.Length} filer.");
    }
}