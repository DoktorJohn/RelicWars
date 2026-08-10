using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    [Serializable]
    public class RPGSharedInventory
    {
        private readonly List<RPGItemDefinitionSO> items = new();

        // This list now represents inventory slots.
        // Null entries are valid and mean "empty gap".
        public IReadOnlyList<RPGItemDefinitionSO> Items => items;


        public int slotCount;
        public int Count => GetOccupiedItemCount();

        public event Action<RPGItemDefinitionSO> OnItemAdded;
        public event Action<RPGItemDefinitionSO> OnItemRemoved;
        public event Action OnItemsReordered;
        
        

        public RPGSharedInventory()
        {
        }

        public RPGSharedInventory(IEnumerable<RPGItemDefinitionSO> startingItems)
        {
            AddItems(startingItems);
            slotCount = items.Count + 5;
        }

        public void AddItem(RPGItemDefinitionSO item)
        {
            if (item == null)
                return;

            int firstEmptySlot = FindFirstEmptySlot();

            if (firstEmptySlot >= 0)
                items[firstEmptySlot] = item;
            else
                items.Add(item);

            OnItemAdded?.Invoke(item);
        }

        public void AddItems(IEnumerable<RPGItemDefinitionSO> newItems)
        {
            foreach (RPGItemDefinitionSO item in newItems)
                AddItem(item);
        }

        public bool RemoveItem(RPGItemDefinitionSO item)
        {
            if (item == null)
                return false;

            int index = IndexOf(item);

            if (index < 0)
                return false;

            return RemoveItemAt(index);
        }

        public bool RemoveItemAt(int index)
        {
            if (!IsValidExistingSlot(index))
                return false;

            RPGItemDefinitionSO removedItem = items[index];

            if (removedItem == null)
                return false;

            items[index] = null;
            TrimTrailingEmptySlots();

            OnItemRemoved?.Invoke(removedItem);
            OnItemsReordered?.Invoke();

            return true;
        }

        public bool TryMoveItemToSlot(int fromSlotIndex, int toSlotIndex)
        {
            if (!IsValidExistingSlot(fromSlotIndex))
                return false;

            if (toSlotIndex < 0)
                return false;

            RPGItemDefinitionSO movedItem = items[fromSlotIndex];

            if (movedItem == null)
                return false;

            if (fromSlotIndex == toSlotIndex)
                return false;

            EnsureSlotExists(toSlotIndex);

            RPGItemDefinitionSO targetItem = items[toSlotIndex];

            items[toSlotIndex] = movedItem;
            items[fromSlotIndex] = targetItem;

            TrimTrailingEmptySlots();

            OnItemsReordered?.Invoke();
            return true;
        }

        public bool TrySwapItems(int firstIndex, int secondIndex)
        {
            return TryMoveItemToSlot(firstIndex, secondIndex);
        }

        public bool TryMoveItem(int fromIndex, int toIndex)
        {
            return TryMoveItemToSlot(fromIndex, toIndex);
        }

        public RPGItemDefinitionSO GetItemAtSlot(int slotIndex)
        {
            if (!IsValidExistingSlot(slotIndex))
                return null;

            return items[slotIndex];
        }

        public int IndexOf(RPGItemDefinitionSO item)
        {
            if (item == null)
                return -1;

            return items.IndexOf(item);
        }

        public bool Contains(RPGItemDefinitionSO item)
        {
            return IndexOf(item) >= 0;
        }

        public void Clear()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                RPGItemDefinitionSO item = items[i];

                if (item != null)
                    OnItemRemoved?.Invoke(item);
            }

            items.Clear();
            OnItemsReordered?.Invoke();
        }

        public int GetTotalInventorySlotCount()
        {
            return slotCount;
        }

        private int GetOccupiedItemCount()
        {
            int count = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                    count++;
            }

            return count;
        }

        private int FindFirstEmptySlot()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    return i;
            }

            return -1;
        }

        private void EnsureSlotExists(int slotIndex)
        {
            while (items.Count <= slotIndex)
                items.Add(null);
        }

        private void TrimTrailingEmptySlots()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] != null)
                    break;

                items.RemoveAt(i);
            }
        }

        private bool IsValidExistingSlot(int index)
        {
            return index >= 0 && index < items.Count;
        }
        
        public bool TryReplaceItemAtSlot(int slotIndex, RPGItemDefinitionSO newItem, out RPGItemDefinitionSO previousItem)
        {
            previousItem = null;

            if (slotIndex < 0)
                return false;

            EnsureSlotExists(slotIndex);

            previousItem = items[slotIndex];

            if (previousItem == newItem)
                return false;

            items[slotIndex] = newItem;

            TrimTrailingEmptySlots();

            if (previousItem != null)
                OnItemRemoved?.Invoke(previousItem);

            if (newItem != null)
                OnItemAdded?.Invoke(newItem);

            OnItemsReordered?.Invoke();

            return true;
        }

        public bool TryMoveItemToFirstEmptySlot(int fromSlotIndex)
        {
            if (!IsValidExistingSlot(fromSlotIndex))
                return false;

            RPGItemDefinitionSO movedItem = items[fromSlotIndex];

            if (movedItem == null)
                return false;

            int targetSlotIndex = FindFirstEmptySlot();

            if (targetSlotIndex < 0)
                targetSlotIndex = items.Count;

            if (slotCount > 0 && targetSlotIndex >= slotCount)
                return false;

            return TryMoveItemToSlot(fromSlotIndex, targetSlotIndex);
        }
    }
}
