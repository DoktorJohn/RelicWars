using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;
using Sunvale.Common.Tweening;


namespace Sunvale.AncientRomeUI.Buttons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Sunvale/AncientRomeUI/RPGSkillButton")]
    public class RPGSkillButton : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        ITweenClient
    {
        [Serializable]
        public class VisualState
        {
            [Header("Icon")]
            [Range(0.2f, 2.5f)] public float iconScale = 1f;
            [Range(-0.35f, 0.35f)] public float iconOffsetX = 0f;
            [Range(-0.35f, 0.35f)] public float iconOffsetY = 0f;
            [Range(0f, 2f)] public float iconBrightness = 1f;
            [Range(0f, 2f)] public float iconSaturation = 1f;
            [Range(0f, 2f)] public float iconContrast = 1f;

            [Header("Background/Core Texture")]
            [Range(0.25f, 4f)] public float backgroundScale = 1f;
            [Range(-1f, 1f)] public float backgroundOffsetX = 0f;
            [Range(-1f, 1f)] public float backgroundOffsetY = 0f;
            [Range(-1f, 1f)] public float backgroundBrightness = 0f;
            [Range(0f, 2f)] public float backgroundContrast = 1f;
            [Range(0f, 2f)] public float backgroundSaturation = 1f;

            [Header("Overlay Texture")]
            [Range(0.25f, 8f)] public float overlayScale = 1f;
            [Range(-2f, 2f)] public float overlayOffsetX = 0f;
            [Range(-2f, 2f)] public float overlayOffsetY = 0f;
            [Range(0f, 1f)] public float overlayOpacity = 0f;

            [Header("Frame")]
            public Color frameColor = Color.white;
        }

        private struct RuntimeVisualState
        {
            public float iconScale;
            public float iconOffsetX;
            public float iconOffsetY;
            public float iconBrightness;
            public float iconSaturation;
            public float iconContrast;

            public float backgroundScale;
            public float backgroundOffsetX;
            public float backgroundOffsetY;
            public float backgroundBrightness;
            public float backgroundContrast;
            public float backgroundSaturation;

            public float overlayScale;
            public float overlayOffsetX;
            public float overlayOffsetY;
            public float overlayOpacity;

            public Color frameColor;

            public static RuntimeVisualState From(VisualState state)
            {
                return new RuntimeVisualState
                {
                    iconScale = state.iconScale,
                    iconOffsetX = state.iconOffsetX,
                    iconOffsetY = state.iconOffsetY,
                    iconBrightness = state.iconBrightness,
                    iconSaturation = state.iconSaturation,
                    iconContrast = state.iconContrast,

                    backgroundScale = state.backgroundScale,
                    backgroundOffsetX = state.backgroundOffsetX,
                    backgroundOffsetY = state.backgroundOffsetY,
                    backgroundBrightness = state.backgroundBrightness,
                    backgroundContrast = state.backgroundContrast,
                    backgroundSaturation = state.backgroundSaturation,

                    overlayScale = state.overlayScale,
                    overlayOffsetX = state.overlayOffsetX,
                    overlayOffsetY = state.overlayOffsetY,
                    overlayOpacity = state.overlayOpacity,

                    frameColor = state.frameColor
                };
            }

            public static RuntimeVisualState Lerp(RuntimeVisualState a, RuntimeVisualState b, float t)
            {
                return new RuntimeVisualState
                {
                    iconScale = Mathf.Lerp(a.iconScale, b.iconScale, t),
                    iconOffsetX = Mathf.Lerp(a.iconOffsetX, b.iconOffsetX, t),
                    iconOffsetY = Mathf.Lerp(a.iconOffsetY, b.iconOffsetY, t),
                    iconBrightness = Mathf.Lerp(a.iconBrightness, b.iconBrightness, t),
                    iconSaturation = Mathf.Lerp(a.iconSaturation, b.iconSaturation, t),
                    iconContrast = Mathf.Lerp(a.iconContrast, b.iconContrast, t),

                    backgroundScale = Mathf.Lerp(a.backgroundScale, b.backgroundScale, t),
                    backgroundOffsetX = Mathf.Lerp(a.backgroundOffsetX, b.backgroundOffsetX, t),
                    backgroundOffsetY = Mathf.Lerp(a.backgroundOffsetY, b.backgroundOffsetY, t),
                    backgroundBrightness = Mathf.Lerp(a.backgroundBrightness, b.backgroundBrightness, t),
                    backgroundContrast = Mathf.Lerp(a.backgroundContrast, b.backgroundContrast, t),
                    backgroundSaturation = Mathf.Lerp(a.backgroundSaturation, b.backgroundSaturation, t),

                    overlayScale = Mathf.Lerp(a.overlayScale, b.overlayScale, t),
                    overlayOffsetX = Mathf.Lerp(a.overlayOffsetX, b.overlayOffsetX, t),
                    overlayOffsetY = Mathf.Lerp(a.overlayOffsetY, b.overlayOffsetY, t),
                    overlayOpacity = Mathf.Lerp(a.overlayOpacity, b.overlayOpacity, t),

                    frameColor = Color.Lerp(a.frameColor, b.frameColor, t)
                };
            }
        }

        public enum InteractionState
        {
            ExitNothingHappening,
            HoverState,
            PointerDownState,
            SelectedState,
            SelectedAndHoveredState
        }

        public enum EaseType
        {
            EaseOutQuad,
            EaseOutCubic,
            EaseOutQuart,
            Linear,
            EaseInQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseInOutCubic,
            EaseInQuart,
            EaseInOutQuart
        }

        private static readonly int IconScaleId = Shader.PropertyToID("_IconScale");
        private static readonly int IconOffsetXId = Shader.PropertyToID("_IconOffsetX");
        private static readonly int IconOffsetYId = Shader.PropertyToID("_IconOffsetY");
        private static readonly int IconBrightnessId = Shader.PropertyToID("_IconBrightness");
        private static readonly int IconSaturationId = Shader.PropertyToID("_IconSaturation");
        private static readonly int IconContrastId = Shader.PropertyToID("_IconContrast");

        private static readonly int BgScaleId = Shader.PropertyToID("_BgScale");
        private static readonly int BgOffsetXId = Shader.PropertyToID("_BgOffsetX");
        private static readonly int BgOffsetYId = Shader.PropertyToID("_BgOffsetY");
        private static readonly int BgBrightnessId = Shader.PropertyToID("_BgBrightness");
        private static readonly int BgContrastId = Shader.PropertyToID("_BgContrast");
        private static readonly int BgSaturationId = Shader.PropertyToID("_BgSaturation");

        private static readonly int OverlayEnabledId = Shader.PropertyToID("_OverlayEnabled");
        private static readonly int OverlayScaleId = Shader.PropertyToID("_OverlayScale");
        private static readonly int OverlayOffsetXId = Shader.PropertyToID("_OverlayOffsetX");
        private static readonly int OverlayOffsetYId = Shader.PropertyToID("_OverlayOffsetY");
        private static readonly int OverlayOpacityId = Shader.PropertyToID("_OverlayOpacity");

        private static readonly int CooldownEnabledId = Shader.PropertyToID("_CooldownEnabled");
        private static readonly int CooldownProgressId = Shader.PropertyToID("_CooldownProgress");

        private static readonly int GreyscaleDisabledId = Shader.PropertyToID("_GreyscaleDisabled");
        private static readonly int GreyscaleDisabledDarknessId = Shader.PropertyToID("_GreyscaleDisabledDarkness");

        private static readonly int SweepHighlightEnabledId = Shader.PropertyToID("_SweepHighlightEnabled");
        private static readonly int SweepHighlightPositionId = Shader.PropertyToID("_SweepHighlightPosition");
        private static readonly int SweepHighlightOpacityId = Shader.PropertyToID("_SweepHighlightOpacity");

        [Header("References")]
        [SerializeField] public RectTransform myRectTransform;
        [SerializeField] public Image frameImage;
        [SerializeField] public Image coreImage;
        [SerializeField] public TextMeshProUGUI numberLabelForCooldown;
        
        
        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;
        [SerializeField] public UISoundConfig clickDeniedConfig;
        
        
        [Header("Visual States")]
        public VisualState normalVisuals = new VisualState
        {
            iconScale = 1f,
            iconOffsetX = 0f,
            iconOffsetY = 0f,
            iconBrightness = 1f,
            iconSaturation = 1f,
            iconContrast = 1f,

            backgroundScale = 1f,
            backgroundOffsetX = 0f,
            backgroundOffsetY = 0f,
            backgroundBrightness = 0f,
            backgroundContrast = 1f,
            backgroundSaturation = 1f,

            overlayScale = 1f,
            overlayOffsetX = 0f,
            overlayOffsetY = 0f,
            overlayOpacity = 0f,

            frameColor = Color.white
        };

        public VisualState hoverVisuals = new VisualState
        {
            iconScale = 1.045f,
            iconOffsetX = 0.008f,
            iconOffsetY = 0.012f,
            iconBrightness = 1.13f,
            iconSaturation = 1.12f,
            iconContrast = 1.04f,

            backgroundScale = 1.025f,
            backgroundOffsetX = 0f,
            backgroundOffsetY = 0f,
            backgroundBrightness = 0.08f,
            backgroundContrast = 1.05f,
            backgroundSaturation = 1.04f,

            overlayScale = 1.035f,
            overlayOffsetX = 0.006f,
            overlayOffsetY = 0.008f,
            overlayOpacity = 0.15f,

            frameColor = Color.white
        };

        public VisualState selectedVisuals = new VisualState
        {
            iconScale = 0.94f,
            iconOffsetX = 0f,
            iconOffsetY = -0.01f,
            iconBrightness = 1.06f,
            iconSaturation = 1.08f,
            iconContrast = 1.03f,

            backgroundScale = 0.965f,
            backgroundOffsetX = 0f,
            backgroundOffsetY = 0f,
            backgroundBrightness = 0.02f,
            backgroundContrast = 1.04f,
            backgroundSaturation = 1.03f,

            overlayScale = 0.975f,
            overlayOffsetX = 0f,
            overlayOffsetY = -0.006f,
            overlayOpacity = 0.20f,

            frameColor = Color.white
        };

        public VisualState selectedAndHoveredVisuals = new VisualState
        {
            iconScale = 0.985f,
            iconOffsetX = 0.006f,
            iconOffsetY = 0.004f,
            iconBrightness = 1.16f,
            iconSaturation = 1.16f,
            iconContrast = 1.06f,

            backgroundScale = 0.99f,
            backgroundOffsetX = 0f,
            backgroundOffsetY = 0f,
            backgroundBrightness = 0.09f,
            backgroundContrast = 1.08f,
            backgroundSaturation = 1.05f,

            overlayScale = 1.015f,
            overlayOffsetX = 0.006f,
            overlayOffsetY = 0.004f,
            overlayOpacity = 0.25f,

            frameColor = Color.white
        };

        public VisualState pointerDownVisuals = new VisualState
        {
            iconScale = 0.90f,
            iconOffsetX = 0f,
            iconOffsetY = -0.018f,
            iconBrightness = 0.96f,
            iconSaturation = 1.05f,
            iconContrast = 1.02f,

            backgroundScale = 0.94f,
            backgroundOffsetX = 0f,
            backgroundOffsetY = 0f,
            backgroundBrightness = -0.035f,
            backgroundContrast = 1.02f,
            backgroundSaturation = 1.01f,

            overlayScale = 0.94f,
            overlayOffsetX = 0f,
            overlayOffsetY = -0.012f,
            overlayOpacity = 0.22f,

            frameColor = Color.white
        };

        [Header("Shader Overlay Control")]
        public bool driveOverlayEnabledFromOpacity = true;

        [Header("Animation Durations")]
        public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;
        public float selectedAnimationDuration = 0.10f;

        [Header("Selected State")]
        [SerializeField] private bool isSelected;

        [Header("Cooldown - Manual Tick Driven")]
        [SerializeField] private bool isCooldownActive;
        [SerializeField] private float cooldownDurationTicks = 1f;
        [SerializeField] private float cooldownRemainingTicks = 0f;
        [SerializeField, Range(0f, 1f)] private float cooldownProgress01 = 1f;
        public bool hideCooldownOverlayWhenFinished = true;

        [Header("Disabled Visual")]
        [SerializeField] private bool greyscaledAndDisabled;
        [Range(0f, 1f)] public float greyscaleDisabledDarkness = 0.45f;

        [Header("Rolling Highlight One-Shot Animation Switches")]
        public bool rollingAnimationEnabled = true;
        public bool pulseAnimationEnabled = true;
        public bool scalePulseEnabled = false;

        [Header("Rolling Highlight - Sweep")]
        [Range(-1.5f, 1.5f)] public float rollingHighlightStartPosition = -1.2f;
        [Range(-1.5f, 1.5f)] public float rollingHighlightEndPosition = 1.2f;
        [Min(0.0001f)] public float rollingHighlightDuration = 0.65f;
        public EaseType rollingHighlightEase = EaseType.EaseOutCubic;
        [Range(0f, 1f)] public float rollingHighlightOpacity = 0.55f;

        [Header("Rolling Highlight - Light Pulse Timing")]
        [Min(0f)] public float pulseRampUpDuration = 0.08f;
        [Min(0f)] public float pulseAnimationDuration = 0.12f;
        [Min(0f)] public float pulseRampDownDuration = 0.18f;
        public EaseType pulseRampUpEase = EaseType.EaseOutQuad;
        public EaseType pulseRampDownEase = EaseType.EaseOutCubic;

        [Header("Rolling Highlight - Light Pulse Values")]
        [Range(0f, 2.5f)] public float pulseIconBrightnessMultiplier = 1.18f;
        [Range(0f, 2.5f)] public float pulseIconSaturationMultiplier = 1.08f;
        [Range(0f, 2.5f)] public float pulseIconContrastMultiplier = 1.06f;
        [Range(-1f, 1f)] public float pulseBackgroundBrightnessOffset = 0.08f;
        [Range(0f, 2.5f)] public float pulseBackgroundContrastMultiplier = 1.08f;
        [Range(0f, 2.5f)] public float pulseBackgroundSaturationMultiplier = 1.06f;

        [Header("Rolling Highlight - Scale Pulse Timing")]
        [Min(0f)] public float scalePulseRampUpDuration = 0.08f;
        [Min(0f)] public float scalePulseDuration = 0.10f;
        [Min(0f)] public float scalePulseRampDownDuration = 0.18f;
        public EaseType scalePulseRampUpEase = EaseType.EaseOutQuad;
        public EaseType scalePulseRampDownEase = EaseType.EaseOutCubic;

        [Header("Rolling Highlight - Scale Pulse Values")]
        [Range(0.2f, 2.5f)] public float scalePulseIconScaleMultiplier = 1.08f;
        [Range(0.25f, 4f)] public float scalePulseBackgroundScaleMultiplier = 1.035f;
        [Range(0.25f, 8f)] public float scalePulseOverlayScaleMultiplier = 1.035f;

        [Header("Unity Pointer Events Only")]
        public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;

        [Header("Editor")]
        public bool previewInEditMode = false;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isVisualTransitionAnimating;

        private bool isRollingHighlightAnimating;
        private bool isPulseAnimating;
        private bool isScalePulseAnimating;

        private float rollingHighlightElapsedTime;
        private float pulseElapsedTime;
        private float scalePulseElapsedTime;

        private bool isHovered;
        private bool isPointerDown;

        private RuntimeVisualState startVisuals;
        private RuntimeVisualState targetVisuals;
        private RuntimeVisualState currentVisuals;
        private bool hasCurrentVisuals;

        private Material runtimeCoreMaterial;

        public bool IsSelected => isSelected;
        public bool IsHovered => isHovered;
        public bool IsPointerDown => isPointerDown;
        public bool IsCooldownActive => isCooldownActive;
        public float CooldownProgress01 => cooldownProgress01;
        public float CooldownRemainingTicks => cooldownRemainingTicks;
        public float CooldownDurationTicks => cooldownDurationTicks;
        public bool GreyscaledAndDisabled => greyscaledAndDisabled;
        public bool IsRollingHighlightAnimating => isRollingHighlightAnimating;
        public bool IsRollingHighlightPulseAnimating => isPulseAnimating;
        public bool IsRollingHighlightScalePulseAnimating => isScalePulseAnimating;
        public bool IsAnyRollingHighlightOneShotAnimationActive => AnyOneShotRollingHighlightAnimationActive();

        public delegate void SkillButtonPointerDelegate(RPGSkillButton button, PointerEventData eventData);

        public event SkillButtonPointerDelegate OnPointerDownEvent;
        public event SkillButtonPointerDelegate OnPointerUpEvent;
        public event SkillButtonPointerDelegate OnButtonPointerEnterEvent;
        public event SkillButtonPointerDelegate OnButtonPointerExitEvent;

        private void Reset()
        {
            myRectTransform = GetComponent<RectTransform>();

            if (coreImage == null)
                coreImage = GetComponent<Image>();
        }
    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (myRectTransform == null)
                myRectTransform = GetComponent<RectTransform>();

            cooldownDurationTicks = Mathf.Max(0.0001f, cooldownDurationTicks);
            cooldownRemainingTicks = Mathf.Clamp(cooldownRemainingTicks, 0f, cooldownDurationTicks);
            cooldownProgress01 = Mathf.Clamp01(cooldownProgress01);
            greyscaleDisabledDarkness = Mathf.Clamp01(greyscaleDisabledDarkness);
            rollingHighlightDuration = Mathf.Max(0.0001f, rollingHighlightDuration);
            rollingHighlightOpacity = Mathf.Clamp01(rollingHighlightOpacity);

            pulseRampUpDuration = Mathf.Max(0f, pulseRampUpDuration);
            pulseAnimationDuration = Mathf.Max(0f, pulseAnimationDuration);
            pulseRampDownDuration = Mathf.Max(0f, pulseRampDownDuration);

            scalePulseRampUpDuration = Mathf.Max(0f, scalePulseRampUpDuration);
            scalePulseDuration = Mathf.Max(0f, scalePulseDuration);
            scalePulseRampDownDuration = Mathf.Max(0f, scalePulseRampDownDuration);

            if (!Application.isPlaying && previewInEditMode)
            {
                ApplyVisualsInstant();
                ApplyCooldownShaderValues();
                ApplyDisabledShaderValues();
                ApplyRollingHighlightShaderValues();
            }
        }

        #endif
        private void Awake()
        {
            GetCoreMaterial();
            ApplyVisualsInstant();
            ApplyCooldownShaderValues();
            ApplyDisabledShaderValues();
            ApplyRollingHighlightShaderValues();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            GetCoreMaterial();

            ApplyVisualsInstant();
            ApplyCooldownShaderValues();
            ApplyDisabledShaderValues();
            ApplyRollingHighlightShaderValues();
        }

        private void OnDisable()
        {
            isPointerDown = false;
            isVisualTransitionAnimating = false;
            ResetOneShotRollingHighlightAnimations();

            SimpleTweenManager.UnregisterTween(this);
        }

        private void OnDestroy()
        {
            if (runtimeCoreMaterial != null)
            {
                Destroy(runtimeCoreMaterial);
                runtimeCoreMaterial = null;
            }
        }

        public void EnableCooldownTextLabel()
        {
            numberLabelForCooldown.transform.parent.gameObject.SetActive(true);
        }

        public void DisableCooldownTextLabel()
        {
            numberLabelForCooldown.transform.parent.gameObject.SetActive(false);
        }

        public void SetCooldownTextLabel(string s)
        {
            numberLabelForCooldown.SetText(s);
        }


        public void PlayClickSound()
        {
            if (clickSoundConfig.playSound)
            {
                SimpleSoundManager.Play(clickSoundConfig);
            }
        }

        public void PlayHoverSound()
        {
            if (hoverSoundConfig.playSound)
            {
                SimpleSoundManager.Play(hoverSoundConfig);
            }
        }

        public void PlayClickDeniedSound()
        {
            if (clickDeniedConfig.playSound)
            {
                SimpleSoundManager.Play(clickDeniedConfig);
            }
        }
        

        public void SetSelected(bool selected, bool withAnimation = true)
        {
            isSelected = selected;

            if (withAnimation)
                RefreshCurrentStateWithAnimation();
            else
                ApplyVisualsInstant();
        }

        public void SetAsSelected(bool withAnimation = true)
        {
            SetSelected(true, withAnimation);
        }

        public void SetAsDeselected(bool withAnimation = true)
        {
            SetSelected(false, withAnimation);
        }

        public void ToggleSelected(bool withAnimation = true)
        {
            SetSelected(!isSelected, withAnimation);
        }

        public void StartCooldown(float durationTicks)
        {
            cooldownDurationTicks = Mathf.Max(0.0001f, durationTicks);
            cooldownRemainingTicks = cooldownDurationTicks;
            cooldownProgress01 = 0f;
            isCooldownActive = true;

            ApplyCooldownShaderValues();
        }

        public void TickCooldown(float spentTicks)
        {
            if (!isCooldownActive)
                return;

            if (spentTicks <= 0f)
                return;

            SetCooldownRemaining(cooldownRemainingTicks - spentTicks, cooldownDurationTicks);
        }

        public void SetCooldownRemaining(float remainingTicks, float durationTicks)
        {
            cooldownDurationTicks = Mathf.Max(0.0001f, durationTicks);
            cooldownRemainingTicks = Mathf.Clamp(remainingTicks, 0f, cooldownDurationTicks);

            cooldownProgress01 = 1f - cooldownRemainingTicks / cooldownDurationTicks;
            cooldownProgress01 = Mathf.Clamp01(cooldownProgress01);

            isCooldownActive = cooldownRemainingTicks > 0.0001f;

            if (!isCooldownActive)
            {
                cooldownRemainingTicks = 0f;
                cooldownProgress01 = 1f;
            }

            ApplyCooldownShaderValues();
        }

        public void SetCooldownProgress(float progress01)
        {
            cooldownProgress01 = Mathf.Clamp01(progress01);

            cooldownDurationTicks = Mathf.Max(0.0001f, cooldownDurationTicks);
            cooldownRemainingTicks = cooldownDurationTicks * (1f - cooldownProgress01);

            isCooldownActive = cooldownProgress01 < 0.999f;

            if (!isCooldownActive)
            {
                cooldownRemainingTicks = 0f;
                cooldownProgress01 = 1f;
            }

            ApplyCooldownShaderValues();
        }

        public void FinishCooldown()
        {
            cooldownRemainingTicks = 0f;
            cooldownProgress01 = 1f;
            isCooldownActive = false;

            ApplyCooldownShaderValues();
        }

        public void CancelCooldown()
        {
            FinishCooldown();
        }

        public void SetGreyscaleDisabled(bool disabled)
        {
            greyscaledAndDisabled = disabled;
            ApplyDisabledShaderValues();
        }

        public void SetGreyscaleDisabled(bool disabled, float darkness01)
        {
            greyscaledAndDisabled = disabled;
            greyscaleDisabledDarkness = Mathf.Clamp01(darkness01);

            ApplyDisabledShaderValues();
        }

        public void SetAvailable(bool available)
        {
            SetGreyscaleDisabled(!available);
        }

        public void StartRollingHighlightAnimation()
        {
            ResetOneShotRollingHighlightTimers();

            isRollingHighlightAnimating = rollingAnimationEnabled;
            isPulseAnimating = pulseAnimationEnabled;
            isScalePulseAnimating = scalePulseEnabled;

            if (!AnyOneShotRollingHighlightAnimationActive())
            {
                ResetOneShotRollingHighlightAnimations();
                UnregisterTweenIfNothingActive();
                return;
            }

            ApplyRollingHighlightShaderValues();
            ApplyCurrentVisualsToShader();
            RegisterTweenIfNeeded();
        }

        public void StopRollingHighlightAnimation()
        {
            ResetOneShotRollingHighlightAnimations();
            UnregisterTweenIfNothingActive();
        }

        public void RestartRollingHighlightAnimation()
        {
            StopRollingHighlightAnimation();
            StartRollingHighlightAnimation();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;

            OnPointerDownEvent?.Invoke(this, eventData);
            onUnityPointerDown?.Invoke();
            
            if (!greyscaledAndDisabled)
            {
                PlayClickSound();
            }
            else
            {
                PlayClickDeniedSound();
            }

            RefreshCurrentStateWithAnimation();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;

            OnPointerUpEvent?.Invoke(this, eventData);
            onUnityPointerUp?.Invoke();

            RefreshCurrentStateWithAnimation();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            OnButtonPointerEnterEvent?.Invoke(this, eventData);
            onUnityPointerEnter?.Invoke();
            
            PlayHoverSound();

            RefreshCurrentStateWithAnimation();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            OnButtonPointerExitEvent?.Invoke(this, eventData);
            onUnityPointerExit?.Invoke();

            RefreshCurrentStateWithAnimation();
        }

        private void RefreshCurrentStateWithAnimation()
        {
            SetupTransition(GetDesiredInnerState());
        }

        private InteractionState GetDesiredInnerState()
        {
            if (isPointerDown)
                return InteractionState.PointerDownState;

            if (isSelected && isHovered)
                return InteractionState.SelectedAndHoveredState;

            if (isSelected)
                return InteractionState.SelectedState;

            if (isHovered)
                return InteractionState.HoverState;

            return InteractionState.ExitNothingHappening;
        }

        private VisualState GetVisualStateForInnerState(InteractionState state)
        {
            switch (state)
            {
                case InteractionState.HoverState:
                    return hoverVisuals;

                case InteractionState.PointerDownState:
                    return pointerDownVisuals;

                case InteractionState.SelectedState:
                    return selectedVisuals;

                case InteractionState.SelectedAndHoveredState:
                    return selectedAndHoveredVisuals;

                case InteractionState.ExitNothingHappening:
                default:
                    return normalVisuals;
            }
        }

        private void ApplyVisualsInstant()
        {
            isVisualTransitionAnimating = false;

            if (Application.isPlaying)
                UnregisterTweenIfNothingActive();

            myInnerState = GetDesiredInnerState();
            targetVisuals = RuntimeVisualState.From(GetVisualStateForInnerState(myInnerState));

            ApplyRuntimeVisuals(targetVisuals);
        }

        private void SetupTransition(InteractionState targetStateOfMouseInteraction)
        {
            myInnerState = targetStateOfMouseInteraction;
            elapsedTime = 0f;
            isVisualTransitionAnimating = false;

            startVisuals = GetCurrentVisuals();
            targetVisuals = RuntimeVisualState.From(GetVisualStateForInnerState(myInnerState));

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                ApplyRuntimeVisuals(targetVisuals);
                return;
            }

            if (GetAnimationDurationForState(myInnerState) <= 0f)
            {
                ApplyRuntimeVisuals(targetVisuals);
                UnregisterTweenIfNothingActive();
                return;
            }

            isVisualTransitionAnimating = true;
            RegisterTweenIfNeeded();
        }

        private RuntimeVisualState GetCurrentVisuals()
        {
            if (hasCurrentVisuals)
                return currentVisuals;

            return RuntimeVisualState.From(GetVisualStateForInnerState(GetDesiredInnerState()));
        }

        public void CustomUpdate(float deltaTime)
        {
            if (isVisualTransitionAnimating)
            {
                AnimateProperties(
                    GetAnimationDurationForState(myInnerState),
                    deltaTime,
                    GetEaseForState(myInnerState)
                );
            }

            if (AnyOneShotRollingHighlightAnimationActive())
                AnimateRollingHighlight(deltaTime);

            UnregisterTweenIfNothingActive();
        }

        private float GetAnimationDurationForState(InteractionState state)
        {
            switch (state)
            {
                case InteractionState.PointerDownState:
                    return pointerDownAnimationDuration;

                case InteractionState.HoverState:
                case InteractionState.SelectedAndHoveredState:
                    return hoverAnimationDuration;

                case InteractionState.SelectedState:
                    return selectedAnimationDuration;

                case InteractionState.ExitNothingHappening:
                default:
                    return pointerExitAnimationDuration;
            }
        }

        private EaseType GetEaseForState(InteractionState state)
        {
            switch (state)
            {
                case InteractionState.PointerDownState:
                    return EaseType.EaseOutQuart;

                case InteractionState.ExitNothingHappening:
                    return EaseType.EaseOutCubic;

                case InteractionState.HoverState:
                case InteractionState.SelectedState:
                case InteractionState.SelectedAndHoveredState:
                default:
                    return EaseType.EaseOutQuad;
            }
        }

        private void AnimateProperties(float duration, float deltaTime, EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            RuntimeVisualState lerpedVisuals = RuntimeVisualState.Lerp(startVisuals, targetVisuals, tEased);
            ApplyRuntimeVisuals(lerpedVisuals);

            if (t >= 1f)
            {
                ApplyRuntimeVisuals(targetVisuals);
                isVisualTransitionAnimating = false;
            }
        }

        private float GetEasedTime(float t, EaseType easeType)
        {
            t = Mathf.Clamp01(t);

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

                case EaseType.EaseInQuad:
                    return t * t;

                case EaseType.EaseInOutQuad:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

                case EaseType.EaseInCubic:
                    return t * t * t;

                case EaseType.EaseInOutCubic:
                    return t < 0.5f
                        ? 4f * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

                case EaseType.EaseInQuart:
                    return t * t * t * t;

                case EaseType.EaseInOutQuart:
                    return t < 0.5f
                        ? 8f * t * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;

                case EaseType.Linear:
                default:
                    return t;
            }
        }

        private void AnimateRollingHighlight(float deltaTime)
        {
            bool changed = false;

            if (isRollingHighlightAnimating)
            {
                rollingHighlightElapsedTime += deltaTime;

                if (rollingHighlightElapsedTime >= GetSafeRollingHighlightDuration())
                {
                    isRollingHighlightAnimating = false;
                    rollingHighlightElapsedTime = 0f;
                }

                changed = true;
            }

            if (isPulseAnimating)
            {
                pulseElapsedTime += deltaTime;

                if (pulseElapsedTime >= GetPulseTotalDuration())
                {
                    isPulseAnimating = false;
                    pulseElapsedTime = 0f;
                }

                changed = true;
            }

            if (isScalePulseAnimating)
            {
                scalePulseElapsedTime += deltaTime;

                if (scalePulseElapsedTime >= GetScalePulseTotalDuration())
                {
                    isScalePulseAnimating = false;
                    scalePulseElapsedTime = 0f;
                }

                changed = true;
            }

            if (!changed)
                return;

            ApplyRollingHighlightShaderValues();
            ApplyCurrentVisualsToShader();
        }

        private float GetSafeRollingHighlightDuration()
        {
            return Mathf.Max(0.0001f, rollingHighlightDuration);
        }

        private float GetPulseTotalDuration()
        {
            return Mathf.Max(0.0001f, pulseRampUpDuration + pulseAnimationDuration + pulseRampDownDuration);
        }

        private float GetScalePulseTotalDuration()
        {
            return Mathf.Max(0.0001f, scalePulseRampUpDuration + scalePulseDuration + scalePulseRampDownDuration);
        }

        private float GetRollingHighlightTime01()
        {
            if (!isRollingHighlightAnimating)
                return 0f;

            return Mathf.Clamp01(rollingHighlightElapsedTime / GetSafeRollingHighlightDuration());
        }

        private float GetRampHoldRampAmount01(
            float elapsed,
            float rampUpDuration,
            float holdDuration,
            float rampDownDuration,
            EaseType rampUpEase,
            EaseType rampDownEase)
        {
            rampUpDuration = Mathf.Max(0f, rampUpDuration);
            holdDuration = Mathf.Max(0f, holdDuration);
            rampDownDuration = Mathf.Max(0f, rampDownDuration);

            if (rampUpDuration > 0f && elapsed < rampUpDuration)
            {
                float t = Mathf.Clamp01(elapsed / rampUpDuration);
                return GetEasedTime(t, rampUpEase);
            }

            elapsed -= rampUpDuration;

            if (holdDuration > 0f && elapsed < holdDuration)
                return 1f;

            elapsed -= holdDuration;

            if (rampDownDuration > 0f && elapsed < rampDownDuration)
            {
                float t = Mathf.Clamp01(elapsed / rampDownDuration);
                return 1f - GetEasedTime(t, rampDownEase);
            }

            return 0f;
        }

        private float GetPulseAmount01()
        {
            if (!isPulseAnimating)
                return 0f;

            return GetRampHoldRampAmount01(
                pulseElapsedTime,
                pulseRampUpDuration,
                pulseAnimationDuration,
                pulseRampDownDuration,
                pulseRampUpEase,
                pulseRampDownEase
            );
        }

        private float GetScalePulseAmount01()
        {
            if (!isScalePulseAnimating)
                return 0f;

            return GetRampHoldRampAmount01(
                scalePulseElapsedTime,
                scalePulseRampUpDuration,
                scalePulseDuration,
                scalePulseRampDownDuration,
                scalePulseRampUpEase,
                scalePulseRampDownEase
            );
        }

        private void ResetOneShotRollingHighlightTimers()
        {
            rollingHighlightElapsedTime = 0f;
            pulseElapsedTime = 0f;
            scalePulseElapsedTime = 0f;
        }

        private void ResetOneShotRollingHighlightAnimations()
        {
            isRollingHighlightAnimating = false;
            isPulseAnimating = false;
            isScalePulseAnimating = false;

            ResetOneShotRollingHighlightTimers();

            ApplyRollingHighlightShaderValues();
            ApplyCurrentVisualsToShader();
        }

        private bool AnyOneShotRollingHighlightAnimationActive()
        {
            return isRollingHighlightAnimating || isPulseAnimating || isScalePulseAnimating;
        }

        private void ApplyRollingHighlightShaderValues()
        {
            Material coreMaterial = GetCoreMaterial();

            float time01 = GetRollingHighlightTime01();
            float easedTime01 = GetEasedTime(time01, rollingHighlightEase);
            float position = Mathf.Lerp(rollingHighlightStartPosition, rollingHighlightEndPosition, easedTime01);

            bool shouldShowHighlight = isRollingHighlightAnimating && rollingHighlightOpacity > 0.0001f;

            if (!isRollingHighlightAnimating)
                position = rollingHighlightStartPosition;

            SetFloatIfExists(coreMaterial, SweepHighlightEnabledId, shouldShowHighlight ? 1f : 0f);
            SetFloatIfExists(coreMaterial, SweepHighlightPositionId, position);
            SetFloatIfExists(coreMaterial, SweepHighlightOpacityId, shouldShowHighlight ? rollingHighlightOpacity : 0f);
        }

        private void ApplyRuntimeVisuals(RuntimeVisualState visuals)
        {
            currentVisuals = visuals;
            hasCurrentVisuals = true;

            ApplyCurrentVisualsToShader();
        }

        private void ApplyCurrentVisualsToShader()
        {
            RuntimeVisualState visuals = GetCurrentVisualsWithOneShotRollingHighlightAnimations();
            Material coreMaterial = GetCoreMaterial();

            SetFloatIfExists(coreMaterial, IconScaleId, visuals.iconScale);
            SetFloatIfExists(coreMaterial, IconOffsetXId, visuals.iconOffsetX);
            SetFloatIfExists(coreMaterial, IconOffsetYId, visuals.iconOffsetY);
            SetFloatIfExists(coreMaterial, IconBrightnessId, visuals.iconBrightness);
            SetFloatIfExists(coreMaterial, IconSaturationId, visuals.iconSaturation);
            SetFloatIfExists(coreMaterial, IconContrastId, visuals.iconContrast);

            SetFloatIfExists(coreMaterial, BgScaleId, visuals.backgroundScale);
            SetFloatIfExists(coreMaterial, BgOffsetXId, visuals.backgroundOffsetX);
            SetFloatIfExists(coreMaterial, BgOffsetYId, visuals.backgroundOffsetY);
            SetFloatIfExists(coreMaterial, BgBrightnessId, visuals.backgroundBrightness);
            SetFloatIfExists(coreMaterial, BgContrastId, visuals.backgroundContrast);
            SetFloatIfExists(coreMaterial, BgSaturationId, visuals.backgroundSaturation);

            if (driveOverlayEnabledFromOpacity)
                SetFloatIfExists(coreMaterial, OverlayEnabledId, visuals.overlayOpacity > 0.0001f ? 1f : 0f);

            SetFloatIfExists(coreMaterial, OverlayScaleId, visuals.overlayScale);
            SetFloatIfExists(coreMaterial, OverlayOffsetXId, visuals.overlayOffsetX);
            SetFloatIfExists(coreMaterial, OverlayOffsetYId, visuals.overlayOffsetY);
            SetFloatIfExists(coreMaterial, OverlayOpacityId, visuals.overlayOpacity);

            if (frameImage != null)
                frameImage.color = currentVisuals.frameColor;
        }

        private RuntimeVisualState GetCurrentVisualsWithOneShotRollingHighlightAnimations()
        {
            RuntimeVisualState visuals = GetCurrentVisuals();

            if (isPulseAnimating)
            {
                float pulseAmount01 = GetPulseAmount01();

                float iconBrightnessMultiplier = Mathf.Lerp(1f, pulseIconBrightnessMultiplier, pulseAmount01);
                float iconSaturationMultiplier = Mathf.Lerp(1f, pulseIconSaturationMultiplier, pulseAmount01);
                float iconContrastMultiplier = Mathf.Lerp(1f, pulseIconContrastMultiplier, pulseAmount01);

                float backgroundBrightnessOffset = Mathf.Lerp(0f, pulseBackgroundBrightnessOffset, pulseAmount01);
                float backgroundContrastMultiplier = Mathf.Lerp(1f, pulseBackgroundContrastMultiplier, pulseAmount01);
                float backgroundSaturationMultiplier = Mathf.Lerp(1f, pulseBackgroundSaturationMultiplier, pulseAmount01);

                visuals.iconBrightness = Mathf.Clamp(visuals.iconBrightness * iconBrightnessMultiplier, 0f, 2f);
                visuals.iconSaturation = Mathf.Clamp(visuals.iconSaturation * iconSaturationMultiplier, 0f, 2f);
                visuals.iconContrast = Mathf.Clamp(visuals.iconContrast * iconContrastMultiplier, 0f, 2f);

                visuals.backgroundBrightness = Mathf.Clamp(visuals.backgroundBrightness + backgroundBrightnessOffset, -1f, 1f);
                visuals.backgroundContrast = Mathf.Clamp(visuals.backgroundContrast * backgroundContrastMultiplier, 0f, 2f);
                visuals.backgroundSaturation = Mathf.Clamp(visuals.backgroundSaturation * backgroundSaturationMultiplier, 0f, 2f);
            }

            if (isScalePulseAnimating)
            {
                float scaleAmount01 = GetScalePulseAmount01();

                visuals.iconScale = Mathf.Clamp(
                    visuals.iconScale * Mathf.Lerp(1f, scalePulseIconScaleMultiplier, scaleAmount01),
                    0.2f,
                    2.5f
                );

                visuals.backgroundScale = Mathf.Clamp(
                    visuals.backgroundScale * Mathf.Lerp(1f, scalePulseBackgroundScaleMultiplier, scaleAmount01),
                    0.25f,
                    4f
                );

                visuals.overlayScale = Mathf.Clamp(
                    visuals.overlayScale * Mathf.Lerp(1f, scalePulseOverlayScaleMultiplier, scaleAmount01),
                    0.25f,
                    8f
                );
            }

            return visuals;
        }

        private void ApplyCooldownShaderValues()
        {
            Material coreMaterial = GetCoreMaterial();

            float enabledValue = isCooldownActive ? 1f : 0f;

            if (!isCooldownActive && !hideCooldownOverlayWhenFinished)
                enabledValue = 1f;

            SetFloatIfExists(coreMaterial, CooldownEnabledId, enabledValue);
            SetFloatIfExists(coreMaterial, CooldownProgressId, cooldownProgress01);
        }

        private void ApplyDisabledShaderValues()
        {
            Material coreMaterial = GetCoreMaterial();

            SetFloatIfExists(coreMaterial, GreyscaleDisabledId, greyscaledAndDisabled ? 1f : 0f);
            SetFloatIfExists(coreMaterial, GreyscaleDisabledDarknessId, greyscaleDisabledDarkness);
        }

        private Material GetCoreMaterial()
        {
            if (coreImage == null)
                return null;

            if (Application.isPlaying)
            {
                if (runtimeCoreMaterial == null)
                {
                    Material sourceMaterial = coreImage.material;

                    if (sourceMaterial == null)
                        return null;

                    runtimeCoreMaterial = new Material(sourceMaterial);
                    runtimeCoreMaterial.name = sourceMaterial.name + " - RPGSkillButton Instance";
                    runtimeCoreMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

                    coreImage.material = runtimeCoreMaterial;
                }

                return runtimeCoreMaterial;
            }

            return coreImage.material;
        }

        private void SetFloatIfExists(Material material, int propertyId, float value)
        {
            if (material == null)
                return;

            if (!material.HasProperty(propertyId))
                return;

            material.SetFloat(propertyId, value);
        }

        private void RegisterTweenIfNeeded()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            SimpleTweenManager.RegisterTween(this);
        }

        private void UnregisterTweenIfNothingActive()
        {
            if (!Application.isPlaying)
                return;

            if (isVisualTransitionAnimating || AnyOneShotRollingHighlightAnimationActive())
                return;

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
    }
}
