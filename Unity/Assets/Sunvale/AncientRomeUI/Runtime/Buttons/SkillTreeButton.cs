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
    [AddComponentMenu("Sunvale/AncientRomeUI/SkillTreeButton")]
    public class SkillTreeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, ITweenClient
    {
        public enum SkillTreeButtonVisualState
        {
            LockedBehindPredecessors,
            AvailableToUnlock,
            Unlocked
        }

        private enum InteractionState
        {
            exitNothingHappening,
            hoverState,
            pointerDownState
        }

        public enum EaseType
        {
            EaseOutQuad,
            EaseOutCubic,
            EaseOutQuart
        }

        [Serializable]
        public class VisualStateSettings
        {
            [Header("Background")]
            public Sprite backgroundSprite;
            public Color backgroundTint = Color.white;

            [Range(0f, 1f)]
            public float backgroundTintStrength = 0f;

            [Range(-1f, 1f)]
            public float backgroundBrightness = 0f;

            [Tooltip("Uses _BgSaturation if your shader ever gets one. Otherwise this drives _BgContrast on the current shader.")]
            [Range(0f, 2f)]
            public float backgroundSaturation = 1f;

            [Header("Icon")]
            public Color iconTint = Color.white;

            [Range(0f, 1f)]
            public float iconOpacity = 1f;

            [Range(0f, 2f)]
            public float iconSaturation = 1f;

            [Range(0f, 2f)]
            public float iconBrightness = 1f;

            [Range(0.2f, 2.5f)]
            public float iconScale = 1f;

            [Header("Disabled Look")]
            public bool greyscaleDisabled = false;

            [Range(0f, 1f)]
            public float greyscaleDisabledDarkness = 0.45f;

            [Header("Behaviour")]
            public bool checkMarkEnabled = false;
            public bool allowPointerAnimations = true;
            public bool allowClickEvent = true;
        }

        [Header("References")]
        public Image coreImage;
        public Image checkMarkImage;
        public TextMeshProUGUI tmpLabel;

        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;

        [Header("Visual State Settings")]
        public VisualStateSettings unlockedState = new VisualStateSettings
        {
            checkMarkEnabled = true,
            allowPointerAnimations = true,
            allowClickEvent = false,
            greyscaleDisabled = false,
            backgroundBrightness = 0f,
            backgroundSaturation = 1f,
            iconSaturation = 1f,
            iconBrightness = 1f,
            iconScale = 1f
        };

        public VisualStateSettings availableToUnlockState = new VisualStateSettings
        {
            checkMarkEnabled = false,
            allowPointerAnimations = true,
            allowClickEvent = true,
            greyscaleDisabled = false,
            backgroundBrightness = 0f,
            backgroundSaturation = 1.05f,
            iconSaturation = 1f,
            iconBrightness = 1f,
            iconScale = 1f
        };

        public VisualStateSettings lockedBehindPredecessorsState = new VisualStateSettings
        {
            checkMarkEnabled = false,
            allowPointerAnimations = false,
            allowClickEvent = false,
            greyscaleDisabled = true,
            greyscaleDisabledDarkness = 0.45f,
            backgroundBrightness = -0.1f,
            backgroundSaturation = 0.8f,
            iconSaturation = 0.85f,
            iconBrightness = 0.9f,
            iconScale = 1f
        };

        [Header("Animation Durations")]
        public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("Hover Background Target Values")]
        public float hoverBackgroundBrightnessAdd = 0.08f;
        public float hoverBackgroundSaturationMultiplier = 1.12f;

        [Header("Hover Icon Target Values")]
        public float hoverIconScaleMultiplier = 1.1f;
        public float hoverIconSaturationMultiplier = 1.12f;
        public float hoverIconBrightnessMultiplier = 1.12f;

        [Header("Click Down Background Target Values")]
        public float clickDownBackgroundBrightnessAdd = 0.15f;
        public float clickDownBackgroundSaturationMultiplier = 1.18f;

        [Header("Click Down Icon Target Values")]
        public float clickDownIconScaleMultiplier = 0.92f;
        public float clickDownIconSaturationMultiplier = 1.2f;
        public float clickDownIconBrightnessMultiplier = 1.2f;

        [Header("Unity Events")]
        public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent buttonActivatedClicked;

        public delegate void MyDelegateForButtonInteraction(SkillTreeButton theButton);

        public event MyDelegateForButtonInteraction OnPointerDownEvent;
        public event MyDelegateForButtonInteraction OnPointerUpEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerEnterEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerExitEvent;
        public event MyDelegateForButtonInteraction OnButtonActivatedClicked;

        private static readonly int BgTex = Shader.PropertyToID("_BgTex");
        private static readonly int BgTint = Shader.PropertyToID("_BgTint");
        private static readonly int BgTintStrength = Shader.PropertyToID("_BgTintStrength");
        private static readonly int BgBrightness = Shader.PropertyToID("_BgBrightness");

        // Current shader does not have _BgSaturation, so we fall back to _BgContrast.
        private static readonly int BgSaturation = Shader.PropertyToID("_BgSaturation");
        private static readonly int BgContrast = Shader.PropertyToID("_BgContrast");

        private static readonly int IconTint = Shader.PropertyToID("_IconTint");
        private static readonly int IconOpacity = Shader.PropertyToID("_IconOpacity");
        private static readonly int IconSaturation = Shader.PropertyToID("_IconSaturation");
        private static readonly int IconBrightness = Shader.PropertyToID("_IconBrightness");
        private static readonly int IconScale = Shader.PropertyToID("_IconScale");

        private static readonly int GreyscaleDisabled = Shader.PropertyToID("_GreyscaleDisabled");
        private static readonly int GreyscaleDisabledDarkness = Shader.PropertyToID("_GreyscaleDisabledDarkness");

        private Material runtimeMaterial;
        private bool ownsRuntimeMaterial;
        private bool materialWasChanged;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered;

        private SkillTreeButtonVisualState currentVisualState;
        private VisualStateSettings currentStateSettings;

        private float restBackgroundBrightness;
        private float restBackgroundSaturation;
        private float restIconScale;
        private float restIconSaturation;
        private float restIconBrightness;

        private float startBackgroundBrightness;
        private float startBackgroundSaturation;
        private float startIconScale;
        private float startIconSaturation;
        private float startIconBrightness;

        public SkillTreeButtonVisualState CurrentVisualState => currentVisualState;
        public bool IsUnlocked => currentVisualState == SkillTreeButtonVisualState.Unlocked;
        public bool IsAvailableToUnlock => currentVisualState == SkillTreeButtonVisualState.AvailableToUnlock;
        public bool IsLockedBehindPredecessors => currentVisualState == SkillTreeButtonVisualState.LockedBehindPredecessors;

        private void Reset()
        {
            coreImage = GetComponent<Image>();
        }

        private void Awake()
        {
            EnsureRuntimeMaterial();
        }

        private void OnDisable()
        {
            SimpleTweenManager.UnregisterTween(this);
            isHovered = false;
            myInnerState = InteractionState.exitNothingHappening;
        }

        private void OnDestroy()
        {
            if (!ownsRuntimeMaterial || runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }

            runtimeMaterial = null;
        }

        private void EnsureRuntimeMaterial()
        {
            if (runtimeMaterial != null)
            {
                return;
            }

            if (coreImage == null)
            {
                return;
            }

            Material sourceMaterial = coreImage.material;

            if (sourceMaterial == null)
            {
                return;
            }

            runtimeMaterial = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + " (SkillTreeButton Runtime)"
            };

            coreImage.material = runtimeMaterial;
            ownsRuntimeMaterial = true;
        }

        public void SetTextOnLabel(string s)
        {
            if (tmpLabel != null)
            {
                tmpLabel.SetText(s);
            }
        }

        public void SetIconSprite(Sprite iconSprite)
        {
            if (coreImage != null)
            {
                coreImage.sprite = iconSprite;
            }
        }

        public void SetUnlocked()
        {
            SetVisualState(SkillTreeButtonVisualState.Unlocked, true);
        }

        public void SetAvailableToUnlock()
        {
            SetVisualState(SkillTreeButtonVisualState.AvailableToUnlock, true);
        }

        public void SetAvailable()
        {
            SetAvailableToUnlock();
        }

        public void SetLockedBehindPredecessors()
        {
            SetVisualState(SkillTreeButtonVisualState.LockedBehindPredecessors, true);
        }

        public void SetLocked()
        {
            SetLockedBehindPredecessors();
        }

        public void SetVisualState(SkillTreeButtonVisualState newState)
        {
            SetVisualState(newState, true);
        }

        public void SetVisualState(SkillTreeButtonVisualState newState, bool instant)
        {
            EnsureRuntimeMaterial();

            currentVisualState = newState;
            currentStateSettings = GetSettingsForState(newState);

            if (instant)
            {
                SimpleTweenManager.UnregisterTween(this);
                myInnerState = InteractionState.exitNothingHappening;
            }

            ApplyVisualStateSettings(currentStateSettings, instant);

            if (!instant)
            {
                if (isHovered && CanAnimateCurrentState())
                {
                    GoToHoverState();
                }
                else
                {
                    GoToPointerExitState();
                }
            }
        }

        private VisualStateSettings GetSettingsForState(SkillTreeButtonVisualState state)
        {
            switch (state)
            {
                case SkillTreeButtonVisualState.Unlocked:
                    return unlockedState;

                case SkillTreeButtonVisualState.AvailableToUnlock:
                    return availableToUnlockState;

                case SkillTreeButtonVisualState.LockedBehindPredecessors:
                default:
                    return lockedBehindPredecessorsState;
            }
        }

        private void ApplyVisualStateSettings(VisualStateSettings settings, bool snapInteractionValues)
        {
            if (settings == null)
            {
                return;
            }

            restBackgroundBrightness = Mathf.Clamp(settings.backgroundBrightness, -1f, 1f);
            restBackgroundSaturation = Mathf.Clamp(settings.backgroundSaturation, 0f, 2f);
            restIconScale = Mathf.Clamp(settings.iconScale, 0.2f, 2.5f);
            restIconSaturation = Mathf.Clamp(settings.iconSaturation, 0f, 2f);
            restIconBrightness = Mathf.Clamp(settings.iconBrightness, 0f, 2f);

            if (checkMarkImage != null)
            {
                checkMarkImage.enabled = settings.checkMarkEnabled;
            }

            if (settings.backgroundSprite != null)
            {
                SetTextureIfHas(BgTex, settings.backgroundSprite.texture);
            }

            SetColorIfHas(BgTint, settings.backgroundTint);
            SetFloatIfHas(BgTintStrength, settings.backgroundTintStrength);

            SetColorIfHas(IconTint, settings.iconTint);
            SetFloatIfHas(IconOpacity, settings.iconOpacity);

            SetFloatIfHas(GreyscaleDisabled, settings.greyscaleDisabled ? 1f : 0f);
            SetFloatIfHas(GreyscaleDisabledDarkness, settings.greyscaleDisabledDarkness);

            if (snapInteractionValues)
            {
                SetFloatIfHas(BgBrightness, restBackgroundBrightness);
                SetBackgroundSaturationLike(restBackgroundSaturation);

                SetFloatIfHas(IconScale, restIconScale);
                SetFloatIfHas(IconSaturation, restIconSaturation);
                SetFloatIfHas(IconBrightness, restIconBrightness);
            }

            ApplyMaterialChanges();
        }

        public void GoToHoverState()
        {
            if (!CanAnimateCurrentState())
            {
                return;
            }

            SetupTransition(InteractionState.hoverState);
        }

        public void GoToPointerExitState()
        {
            SetupTransition(InteractionState.exitNothingHappening);
        }

        public void GoToClickDownState()
        {
            if (!CanAnimateCurrentState())
            {
                return;
            }

            SetupTransition(InteractionState.pointerDownState);
            SimpleSoundManager.Play(clickSoundConfig);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownEvent?.Invoke(this);
            onUnityPointerDown?.Invoke();

            if (CanAnimateCurrentState())
            {
                GoToClickDownState();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpEvent?.Invoke(this);
            onUnityPointerUp?.Invoke();

            if (isHovered && CanActivateCurrentState())
            {
                buttonActivatedClicked?.Invoke();
                OnButtonActivatedClicked?.Invoke(this);
            }

            if (!CanAnimateCurrentState())
            {
                return;
            }

            if (isHovered)
            {
                GoToHoverState();
            }
            else
            {
                GoToPointerExitState();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            OnButtonPointerEnterEvent?.Invoke(this);
            onUnityPointerEnter?.Invoke();

            if (!CanAnimateCurrentState())
            {
                return;
            }

            SimpleSoundManager.Play(hoverSoundConfig);

            if (myInnerState != InteractionState.pointerDownState)
            {
                GoToHoverState();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            OnButtonPointerExitEvent?.Invoke(this);
            onUnityPointerExit?.Invoke();

            if (!CanAnimateCurrentState())
            {
                return;
            }

            if (myInnerState != InteractionState.pointerDownState)
            {
                GoToPointerExitState();
            }
        }

        private bool CanAnimateCurrentState()
        {
            return currentStateSettings != null && currentStateSettings.allowPointerAnimations;
        }

        private bool CanActivateCurrentState()
        {
            return currentStateSettings != null && currentStateSettings.allowClickEvent;
        }

        private void SetupTransition(InteractionState targetState)
        {
            EnsureRuntimeMaterial();

            if (runtimeMaterial == null)
            {
                return;
            }

            myInnerState = targetState;
            elapsedTime = 0f;

            startBackgroundBrightness = GetFloatOrDefault(BgBrightness, restBackgroundBrightness);
            startBackgroundSaturation = GetBackgroundSaturationLike(restBackgroundSaturation);

            startIconScale = GetFloatOrDefault(IconScale, restIconScale);
            startIconSaturation = GetFloatOrDefault(IconSaturation, restIconSaturation);
            startIconBrightness = GetFloatOrDefault(IconBrightness, restIconBrightness);

            SimpleTweenManager.RegisterTween(this);
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
            float targetBackgroundBrightness =
                Mathf.Clamp(restBackgroundBrightness + hoverBackgroundBrightnessAdd, -1f, 1f);

            float targetBackgroundSaturation =
                Mathf.Clamp(restBackgroundSaturation * hoverBackgroundSaturationMultiplier, 0f, 2f);

            float targetIconScale =
                Mathf.Clamp(restIconScale * hoverIconScaleMultiplier, 0.2f, 2.5f);

            float targetIconSaturation =
                Mathf.Clamp(restIconSaturation * hoverIconSaturationMultiplier, 0f, 2f);

            float targetIconBrightness =
                Mathf.Clamp(restIconBrightness * hoverIconBrightnessMultiplier, 0f, 2f);

            AnimateProperties(
                targetBackgroundSaturation,
                targetBackgroundBrightness,
                targetIconScale,
                targetIconSaturation,
                targetIconBrightness,
                hoverAnimationDuration,
                deltaTime,
                EaseType.EaseOutQuad
            );
        }

        private void PointerExitAnimation(float deltaTime)
        {
            AnimateProperties(
                restBackgroundSaturation,
                restBackgroundBrightness,
                restIconScale,
                restIconSaturation,
                restIconBrightness,
                pointerExitAnimationDuration,
                deltaTime,
                EaseType.EaseOutCubic
            );
        }

        private void PointerDownAnimation(float deltaTime)
        {
            float targetBackgroundBrightness =
                Mathf.Clamp(restBackgroundBrightness + clickDownBackgroundBrightnessAdd, -1f, 1f);

            float targetBackgroundSaturation =
                Mathf.Clamp(restBackgroundSaturation * clickDownBackgroundSaturationMultiplier, 0f, 2f);

            float targetIconScale =
                Mathf.Clamp(restIconScale * clickDownIconScaleMultiplier, 0.2f, 2.5f);

            float targetIconSaturation =
                Mathf.Clamp(restIconSaturation * clickDownIconSaturationMultiplier, 0f, 2f);

            float targetIconBrightness =
                Mathf.Clamp(restIconBrightness * clickDownIconBrightnessMultiplier, 0f, 2f);

            AnimateProperties(
                targetBackgroundSaturation,
                targetBackgroundBrightness,
                targetIconScale,
                targetIconSaturation,
                targetIconBrightness,
                pointerDownAnimationDuration,
                deltaTime,
                EaseType.EaseOutQuart
            );
        }

        private void AnimateProperties(
            float targetBackgroundSaturation,
            float targetBackgroundBrightness,
            float targetIconScale,
            float targetIconSaturation,
            float targetIconBrightness,
            float duration,
            float deltaTime,
            EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            float currentBackgroundBrightness =
                Mathf.Lerp(startBackgroundBrightness, targetBackgroundBrightness, tEased);

            float currentBackgroundSaturation =
                Mathf.Lerp(startBackgroundSaturation, targetBackgroundSaturation, tEased);

            float currentIconScale =
                Mathf.Lerp(startIconScale, targetIconScale, tEased);

            float currentIconSaturation =
                Mathf.Lerp(startIconSaturation, targetIconSaturation, tEased);

            float currentIconBrightness =
                Mathf.Lerp(startIconBrightness, targetIconBrightness, tEased);

            SetFloatIfHas(BgBrightness, currentBackgroundBrightness);
            SetBackgroundSaturationLike(currentBackgroundSaturation);

            SetFloatIfHas(IconScale, currentIconScale);
            SetFloatIfHas(IconSaturation, currentIconSaturation);
            SetFloatIfHas(IconBrightness, currentIconBrightness);

            ApplyMaterialChanges();

            if (t >= 1f)
            {
                SimpleTweenManager.UnregisterTween(this);
            }
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

        private void SetBackgroundSaturationLike(float value)
        {
            if (TryGetBackgroundSaturationLikeProperty(out int propertyId))
            {
                SetFloatIfHas(propertyId, value);
            }
        }

        private float GetBackgroundSaturationLike(float fallback)
        {
            if (TryGetBackgroundSaturationLikeProperty(out int propertyId))
            {
                return GetFloatOrDefault(propertyId, fallback);
            }

            return fallback;
        }

        private bool TryGetBackgroundSaturationLikeProperty(out int propertyId)
        {
            propertyId = 0;

            if (runtimeMaterial == null)
            {
                return false;
            }

            if (runtimeMaterial.HasProperty(BgSaturation))
            {
                propertyId = BgSaturation;
                return true;
            }

            if (runtimeMaterial.HasProperty(BgContrast))
            {
                propertyId = BgContrast;
                return true;
            }

            return false;
        }

        private float GetFloatOrDefault(int propertyId, float fallback)
        {
            if (runtimeMaterial != null && runtimeMaterial.HasProperty(propertyId))
            {
                return runtimeMaterial.GetFloat(propertyId);
            }

            return fallback;
        }

        private void SetFloatIfHas(int propertyId, float value)
        {
            if (runtimeMaterial == null || !runtimeMaterial.HasProperty(propertyId))
            {
                return;
            }

            runtimeMaterial.SetFloat(propertyId, value);
            materialWasChanged = true;
        }

        private void SetColorIfHas(int propertyId, Color value)
        {
            if (runtimeMaterial == null || !runtimeMaterial.HasProperty(propertyId))
            {
                return;
            }

            runtimeMaterial.SetColor(propertyId, value);
            materialWasChanged = true;
        }

        private void SetTextureIfHas(int propertyId, Texture texture)
        {
            if (runtimeMaterial == null || texture == null || !runtimeMaterial.HasProperty(propertyId))
            {
                return;
            }

            runtimeMaterial.SetTexture(propertyId, texture);
            materialWasChanged = true;
        }

        private void ApplyMaterialChanges()
        {
            if (!materialWasChanged)
            {
                return;
            }

            materialWasChanged = false;

            if (coreImage != null)
            {
                coreImage.SetMaterialDirty();
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
    }
}
