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
    [AddComponentMenu("Sunvale/AncientRomeUI/BuildingSlotButton")]
    public class BuildingSlotButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, ITweenClient
    {
        [Header("References")] public Image iconImage;
        public Image backgroundImage;
        public Image levelLabelBackgroundImage;
        public TextMeshProUGUI levelLabelTMP;
        public Image frameImage;
        public GameObject levelLabelWrapper;
        public Image emptyPlusImage;

        [Header("Radial")] public Image radialFillImage;
        public GameObject radialFillLabelWrapper;
        public TextMeshProUGUI radialTMPLabel;

        [Header("Scale Target")]
        [Tooltip("Usually this is this.transform. Exposed in case you want to scale only an inner wrapper.")]
        public Transform scaleRoot;

        [Header("Sound")] public UISoundConfig hoverSoundConfig;
        public UISoundConfig clickSoundConfig;

        [Header("Animation Durations")] public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("HSV Target Values")] public float hoverSaturationValue = 1.2f;
        public float hoverBrightnessValue = 1.2f;
        public float clickDownSaturationValue = 1.45f;
        public float clickDownBrightnessValue = 1.45f;

        [Header("Frame Target Values")] public float hoverFramePixelsPerUnitMultiplier = 1.08f;
        public float clickDownFramePixelsPerUnitMultiplier = 1.08f;

        [Header("Transform Target Values")] public float hoverScale = 1f;
        public float clickScale = 0.98f;

        [Header("State")] public bool interactable = true;
        public bool startsAsEmptySlot = false;

        [Header("Unity Events")] public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent buttonActivatedClicked;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered;
        private bool isPointerDown;
        private bool isEmptySlot;

        private Material sharedTweenMaterial;

        private float baseSaturation = 1f;
        private float baseBrightness = 1f;
        private Vector3 baseScale = Vector3.one;
        private float baseFramePixelsPerUnitMultiplier = 1f;

        private float startSaturation;
        private float startBrightness;
        private Vector3 startScale;
        private float startFramePixelsPerUnitMultiplier;

        private bool requestedLevelLabelVisible = true;
        private bool requestedRadialFillVisible = false;
        private bool requestedRadialLabelVisible = false;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBright = Shader.PropertyToID("_HsvBright");

        public delegate void MyDelegateForButtonInteraction(BuildingSlotButton theButton);

        public event MyDelegateForButtonInteraction OnPointerDownEvent;
        public event MyDelegateForButtonInteraction OnPointerUpEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerEnterEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerExitEvent;
        public event MyDelegateForButtonInteraction OnButtonActivatedClicked;

        public enum InteractionState
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

        private void Awake()
        {
            if (scaleRoot == null)
            {
                scaleRoot = transform;
            }

            CreateSharedTweenMaterial();

            baseSaturation = GetSharedMaterialFloat(HsvSaturation, 1f);
            baseBrightness = GetSharedMaterialFloat(HsvBright, 1f);
            baseScale = scaleRoot.localScale;

            if (frameImage != null)
            {
                baseFramePixelsPerUnitMultiplier = frameImage.pixelsPerUnitMultiplier;
            }

            requestedLevelLabelVisible = levelLabelWrapper == null || levelLabelWrapper.activeSelf;
            requestedRadialFillVisible = radialFillImage != null && radialFillImage.enabled;
            requestedRadialLabelVisible = radialFillLabelWrapper != null && radialFillLabelWrapper.activeSelf;

            SetEmptySlot(startsAsEmptySlot);
            RefreshRadialVisuals();
        }

        private void OnDestroy()
        {
            if (sharedTweenMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(sharedTweenMaterial);
            }
            else
            {
                DestroyImmediate(sharedTweenMaterial);
            }
        }

        private void CreateSharedTweenMaterial()
        {
            Image sourceImage = GetFirstMaterialSourceImage();

            if (sourceImage == null || sourceImage.material == null)
            {
                return;
            }

            sharedTweenMaterial = new Material(sourceImage.material)
            {
                name = $"{sourceImage.material.name} BuildingSlotButton Instance"
            };

            ApplySharedMaterialToTweenTargets();
        }

        private Image GetFirstMaterialSourceImage()
        {
            if (iconImage != null)
                return iconImage;

            if (backgroundImage != null)
                return backgroundImage;

            if (levelLabelBackgroundImage != null)
                return levelLabelBackgroundImage;

            if (emptyPlusImage != null)
                return emptyPlusImage;

            return null;
        }

        private void ApplySharedMaterialToTweenTargets()
        {
            ApplySharedMaterial(iconImage);
            ApplySharedMaterial(backgroundImage);
            ApplySharedMaterial(levelLabelBackgroundImage);
            ApplySharedMaterial(emptyPlusImage);
        }

        private void ApplySharedMaterial(Image image)
        {
            if (image == null || sharedTweenMaterial == null)
            {
                return;
            }

            image.material = sharedTweenMaterial;
        }

        private float GetSharedMaterialFloat(int propertyID, float fallback)
        {
            if (sharedTweenMaterial == null || !sharedTweenMaterial.HasProperty(propertyID))
            {
                return fallback;
            }

            return sharedTweenMaterial.GetFloat(propertyID);
        }

        private void SetSharedMaterialFloat(int propertyID, float value)
        {
            if (sharedTweenMaterial == null || !sharedTweenMaterial.HasProperty(propertyID))
            {
                return;
            }

            sharedTweenMaterial.SetFloat(propertyID, value);
        }

        private void SetupTransition(InteractionState targetState)
        {
            myInnerState = targetState;
            elapsedTime = 0f;

            startSaturation = GetSharedMaterialFloat(HsvSaturation, baseSaturation);
            startBrightness = GetSharedMaterialFloat(HsvBright, baseBrightness);
            startScale = scaleRoot != null ? scaleRoot.localScale : Vector3.one;
            startFramePixelsPerUnitMultiplier = frameImage != null
                ? frameImage.pixelsPerUnitMultiplier
                : baseFramePixelsPerUnitMultiplier;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable || IsWrongPointerButton(eventData))
            {
                return;
            }

            isPointerDown = true;

            OnPointerDownEvent?.Invoke(this);
            onUnityPointerDown?.Invoke();

            GoToClickDownState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable || IsWrongPointerButton(eventData))
            {
                return;
            }

            if (!isPointerDown)
            {
                return;
            }

            isPointerDown = false;

            OnPointerUpEvent?.Invoke(this);
            onUnityPointerUp?.Invoke();

            if (isHovered)
            {
                buttonActivatedClicked?.Invoke();
                OnButtonActivatedClicked?.Invoke(this);
                GoToHoverState();
            }
            else
            {
                GoToPointerExitState();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable)
            {
                return;
            }

            isHovered = true;

            OnButtonPointerEnterEvent?.Invoke(this);
            onUnityPointerEnter?.Invoke();

            SimpleSoundManager.Play(hoverSoundConfig);

            if (myInnerState != InteractionState.pointerDownState)
            {
                GoToHoverState();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!interactable)
            {
                return;
            }

            isHovered = false;

            OnButtonPointerExitEvent?.Invoke(this);
            onUnityPointerExit?.Invoke();

            if (myInnerState != InteractionState.pointerDownState)
            {
                GoToPointerExitState();
            }
        }

        private bool IsWrongPointerButton(PointerEventData eventData)
        {
            return eventData != null && eventData.button != PointerEventData.InputButton.Left;
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

        private void AnimateProperties(
            float targetSat,
            float targetBright,
            float targetScaleMultiplier,
            float targetFramePixelsPerUnitMultiplier,
            float duration,
            float deltaTime,
            EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            float currentSat = Mathf.Lerp(startSaturation, targetSat, tEased);
            float currentBright = Mathf.Lerp(startBrightness, targetBright, tEased);
            Vector3 currentScale = Vector3.LerpUnclamped(startScale, baseScale * targetScaleMultiplier, tEased);
            float currentFramePixelsPerUnitMultiplier = Mathf.Lerp(
                startFramePixelsPerUnitMultiplier,
                targetFramePixelsPerUnitMultiplier,
                tEased
            );

            SetSharedMaterialFloat(HsvSaturation, currentSat);
            SetSharedMaterialFloat(HsvBright, currentBright);

            if (scaleRoot != null)
            {
                scaleRoot.localScale = currentScale;
            }

            SetFramePixelsPerUnitMultiplier(currentFramePixelsPerUnitMultiplier);

            if (t >= 1f)
            {
                SimpleTweenManager.UnregisterTween(this);
            }
        }

        private void HoverAnimation(float deltaTime)
        {
            AnimateProperties(
                hoverSaturationValue,
                hoverBrightnessValue,
                hoverScale,
                hoverFramePixelsPerUnitMultiplier,
                hoverAnimationDuration,
                deltaTime,
                EaseType.EaseOutQuad
            );
        }

        private void PointerExitAnimation(float deltaTime)
        {
            AnimateProperties(
                baseSaturation,
                baseBrightness,
                1f,
                baseFramePixelsPerUnitMultiplier,
                pointerExitAnimationDuration,
                deltaTime,
                EaseType.EaseOutCubic
            );
        }

        private void PointerDownAnimation(float deltaTime)
        {
            AnimateProperties(
                clickDownSaturationValue,
                clickDownBrightnessValue,
                clickScale,
                clickDownFramePixelsPerUnitMultiplier,
                pointerDownAnimationDuration,
                deltaTime,
                EaseType.EaseOutQuart
            );
        }

        private void SetFramePixelsPerUnitMultiplier(float value)
        {
            if (frameImage == null)
            {
                return;
            }

            frameImage.pixelsPerUnitMultiplier = value;
            frameImage.SetVerticesDirty();
        }

        // ---------------------------------------------------------------------
        // Public API for manager
        // ---------------------------------------------------------------------

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (!interactable)
            {
                isHovered = false;
                isPointerDown = false;
                GoToPointerExitState();
            }
        }

        public void SetIcon(Sprite sprite)
        {
            iconImage.sprite = sprite;
            RefreshEmptySlotVisuals();
        }

        public void SetBackground(Sprite sprite)
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = sprite;
            backgroundImage.enabled = sprite != null;
        }

        public void SetLevelLabelBackground(Sprite sprite)
        {
            if (levelLabelBackgroundImage == null)
            {
                return;
            }

            levelLabelBackgroundImage.sprite = sprite;
            levelLabelBackgroundImage.enabled = sprite != null && requestedLevelLabelVisible && !isEmptySlot;
        }

        public void SetFrame(Sprite sprite)
        {
            if (frameImage == null)
            {
                return;
            }

            frameImage.sprite = sprite;
            frameImage.enabled = sprite != null;
        }

        public void SetEmptyPlus(Sprite sprite)
        {
            if (emptyPlusImage == null)
            {
                return;
            }

            emptyPlusImage.sprite = sprite;
            RefreshEmptySlotVisuals();
        }

        public void SetLevel(int level)
        {
            SetLevelLabel(level.ToString());
        }

        public void SetLevelLabel(string text)
        {
            if (levelLabelTMP != null)
            {
                levelLabelTMP.text = text;
            }

            SetLevelLabelVisible(!string.IsNullOrEmpty(text));
        }

        public void SetLevelLabelVisible(bool visible)
        {
            requestedLevelLabelVisible = visible;
            ApplyLevelLabelVisibility();
        }

        public void SetEmptySlot(bool empty)
        {
            isEmptySlot = empty;
            RefreshEmptySlotVisuals();
        }

        public void SetBuildingSlot(Sprite icon, string levelText)
        {
            SetEmptySlot(false);
            SetIcon(icon);
            SetLevelLabel(levelText);
        }

        public void SetEmptySlotVisual(Sprite plusSprite = null)
        {
            if (plusSprite != null)
            {
                SetEmptyPlus(plusSprite);
            }

            SetEmptySlot(true);
        }

        public void SetRadialVisible(bool visible)
        {
            requestedRadialFillVisible = visible;
            RefreshRadialVisuals();
        }

        public void SetRadialFill(float normalizedFillAmount)
        {
            radialFillImage.fillAmount = Mathf.Clamp01(normalizedFillAmount);
        }

        public void SetRadialLabel(string text, bool visible = true)
        {
            if (radialTMPLabel != null)
            {
                radialTMPLabel.text = text;
            }

            requestedRadialLabelVisible = visible && !string.IsNullOrEmpty(text);
            RefreshRadialVisuals();
        }

        public void SetRadialProgress(float normalizedFillAmount, string labelText, bool visible)
        {
            SetRadialVisible(visible);
            SetRadialFill(normalizedFillAmount);
            SetRadialLabel(labelText, visible);
        }

        public void ClearRadial()
        {
            SetRadialProgress(0f, string.Empty, false);
        }

        private void RefreshEmptySlotVisuals()
        {
            if (iconImage != null)
            {
                iconImage.enabled = !isEmptySlot && iconImage.sprite != null;
            }

            if (emptyPlusImage != null)
            {
                emptyPlusImage.enabled = isEmptySlot && emptyPlusImage.sprite != null;
            }

            ApplyLevelLabelVisibility();
        }

        private void ApplyLevelLabelVisibility()
        {
            bool shouldShow = requestedLevelLabelVisible && !isEmptySlot;

            if (levelLabelWrapper != null)
            {
                levelLabelWrapper.SetActive(shouldShow);
            }

            if (levelLabelBackgroundImage != null)
            {
                levelLabelBackgroundImage.enabled = shouldShow && levelLabelBackgroundImage.sprite != null;
            }

            if (levelLabelTMP != null)
            {
                levelLabelTMP.enabled = shouldShow;
            }
        }

        private void RefreshRadialVisuals()
        {
            radialFillImage.enabled = requestedRadialFillVisible;
            bool shouldShowLabel = requestedRadialFillVisible && requestedRadialLabelVisible;
            radialFillLabelWrapper.SetActive(shouldShowLabel);
            radialTMPLabel.enabled = shouldShowLabel;
        }

        public bool IsEmptySlot()
        {
            return isEmptySlot;
        }

        public void SetIndexNumber(int number)
        {
            myTweenNumber = number;
        }

        public int GetIndexNumber()
        {
            return myTweenNumber;
        }

    #if UNITY_EDITOR
        private void Reset()
        {
            scaleRoot = transform;
        }

        private void OnValidate()
        {
            if (scaleRoot == null)
            {
                scaleRoot = transform;
            }
        }
    #endif
    }

}
