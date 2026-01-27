using UnityEngine;
using System;
using Project.Scripts.Domain.Enums;
using UnityEngine.Tilemaps;
using UnityEditor;

[CustomEditor(typeof(BiomeVisuals))]
public class BiomeVisualsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BiomeVisuals script = (BiomeVisuals)target;

        GUILayout.Space(20);
        if (GUILayout.Button("Auto-Map Tiles from Folder", GUILayout.Height(40)))
        {
            string path = EditorUtility.OpenFolderPanel("Vælg mappe med Tiles", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // Konverter absolut sti til Unity relativ sti
                path = "Assets" + path.Substring(Application.dataPath.Length);
                MapTiles(script, path);
            }
        }
    }

    private void MapTiles(BiomeVisuals registry, string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:TileBase", new[] { folderPath });
        registry.Biomes.Clear();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);

            // Prøv at matche filnavnet med en Enum værdi
            if (Enum.TryParse(tile.name, out WorldBiomeVariantType variantType))
            {
                registry.Biomes.Add(new BiomeVisuals.BiomeBinding
                {
                    Type = variantType,
                    TileAsset = tile
                });
            }
        }

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BiomeVisuals] Færdig! Mappede {registry.Biomes.Count} tiles.");
    }
}