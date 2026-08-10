using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class RPGEquipmentSlotView : MonoBehaviour
    {
        [Header("References")] public RPGInventorySlotView slotDisplayer;

        [Header("Accepted Item Slot")] public RPGEquipmentSlot acceptedItemTypeSlot = RPGEquipmentSlot.noneNull;

        private RPGCharacterInventory currentCharacterInventory;
        private bool wasInitialized;

        public RPGItemDefinitionSO CurrentItem { get; private set; }
        public RPGEquipmentSlot AcceptedItemTypeSlot => acceptedItemTypeSlot;
        public bool IsEmpty => CurrentItem == null;

        public delegate void CharacterSlotPointerDelegate(RPGEquipmentSlotView slot, PointerEventData eventData);

        public event CharacterSlotPointerDelegate OnSlotBeginDragEvent;
        public event CharacterSlotPointerDelegate OnSlotDragEvent;
        public event CharacterSlotPointerDelegate OnSlotEndDragEvent;
        
        public event CharacterSlotPointerDelegate OnSlotClickEvent;


        private static readonly int ItemOffsetId = Shader.PropertyToID("_ItemOffset");
        private static readonly int ItemScaleId = Shader.PropertyToID("_ItemScale");
        private static readonly int ItemOpacityId = Shader.PropertyToID("_ItemOpacity");
        private static readonly int ItemAspectScaleId = Shader.PropertyToID("_ItemAspectScale");
        private static readonly int ItemAtlasRectId = Shader.PropertyToID("_ItemAtlasRect");

        private void Awake()
        {
            InnerInitialization();
        }

        private void InnerInitialization()
        {
            if (wasInitialized)
                return;

            wasInitialized = true;

            if (slotDisplayer == null)
                slotDisplayer = GetComponentInChildren<RPGInventorySlotView>();

            slotDisplayer.OnSlotBeginDragEvent += InnerSlotBeginDrag;
            slotDisplayer.OnSlotDragEvent += InnerSlotDrag;
            slotDisplayer.OnSlotEndDragEvent += InnerSlotEndDrag;
            slotDisplayer.OnSlotClickEvent += InnerSlotClick;
        }

        public void BindToCharacterInventory(RPGCharacterInventory characterInventory, RPGEquipmentSlot newAcceptedSlot)
        {
            InnerInitialization();

            currentCharacterInventory = characterInventory;
            acceptedItemTypeSlot = newAcceptedSlot;

            RefreshFromInventory();
        }

        public void SetAcceptedItemTypeSlot(RPGEquipmentSlot newAcceptedSlot)
        {
            acceptedItemTypeSlot = newAcceptedSlot;
        }

        public bool AcceptsItem(RPGItemDefinitionSO item)
        {
            return RPGCharacterInventory.IsItemAllowedForSlot(item, acceptedItemTypeSlot);
        }

        public void RefreshFromInventory()
        {
            CurrentItem = currentCharacterInventory.GetEquippedItem(acceptedItemTypeSlot);


            if (CurrentItem != null)
                slotDisplayer.BindToItem(CurrentItem, 0, -1);
            else
                slotDisplayer.SetToEmpty(0, -1);

            if (CurrentItem != null)
            {
                float circleScaleMultiplier = 0.95f;

                // Circle-only visual nudge, still in 100x100 slot pixels.
                float circleExtraXOffsetPixels = 0f;
                float circleExtraYOffsetPixels = 0f;

                if (CurrentItem.itemType == RPGItemType.shield)
                {
                    circleScaleMultiplier = 0.65f;
                   // circleExtraYOffsetPixels = 2f;
                }

                float offsetX = (CurrentItem.itemIconXOffset + circleExtraXOffsetPixels) / 100f;
                float offsetY = (CurrentItem.itemIconYOffset + circleExtraYOffsetPixels) / 100f;

                slotDisplayer.coreImage.material.SetFloat(
                    ItemScaleId,
                    CurrentItem.itemIconSlotScale * circleScaleMultiplier
                );

                slotDisplayer.SetEffectiveBaseItemScale(CurrentItem.itemIconSlotScale * circleScaleMultiplier);
               
                slotDisplayer.coreImage.material.SetVector(
                    ItemOffsetId,
                    new Vector4(offsetX, offsetY, 0f, 0f)
                );
            }
        }

        public void SetDragSourceVisual(bool value)
        {
            slotDisplayer.SetDragSourceVisual(value);
        }

        public void SetDropCandidateVisual(bool value, bool canAccept)
        {
            if (!value)
            {
                slotDisplayer.SetDropTargetVisual(false);
                slotDisplayer.SetDeleteCandidateVisual(false);
                return;
            }

            slotDisplayer.SetDropTargetVisual(canAccept);
            slotDisplayer.SetDeleteCandidateVisual(!canAccept);
        }

        private void InnerSlotBeginDrag(RPGInventorySlotView slot, PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            OnSlotBeginDragEvent?.Invoke(this, eventData);
        }

        private void InnerSlotDrag(RPGInventorySlotView slot, PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            OnSlotDragEvent?.Invoke(this, eventData);
        }

        private void InnerSlotEndDrag(RPGInventorySlotView slot, PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            OnSlotEndDragEvent?.Invoke(this, eventData);
        }
        
        private void InnerSlotClick(RPGInventorySlotView slot, PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            OnSlotClickEvent?.Invoke(this, eventData);
        }

        private void OnDestroy()
        {
            if (!wasInitialized)
                return;

            slotDisplayer.OnSlotBeginDragEvent -= InnerSlotBeginDrag;
            slotDisplayer.OnSlotDragEvent -= InnerSlotDrag;
            slotDisplayer.OnSlotEndDragEvent -= InnerSlotEndDrag;
            slotDisplayer.OnSlotClickEvent -= InnerSlotClick;
        }

        private void Reset()
        {
            slotDisplayer = GetComponentInChildren<RPGInventorySlotView>();
        }
        
        
    }
}
