using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;
using Sunvale.Common.Tweening;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class RPGInventorySlotView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        ITweenClient
    {
        [Header("References")]
        public Image coreImage;
        public RectTransform myRectTransform;

        [Header("Sprites")]
        public Sprite emptySprite;

        [Header("Sound")]
        public UISoundConfig hoverSound;

        [Header("Animation Durations")]
        public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;
        public float dropTargetAnimationDuration = 0.08f;
        public float dragSourceAnimationDuration = 0.08f;

        [Header("Hover Values")]
        public float hoverBgBrightness = 1.16f;
        public float hoverBgSaturation = 1.08f;
        public float hoverItemBrightness = 1.12f;
        public float hoverItemSaturation = 1.12f;

        [Tooltip("If disabled, hover / pointer down / pointer exit states will keep the icon at its base item scale.")]
        public bool scaleIconOnInteraction = true;

        [Tooltip("Icon-only scale multiplier on hover. This changes shader _ItemScale, not the whole slot transform.")]
        public float hoverScale = 1.06f;

        [Header("Pointer Down Values")]
        public float pointerDownBgBrightness = 1.25f;
        public float pointerDownBgSaturation = 1.14f;
        public float pointerDownItemBrightness = 1.2f;
        public float pointerDownItemSaturation = 1.18f;

        [Tooltip("Icon-only scale multiplier on pointer down. This changes shader _ItemScale, not the whole slot transform.")]
        public float pointerDownScale = 0.94f;

        [Header("Icon Scale Context")]
        [Tooltip("Extra base icon scale for this slot context. Use 1 for normal square inventory slots. Use smaller values for circular equipment / character slots.")]
        public float slotIconScaleMultiplier = 1f;

        [Header("Drop Target Values")]
        public float dropTargetBgBrightness = 1.38f;
        public float dropTargetBgContrast = 1.16f;
        public float dropTargetItemBrightness = 1.25f;
        public float dropTargetScale = 1.05f;

        [Header("Drag Source Values")]
        public float dragSourceBgBrightness = 0.78f;
        public float dragSourceItemOpacity = 0.25f;
        public float dragSourceScale = 0.96f;

        [Header("Delete Candidate Values")]
        public float deleteCandidateBgBrightness = 0.65f;
        public float deleteCandidateItemOpacity = 0.42f;
        public float deleteCandidateScale = 0.88f;
        public Color deleteCandidateColor = new Color(1f, 0.35f, 0.35f, 0.85f);

        private static readonly int ItemOffsetId = Shader.PropertyToID("_ItemOffset");
        private static readonly int ItemScaleId = Shader.PropertyToID("_ItemScale");
        private static readonly int ItemOpacityId = Shader.PropertyToID("_ItemOpacity");
        private static readonly int ItemAspectScaleId = Shader.PropertyToID("_ItemAspectScale");
        private static readonly int ItemAtlasRectId = Shader.PropertyToID("_ItemAtlasRect");

        private static readonly int ItemBrightnessId = Shader.PropertyToID("_ItemBrightness");
        private static readonly int ItemContrastId = Shader.PropertyToID("_ItemContrast");
        private static readonly int ItemSaturationId = Shader.PropertyToID("_ItemSaturation");

        private static readonly int BgBrightnessId = Shader.PropertyToID("_BgBrightness");
        private static readonly int BgContrastId = Shader.PropertyToID("_BgContrast");
        private static readonly int BgSaturationId = Shader.PropertyToID("_BgSaturation");
        private static readonly int BgCenterColor = Shader.PropertyToID("_BgCenterColor");

        public RPGItemDefinitionSO CurrentItem { get; private set; }
        public int DisplayIndex { get; private set; }
        public int InventorySlotIndex { get; private set; }
        public int GlobalItemIndex => InventorySlotIndex;
        public bool IsEmpty => CurrentItem == null;

        public float ItemBaseIconScale => itemBaseIconScale;
        public float EffectiveBaseItemScale => baseItemScale;

        private InnerVisualState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;

        private bool isHovered;
        private bool isPressed;
        private bool isDragSource;
        private bool isDropTarget;
        private bool isDeleteCandidate;

        private float baseBgBrightness = 1f;
        private float baseBgContrast = 1f;
        private float baseBgSaturation = 1f;

        private float baseItemBrightness = 1f;
        private float baseItemContrast = 1f;
        private float baseItemSaturation = 1f;
        private float baseItemOpacity = 1f;

        private float itemBaseIconScale = 1f;

        [NonSerialized] public float baseItemScale = 1f;

        private float baseScale = 1f;
        private Color baseImageColor = Color.white;

        private float startBgBrightness;
        private float startBgContrast;
        private float startBgSaturation;

        private float startItemBrightness;
        private float startItemContrast;
        private float startItemSaturation;
        private float startItemOpacity;
        private float startItemScale;

        private float startScale;
        private Color startImageColor;

        public Color tierZeroColor;
        public Color tierOneColor;
        public Color tierTwoColor;

        public delegate void SlotPointerDelegate(RPGInventorySlotView slot, PointerEventData eventData);

        public event SlotPointerDelegate OnSlotBeginDragEvent;
        public event SlotPointerDelegate OnSlotDragEvent;
        public event SlotPointerDelegate OnSlotEndDragEvent;
        public event SlotPointerDelegate OnSlotClickEvent;

        private enum InnerVisualState
        {
            normal,
            hover,
            pointerDown,
            dropTarget,
            dragSource,
            deleteCandidate
        }

        public enum EaseType
        {
            EaseOutQuad,
            EaseOutCubic,
            EaseOutQuart
        }

        private void Awake()
        {
            EnsureMaterialInstance();
            CacheBaseValues();
        }

        private void EnsureMaterialInstance()
        {
            coreImage.material = new Material(coreImage.material);
        }

        private void CacheBaseValues()
        {
            baseBgBrightness = coreImage.material.GetFloat(BgBrightnessId);
            baseBgContrast = coreImage.material.GetFloat(BgContrastId);
            baseBgSaturation = coreImage.material.GetFloat(BgSaturationId);

            baseItemBrightness = coreImage.material.GetFloat(ItemBrightnessId);
            baseItemContrast = coreImage.material.GetFloat(ItemContrastId);
            baseItemSaturation = coreImage.material.GetFloat(ItemSaturationId);
            baseItemOpacity = 1;    

            itemBaseIconScale = coreImage.material.GetFloat(ItemScaleId);
            RebuildEffectiveBaseItemScale();

            baseScale = coreImage.transform.localScale.x;
            baseImageColor = coreImage.color;
        }

        public void BindToItem(RPGItemDefinitionSO item, int displayIndex, int inventorySlotIndex)
        {
            CurrentItem = item;
            DisplayIndex = displayIndex;
            InventorySlotIndex = inventorySlotIndex;

            coreImage.sprite = item.itemSprite;

            var itemTier = item.itemTier;

            switch (itemTier)
            {
                case 0:
                    coreImage.material.SetColor(BgCenterColor, tierZeroColor);
                    break;

                case 1:
                    coreImage.material.SetColor(BgCenterColor, tierOneColor);
                    break;

                case 2:
                    coreImage.material.SetColor(BgCenterColor, tierTwoColor);
                    break;
            }

            float uvOffsetX = item.itemIconXOffset / 100f;
            float uvOffsetY = item.itemIconYOffset / 100f;
            coreImage.material.SetVector(ItemOffsetId, new Vector4(uvOffsetX, uvOffsetY, 0f, 0f));

            float aspectScaleX = 1f;
            float aspectScaleY = 1f;
            Vector4 atlasRect = new Vector4(0f, 0f, 1f, 1f);

            if (item.itemSprite != null)
            {
                float spriteWidth = item.itemSprite.textureRect.width;
                float spriteHeight = item.itemSprite.textureRect.height;

                if (spriteWidth > 0 && spriteHeight > 0)
                {
                    float aspect = spriteWidth / spriteHeight;

                    if (aspect >= 1f)
                        aspectScaleY = aspect;
                    else
                        aspectScaleX = 1f / aspect;
                }

                Texture2D tex = item.itemSprite.texture;
                Rect texRect = item.itemSprite.textureRect;

                if (tex != null && tex.width > 0 && tex.height > 0)
                {
                    atlasRect = new Vector4(
                        texRect.x / tex.width,
                        texRect.y / tex.height,
                        texRect.width / tex.width,
                        texRect.height / tex.height
                    );
                }
            }

            coreImage.material.SetVector(ItemAspectScaleId, new Vector4(aspectScaleX, aspectScaleY, 0f, 0f));
            coreImage.material.SetVector(ItemAtlasRectId, atlasRect);

            itemBaseIconScale = item.itemIconSlotScale;
            RebuildEffectiveBaseItemScale();

            coreImage.material.SetFloat(ItemScaleId, baseItemScale);
            coreImage.material.SetFloat(ItemOpacityId, baseItemOpacity);

            ResetInteractionFlags();

            ApplyVisualValuesImmediately(
                baseBgBrightness,
                baseBgContrast,
                baseBgSaturation,
                baseItemBrightness,
                baseItemContrast,
                baseItemSaturation,
                baseItemOpacity,
                baseItemScale,
                baseScale,
                baseImageColor
            );
        }

        public void SetToEmpty(int displayIndex, int inventorySlotIndex)
        {
            CurrentItem = null;
            DisplayIndex = displayIndex;
            InventorySlotIndex = inventorySlotIndex;

            coreImage.sprite = emptySprite;

            coreImage.material.SetVector(ItemOffsetId, Vector4.zero);
            coreImage.material.SetVector(ItemAspectScaleId, new Vector4(1f, 1f, 0f, 0f));
            coreImage.material.SetVector(ItemAtlasRectId, new Vector4(0f, 0f, 1f, 1f));

            itemBaseIconScale = 1f;
            RebuildEffectiveBaseItemScale();

            coreImage.material.SetFloat(ItemScaleId, baseItemScale);
            coreImage.material.SetFloat(ItemOpacityId, 1f);
            coreImage.material.SetColor(BgCenterColor, tierZeroColor);

            ResetInteractionFlags();

            ApplyVisualValuesImmediately(
                baseBgBrightness,
                baseBgContrast,
                baseBgSaturation,
                baseItemBrightness,
                baseItemContrast,
                baseItemSaturation,
                0f,
                baseItemScale,
                baseScale,
                baseImageColor
            );
        }

        private void ResetInteractionFlags()
        {
            isHovered = false;
            isPressed = false;
            isDragSource = false;
            isDropTarget = false;
            isDeleteCandidate = false;
            myInnerState = InnerVisualState.normal;
            SimpleTweenManager.UnregisterTween(this);
        }

        public void SetDragSourceVisual(bool value)
        {
            if (isDragSource == value)
                return;

            isDragSource = value;
            RefreshVisualState();
        }

        public void SetDropTargetVisual(bool value)
        {
            if (isDropTarget == value)
                return;

            isDropTarget = value;
            RefreshVisualState();
        }

        public void SetDeleteCandidateVisual(bool value)
        {
            if (isDeleteCandidate == value)
                return;

            isDeleteCandidate = value;
            RefreshVisualState();
        }

        public void SetSlotIconScaleMultiplier(float multiplier, bool snapNow = true)
        {
            slotIconScaleMultiplier = Mathf.Max(0.0001f, multiplier);
            RebuildEffectiveBaseItemScale();
            ApplyBaseItemScaleChange(snapNow);
        }

        public void SetItemBaseIconScale(float itemScale, bool snapNow = true)
        {
            itemBaseIconScale = Mathf.Max(0.0001f, itemScale);
            RebuildEffectiveBaseItemScale();
            ApplyBaseItemScaleChange(snapNow);
        }

        public void SetEffectiveBaseItemScale(float effectiveBaseScale, bool snapNow = true)
        {
            slotIconScaleMultiplier = Mathf.Max(0.0001f, slotIconScaleMultiplier);
            baseItemScale = Mathf.Max(0.0001f, effectiveBaseScale);
            itemBaseIconScale = baseItemScale / slotIconScaleMultiplier;
            ApplyBaseItemScaleChange(snapNow);
        }

        public void SetBaseItemScale(float effectiveBaseScale, bool snapNow = true)
        {
            SetEffectiveBaseItemScale(effectiveBaseScale, snapNow);
        }

        private void RebuildEffectiveBaseItemScale()
        {
            itemBaseIconScale = Mathf.Max(0.0001f, itemBaseIconScale);
            slotIconScaleMultiplier = Mathf.Max(0.0001f, slotIconScaleMultiplier);
            baseItemScale = itemBaseIconScale * slotIconScaleMultiplier;
        }

        private void ApplyBaseItemScaleChange(bool snapNow)
        {
            if (coreImage == null || coreImage.material == null)
                return;

            if (snapNow)
                coreImage.material.SetFloat(ItemScaleId, baseItemScale);

            if (isActiveAndEnabled)
                RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            if (isDeleteCandidate)
            {
                SetupTransition(InnerVisualState.deleteCandidate);
                return;
            }

            if (isDragSource)
            {
                SetupTransition(InnerVisualState.dragSource);
                return;
            }

            if (isDropTarget)
            {
                SetupTransition(InnerVisualState.dropTarget);
                return;
            }

            if (isPressed)
            {
                SetupTransition(InnerVisualState.pointerDown);
                return;
            }

            if (isHovered)
            {
                SetupTransition(InnerVisualState.hover);
                return;
            }

            SetupTransition(InnerVisualState.normal);
        }

        private void SetupTransition(InnerVisualState targetState)
        {
            myInnerState = targetState;
            elapsedTime = 0f;

            startBgBrightness = coreImage.material.GetFloat(BgBrightnessId);
            startBgContrast = coreImage.material.GetFloat(BgContrastId);
            startBgSaturation = coreImage.material.GetFloat(BgSaturationId);

            startItemBrightness = coreImage.material.GetFloat(ItemBrightnessId);
            startItemContrast = coreImage.material.GetFloat(ItemContrastId);
            startItemSaturation = coreImage.material.GetFloat(ItemSaturationId);
            startItemOpacity = coreImage.material.GetFloat(ItemOpacityId);
            startItemScale = coreImage.material.GetFloat(ItemScaleId);

            startScale = coreImage.transform.localScale.x;
            startImageColor = coreImage.color;

            SimpleTweenManager.RegisterTween(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            RefreshVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            RefreshVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            RefreshVisualState();
            SimpleSoundManager.Play(hoverSound);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
            RefreshVisualState();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            OnSlotBeginDragEvent?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            OnSlotDragEvent?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            isPressed = false;
            OnSlotEndDragEvent?.Invoke(this, eventData);
        }

        public void CustomUpdate(float deltaTime)
        {
            switch (myInnerState)
            {
                case InnerVisualState.normal:
                    AnimateProperties(
                        baseBgBrightness,
                        baseBgContrast,
                        baseBgSaturation,
                        baseItemBrightness,
                        baseItemContrast,
                        baseItemSaturation,
                        IsEmpty ? 0f : baseItemOpacity,
                        baseItemScale,
                        baseScale,
                        baseImageColor,
                        pointerExitAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutCubic
                    );
                    break;

                case InnerVisualState.hover:
                    AnimateProperties(
                        hoverBgBrightness,
                        baseBgContrast,
                        hoverBgSaturation,
                        hoverItemBrightness,
                        baseItemContrast,
                        hoverItemSaturation,
                        IsEmpty ? 0f : baseItemOpacity,
                        GetInteractionItemScale(hoverScale),
                        baseScale,
                        baseImageColor,
                        hoverAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutQuad
                    );
                    break;

                case InnerVisualState.pointerDown:
                    AnimateProperties(
                        pointerDownBgBrightness,
                        baseBgContrast,
                        pointerDownBgSaturation,
                        pointerDownItemBrightness,
                        baseItemContrast,
                        pointerDownItemSaturation,
                        IsEmpty ? 0f : baseItemOpacity,
                        GetInteractionItemScale(pointerDownScale),
                        baseScale,
                        baseImageColor,
                        pointerDownAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutQuart
                    );
                    break;

                case InnerVisualState.dropTarget:
                    AnimateProperties(
                        dropTargetBgBrightness,
                        dropTargetBgContrast,
                        baseBgSaturation,
                        dropTargetItemBrightness,
                        baseItemContrast,
                        baseItemSaturation,
                        IsEmpty ? 0f : baseItemOpacity,
                        baseItemScale,
                        dropTargetScale,
                        baseImageColor,
                        dropTargetAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutQuad
                    );
                    break;

                case InnerVisualState.dragSource:
                    AnimateProperties(
                        dragSourceBgBrightness,
                        baseBgContrast,
                        baseBgSaturation,
                        baseItemBrightness,
                        baseItemContrast,
                        baseItemSaturation,
                        dragSourceItemOpacity,
                        baseItemScale,
                        dragSourceScale,
                        baseImageColor,
                        dragSourceAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutQuad
                    );
                    break;

                case InnerVisualState.deleteCandidate:
                    AnimateProperties(
                        deleteCandidateBgBrightness,
                        baseBgContrast,
                        baseBgSaturation,
                        baseItemBrightness,
                        baseItemContrast,
                        baseItemSaturation,
                        deleteCandidateItemOpacity,
                        baseItemScale,
                        deleteCandidateScale,
                        deleteCandidateColor,
                        dragSourceAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutQuad
                    );
                    break;
            }
        }

        private void AnimateProperties(
            float targetBgBrightness,
            float targetBgContrast,
            float targetBgSaturation,
            float targetItemBrightness,
            float targetItemContrast,
            float targetItemSaturation,
            float targetItemOpacity,
            float targetItemScale,
            float targetScale,
            Color targetImageColor,
            float duration,
            float deltaTime,
            EaseType easeType
        )
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            coreImage.material.SetFloat(BgBrightnessId, Mathf.Lerp(startBgBrightness, targetBgBrightness, tEased));
            coreImage.material.SetFloat(BgContrastId, Mathf.Lerp(startBgContrast, targetBgContrast, tEased));
            coreImage.material.SetFloat(BgSaturationId, Mathf.Lerp(startBgSaturation, targetBgSaturation, tEased));

            coreImage.material.SetFloat(ItemBrightnessId, Mathf.Lerp(startItemBrightness, targetItemBrightness, tEased));
            coreImage.material.SetFloat(ItemContrastId, Mathf.Lerp(startItemContrast, targetItemContrast, tEased));
            coreImage.material.SetFloat(ItemSaturationId, Mathf.Lerp(startItemSaturation, targetItemSaturation, tEased));
            coreImage.material.SetFloat(ItemOpacityId, Mathf.Lerp(startItemOpacity, targetItemOpacity, tEased));
            coreImage.material.SetFloat(ItemScaleId, Mathf.Lerp(startItemScale, targetItemScale, tEased));

            float currentScale = Mathf.Lerp(startScale, targetScale, tEased);
            coreImage.transform.localScale = new Vector3(currentScale, currentScale, currentScale);

            coreImage.color = Color.Lerp(startImageColor, targetImageColor, tEased);

            if (t >= 1f)
                SimpleTweenManager.UnregisterTween(this);
        }

        private float GetInteractionItemScale(float interactionScale)
        {
            if (!scaleIconOnInteraction)
                return baseItemScale;

            return baseItemScale * interactionScale;
        }

        private void ApplyVisualValuesImmediately(
            float bgBrightness,
            float bgContrast,
            float bgSaturation,
            float itemBrightness,
            float itemContrast,
            float itemSaturation,
            float itemOpacity,
            float itemScale,
            float scale,
            Color imageColor
        )
        {
            coreImage.material.SetFloat(BgBrightnessId, bgBrightness);
            coreImage.material.SetFloat(BgContrastId, bgContrast);
            coreImage.material.SetFloat(BgSaturationId, bgSaturation);

            coreImage.material.SetFloat(ItemBrightnessId, itemBrightness);
            coreImage.material.SetFloat(ItemContrastId, itemContrast);
            coreImage.material.SetFloat(ItemSaturationId, itemSaturation);
            coreImage.material.SetFloat(ItemOpacityId, itemOpacity);
            coreImage.material.SetFloat(ItemScaleId, itemScale);

            coreImage.transform.localScale = new Vector3(scale, scale, scale);
            coreImage.color = imageColor;
        }

        private float GetEasedTime(float t, EaseType easeType)
        {
            switch (easeType)
            {
                case EaseType.EaseOutQuad:
                    return 1f - (1f - t) * (1f - t);

                case EaseType.EaseOutCubic:
                    float invCubic = 1f - t;
                    return 1f - invCubic * invCubic * invCubic;

                case EaseType.EaseOutQuart:
                    float invQuart = 1f - t;
                    return 1f - invQuart * invQuart * invQuart * invQuart;

                default:
                    return t;
            }
        }

        private void OnDisable()
        {
            SimpleTweenManager.UnregisterTween(this);
        }

        public void SetIndexNumber(int number)
        {
            myTweenNumber = number;
        }

        public int GetIndexNumber()
        {
            return myTweenNumber;
        }

        private void Reset()
        {
            myRectTransform = GetComponent<RectTransform>();
            coreImage = GetComponent<Image>();
        }

        private void OnValidate()
        {
            slotIconScaleMultiplier = Mathf.Max(0.0001f, slotIconScaleMultiplier);
            hoverScale = Mathf.Max(0.0001f, hoverScale);
            pointerDownScale = Mathf.Max(0.0001f, pointerDownScale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsEmpty)
                return;

            OnSlotClickEvent?.Invoke(this, eventData);
        }
    }

}
