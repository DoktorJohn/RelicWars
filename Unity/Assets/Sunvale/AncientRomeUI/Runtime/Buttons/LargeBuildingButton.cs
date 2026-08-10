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
    [AddComponentMenu("Sunvale/AncientRomeUI/LargeBuildingButton")]
    public class LargeBuildingButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, ITweenClient
    {
        [Header("References")]
        public Image iconImage;
        public Image backgroundImage;
        public Image frameImage;

        [Header("Text")]
        public TMP_Text nameTMP;
        public TMP_Text secondaryTMP;
        public TMP_Text costTMP;
        public TMP_Text turnCountTMP;

        [Header("Sound")]
        public UISoundConfig hoverSoundConfig;
        public UISoundConfig clickSoundConfig;

        [Header("Animation Durations")]
        public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("HSV Target Values")]
        public float hoverSaturationValue = 1.15f;
        public float hoverBrightnessValue = 1.12f;
        public float clickDownSaturationValue = 1.35f;
        public float clickDownBrightnessValue = 1.25f;

        [Header("Frame Target Values")]
        public float hoverFramePixelsPerUnitMultiplier = 1.08f;
        public float clickDownFramePixelsPerUnitMultiplier = 1.08f;

        public Color hoverFrameColorTint = Color.white;
        public Color clickDownFrameColorTint = Color.white;

        [Header("Transform Target Values")]
        public float clickScale = 0.975f;

        [Header("Unity Events")]
        public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent buttonActivatedClicked;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered;

        private float baseIconSaturation = 1f;
        private float baseIconBrightness = 1f;

        private float baseBackgroundSaturation = 1f;
        private float baseBackgroundBrightness = 1f;

        private float baseFramePixelsPerUnitMultiplier = 1f;
        private Color baseFrameColor;
        private Vector3 baseScale;

        private float startIconSaturation;
        private float startIconBrightness;

        private float startBackgroundSaturation;
        private float startBackgroundBrightness;

        private float startFramePixelsPerUnitMultiplier;
        private Color startFrameColor;
        private Vector3 startScale;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBright = Shader.PropertyToID("_HsvBright");

        public delegate void MyDelegateForButtonInteraction(LargeBuildingButton theButton);

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
            iconImage.material = new Material(iconImage.material);
            backgroundImage.material = new Material(backgroundImage.material);

            baseIconSaturation = iconImage.material.GetFloat(HsvSaturation);
            baseIconBrightness = iconImage.material.GetFloat(HsvBright);

            baseBackgroundSaturation = backgroundImage.material.GetFloat(HsvSaturation);
            baseBackgroundBrightness = backgroundImage.material.GetFloat(HsvBright);

            baseFramePixelsPerUnitMultiplier = frameImage.pixelsPerUnitMultiplier;
            baseFrameColor = frameImage.color;

            baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            SimpleTweenManager.UnregisterTween(this);
        }

        private void SetupTransition(InteractionState targetState)
        {
            myInnerState = targetState;
            elapsedTime = 0f;

            startIconSaturation = iconImage.material.GetFloat(HsvSaturation);
            startIconBrightness = iconImage.material.GetFloat(HsvBright);

            startBackgroundSaturation = backgroundImage.material.GetFloat(HsvSaturation);
            startBackgroundBrightness = backgroundImage.material.GetFloat(HsvBright);

            startFramePixelsPerUnitMultiplier = frameImage.pixelsPerUnitMultiplier;
            startFrameColor = frameImage.color;

            startScale = transform.localScale;

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
            isHovered = false;

            OnButtonPointerExitEvent?.Invoke(this);
            onUnityPointerExit?.Invoke();

            if (myInnerState != InteractionState.pointerDownState)
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

        private Color GetFrameTintedColor(Color tint)
        {
            return new Color(
                baseFrameColor.r * tint.r,
                baseFrameColor.g * tint.g,
                baseFrameColor.b * tint.b,
                baseFrameColor.a * tint.a
            );
        }

        private void AnimateProperties(
            float targetSaturation,
            float targetBrightness,
            float targetFramePixelsPerUnitMultiplier,
            Color targetFrameColor,
            Vector3 targetScale,
            float duration,
            float deltaTime,
            EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            float currentIconSaturation = Mathf.Lerp(startIconSaturation, targetSaturation, tEased);
            float currentIconBrightness = Mathf.Lerp(startIconBrightness, targetBrightness, tEased);

            float currentBackgroundSaturation = Mathf.Lerp(startBackgroundSaturation, targetSaturation, tEased);
            float currentBackgroundBrightness = Mathf.Lerp(startBackgroundBrightness, targetBrightness, tEased);

            float currentFramePixelsPerUnitMultiplier = Mathf.Lerp(
                startFramePixelsPerUnitMultiplier,
                targetFramePixelsPerUnitMultiplier,
                tEased
            );

            Color currentFrameColor = Color.Lerp(startFrameColor, targetFrameColor, tEased);
            Vector3 currentScale = Vector3.Lerp(startScale, targetScale, tEased);

            iconImage.material.SetFloat(HsvSaturation, currentIconSaturation);
            iconImage.material.SetFloat(HsvBright, currentIconBrightness);

            backgroundImage.material.SetFloat(HsvSaturation, currentBackgroundSaturation);
            backgroundImage.material.SetFloat(HsvBright, currentBackgroundBrightness);

            frameImage.pixelsPerUnitMultiplier = currentFramePixelsPerUnitMultiplier;
            frameImage.color = currentFrameColor;
            frameImage.SetVerticesDirty();

            transform.localScale = currentScale;

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
                hoverFramePixelsPerUnitMultiplier,
                GetFrameTintedColor(hoverFrameColorTint),
                baseScale,
                hoverAnimationDuration,
                deltaTime,
                EaseType.EaseOutQuad
            );
        }

        private void PointerExitAnimation(float deltaTime)
        {
            AnimateProperties(
                baseIconSaturation,
                baseIconBrightness,
                baseFramePixelsPerUnitMultiplier,
                baseFrameColor,
                baseScale,
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
                clickDownFramePixelsPerUnitMultiplier,
                GetFrameTintedColor(clickDownFrameColorTint),
                baseScale * clickScale,
                pointerDownAnimationDuration,
                deltaTime,
                EaseType.EaseOutQuart
            );
        }

        public void SetText(string buildingName, string secondary, int cost, int turnCount)
        {
            nameTMP.SetText(buildingName);
            secondaryTMP.SetText(secondary);
            costTMP.SetText(cost.ToString("N0").Replace(",", " "));
            turnCountTMP.SetText(turnCount.ToString());
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
