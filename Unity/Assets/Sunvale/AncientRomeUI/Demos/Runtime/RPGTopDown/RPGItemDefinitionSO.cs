using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    [Serializable]
    public enum RPGEquipmentSlot
    {
            noneNull,
            helmet,
            mainHand,
            offHand,
            amulet,
            ring,
            cloak,
            armor,
            boots
    }

    [Serializable]
    public enum RPGItemCategory
    {
            noneNull,
            armor,
            jewelery,
            weapon,
            misc
    }

    [Serializable]
    public enum RPGItemType
    {
            noneNull,
            armorBody,
            boots,
            amulet,
            ring,
            shield,
            cloak,
            helmet,
            oneHandedWeaponSword,
            oneHandedAxe,
            oneHandedSpear,
            oneHandedDagger,
            oneHandedHammer
    }

    [CreateAssetMenu(fileName = "New RPG Item", menuName = "RPG/Item", order = 0)]
    public class RPGItemDefinitionSO : ScriptableObject
    {
            [Header("Item Type")]
            public RPGItemCategory itemCategoryType;
            public RPGEquipmentSlot itemTypeSlot;
            public RPGItemType itemType;
            public int itemTier;
            public string itemName;

            [Header("Icon")]
            public Sprite itemSprite;

            [Header("Sounds")] public UISoundConfig inventoryBeginDragActionSoundConfig;
            public UISoundConfig inventoryEndDragActionSoundConfig;
            
            [Header("Buffs")]
            public List<RPGItemBuff> itemBuffs = new();

            [Header("Icon Slot Display")]
            [Tooltip("Visual scale of this item's icon inside a 100x100 item slot.")]
            public float itemIconSlotScale = 1f;

            [Tooltip("Horizontal icon offset in slot pixels. Positive moves the icon right.")]
            public float itemIconXOffset;

            [Tooltip("Vertical icon offset in slot pixels. Positive moves the icon up.")]
            public float itemIconYOffset;

            public virtual void OnEquipped(RPGCharacterInventory characterInventory, RPGEquipmentSlot equippedSlot)
            {
                    characterInventory.Character.ApplyItemBuffs(this, equippedSlot);
            }

            public virtual void OnUnequipped(RPGCharacterInventory characterInventory, RPGEquipmentSlot equippedSlot)
            {
                    characterInventory.Character.RemoveItemBuffs(this);
            }

    #if UNITY_EDITOR

            public void SetupIconOffsetsEtc()
            {
                    itemIconSlotScale = 1f;
                    itemIconXOffset = 0f;
                    itemIconYOffset = 0f;

                    switch (itemType)
                    {
                            case RPGItemType.armorBody:
                                    itemIconSlotScale = 0.82f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.helmet:
                                    itemIconSlotScale = 0.71f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 2f;
                                    break;

                            case RPGItemType.boots:
                                    itemIconSlotScale = 0.88f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.cloak:
                                    itemIconSlotScale = 0.88f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.amulet:
                                    itemIconSlotScale = 0.74f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.ring:
                                    itemIconSlotScale = 0.70f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.shield:
                                    itemIconSlotScale = 0.92f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.oneHandedWeaponSword:
                                    itemIconSlotScale = 1.28f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.oneHandedAxe:
                                    itemIconSlotScale = 1.10f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.oneHandedSpear:
                                    itemIconSlotScale = 1.45f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.oneHandedDagger:
                                    itemIconSlotScale = 1.00f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;

                            case RPGItemType.oneHandedHammer:
                                    itemIconSlotScale = 1.08f;
                                    itemIconXOffset = 0f;
                                    itemIconYOffset = 0f;
                                    break;
                    }
            }

    #endif
    }

}
