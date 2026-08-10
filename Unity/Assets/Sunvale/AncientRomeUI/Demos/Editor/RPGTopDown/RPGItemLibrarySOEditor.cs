using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Editor;
using Sunvale.AncientRomeUI.Demos.RPGTopDown;


namespace Sunvale.AncientRomeUI.Demos.Editor.RPGTopDown
{
    [CustomEditor(typeof(RPGItemLibrarySO))]
    [CanEditMultipleObjects]
    public class RPGItemLibrarySOEditor : UnityEditor.Editor
    {
            private Texture2D packIcon;

            private const string Description =
                    "Categorized registry for RPGItemDefinitionSO assets used by the RPG demo inventory. The collect button scans the project and fills per-type item lists.";

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

                    GUILayout.Space(10);

                    if (GUILayout.Button("Collect All RPG Items In Project", GUILayout.Height(30)))
                    {
                            foreach (Object selectedTarget in targets)
                            {
                                    RPGItemLibrarySO itemLibrary = selectedTarget as RPGItemLibrarySO;

                                    if (itemLibrary == null)
                                            continue;

                                    Undo.RecordObject(itemLibrary, "Collect RPG Item Library");

                                    itemLibrary.CollectAllItemsInProject();

                                    EditorUtility.SetDirty(itemLibrary);
                            }

                            AssetDatabase.SaveAssets();
                    }

                    GUILayout.Space(4);

                    if (GUILayout.Button("Clear Item Library Lists", GUILayout.Height(24)))
                    {
                            foreach (Object selectedTarget in targets)
                            {
                                    RPGItemLibrarySO itemLibrary = selectedTarget as RPGItemLibrarySO;

                                    if (itemLibrary == null)
                                            continue;

                                    Undo.RecordObject(itemLibrary, "Clear RPG Item Library");

                                    itemLibrary.ClearAllLists();

                                    EditorUtility.SetDirty(itemLibrary);
                            }

                            AssetDatabase.SaveAssets();
                    }

                    GUILayout.Space(8);

                    RPGItemLibrarySO library = target as RPGItemLibrarySO;

                    if (library != null)
                    {
                            EditorGUILayout.HelpBox(
                                    $"All Items: {library.allItems.Count}\n" +
                                    $"Armor Body: {library.armorBodyItems.Count}\n" +
                                    $"Boots: {library.bootsItems.Count}\n" +
                                    $"Cloaks: {library.cloakItems.Count}\n" +
                                    $"Helmets: {library.helmetItems.Count}\n" +
                                    $"Amulets: {library.amuletItems.Count}\n" +
                                    $"Rings: {library.ringItems.Count}\n" +
                                    $"Shields: {library.shieldItems.Count}\n" +
                                    $"Swords: {library.oneHandedWeaponSwordItems.Count}\n" +
                                    $"Axes: {library.oneHandedAxeItems.Count}\n" +
                                    $"Spears: {library.oneHandedSpearItems.Count}\n" +
                                    $"Daggers: {library.oneHandedDaggerItems.Count}\n" +
                                    $"Hammers: {library.oneHandedHammerItems.Count}\n" +
                                    $"Not Setup: {library.noneNullItems.Count}",
                                    MessageType.Info
                            );
                    }
            }
    }

}
