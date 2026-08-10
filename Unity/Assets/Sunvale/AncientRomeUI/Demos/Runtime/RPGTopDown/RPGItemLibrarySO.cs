using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [CreateAssetMenu(fileName = "New Item Library", menuName = "RPG/Item Library", order = 1)]
    public class RPGItemLibrarySO : ScriptableObject
    {
            [Header("All Items")]
            public List<RPGItemDefinitionSO> allItems = new();

            [Header("Invalid / Not Setup")]
            public List<RPGItemDefinitionSO> noneNullItems = new();

            [Header("Armor")]
            public List<RPGItemDefinitionSO> armorBodyItems = new();
            public List<RPGItemDefinitionSO> bootsItems = new();
            public List<RPGItemDefinitionSO> cloakItems = new();
            public List<RPGItemDefinitionSO> helmetItems = new();

            [Header("Jewelry")]
            public List<RPGItemDefinitionSO> amuletItems = new();
            public List<RPGItemDefinitionSO> ringItems = new();

            [Header("Weapons / Shields")]
            public List<RPGItemDefinitionSO> shieldItems = new();
            public List<RPGItemDefinitionSO> oneHandedWeaponSwordItems = new();
            public List<RPGItemDefinitionSO> oneHandedAxeItems = new();
            public List<RPGItemDefinitionSO> oneHandedSpearItems = new();
            public List<RPGItemDefinitionSO> oneHandedDaggerItems = new();
            public List<RPGItemDefinitionSO> oneHandedHammerItems = new();

            public void ClearAllLists()
            {
                    allItems.Clear();

                    noneNullItems.Clear();

                    armorBodyItems.Clear();
                    bootsItems.Clear();
                    cloakItems.Clear();
                    helmetItems.Clear();

                    amuletItems.Clear();
                    ringItems.Clear();

                    shieldItems.Clear();
                    oneHandedWeaponSwordItems.Clear();
                    oneHandedAxeItems.Clear();
                    oneHandedSpearItems.Clear();
                    oneHandedDaggerItems.Clear();
                    oneHandedHammerItems.Clear();
            }

            public List<RPGItemDefinitionSO> GetItemsByType(RPGItemType itemType)
            {
                    switch (itemType)
                    {
                            case RPGItemType.noneNull:
                                    return noneNullItems;

                            case RPGItemType.armorBody:
                                    return armorBodyItems;

                            case RPGItemType.boots:
                                    return bootsItems;

                            case RPGItemType.amulet:
                                    return amuletItems;

                            case RPGItemType.ring:
                                    return ringItems;

                            case RPGItemType.shield:
                                    return shieldItems;

                            case RPGItemType.cloak:
                                    return cloakItems;

                            case RPGItemType.helmet:
                                    return helmetItems;

                            case RPGItemType.oneHandedWeaponSword:
                                    return oneHandedWeaponSwordItems;

                            case RPGItemType.oneHandedAxe:
                                    return oneHandedAxeItems;

                            case RPGItemType.oneHandedSpear:
                                    return oneHandedSpearItems;

                            case RPGItemType.oneHandedDagger:
                                    return oneHandedDaggerItems;

                            case RPGItemType.oneHandedHammer:
                                    return oneHandedHammerItems;

                            default:
                                    return noneNullItems;
                    }
            }

            public void AddItem(RPGItemDefinitionSO item)
            {
                    if (item == null)
                            return;

                    if (!allItems.Contains(item))
                            allItems.Add(item);

                    List<RPGItemDefinitionSO> typedList = GetItemsByType(item.itemType);

                    if (!typedList.Contains(item))
                            typedList.Add(item);
            }

            public void SortAllLists()
            {
                    SortList(allItems);

                    SortList(noneNullItems);

                    SortList(armorBodyItems);
                    SortList(bootsItems);
                    SortList(cloakItems);
                    SortList(helmetItems);

                    SortList(amuletItems);
                    SortList(ringItems);

                    SortList(shieldItems);
                    SortList(oneHandedWeaponSwordItems);
                    SortList(oneHandedAxeItems);
                    SortList(oneHandedSpearItems);
                    SortList(oneHandedDaggerItems);
                    SortList(oneHandedHammerItems);
            }

            private static void SortList(List<RPGItemDefinitionSO> list)
            {
                    list.Sort(CompareItems);
            }

            private static int CompareItems(RPGItemDefinitionSO a, RPGItemDefinitionSO b)
            {
                    if (a == null && b == null)
                            return 0;

                    if (a == null)
                            return 1;

                    if (b == null)
                            return -1;

                    int typeCompare = a.itemType.CompareTo(b.itemType);

                    if (typeCompare != 0)
                            return typeCompare;

                    int tierCompare = a.itemTier.CompareTo(b.itemTier);

                    if (tierCompare != 0)
                            return tierCompare;

                    return string.Compare(a.itemName, b.itemName, System.StringComparison.Ordinal);
            }

    #if UNITY_EDITOR

            public void CollectAllItemsInProject()
            {
                    ClearAllLists();

                    string[] itemGuids = AssetDatabase.FindAssets("t:RPGItemDefinitionSO");

                    foreach (string itemGuid in itemGuids)
                    {
                            string assetPath = AssetDatabase.GUIDToAssetPath(itemGuid);
                            RPGItemDefinitionSO item = AssetDatabase.LoadAssetAtPath<RPGItemDefinitionSO>(assetPath);

                            AddItem(item);
                    }

                    SortAllLists();

                    EditorUtility.SetDirty(this);
            }

    #endif
    }

}
