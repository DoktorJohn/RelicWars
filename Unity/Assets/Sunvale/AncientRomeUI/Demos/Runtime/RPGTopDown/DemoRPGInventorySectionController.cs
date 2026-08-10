using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;
using Sunvale.AncientRomeUI.Buttons;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class DemoRPGInventorySectionController : MonoBehaviour
    {
        [Header("References")] public DemoRPGCharacterStatSheetView statSheet;
        public GridLayoutGroup inventoryGridLayout;
        public RectTransform itemsSlotsContainer;

        public CarvedPressButton unequipAllButton;
        public CarvedPressButton autoEquipButton;
        public Image silhuetteImage;

        public RPGEquipmentSlotView headSlot;
        public RPGEquipmentSlotView cloakSLot;
        public RPGEquipmentSlotView mainHandSlot;
        public RPGEquipmentSlotView offHandSlot;
        public RPGEquipmentSlotView amuletSlot;
        public RPGEquipmentSlotView ringSlot;
        public RPGEquipmentSlotView bootsSlot;
        public RPGEquipmentSlotView armorSlot;

        [Tooltip("Set this to the whole inventory/equipment window. If null, itemsSlotsContainer is used.")]
        public RectTransform inventoryWindowRectTransform;

        [Tooltip("Container used for the floating dragged item visual. Keep it as the last sibling.")]
        public RectTransform draggingItemContainer;

        [Header("Category Tabs")] public TextColorTabButton allItemsTab;
        public TextColorTabButton weaponsItemsTab;
        public TextColorTabButton armorItemsTab;
        public TextColorTabButton jewelryItemsTab;
        public TextColorTabButton miscItemsTab;

        [Header("Icon Size Buttons")] public CarvedPressButton smallIconsButton;
        public CarvedPressButton largeIconsButton;

        [Header("Slot Pool")] public RPGInventorySlotView slotPrefab;
        public List<RPGInventorySlotView> slotPool = new();

        [Header("Drag And Drop")] public bool deleteItemWhenReleasedOutsideWindow = true;

        [Tooltip("Optional. If null, one is generated at runtime under draggingItemContainer.")]
        public RPGDraggedItemView draggingItemDisplayer;

        [Header("Layout")] [SerializeField] private IconLayoutState defaultIconLayoutState = IconLayoutState.large;

        private readonly List<RPGItemDefinitionSO> filteredItems = new();
        private readonly List<int> filteredGlobalItemIndices = new();
        private readonly Dictionary<TextColorTabButton, InventoryTabFilter> tabToFilter = new();
        private readonly List<RaycastResult> dragRaycastResults = new();
        private readonly HashSet<RPGInventorySlotView> registeredInventorySlots = new();
        private readonly HashSet<RPGEquipmentSlotView> registeredCharacterSlots = new();

        [NonSerialized] private RPGSharedInventory currentGlobalInventory;
        [NonSerialized] private RPGCharacterData currentCharacter;
        [NonSerialized] private RPGCharacterInventory currentCharacterInventory;

        private RPGDraggedItemView draggingVisual;

        private RPGInventorySlotView dragSourceInventorySlot;
        private RPGEquipmentSlotView dragSourceCharacterSlot;

        private RPGInventorySlotView currentDropTargetInventorySlot;
        private RPGEquipmentSlotView currentDropTargetCharacterSlot;

        private RPGItemDefinitionSO draggedItem;
        private int draggedGlobalItemIndex = -1;
        private RPGEquipmentSlot draggedEquipmentSlotType = RPGEquipmentSlot.noneNull;

        private bool isDraggingInventoryItem;
        private bool currentInventoryDropTargetIsValid;
        private bool currentCharacterDropTargetIsValid;

        private DragSourceKind dragSourceKind = DragSourceKind.none;

        private Canvas rootCanvas;

        private IconLayoutState currentIconLayoutState;
        private InventoryTabFilter currentInventoryTabFilter = InventoryTabFilter.all;
        private bool wasInitialized;

        private int currentVisibleSlotCount;
        private int currentEmptySlotCount;

        public List<Sprite> darkCharSpriteList;
        public IReadOnlyList<RPGItemDefinitionSO> FilteredItems => filteredItems;
        public int CurrentVisibleSlotCount => currentVisibleSlotCount;
        public int CurrentEmptySlotCount => currentEmptySlotCount;

        public enum IconLayoutState
        {
            small,
            large
        }

        private enum InventoryTabFilter
        {
            all,
            weapons,
            armor,
            jewelry,
            misc
        }

        private enum DragSourceKind
        {
            none,
            inventory,
            equipment
        }

        private void InnerInitialization()
        {
            if (wasInitialized)
                return;

            wasInitialized = true;

            rootCanvas = GetComponentInParent<Canvas>();

            draggingItemContainer.SetAsLastSibling();

            RegisterTab(allItemsTab, InventoryTabFilter.all);
            RegisterTab(weaponsItemsTab, InventoryTabFilter.weapons);
            RegisterTab(armorItemsTab, InventoryTabFilter.armor);
            RegisterTab(jewelryItemsTab, InventoryTabFilter.jewelry);
            RegisterTab(miscItemsTab, InventoryTabFilter.misc);

            unequipAllButton.OnButtonActivatedClicked += UnequipAllButtonClicked;
            autoEquipButton.OnButtonActivatedClicked += AutoEquipButtonClicked;

            RegisterExistingInventorySlotPool();

            RegisterCharacterSlot(headSlot, RPGEquipmentSlot.helmet);
            RegisterCharacterSlot(cloakSLot, RPGEquipmentSlot.cloak);
            RegisterCharacterSlot(mainHandSlot, RPGEquipmentSlot.mainHand);
            RegisterCharacterSlot(offHandSlot, RPGEquipmentSlot.offHand);
            RegisterCharacterSlot(amuletSlot, RPGEquipmentSlot.amulet);
            RegisterCharacterSlot(ringSlot, RPGEquipmentSlot.ring);
            RegisterCharacterSlot(bootsSlot, RPGEquipmentSlot.boots);
            RegisterCharacterSlot(armorSlot, RPGEquipmentSlot.armor);

            smallIconsButton.OnButtonActivatedClicked += SmallIconsButtonClicked;
            largeIconsButton.OnButtonActivatedClicked += LargeIconsButtonClicked;

            currentIconLayoutState = defaultIconLayoutState == IconLayoutState.small
                ? IconLayoutState.large
                : IconLayoutState.small;

            SwitchIconSizeLayout(defaultIconLayoutState);
            ApplyTabSelectionVisuals(false);
            EnsureDraggingVisual();
        }

        public void InitializeForCharacter(RPGCharacterData character, RPGDemoController sceneWithData, bool withAnimations)
        {
            InnerInitialization();

            statSheet.InitializeForCharacter(character, withAnimations);

            UnregisterFromCurrentInventory();
            UnregisterFromCurrentCharacterInventory();

            currentCharacter = character;
            currentCharacterInventory = GetOrCreateCharacterInventory(character);
            currentGlobalInventory = sceneWithData.globalInventory;

            RegisterToCurrentInventory();
            RegisterToCurrentCharacterInventory();

            BindCharacterSlotsToInventory();

            SetInventoryTabFilter(InventoryTabFilter.all, withAnimations, true);
            RefreshCharacterEquipmentSlots();

            silhuetteImage.sprite = darkCharSpriteList[currentCharacter.darkSilhuetteIndex];
        }

        private RPGCharacterInventory GetOrCreateCharacterInventory(RPGCharacterData character)
        {
            if (character.myInventory == null)
                return new RPGCharacterInventory(character);

            character.myInventory.BindCharacter(character);
            return character.myInventory;
        }

        private void RegisterTab(TextColorTabButton tab, InventoryTabFilter filter)
        {
            tabToFilter.Add(tab, filter);
            tab.OnButtonActivatedClicked += InventoryTabClicked;
        }

        private void InventoryTabClicked(TextColorTabButton tab)
        {
            SetInventoryTabFilter(tabToFilter[tab], true, false);
        }

        private void SetInventoryTabFilter(InventoryTabFilter newFilter, bool withAnimations, bool forceRefresh)
        {
            if (!forceRefresh && currentInventoryTabFilter == newFilter)
                return;

            currentInventoryTabFilter = newFilter;

            ApplyTabSelectionVisuals(withAnimations);
            RefreshInventorySlots();
        }

        private void ApplyTabSelectionVisuals(bool withAnimations)
        {
            allItemsTab.SetSelected(currentInventoryTabFilter == InventoryTabFilter.all, withAnimations);
            weaponsItemsTab.SetSelected(currentInventoryTabFilter == InventoryTabFilter.weapons, withAnimations);
            armorItemsTab.SetSelected(currentInventoryTabFilter == InventoryTabFilter.armor, withAnimations);
            jewelryItemsTab.SetSelected(currentInventoryTabFilter == InventoryTabFilter.jewelry, withAnimations);
            miscItemsTab.SetSelected(currentInventoryTabFilter == InventoryTabFilter.misc, withAnimations);
        }

        private void RegisterToCurrentInventory()
        {
            currentGlobalInventory.OnItemAdded += InventoryItemAdded;
            currentGlobalInventory.OnItemRemoved += InventoryItemRemoved;
            currentGlobalInventory.OnItemsReordered += InventoryItemsReordered;
        }

        private void UnregisterFromCurrentInventory()
        {
            if (currentGlobalInventory == null)
                return;

            currentGlobalInventory.OnItemAdded -= InventoryItemAdded;
            currentGlobalInventory.OnItemRemoved -= InventoryItemRemoved;
            currentGlobalInventory.OnItemsReordered -= InventoryItemsReordered;
        }

        private void RegisterToCurrentCharacterInventory()
        {
            currentCharacterInventory.OnEquipmentChanged += CharacterEquipmentChanged;
        }

        private void UnregisterFromCurrentCharacterInventory()
        {
            if (currentCharacterInventory == null)
                return;

            currentCharacterInventory.OnEquipmentChanged -= CharacterEquipmentChanged;
        }

        private void InventoryItemAdded(RPGItemDefinitionSO item)
        {
            RefreshInventorySlots();
        }

        private void InventoryItemRemoved(RPGItemDefinitionSO item)
        {
            RefreshInventorySlots();
        }

        private void InventoryItemsReordered()
        {
            RefreshInventorySlots();
        }

        private void CharacterEquipmentChanged(
            RPGCharacterInventory inventory,
            RPGEquipmentSlot slot,
            RPGItemDefinitionSO previousItem,
            RPGItemDefinitionSO newItem
        )
        {
            RefreshCharacterEquipmentSlots();
        }

        private void RefreshInventorySlots()
        {
            filteredItems.Clear();
            filteredGlobalItemIndices.Clear();

            IReadOnlyList<RPGItemDefinitionSO> allInventorySlots = currentGlobalInventory.Items;

            if (currentInventoryTabFilter == InventoryTabFilter.all)
            {
                RefreshAllItemsTab(allInventorySlots);
                return;
            }

            RefreshFilteredItemsTab(allInventorySlots);
        }

        private void RefreshAllItemsTab(IReadOnlyList<RPGItemDefinitionSO> allInventorySlots)
        {
            currentVisibleSlotCount = currentGlobalInventory.GetTotalInventorySlotCount();
            currentEmptySlotCount = currentVisibleSlotCount - currentGlobalInventory.Count;

            EnsureInventorySlotPoolSize(currentVisibleSlotCount);

            for (int i = 0; i < slotPool.Count; i++)
            {
                bool shouldBeActive = i < currentVisibleSlotCount;
                slotPool[i].gameObject.SetActive(shouldBeActive);

                if (!shouldBeActive)
                    continue;

                RPGItemDefinitionSO item = i < allInventorySlots.Count ? allInventorySlots[i] : null;

                if (item != null)
                    slotPool[i].BindToItem(item, i, i);
                else
                    slotPool[i].SetToEmpty(i, i);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsSlotsContainer);
        }

        private void RefreshFilteredItemsTab(IReadOnlyList<RPGItemDefinitionSO> allInventorySlots)
        {
            for (int globalSlotIndex = 0; globalSlotIndex < allInventorySlots.Count; globalSlotIndex++)
            {
                RPGItemDefinitionSO item = allInventorySlots[globalSlotIndex];

                if (item == null)
                    continue;

                if (!PassesCurrentFilter(item))
                    continue;

                filteredItems.Add(item);
                filteredGlobalItemIndices.Add(globalSlotIndex);
            }

            currentEmptySlotCount = 5;
            currentVisibleSlotCount = filteredItems.Count + currentEmptySlotCount;

            EnsureInventorySlotPoolSize(currentVisibleSlotCount);

            for (int i = 0; i < slotPool.Count; i++)
            {
                bool shouldBeActive = i < currentVisibleSlotCount;
                slotPool[i].gameObject.SetActive(shouldBeActive);

                if (!shouldBeActive)
                    continue;

                if (i < filteredItems.Count)
                {
                    slotPool[i].BindToItem(
                        filteredItems[i],
                        i,
                        filteredGlobalItemIndices[i]
                    );
                }
                else
                {
                    slotPool[i].SetToEmpty(i, -1);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsSlotsContainer);
        }

        private bool PassesCurrentFilter(RPGItemDefinitionSO item)
        {
            switch (currentInventoryTabFilter)
            {
                case InventoryTabFilter.all:
                    return true;

                case InventoryTabFilter.weapons:
                    return item.itemCategoryType == RPGItemCategory.weapon;

                case InventoryTabFilter.armor:
                    return item.itemCategoryType == RPGItemCategory.armor;

                case InventoryTabFilter.jewelry:
                    return item.itemCategoryType == RPGItemCategory.jewelery;

                case InventoryTabFilter.misc:
                    return item.itemCategoryType == RPGItemCategory.misc;

                default:
                    return false;
            }
        }

        private void RegisterExistingInventorySlotPool()
        {
            for (int i = 0; i < slotPool.Count; i++)
                RegisterInventorySlot(slotPool[i]);
        }

        private void EnsureInventorySlotPoolSize(int requiredCount)
        {
            while (slotPool.Count < requiredCount)
            {
                RPGInventorySlotView newSlot = Instantiate(slotPrefab, itemsSlotsContainer);
                newSlot.gameObject.SetActive(false);

                slotPool.Add(newSlot);
                RegisterInventorySlot(newSlot);
            }
        }

        private void RegisterInventorySlot(RPGInventorySlotView slot)
        {
            if (registeredInventorySlots.Contains(slot))
                return;

            slot.OnSlotBeginDragEvent += SlotBeginDrag;
            slot.OnSlotDragEvent += SlotDrag;
            slot.OnSlotEndDragEvent += SlotEndDrag;
            slot.OnSlotClickEvent += InventorySlotClicked;

            registeredInventorySlots.Add(slot);
        }

        private void UnregisterAllInventorySlots()
        {
            foreach (RPGInventorySlotView slot in registeredInventorySlots)
            {
                if (slot == null)
                    continue;

                slot.OnSlotBeginDragEvent -= SlotBeginDrag;
                slot.OnSlotDragEvent -= SlotDrag;
                slot.OnSlotEndDragEvent -= SlotEndDrag;
                slot.OnSlotClickEvent -= InventorySlotClicked;
            }

            registeredInventorySlots.Clear();
        }

        private void RegisterCharacterSlot(RPGEquipmentSlotView slot, RPGEquipmentSlot acceptedSlot)
        {
            slot.SetAcceptedItemTypeSlot(acceptedSlot);

            slot.OnSlotBeginDragEvent += CharacterSlotBeginDrag;
            slot.OnSlotDragEvent += CharacterSlotDrag;
            slot.OnSlotEndDragEvent += CharacterSlotEndDrag;
            slot.OnSlotClickEvent += CharacterSlotClicked;

            registeredCharacterSlots.Add(slot);
        }

        private void BindCharacterSlotsToInventory()
        {
            headSlot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.helmet);
            cloakSLot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.cloak);
            mainHandSlot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.mainHand);
            offHandSlot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.offHand);
            amuletSlot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.amulet);
            ringSlot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.ring);
            bootsSlot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.boots);
            armorSlot.BindToCharacterInventory(currentCharacterInventory, RPGEquipmentSlot.armor);
        }

        private void RefreshCharacterEquipmentSlots()
        {
            foreach (RPGEquipmentSlotView slot in registeredCharacterSlots)
                slot.RefreshFromInventory();
        }


        private void UnregisterAllCharacterSlots()
        {
            foreach (RPGEquipmentSlotView slot in registeredCharacterSlots)
            {
                if (slot == null)
                    continue;

                slot.OnSlotBeginDragEvent -= CharacterSlotBeginDrag;
                slot.OnSlotDragEvent -= CharacterSlotDrag;
                slot.OnSlotEndDragEvent -= CharacterSlotEndDrag;
                slot.OnSlotClickEvent -= CharacterSlotClicked;
            }

            registeredCharacterSlots.Clear();
        }

        private void EnsureDraggingVisual()
        {
            if (draggingVisual != null)
                return;

            if (draggingItemDisplayer != null)
            {
                draggingVisual = draggingItemDisplayer;
                draggingVisual.transform.SetParent(draggingItemContainer, false);
                draggingVisual.AutoWireReferences();
                draggingVisual.gameObject.SetActive(false);
                return;
            }

            GameObject draggingObject = new GameObject(
                "Dragging Inventory Item Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(RPGDraggedItemView)
            );

            draggingObject.transform.SetParent(draggingItemContainer, false);

            draggingVisual = draggingObject.GetComponent<RPGDraggedItemView>();
            draggingVisual.AutoWireReferences();

            draggingVisual.myRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            draggingVisual.myRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            draggingVisual.myRectTransform.pivot = new Vector2(0.5f, 0.5f);

            draggingVisual.itemImage.raycastTarget = false;
            draggingVisual.itemImage.preserveAspect = true;

            draggingVisual.canvasGroup.blocksRaycasts = false;
            draggingVisual.canvasGroup.interactable = false;

            draggingVisual.gameObject.SetActive(false);
        }

        private void SlotBeginDrag(RPGInventorySlotView slot, PointerEventData eventData)
        {
            if (slot.IsEmpty)
                return;

            dragSourceKind = DragSourceKind.inventory;
            dragSourceInventorySlot = slot;
            dragSourceCharacterSlot = null;

            draggedItem = slot.CurrentItem;
            draggedGlobalItemIndex = slot.InventorySlotIndex;
            draggedEquipmentSlotType = RPGEquipmentSlot.noneNull;

            BeginSharedDragVisuals(slot, eventData);

            SimpleSoundManager.Play(slot.CurrentItem.inventoryBeginDragActionSoundConfig);
        }

        private void SlotDrag(RPGInventorySlotView slot, PointerEventData eventData)
        {
            if (!isDraggingInventoryItem)
                return;

            UpdateDraggingVisualPosition(eventData);
            UpdateDragTargetAndDeleteVisuals(eventData);
        }

        private void SlotEndDrag(RPGInventorySlotView slot, PointerEventData eventData)
        {
            EndSharedDrag(eventData);
        }

        private void CharacterSlotBeginDrag(RPGEquipmentSlotView slot, PointerEventData eventData)
        {
            if (slot.IsEmpty)
                return;

            dragSourceKind = DragSourceKind.equipment;
            dragSourceInventorySlot = null;
            dragSourceCharacterSlot = slot;

            draggedItem = slot.CurrentItem;
            draggedGlobalItemIndex = -1;
            draggedEquipmentSlotType = slot.AcceptedItemTypeSlot;

            BeginSharedDragVisuals(slot, eventData);

            SimpleSoundManager.Play(slot.CurrentItem.inventoryBeginDragActionSoundConfig);
        }

        private void CharacterSlotDrag(RPGEquipmentSlotView slot, PointerEventData eventData)
        {
            if (!isDraggingInventoryItem)
                return;

            UpdateDraggingVisualPosition(eventData);
            UpdateDragTargetAndDeleteVisuals(eventData);
        }

        private void CharacterSlotEndDrag(RPGEquipmentSlotView slot, PointerEventData eventData)
        {
            EndSharedDrag(eventData);
        }

        private void BeginSharedDragVisuals(RPGInventorySlotView sourceSlot, PointerEventData eventData)
        {
            isDraggingInventoryItem = true;

            draggingItemContainer.SetAsLastSibling();

            draggingVisual.gameObject.SetActive(true);
            draggingVisual.transform.SetAsLastSibling();
            draggingVisual.Show(draggedItem);

            sourceSlot.SetDragSourceVisual(true);

            UpdateDraggingVisualPosition(eventData);
            UpdateDragTargetAndDeleteVisuals(eventData);
        }

        private void BeginSharedDragVisuals(RPGEquipmentSlotView sourceSlot, PointerEventData eventData)
        {
            isDraggingInventoryItem = true;

            draggingItemContainer.SetAsLastSibling();

            draggingVisual.gameObject.SetActive(true);
            draggingVisual.transform.SetAsLastSibling();
            draggingVisual.Show(draggedItem);

            sourceSlot.SetDragSourceVisual(true);

            UpdateDraggingVisualPosition(eventData);
            UpdateDragTargetAndDeleteVisuals(eventData);
        }

        private void EndSharedDrag(PointerEventData eventData)
        {
            if (!isDraggingInventoryItem)
                return;


            bool releasedInsideWindow = IsPointerInsideInventoryWindow(eventData);

            RPGEquipmentSlotView releasedOverCharacterSlot = GetCharacterSlotUnderPointer(eventData);

            if (releasedOverCharacterSlot == dragSourceCharacterSlot)
                releasedOverCharacterSlot = null;

            RPGInventorySlotView releasedOverInventorySlot = GetInventorySlotUnderPointer(eventData);

            if (releasedOverInventorySlot == dragSourceInventorySlot)
                releasedOverInventorySlot = null;

            RPGItemDefinitionSO itemToDrop = draggedItem;
            SimpleSoundManager.Play(itemToDrop.inventoryEndDragActionSoundConfig);
            int sourceInventoryIndex = GetCurrentDraggedItemIndex();
            RPGEquipmentSlot sourceEquipmentSlot = draggedEquipmentSlotType;
            DragSourceKind sourceKind = dragSourceKind;

            bool canDeleteOutside = sourceKind == DragSourceKind.inventory && deleteItemWhenReleasedOutsideWindow;
            bool shouldDeleteOutside = canDeleteOutside && !releasedInsideWindow;

            bool canDropOnCharacter = releasedOverCharacterSlot != null &&
                                      CanDropOnCharacterSlot(
                                          itemToDrop,
                                          sourceKind,
                                          sourceEquipmentSlot,
                                          releasedOverCharacterSlot
                                      );

            bool canDropOnInventory = releasedOverInventorySlot != null &&
                                      CanDropOnInventorySlot(
                                          sourceKind,
                                          sourceEquipmentSlot,
                                          releasedOverInventorySlot
                                      );

            ClearDragStateVisuals();

            if (shouldDeleteOutside)
            {
                if (sourceInventoryIndex >= 0)
                    currentGlobalInventory.RemoveItemAt(sourceInventoryIndex);

                return;
            }

            if (canDropOnCharacter)
            {
                DropOnCharacterSlot(
                    sourceKind,
                    sourceInventoryIndex,
                    sourceEquipmentSlot,
                    releasedOverCharacterSlot
                );

                return;
            }

            if (canDropOnInventory)
            {
                DropOnInventorySlot(
                    sourceKind,
                    sourceInventoryIndex,
                    sourceEquipmentSlot,
                    releasedOverInventorySlot
                );
            }
        }

        private bool CanDropOnCharacterSlot(
            RPGItemDefinitionSO item,
            DragSourceKind sourceKind,
            RPGEquipmentSlot sourceEquipmentSlot,
            RPGEquipmentSlotView targetSlot
        )
        {
            if (targetSlot == null)
                return false;

            switch (sourceKind)
            {
                case DragSourceKind.inventory:
                    return targetSlot.AcceptsItem(item);

                case DragSourceKind.equipment:
                    return currentCharacterInventory.CanMoveEquippedItem(
                        sourceEquipmentSlot,
                        targetSlot.AcceptedItemTypeSlot
                    );

                default:
                    return false;
            }
        }

        private bool CanDropOnInventorySlot(
            DragSourceKind sourceKind,
            RPGEquipmentSlot sourceEquipmentSlot,
            RPGInventorySlotView targetSlot
        )
        {
            if (targetSlot == null)
                return false;

            if (sourceKind == DragSourceKind.inventory)
                return true;

            if (sourceKind != DragSourceKind.equipment)
                return false;

            int targetInventorySlotIndex = targetSlot.InventorySlotIndex;

            if (targetInventorySlotIndex < 0)
                return true;

            RPGItemDefinitionSO inventoryItem = currentGlobalInventory.GetItemAtSlot(targetInventorySlotIndex);

            if (inventoryItem == null)
                return true;

            return currentCharacterInventory.CanEquipItemToSlot(inventoryItem, sourceEquipmentSlot);
        }

        private void DropOnCharacterSlot(
            DragSourceKind sourceKind,
            int sourceInventoryIndex,
            RPGEquipmentSlot sourceEquipmentSlot,
            RPGEquipmentSlotView targetSlot
        )
        {
            switch (sourceKind)
            {
                case DragSourceKind.inventory:
                    currentCharacterInventory.TryEquipFromInventorySlot(
                        currentGlobalInventory,
                        sourceInventoryIndex,
                        targetSlot.AcceptedItemTypeSlot
                    );
                    break;

                case DragSourceKind.equipment:
                    currentCharacterInventory.TryMoveEquippedItem(
                        sourceEquipmentSlot,
                        targetSlot.AcceptedItemTypeSlot
                    );
                    break;
            }

            RefreshInventorySlots();
            RefreshCharacterEquipmentSlots();
        }

        private void DropOnInventorySlot(
            DragSourceKind sourceKind,
            int sourceInventoryIndex,
            RPGEquipmentSlot sourceEquipmentSlot,
            RPGInventorySlotView targetSlot
        )
        {
            int targetInventorySlotIndex = targetSlot.InventorySlotIndex;

            switch (sourceKind)
            {
                case DragSourceKind.inventory:
                    if (targetInventorySlotIndex < 0)
                        currentGlobalInventory.TryMoveItemToFirstEmptySlot(sourceInventoryIndex);
                    else
                        currentGlobalInventory.TryMoveItemToSlot(sourceInventoryIndex, targetInventorySlotIndex);
                    break;

                case DragSourceKind.equipment:
                    currentCharacterInventory.TryUnequipToInventorySlot(
                        currentGlobalInventory,
                        sourceEquipmentSlot,
                        targetInventorySlotIndex
                    );
                    break;
            }

            RefreshInventorySlots();
            RefreshCharacterEquipmentSlots();
        }

        private int GetCurrentDraggedItemIndex()
        {
            IReadOnlyList<RPGItemDefinitionSO> items = currentGlobalInventory.Items;

            if (draggedGlobalItemIndex >= 0 &&
                draggedGlobalItemIndex < items.Count &&
                items[draggedGlobalItemIndex] == draggedItem)
            {
                return draggedGlobalItemIndex;
            }

            return currentGlobalInventory.IndexOf(draggedItem);
        }

        private void UpdateDraggingVisualPosition(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                draggingItemContainer,
                eventData.position,
                GetEventCamera(eventData),
                out Vector2 localPoint
            );

            draggingVisual.SetAnchoredPosition(localPoint);
        }

        private void UpdateDragTargetAndDeleteVisuals(PointerEventData eventData)
        {
            bool outsideWindow = dragSourceKind == DragSourceKind.inventory &&
                                 deleteItemWhenReleasedOutsideWindow &&
                                 !IsPointerInsideInventoryWindow(eventData);

            if (dragSourceInventorySlot != null)
                dragSourceInventorySlot.SetDeleteCandidateVisual(outsideWindow);

            draggingVisual.SetDeleteCandidateVisual(outsideWindow);

            if (outsideWindow)
            {
                SetCurrentInventoryDropTarget(null, false);
                SetCurrentCharacterDropTarget(null, false);
                return;
            }

            RPGEquipmentSlotView characterSlotUnderPointer = GetCharacterSlotUnderPointer(eventData);

            if (characterSlotUnderPointer == dragSourceCharacterSlot)
                characterSlotUnderPointer = null;

            if (characterSlotUnderPointer != null)
            {
                bool canDrop = CanDropOnCharacterSlot(
                    draggedItem,
                    dragSourceKind,
                    draggedEquipmentSlotType,
                    characterSlotUnderPointer
                );

                SetCurrentCharacterDropTarget(characterSlotUnderPointer, canDrop);
                SetCurrentInventoryDropTarget(null, false);

                draggingVisual.SetDeleteCandidateVisual(!canDrop);
                return;
            }

            RPGInventorySlotView inventorySlotUnderPointer = GetInventorySlotUnderPointer(eventData);

            if (inventorySlotUnderPointer == dragSourceInventorySlot)
                inventorySlotUnderPointer = null;

            if (inventorySlotUnderPointer != null)
            {
                bool canDrop = CanDropOnInventorySlot(
                    dragSourceKind,
                    draggedEquipmentSlotType,
                    inventorySlotUnderPointer
                );

                SetCurrentInventoryDropTarget(inventorySlotUnderPointer, canDrop);
                SetCurrentCharacterDropTarget(null, false);

                draggingVisual.SetDeleteCandidateVisual(!canDrop);
                return;
            }

            SetCurrentInventoryDropTarget(null, false);
            SetCurrentCharacterDropTarget(null, false);
            draggingVisual.SetDeleteCandidateVisual(false);
        }

        private void SetCurrentInventoryDropTarget(RPGInventorySlotView newDropTarget, bool canDrop)
        {
            if (currentDropTargetInventorySlot == newDropTarget &&
                currentInventoryDropTargetIsValid == canDrop)
            {
                return;
            }

            if (currentDropTargetInventorySlot != null)
            {
                currentDropTargetInventorySlot.SetDropTargetVisual(false);
                currentDropTargetInventorySlot.SetDeleteCandidateVisual(false);
            }

            currentDropTargetInventorySlot = newDropTarget;
            currentInventoryDropTargetIsValid = canDrop;

            if (currentDropTargetInventorySlot == null)
                return;

            currentDropTargetInventorySlot.SetDropTargetVisual(canDrop);
            currentDropTargetInventorySlot.SetDeleteCandidateVisual(!canDrop);
        }

        private void SetCurrentCharacterDropTarget(RPGEquipmentSlotView newDropTarget, bool canDrop)
        {
            if (currentDropTargetCharacterSlot == newDropTarget &&
                currentCharacterDropTargetIsValid == canDrop)
            {
                return;
            }

            if (currentDropTargetCharacterSlot != null)
                currentDropTargetCharacterSlot.SetDropCandidateVisual(false, false);

            currentDropTargetCharacterSlot = newDropTarget;
            currentCharacterDropTargetIsValid = canDrop;

            if (currentDropTargetCharacterSlot == null)
                return;

            currentDropTargetCharacterSlot.SetDropCandidateVisual(true, canDrop);
        }

        private RPGInventorySlotView GetInventorySlotUnderPointer(PointerEventData eventData)
        {
            dragRaycastResults.Clear();

            EventSystem.current.RaycastAll(eventData, dragRaycastResults);

            for (int i = 0; i < dragRaycastResults.Count; i++)
            {
                RPGInventorySlotView slot =
                    dragRaycastResults[i].gameObject.GetComponentInParent<RPGInventorySlotView>();

                if (slot == null)
                    continue;

                if (!registeredInventorySlots.Contains(slot))
                    continue;

                if (!slot.gameObject.activeInHierarchy)
                    continue;

                return slot;
            }

            return null;
        }

        private RPGEquipmentSlotView GetCharacterSlotUnderPointer(PointerEventData eventData)
        {
            dragRaycastResults.Clear();

            EventSystem.current.RaycastAll(eventData, dragRaycastResults);

            for (int i = 0; i < dragRaycastResults.Count; i++)
            {
                RPGEquipmentSlotView slot =
                    dragRaycastResults[i].gameObject.GetComponentInParent<RPGEquipmentSlotView>();

                if (slot == null)
                    continue;

                if (!registeredCharacterSlots.Contains(slot))
                    continue;

                if (!slot.gameObject.activeInHierarchy)
                    continue;

                return slot;
            }

            return null;
        }

        private bool IsPointerInsideInventoryWindow(PointerEventData eventData)
        {
            RectTransform targetRect = inventoryWindowRectTransform != null
                ? inventoryWindowRectTransform
                : itemsSlotsContainer;

            return RectTransformUtility.RectangleContainsScreenPoint(
                targetRect,
                eventData.position,
                GetEventCamera(eventData)
            );
        }

        private Camera GetEventCamera(PointerEventData eventData)
        {
            if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (eventData.pressEventCamera != null)
                return eventData.pressEventCamera;

            if (rootCanvas != null)
                return rootCanvas.worldCamera;

            return null;
        }

        private void ClearDragStateVisuals()
        {
            if (dragSourceInventorySlot != null)
            {
                dragSourceInventorySlot.SetDragSourceVisual(false);
                dragSourceInventorySlot.SetDeleteCandidateVisual(false);
            }

            if (dragSourceCharacterSlot != null)
                dragSourceCharacterSlot.SetDragSourceVisual(false);

            if (currentDropTargetInventorySlot != null)
            {
                currentDropTargetInventorySlot.SetDropTargetVisual(false);
                currentDropTargetInventorySlot.SetDeleteCandidateVisual(false);
            }

            if (currentDropTargetCharacterSlot != null)
                currentDropTargetCharacterSlot.SetDropCandidateVisual(false, false);

            if (draggingVisual != null)
            {
                draggingVisual.SetDeleteCandidateVisual(false);
                draggingVisual.Hide();
            }

            isDraggingInventoryItem = false;

            dragSourceKind = DragSourceKind.none;

            dragSourceInventorySlot = null;
            dragSourceCharacterSlot = null;

            currentDropTargetInventorySlot = null;
            currentDropTargetCharacterSlot = null;

            currentInventoryDropTargetIsValid = false;
            currentCharacterDropTargetIsValid = false;

            draggedItem = null;
            draggedGlobalItemIndex = -1;
            draggedEquipmentSlotType = RPGEquipmentSlot.noneNull;
        }

        private void InventorySlotClicked(RPGInventorySlotView slot, PointerEventData eventData)
        {
            if (slot == null || slot.IsEmpty)
                return;

            if (currentGlobalInventory == null || currentCharacterInventory == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (eventData.clickCount < 2)
                return;

            int sourceInventoryIndex = slot.InventorySlotIndex;

            if (sourceInventoryIndex < 0)
                return;

            RPGItemDefinitionSO item = currentGlobalInventory.GetItemAtSlot(sourceInventoryIndex);

            if (item == null)
                return;

            RPGEquipmentSlot targetEquipmentSlot = GetDefaultDoubleClickEquipSlot(item);

            if (targetEquipmentSlot == RPGEquipmentSlot.noneNull)
                return;

            if (!currentCharacterInventory.CanEquipItemToSlot(item, targetEquipmentSlot))
                return;

            bool didEquip = currentCharacterInventory.TryEquipFromInventorySlot(
                currentGlobalInventory,
                sourceInventoryIndex,
                targetEquipmentSlot
            );

            if (!didEquip)
                return;

            SimpleSoundManager.Play(item.inventoryEndDragActionSoundConfig);

            RefreshInventorySlots();
            RefreshCharacterEquipmentSlots();
        }

        private void CharacterSlotClicked(RPGEquipmentSlotView slot, PointerEventData eventData)
        {
            if (slot == null || slot.IsEmpty)
                return;

            if (currentGlobalInventory == null || currentCharacterInventory == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Right)
                return;

            RPGItemDefinitionSO item = slot.CurrentItem;

            bool didUnequip = currentCharacterInventory.TryUnequipToInventorySlot(
                currentGlobalInventory,
                slot.AcceptedItemTypeSlot,
                -1
            );

            if (!didUnequip)
                return;

            SimpleSoundManager.Play(item.inventoryEndDragActionSoundConfig);

            RefreshInventorySlots();
            RefreshCharacterEquipmentSlots();
        }

        private RPGEquipmentSlot GetDefaultDoubleClickEquipSlot(RPGItemDefinitionSO item)
        {
            if (item == null)
                return RPGEquipmentSlot.noneNull;

            switch (item.itemType)
            {
                case RPGItemType.shield:
                    return RPGEquipmentSlot.offHand;

                case RPGItemType.oneHandedWeaponSword:
                case RPGItemType.oneHandedAxe:
                case RPGItemType.oneHandedSpear:
                case RPGItemType.oneHandedDagger:
                case RPGItemType.oneHandedHammer:
                    return RPGEquipmentSlot.mainHand;
            }

            if (item.itemTypeSlot == RPGEquipmentSlot.mainHand ||
                item.itemTypeSlot == RPGEquipmentSlot.offHand)
            {
                return RPGEquipmentSlot.mainHand;
            }

            return item.itemTypeSlot;
        }

        private void SmallIconsButtonClicked(CarvedPressButton theButton)
        {
            SwitchIconSizeLayout(IconLayoutState.small);
        }

        private void LargeIconsButtonClicked(CarvedPressButton theButton)
        {
            SwitchIconSizeLayout(IconLayoutState.large);
        }

        private void SwitchIconSizeLayout(IconLayoutState newState)
        {
            if (newState == currentIconLayoutState)
                return;

            currentIconLayoutState = newState;

            switch (newState)
            {
                case IconLayoutState.small:
                    inventoryGridLayout.cellSize = new Vector2(58, 58);
                    inventoryGridLayout.spacing = new Vector2(2, 2);
                    inventoryGridLayout.constraintCount = 7;
                    break;

                case IconLayoutState.large:
                    inventoryGridLayout.cellSize = new Vector2(100, 100);
                    inventoryGridLayout.spacing = new Vector2(4, 4);
                    inventoryGridLayout.constraintCount = 4;
                    break;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsSlotsContainer);
        }

        private void UnequipAllButtonClicked(CarvedPressButton theButton)
        {
            if (currentGlobalInventory == null || currentCharacterInventory == null)
                return;

            ClearDragStateVisuals();

            bool changedSomething = false;
            UISoundConfig soundToPlay = null;

            RPGEquipmentSlot[] equipmentSlots = GetEquipmentSlotOrder();

            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                RPGEquipmentSlot slotType = equipmentSlots[i];
                RPGItemDefinitionSO equippedItem = currentCharacterInventory.GetEquippedItem(slotType);

                if (equippedItem == null)
                    continue;

                bool didUnequip = currentCharacterInventory.TryUnequipToInventorySlot(
                    currentGlobalInventory,
                    slotType,
                    -1
                );

                if (!didUnequip)
                    continue;

                changedSomething = true;

                if (soundToPlay == null)
                    soundToPlay = equippedItem.inventoryEndDragActionSoundConfig;
            }

            if (!changedSomething)
                return;

            SimpleSoundManager.Play(soundToPlay);

            RefreshInventorySlots();
            RefreshCharacterEquipmentSlots();
        }

        private void AutoEquipButtonClicked(CarvedPressButton theButton)
        {
            if (currentGlobalInventory == null || currentCharacterInventory == null)
                return;

            ClearDragStateVisuals();

            bool changedSomething = false;
            UISoundConfig soundToPlay = null;

            RPGEquipmentSlot[] equipmentSlots = GetEquipmentSlotOrder();

            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                RPGEquipmentSlot targetSlot = equipmentSlots[i];

                int bestInventoryIndex = FindBestAutoEquipInventoryIndexForSlot(targetSlot);

                if (bestInventoryIndex < 0)
                    continue;

                RPGItemDefinitionSO bestInventoryItem = currentGlobalInventory.GetItemAtSlot(bestInventoryIndex);

                if (bestInventoryItem == null)
                    continue;

                RPGItemDefinitionSO currentlyEquippedItem = currentCharacterInventory.GetEquippedItem(targetSlot);

                // Do not replace equal or better equipped gear.
                // "First best tier" only matters among inventory candidates.
                if (currentlyEquippedItem != null && currentlyEquippedItem.itemTier >= bestInventoryItem.itemTier)
                    continue;

                bool didEquip = currentCharacterInventory.TryEquipFromInventorySlot(
                    currentGlobalInventory,
                    bestInventoryIndex,
                    targetSlot
                );

                if (!didEquip)
                    continue;

                changedSomething = true;

                if (soundToPlay == null)
                    soundToPlay = bestInventoryItem.inventoryEndDragActionSoundConfig;
            }

            if (!changedSomething)
                return;

            SimpleSoundManager.Play(soundToPlay);

            RefreshInventorySlots();
            RefreshCharacterEquipmentSlots();
        }

        private int FindBestAutoEquipInventoryIndexForSlot(RPGEquipmentSlot targetSlot)
        {
            IReadOnlyList<RPGItemDefinitionSO> items = currentGlobalInventory.Items;

            int bestIndex = -1;
            int bestTier = int.MinValue;

            for (int i = 0; i < items.Count; i++)
            {
                RPGItemDefinitionSO item = items[i];

                if (item == null)
                    continue;

                if (!IsAutoEquipCandidateForSlot(item, targetSlot))
                    continue;

                if (!currentCharacterInventory.CanEquipItemToSlot(item, targetSlot))
                    continue;

                // Strictly greater keeps the first found item when tiers are equal.
                if (item.itemTier > bestTier)
                {
                    bestTier = item.itemTier;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private bool IsAutoEquipCandidateForSlot(RPGItemDefinitionSO item, RPGEquipmentSlot targetSlot)
        {
            if (item == null)
                return false;

            switch (targetSlot)
            {
                case RPGEquipmentSlot.mainHand:
                    return IsOneHandedWeapon(item);

                case RPGEquipmentSlot.offHand:
                    return item.itemType == RPGItemType.shield;

                case RPGEquipmentSlot.helmet:
                case RPGEquipmentSlot.amulet:
                case RPGEquipmentSlot.ring:
                case RPGEquipmentSlot.cloak:
                case RPGEquipmentSlot.armor:
                case RPGEquipmentSlot.boots:
                    return item.itemTypeSlot == targetSlot;

                default:
                    return false;
            }
        }

        private bool IsOneHandedWeapon(RPGItemDefinitionSO item)
        {
            if (item == null)
                return false;

            switch (item.itemType)
            {
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

        private RPGEquipmentSlot[] GetEquipmentSlotOrder()
        {
            return new[]
            {
                RPGEquipmentSlot.helmet,
                RPGEquipmentSlot.armor,
                RPGEquipmentSlot.boots,
                RPGEquipmentSlot.cloak,
                RPGEquipmentSlot.amulet,
                RPGEquipmentSlot.ring,
                RPGEquipmentSlot.mainHand,
                RPGEquipmentSlot.offHand
            };
        }

        private void OnDestroy()
        {
            if (!wasInitialized)
                return;

            ClearDragStateVisuals();

            UnregisterFromCurrentInventory();
            UnregisterFromCurrentCharacterInventory();

            smallIconsButton.OnButtonActivatedClicked -= SmallIconsButtonClicked;
            largeIconsButton.OnButtonActivatedClicked -= LargeIconsButtonClicked;

            foreach (KeyValuePair<TextColorTabButton, InventoryTabFilter> pair in tabToFilter)
                pair.Key.OnButtonActivatedClicked -= InventoryTabClicked;

            UnregisterAllInventorySlots();
            UnregisterAllCharacterSlots();

            tabToFilter.Clear();
        }

        private void OnDisable()
        {
            autoEquipButton.gameObject.SetActive(false);
            unequipAllButton.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            autoEquipButton.gameObject.SetActive(true);
            unequipAllButton.gameObject.SetActive(true);
        }

        private void Reset()
        {
            inventoryGridLayout = GetComponentInChildren<GridLayoutGroup>();
            itemsSlotsContainer = inventoryGridLayout != null
                ? inventoryGridLayout.GetComponent<RectTransform>()
                : null;
        }
    }
}
