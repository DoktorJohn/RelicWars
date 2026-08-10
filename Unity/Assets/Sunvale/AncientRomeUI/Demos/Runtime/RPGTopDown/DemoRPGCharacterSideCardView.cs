using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;
using Sunvale.Common.Tweening;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.HealthBars;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    [AddComponentMenu("Sunvale/RPG/DemoRPGCharacterSideCardView")]
    public class DemoRPGCharacterSideCardView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        ITweenClient
    {
        [Header("References")] public TextMeshProUGUI nameLabel;
        public Image portraitImage;

        [Tooltip("Main marble card background. This is NOT animated.")]
        public Image backgroundImage;

        [Tooltip("Portrait background. This gets its own HSV material instance.")]
        public Image charBackgroundImage;

        [Tooltip("Main outer frame. Uses shared frame HSV material instance. No color tinting.")]
        public Image frameImage;

        [Tooltip("Portrait frame. Uses shared frame HSV material instance. No color tinting.")]
        public Image portraitFrame;

        public AnimatedHealthBarFill hpBar;
        public AnimatedHealthBarFill staminaManaBar;

        public RectTransform buffContainer;

        [Tooltip("Maximum number of global buff visuals shown at the same time. A Horizontal Layout Group on buffContainer will arrange the active ones.")]
        [Min(1)] public int buffLimit = 6;

        [Header("Global Buff Visual Pool")]
        [Tooltip("Prefab used for new stacked buff visuals. If this is left empty, the first DemoRPGBuffIconView found under buffContainer is used as the clone template.")]
        public DemoRPGBuffIconView globalBuffVisualPrefab;

        [Tooltip("Extra icon scale applied after copying the skill button icon scale.")]
        [SerializeField, Min(0.01f)] private float globalBuffIconScaleMultiplier = 1.15f;

        [SerializeField] private bool prewarmGlobalBuffPool = true;

        [Tooltip("When the visible stack is full, restart the buff with the least remaining time. If false, extra published buffs are ignored until a slot expires.")]
        [SerializeField] private bool recycleOldestBuffWhenPoolFull = true;

        [Tooltip("Deactivate any manually placed buff visuals under the container during Awake so they behave as pooled slots/templates, not as already-visible buffs.")]
        [SerializeField] private bool deactivateSceneBuffVisualsOnAwake = true;

        private readonly List<DemoRPGBuffIconView> globalBuffPool = new List<DemoRPGBuffIconView>();

        [Header("Interaction Root")] public RectTransform cardRect;
        public Transform scaleRoot;

        [Header("Sound")] public UISoundConfig hoverSoundConfig;
        public UISoundConfig clickSoundConfig;

        [Header("Animation Durations")] public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("Hover Width")] public float hoverWidthMultiplier = 1.06f;

        [Header("Card Scale")] public float hoverScale = 1f;
        public float clickScale = 0.975f;

        [Header("Portrait Scale")] public float hoverPortraitScale = 1.035f;
        public float clickPortraitScale = 1.01f;

        [Header("Character Background HSV")] public float hoverBackgroundSaturationMultiplier = 1.12f;
        public float hoverBackgroundBrightnessMultiplier = 1.10f;

        public float clickDownBackgroundSaturationMultiplier = 1.22f;
        public float clickDownBackgroundBrightnessMultiplier = 1.18f;

        [Header("Frame HSV")] public float hoverFrameSaturationMultiplier = 1.12f;
        public float hoverFrameBrightnessMultiplier = 1.10f;

        public float clickDownFrameSaturationMultiplier = 1.22f;
        public float clickDownFrameBrightnessMultiplier = 1.18f;

        [Header("Unity Events")] public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent cardActivatedClicked;

        public delegate void CharacterSideCardInteraction(DemoRPGCharacterSideCardView card);

        public event CharacterSideCardInteraction OnPointerDownEvent;
        public event CharacterSideCardInteraction OnPointerUpEvent;
        public event CharacterSideCardInteraction OnCardPointerEnterEvent;
        public event CharacterSideCardInteraction OnCardPointerExitEvent;
        public event CharacterSideCardInteraction OnCardActivatedClicked;

        private enum InteractionState
        {
            exitNothingHappening,
            hoverState,
            pointerDownState
        }

        private enum EaseType
        {
            EaseOutQuad,
            EaseOutCubic,
            EaseOutQuart
        }

        private InteractionState myInnerState = InteractionState.exitNothingHappening;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered;

        private RPGCharacterData boundCharacter;
        private bool isRegisteredToCharacterDirty;
        private bool hasCompletedFirstCharacterBind;

        private float baseWidth;
        private float startWidth;

        private Vector3 baseScale;
        private Vector3 startScale;

        private Vector3 basePortraitScale;
        private Vector3 startPortraitScale;

        private Material charBackgroundMaterialInstance;
        private Material sharedFrameMaterialInstance;

        private bool charBackgroundUsesHsv;
        private bool frameUsesHsv;

        private float baseBackgroundSaturation = 1f;
        private float baseBackgroundBrightness = 1f;

        private float startBackgroundSaturation;
        private float startBackgroundBrightness;

        private float baseFrameSaturation = 1f;
        private float baseFrameBrightness = 1f;

        private float startFrameSaturation;
        private float startFrameBrightness;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBright = Shader.PropertyToID("_HsvBright");

        private void Awake()
        {
            if (cardRect == null)
                cardRect = transform as RectTransform;

            if (scaleRoot == null)
                scaleRoot = transform;

            if (cardRect != null)
                baseWidth = cardRect.rect.width;

            if (scaleRoot != null)
                baseScale = scaleRoot.localScale;
            else
                baseScale = Vector3.one;

            if (portraitImage != null)
                basePortraitScale = portraitImage.rectTransform.localScale;
            else
                basePortraitScale = Vector3.one;

            SetupHsvMaterials();
            SetupGlobalBuffPool();
        }

        private void OnEnable()
        {
            RegisterToCharacterDirtyEvent();

            if (boundCharacter != null)
            {
                ApplyCharacterData(animateBars: false);
                hasCompletedFirstCharacterBind = true;
            }
        }

        public void BindToCharacter(RPGCharacterData character)
        {
            if (boundCharacter == character)
            {
                ApplyCharacterData(animateBars: false);
                hasCompletedFirstCharacterBind = true;
                return;
            }

            UnregisterFromCharacterDirtyEvent();

            boundCharacter = character;
            hasCompletedFirstCharacterBind = false;

            RegisterToCharacterDirtyEvent();

            ApplyCharacterData(animateBars: false);
            hasCompletedFirstCharacterBind = true;
        }

        private void RegisterToCharacterDirtyEvent()
        {
            if (boundCharacter == null)
                return;

            if (isRegisteredToCharacterDirty)
                return;

            boundCharacter.OnCharacterDirty += HandleBoundCharacterDirty;
            isRegisteredToCharacterDirty = true;
        }

        private void UnregisterFromCharacterDirtyEvent()
        {
            if (boundCharacter == null)
            {
                isRegisteredToCharacterDirty = false;
                return;
            }

            if (!isRegisteredToCharacterDirty)
                return;

            boundCharacter.OnCharacterDirty -= HandleBoundCharacterDirty;
            isRegisteredToCharacterDirty = false;
        }

        private void HandleBoundCharacterDirty(RPGCharacterData character)
        {
            if (character == null)
                return;

            if (character != boundCharacter)
                return;

            ApplyCharacterData(animateBars: hasCompletedFirstCharacterBind);
            hasCompletedFirstCharacterBind = true;
        }


        public void PublishGlobalBuff(RPGSkillButton sourceButton, float duration)
        {
            if (sourceButton == null)
                return;

            DemoRPGBuffIconView visual = GetGlobalBuffVisualFromPool();

            if (visual == null)
                return;

            visual.IconScaleMultiplier = globalBuffIconScaleMultiplier;
            visual.SpawnFrom(sourceButton, duration);
            visual.transform.SetAsLastSibling();
            RebuildBuffLayout();
        }

        private void SetupGlobalBuffPool()
        {
            CollectExistingGlobalBuffPoolItems(deactivateCollectedVisuals: deactivateSceneBuffVisualsOnAwake);

            if (!Application.isPlaying)
                return;

            if (!prewarmGlobalBuffPool)
                return;

            int targetCount = GetSafeBuffLimit();

            for (int i = globalBuffPool.Count; i < targetCount; i++)
            {
                if (CreateGlobalBuffVisual() == null)
                    break;
            }
        }

        private void CollectExistingGlobalBuffPoolItems(bool deactivateCollectedVisuals = false)
        {
            if (buffContainer == null)
                return;

            DemoRPGBuffIconView[] existing = buffContainer.GetComponentsInChildren<DemoRPGBuffIconView>(true);

            for (int i = 0; i < existing.Length; i++)
            {
                DemoRPGBuffIconView visual = existing[i];

                if (visual == null)
                    continue;

                if (!globalBuffPool.Contains(visual))
                    globalBuffPool.Add(visual);

                visual.IconScaleMultiplier = globalBuffIconScaleMultiplier;

                if (Application.isPlaying && deactivateCollectedVisuals)
                    visual.DespawnInstant();
            }
        }

        private DemoRPGBuffIconView GetGlobalBuffVisualFromPool()
        {
            CollectExistingGlobalBuffPoolItems();
            RemoveNullGlobalBuffPoolEntries();

            int limit = GetSafeBuffLimit();
            int activeCount = GetActiveGlobalBuffCount();

            if (activeCount >= limit)
            {
                if (!recycleOldestBuffWhenPoolFull)
                    return null;

                DemoRPGBuffIconView oldest = GetOldestActiveGlobalBuffVisual();

                if (oldest == null)
                    return null;

                oldest.DespawnInstant();
                RebuildBuffLayout();
                return oldest;
            }

            DemoRPGBuffIconView inactiveVisual = GetFirstInactiveGlobalBuffVisual();

            if (inactiveVisual != null)
                return inactiveVisual;

            return CreateGlobalBuffVisual();
        }

        private DemoRPGBuffIconView GetFirstInactiveGlobalBuffVisual()
        {
            for (int i = 0; i < globalBuffPool.Count; i++)
            {
                DemoRPGBuffIconView visual = globalBuffPool[i];

                if (visual == null)
                    continue;

                if (!visual.gameObject.activeSelf)
                    return visual;
            }

            return null;
        }

        private DemoRPGBuffIconView GetOldestActiveGlobalBuffVisual()
        {
            DemoRPGBuffIconView oldest = null;
            float lowestRemaining = float.MaxValue;

            for (int i = 0; i < globalBuffPool.Count; i++)
            {
                DemoRPGBuffIconView visual = globalBuffPool[i];

                if (visual == null)
                    continue;

                if (!visual.gameObject.activeSelf)
                    continue;

                if (visual.RemainingSeconds < lowestRemaining)
                {
                    oldest = visual;
                    lowestRemaining = visual.RemainingSeconds;
                }
            }

            return oldest;
        }

        private int GetActiveGlobalBuffCount()
        {
            int count = 0;

            for (int i = 0; i < globalBuffPool.Count; i++)
            {
                DemoRPGBuffIconView visual = globalBuffPool[i];

                if (visual == null)
                    continue;

                if (visual.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private int GetSafeBuffLimit()
        {
            return Mathf.Max(1, buffLimit);
        }

        private DemoRPGBuffIconView CreateGlobalBuffVisual()
        {
            DemoRPGBuffIconView template = GetGlobalBuffVisualTemplate();

            if (template == null)
                return null;

            Transform parent = buffContainer != null ? buffContainer : transform;
            DemoRPGBuffIconView visual = Instantiate(template, parent);
            visual.name = template.name + "_Pooled_" + globalBuffPool.Count.ToString("00");
            visual.IconScaleMultiplier = globalBuffIconScaleMultiplier;
            visual.DespawnInstant();

            globalBuffPool.Add(visual);
            return visual;
        }

        private DemoRPGBuffIconView GetGlobalBuffVisualTemplate()
        {
            if (globalBuffVisualPrefab != null)
                return globalBuffVisualPrefab;

            for (int i = 0; i < globalBuffPool.Count; i++)
            {
                DemoRPGBuffIconView visual = globalBuffPool[i];

                if (visual != null)
                    return visual;
            }

            return null;
        }

        private void RemoveNullGlobalBuffPoolEntries()
        {
            for (int i = globalBuffPool.Count - 1; i >= 0; i--)
            {
                if (globalBuffPool[i] == null)
                    globalBuffPool.RemoveAt(i);
            }
        }

        private void ClearGlobalBuffVisuals()
        {
            for (int i = 0; i < globalBuffPool.Count; i++)
            {
                DemoRPGBuffIconView visual = globalBuffPool[i];

                if (visual == null)
                    continue;

                visual.DespawnInstant();
            }

            RebuildBuffLayout();
        }

        private void RebuildBuffLayout()
        {
            if (buffContainer == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(buffContainer);
        }

        private void ApplyCharacterData(bool animateBars)
        {
            if (boundCharacter == null)
            {
                ClearCharacterData();
                return;
            }

            nameLabel.text = GetLastName(boundCharacter.CharacterName);
            portraitImage.sprite = boundCharacter.portraitSprite;
            portraitImage.enabled = boundCharacter.portraitSprite != null;


            float currentHp = boundCharacter.GetBaseStat(RPGStatType.HP);
            float maxHp = boundCharacter.GetStat(RPGStatType.MaxHP);

            float currentStamina = boundCharacter.GetBaseStat(RPGStatType.Stamina);
            float maxStamina = boundCharacter.GetStat(RPGStatType.MaxStamina);

            ApplyResourceBar(hpBar, currentHp, maxHp, animateBars);
            ApplyResourceBar(staminaManaBar, currentStamina, maxStamina, animateBars);
        }

        private void ClearCharacterData()
        {
            if (nameLabel != null)
                nameLabel.text = string.Empty;

            if (portraitImage != null)
            {
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }

            if (hpBar != null)
                hpBar.SetHealthInstant(0f, 0f);

            if (staminaManaBar != null)
                staminaManaBar.SetHealthInstant(0f, 0f);
        }

        private static void ApplyResourceBar(
            AnimatedHealthBarFill bar,
            float currentValue,
            float maxValue,
            bool animate)
        {
            if (bar == null)
                return;

            if (animate)
                bar.AnimateToHealth(currentValue, maxValue);
            else
                bar.SetHealthInstant(currentValue, maxValue);
        }

        private void SetupHsvMaterials()
        {
            if (charBackgroundImage == null)
                return;

            Material sourceMaterial = charBackgroundImage.material;

            if (sourceMaterial == null)
                return;

            bool hasHsv =
                sourceMaterial.HasProperty(HsvSaturation) &&
                sourceMaterial.HasProperty(HsvBright);

            if (!hasHsv)
            {
                Debug.LogWarning(
                    $"{nameof(DemoRPGCharacterSideCardView)} on {gameObject.name}: " +
                    $"{nameof(charBackgroundImage)} material does not have _HsvSaturation and _HsvBright. " +
                    "Assign the HSV-capable UI material to the portrait background image.",
                    this
                );

                return;
            }

            charBackgroundMaterialInstance = new Material(sourceMaterial);
            charBackgroundMaterialInstance.name =
                $"{sourceMaterial.name}_CharacterSideCard_Background_{gameObject.name}";

            charBackgroundImage.material = charBackgroundMaterialInstance;
            charBackgroundImage.SetMaterialDirty();

            charBackgroundUsesHsv = true;

            baseBackgroundSaturation = charBackgroundMaterialInstance.GetFloat(HsvSaturation);
            baseBackgroundBrightness = charBackgroundMaterialInstance.GetFloat(HsvBright);

            ApplyBackgroundHsv(baseBackgroundSaturation, baseBackgroundBrightness);

            sharedFrameMaterialInstance = new Material(sourceMaterial);
            sharedFrameMaterialInstance.name =
                $"{sourceMaterial.name}_CharacterSideCard_SharedFrames_{gameObject.name}";

            if (frameImage != null)
            {
                frameImage.material = sharedFrameMaterialInstance;
                frameImage.SetMaterialDirty();
            }

            if (portraitFrame != null)
            {
                portraitFrame.material = sharedFrameMaterialInstance;
                portraitFrame.SetMaterialDirty();
            }

            frameUsesHsv = frameImage != null || portraitFrame != null;

            baseFrameSaturation = sharedFrameMaterialInstance.GetFloat(HsvSaturation);
            baseFrameBrightness = sharedFrameMaterialInstance.GetFloat(HsvBright);

            ApplyFrameHsv(baseFrameSaturation, baseFrameBrightness);
        }

        private void ApplyBackgroundHsv(float saturation, float brightness)
        {
            if (!charBackgroundUsesHsv || charBackgroundImage == null)
                return;

            if (charBackgroundMaterialInstance != null)
            {
                charBackgroundMaterialInstance.SetFloat(HsvSaturation, saturation);
                charBackgroundMaterialInstance.SetFloat(HsvBright, brightness);
            }

            ApplyHsvToRenderedImageMaterial(charBackgroundImage, saturation, brightness);
        }

        private void ApplyFrameHsv(float saturation, float brightness)
        {
            if (!frameUsesHsv)
                return;

            if (sharedFrameMaterialInstance != null)
            {
                sharedFrameMaterialInstance.SetFloat(HsvSaturation, saturation);
                sharedFrameMaterialInstance.SetFloat(HsvBright, brightness);
            }

            ApplyHsvToRenderedImageMaterial(frameImage, saturation, brightness);
            ApplyHsvToRenderedImageMaterial(portraitFrame, saturation, brightness);
        }

        private void ApplyHsvToRenderedImageMaterial(Image image, float saturation, float brightness)
        {
            if (image == null)
                return;

            Material renderMaterial = image.materialForRendering;

            if (renderMaterial == null)
                return;

            if (renderMaterial.HasProperty(HsvSaturation))
                renderMaterial.SetFloat(HsvSaturation, saturation);

            if (renderMaterial.HasProperty(HsvBright))
                renderMaterial.SetFloat(HsvBright, brightness);
        }

        private void SetupTransition(InteractionState targetState)
        {
            myInnerState = targetState;
            elapsedTime = 0f;

            if (cardRect != null)
                startWidth = cardRect.rect.width;

            if (scaleRoot != null)
                startScale = scaleRoot.localScale;

            if (portraitImage != null)
                startPortraitScale = portraitImage.rectTransform.localScale;

            if (charBackgroundUsesHsv && charBackgroundMaterialInstance != null)
            {
                startBackgroundSaturation = charBackgroundMaterialInstance.GetFloat(HsvSaturation);
                startBackgroundBrightness = charBackgroundMaterialInstance.GetFloat(HsvBright);
            }

            if (frameUsesHsv && sharedFrameMaterialInstance != null)
            {
                startFrameSaturation = sharedFrameMaterialInstance.GetFloat(HsvSaturation);
                startFrameBrightness = sharedFrameMaterialInstance.GetFloat(HsvBright);
            }

            SimpleTweenManager.RegisterTween(this);
        }

        public void GoToHoverState()
        {
            SetupTransition(InteractionState.hoverState);
        }

        public void GoToPointerExitState()
        {
            SetupTransition(InteractionState.exitNothingHappening);
        }

        public void GoToClickDownState()
        {
            SetupTransition(InteractionState.pointerDownState);
            SimpleSoundManager.Play(clickSoundConfig);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            OnCardPointerEnterEvent?.Invoke(this);
            onUnityPointerEnter?.Invoke();

            SimpleSoundManager.Play(hoverSoundConfig);

            if (myInnerState != InteractionState.pointerDownState)
                GoToHoverState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            OnCardPointerExitEvent?.Invoke(this);
            onUnityPointerExit?.Invoke();

            if (myInnerState != InteractionState.pointerDownState)
                GoToPointerExitState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownEvent?.Invoke(this);
            onUnityPointerDown?.Invoke();

            GoToClickDownState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpEvent?.Invoke(this);
            onUnityPointerUp?.Invoke();

            if (isHovered)
            {
                cardActivatedClicked?.Invoke();
                OnCardActivatedClicked?.Invoke(this);

                GoToHoverState();
            }
            else
            {
                GoToPointerExitState();
            }
        }

        public void CustomUpdate(float deltaTime)
        {
            switch (myInnerState)
            {
                case InteractionState.hoverState:
                    HoverAnimation(deltaTime);
                    break;

                case InteractionState.exitNothingHappening:
                    PointerExitAnimation(deltaTime);
                    break;

                case InteractionState.pointerDownState:
                    PointerDownAnimation(deltaTime);
                    break;
            }
        }

        private void HoverAnimation(float deltaTime)
        {
            AnimateProperties(
                targetWidth: baseWidth * hoverWidthMultiplier,
                targetScale: baseScale * hoverScale,
                targetPortraitScale: basePortraitScale * hoverPortraitScale,
                targetBackgroundSaturation: baseBackgroundSaturation * hoverBackgroundSaturationMultiplier,
                targetBackgroundBrightness: baseBackgroundBrightness * hoverBackgroundBrightnessMultiplier,
                targetFrameSaturation: baseFrameSaturation * hoverFrameSaturationMultiplier,
                targetFrameBrightness: baseFrameBrightness * hoverFrameBrightnessMultiplier,
                duration: hoverAnimationDuration,
                deltaTime: deltaTime,
                easeType: EaseType.EaseOutQuad
            );
        }

        private void PointerExitAnimation(float deltaTime)
        {
            AnimateProperties(
                targetWidth: baseWidth,
                targetScale: baseScale,
                targetPortraitScale: basePortraitScale,
                targetBackgroundSaturation: baseBackgroundSaturation,
                targetBackgroundBrightness: baseBackgroundBrightness,
                targetFrameSaturation: baseFrameSaturation,
                targetFrameBrightness: baseFrameBrightness,
                duration: pointerExitAnimationDuration,
                deltaTime: deltaTime,
                easeType: EaseType.EaseOutCubic
            );
        }

        private void PointerDownAnimation(float deltaTime)
        {
            AnimateProperties(
                targetWidth: baseWidth * hoverWidthMultiplier,
                targetScale: baseScale * clickScale,
                targetPortraitScale: basePortraitScale * clickPortraitScale,
                targetBackgroundSaturation: baseBackgroundSaturation * clickDownBackgroundSaturationMultiplier,
                targetBackgroundBrightness: baseBackgroundBrightness * clickDownBackgroundBrightnessMultiplier,
                targetFrameSaturation: baseFrameSaturation * clickDownFrameSaturationMultiplier,
                targetFrameBrightness: baseFrameBrightness * clickDownFrameBrightnessMultiplier,
                duration: pointerDownAnimationDuration,
                deltaTime: deltaTime,
                easeType: EaseType.EaseOutQuart
            );
        }

        private void AnimateProperties(
            float targetWidth,
            Vector3 targetScale,
            Vector3 targetPortraitScale,
            float targetBackgroundSaturation,
            float targetBackgroundBrightness,
            float targetFrameSaturation,
            float targetFrameBrightness,
            float duration,
            float deltaTime,
            EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            if (cardRect != null)
            {
                float currentWidth = Mathf.Lerp(startWidth, targetWidth, tEased);
                cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
            }

            if (scaleRoot != null)
            {
                Vector3 currentScale = Vector3.Lerp(startScale, targetScale, tEased);
                scaleRoot.localScale = currentScale;
            }

            if (portraitImage != null)
            {
                Vector3 currentPortraitScale = Vector3.Lerp(
                    startPortraitScale,
                    targetPortraitScale,
                    tEased
                );

                portraitImage.rectTransform.localScale = currentPortraitScale;
            }

            if (charBackgroundUsesHsv)
            {
                float currentBackgroundSaturation = Mathf.Lerp(
                    startBackgroundSaturation,
                    targetBackgroundSaturation,
                    tEased
                );

                float currentBackgroundBrightness = Mathf.Lerp(
                    startBackgroundBrightness,
                    targetBackgroundBrightness,
                    tEased
                );

                ApplyBackgroundHsv(currentBackgroundSaturation, currentBackgroundBrightness);
            }

            if (frameUsesHsv)
            {
                float currentFrameSaturation = Mathf.Lerp(
                    startFrameSaturation,
                    targetFrameSaturation,
                    tEased
                );

                float currentFrameBrightness = Mathf.Lerp(
                    startFrameBrightness,
                    targetFrameBrightness,
                    tEased
                );

                ApplyFrameHsv(currentFrameSaturation, currentFrameBrightness);
            }

            if (t >= 1f)
                SimpleTweenManager.UnregisterTween(this);
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

        public void SetIndexNumber(int number)
        {
            myTweenNumber = number;
        }

        public int GetIndexNumber()
        {
            return myTweenNumber;
        }

        private void OnDisable()
        {
            UnregisterFromCharacterDirtyEvent();

            if (cardRect != null)
                cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseWidth);

            if (scaleRoot != null)
                scaleRoot.localScale = baseScale;

            if (portraitImage != null)
                portraitImage.rectTransform.localScale = basePortraitScale;

            if (charBackgroundUsesHsv)
                ApplyBackgroundHsv(baseBackgroundSaturation, baseBackgroundBrightness);

            if (frameUsesHsv)
                ApplyFrameHsv(baseFrameSaturation, baseFrameBrightness);

            ClearGlobalBuffVisuals();

            isHovered = false;
            myInnerState = InteractionState.exitNothingHappening;

            SimpleTweenManager.UnregisterTween(this);
        }

        private void OnDestroy()
        {
            UnregisterFromCharacterDirtyEvent();

            if (charBackgroundMaterialInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(charBackgroundMaterialInstance);
                else
                    DestroyImmediate(charBackgroundMaterialInstance);
            }

            if (sharedFrameMaterialInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(sharedFrameMaterialInstance);
                else
                    DestroyImmediate(sharedFrameMaterialInstance);
            }
        }

        private static string GetLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            string trimmedName = fullName.Trim();
            int lastSpaceIndex = trimmedName.LastIndexOf(' ');

            if (lastSpaceIndex < 0)
                return trimmedName;

            return trimmedName.Substring(lastSpaceIndex + 1);
        }
    }
}
