using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;
using Sunvale.Common.Tweening;


namespace Sunvale.Common.UI
{
    [AddComponentMenu("Sunvale/Common/SimpleButton")]
    public class SimpleButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, ITweenClient
    {
        [Header("References")] [SerializeField]
        public Image coreImage;

        [Header("Sound")] public UISoundConfig hoverSoundConfig;
        public UISoundConfig clickSoundConfig;


        [Header("Animation Durations")] public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("HSV Target Values")] public float hoverSaturationValue = 1.2f;
        public float hoverBrightnessValue = 1.2f;
        public float clickDownSaturationValue = 1.4f;
        public float clickDownBrightnessValue = 1.4f;

        [Header("Transform Target Values")] public float hoverScale = 1.01f;
        public float clickScale = 0.98f;

        [Header("Unity Events")] public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent buttonActivatedClicked;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered = false;

        private float baseSaturation = 1f;
        private float baseBrightness = 1f;
        private float baseScale = 1f;

        private float startSaturation;
        private float startBrightness;
        private float startScale;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBright = Shader.PropertyToID("_HsvBright");

        public delegate void MyDelegateForButtonInteraction(SimpleButton theButton);

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
            coreImage.material = new Material(coreImage.material);

            baseSaturation = coreImage.material.GetFloat(HsvSaturation);
            baseBrightness = coreImage.material.GetFloat(HsvBright);

            baseScale = coreImage.transform.localScale.x;
        }

        private void SetupTransition(InteractionState targetState)
        {
            myInnerState = targetState;
            elapsedTime = 0f;

            startSaturation = coreImage.material.GetFloat(HsvSaturation);
            startBrightness = coreImage.material.GetFloat(HsvBright);
            startScale = coreImage.transform.localScale.x;

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

        private void AnimateProperties(float targetSat, float targetBright, float targetScale, float duration,
            float deltaTime, EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            float currentSat = Mathf.Lerp(startSaturation, targetSat, tEased);
            float currentBright = Mathf.Lerp(startBrightness, targetBright, tEased);
            float currentScale = Mathf.Lerp(startScale, targetScale, tEased);

            coreImage.material.SetFloat(HsvSaturation, currentSat);
            coreImage.material.SetFloat(HsvBright, currentBright);
            coreImage.transform.localScale = new Vector3(currentScale, currentScale, currentScale);

            if (t >= 1f)
            {
                SimpleTweenManager.UnregisterTween(this);
            }
        }

        private void HoverAnimation(float deltaTime)
        {
            AnimateProperties(hoverSaturationValue, hoverBrightnessValue, hoverScale,
                hoverAnimationDuration, deltaTime, EaseType.EaseOutQuad);
        }

        private void PointerExitAnimation(float deltaTime)
        {
            AnimateProperties(baseSaturation, baseBrightness, baseScale,
                pointerExitAnimationDuration, deltaTime, EaseType.EaseOutCubic);
        }

        private void PointerDownAnimation(float deltaTime)
        {
            AnimateProperties(clickDownSaturationValue, clickDownBrightnessValue, clickScale,
                pointerDownAnimationDuration, deltaTime, EaseType.EaseOutQuart);
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
