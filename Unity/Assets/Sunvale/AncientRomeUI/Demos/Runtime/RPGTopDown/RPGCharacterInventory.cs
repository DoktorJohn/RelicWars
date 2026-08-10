using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class RPGCharacterInventory
    {
        private readonly Dictionary<RPGEquipmentSlot, RPGItemDefinitionSO> equippedItemsBySlot = new();

        public RPGCharacterData Character { get; private set; }
        public IReadOnlyDictionary<RPGEquipmentSlot, RPGItemDefinitionSO> EquippedItemsBySlot => equippedItemsBySlot;

        public delegate void EquipmentChangedDelegate(
            RPGCharacterInventory inventory,
            RPGEquipmentSlot slot,
            RPGItemDefinitionSO previousItem,
            RPGItemDefinitionSO newItem
        );

        public event EquipmentChangedDelegate OnEquipmentChanged;

        public RPGCharacterInventory(RPGCharacterData character)
        {
            BindCharacter(character);
        }

        public void BindCharacter(RPGCharacterData character)
        {
            Character = character;
            Character.BindWithInventory(this);
        }

        public RPGItemDefinitionSO GetEquippedItem(RPGEquipmentSlot slot)
        {
            if (equippedItemsBySlot.TryGetValue(slot, out RPGItemDefinitionSO item))
                return item;

            return null;
        }

        public bool CanEquipItemToSlot(RPGItemDefinitionSO item, RPGEquipmentSlot targetSlot)
        {
            return IsItemAllowedForSlot(item, targetSlot);
        }

        public static bool IsItemAllowedForSlot(RPGItemDefinitionSO item, RPGEquipmentSlot targetSlot)
        {
            if (item == null)
                return false;

            if (targetSlot == RPGEquipmentSlot.noneNull)
                return false;

            if (targetSlot == RPGEquipmentSlot.mainHand || targetSlot == RPGEquipmentSlot.offHand)
                return IsHandItem(item);

            return item.itemTypeSlot == targetSlot;
        }

        private static bool IsHandItem(RPGItemDefinitionSO item)
        {
            if (item.itemTypeSlot == RPGEquipmentSlot.mainHand || item.itemTypeSlot == RPGEquipmentSlot.offHand)
                return true;

            switch (item.itemType)
            {
                case RPGItemType.shield:
                case RPGItemType.oneHandedWeaponSword:
                case RPGItemType.oneHandedAxe:
                case RPGItemType.oneHandedSpear:
                case RPGItemType.oneHandedDagger:
                case RPGItemType.oneHandedHammer:
                    return true;

                default:
                    return false;
            }
        }

        public bool TryEquipFromInventorySlot(
            RPGSharedInventory globalInventory,
            int sourceInventorySlotIndex,
            RPGEquipmentSlot targetEquipmentSlot
        )
        {
            RPGItemDefinitionSO itemToEquip = globalInventory.GetItemAtSlot(sourceInventorySlotIndex);

            if (!CanEquipItemToSlot(itemToEquip, targetEquipmentSlot))
                return false;

            RPGItemDefinitionSO replacedEquipmentItem = GetEquippedItem(targetEquipmentSlot);

            SetEquippedItem(targetEquipmentSlot, itemToEquip);
            globalInventory.TryReplaceItemAtSlot(sourceInventorySlotIndex, replacedEquipmentItem, out _);

            return true;
        }

        public bool TryUnequipToInventorySlot(
            RPGSharedInventory globalInventory,
            RPGEquipmentSlot sourceEquipmentSlot,
            int targetInventorySlotIndex
        )
        {
            RPGItemDefinitionSO equippedItem = GetEquippedItem(sourceEquipmentSlot);

            if (equippedItem == null)
                return false;

            if (targetInventorySlotIndex < 0)
            {
                SetEquippedItem(sourceEquipmentSlot, null);
                globalInventory.AddItem(equippedItem);
                return true;
            }

            RPGItemDefinitionSO inventoryItem = globalInventory.GetItemAtSlot(targetInventorySlotIndex);

            if (inventoryItem != null && !CanEquipItemToSlot(inventoryItem, sourceEquipmentSlot))
                return false;

            SetEquippedItem(sourceEquipmentSlot, inventoryItem);
            globalInventory.TryReplaceItemAtSlot(targetInventorySlotIndex, equippedItem, out _);

            return true;
        }

        public bool CanMoveEquippedItem(RPGEquipmentSlot sourceSlot, RPGEquipmentSlot targetSlot)
        {
            if (sourceSlot == targetSlot)
                return false;

            RPGItemDefinitionSO sourceItem = GetEquippedItem(sourceSlot);

            if (sourceItem == null)
                return false;

            RPGItemDefinitionSO targetItem = GetEquippedItem(targetSlot);

            if (!CanEquipItemToSlot(sourceItem, targetSlot))
                return false;

            if (targetItem != null && !CanEquipItemToSlot(targetItem, sourceSlot))
                return false;

            return true;
        }

        public bool TryMoveEquippedItem(RPGEquipmentSlot sourceSlot, RPGEquipmentSlot targetSlot)
        {
            if (!CanMoveEquippedItem(sourceSlot, targetSlot))
                return false;

            RPGItemDefinitionSO sourceItem = GetEquippedItem(sourceSlot);
            RPGItemDefinitionSO targetItem = GetEquippedItem(targetSlot);

            if (sourceItem != null)
                sourceItem.OnUnequipped(this, sourceSlot);

            if (targetItem != null)
                targetItem.OnUnequipped(this, targetSlot);

            SetEquippedItemRaw(sourceSlot, targetItem);
            SetEquippedItemRaw(targetSlot, sourceItem);

            if (targetItem != null)
                targetItem.OnEquipped(this, sourceSlot);

            if (sourceItem != null)
                sourceItem.OnEquipped(this, targetSlot);

            OnEquipmentChanged?.Invoke(this, sourceSlot, sourceItem, targetItem);
            OnEquipmentChanged?.Invoke(this, targetSlot, targetItem, sourceItem);

            return true;
        }

        private void SetEquippedItem(RPGEquipmentSlot slot, RPGItemDefinitionSO newItem)
        {
            RPGItemDefinitionSO previousItem = GetEquippedItem(slot);

            if (previousItem == newItem)
                return;

            if (previousItem != null)
                previousItem.OnUnequipped(this, slot);

            SetEquippedItemRaw(slot, newItem);

            if (newItem != null)
                newItem.OnEquipped(this, slot);

            OnEquipmentChanged?.Invoke(this, slot, previousItem, newItem);
        }

        private void SetEquippedItemRaw(RPGEquipmentSlot slot, RPGItemDefinitionSO item)
        {
            if (item == null)
            {
                equippedItemsBySlot.Remove(slot);
                return;
            }

            equippedItemsBySlot[slot] = item;
        }
    }
}
