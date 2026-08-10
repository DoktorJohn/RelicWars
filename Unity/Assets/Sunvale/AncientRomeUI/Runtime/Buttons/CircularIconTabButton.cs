using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Sound;
using Sunvale.Common.Tweening;


namespace Sunvale.AncientRomeUI.Buttons
{
    [AddComponentMenu("Sunvale/AncientRomeUI/CircularIconTabButton")]
    public class CircularIconTabButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ITweenClient
    {
        [Header("References")] [SerializeField]
        public RectTransform myRectTransform;

        [SerializeField] private Image normalBackgroundImage;
        [SerializeField] private Image activeBackgroundImage;
        [SerializeField] private Image iconImage;

        [Header("Scale")] public float normalScale = 1f;
        public float hoverScale = 1.06f;
        public float selectedScale = 1.04f;
        public float selectedAndHoveredScale = 1.09f;
        public float clickDownScale = 0.96f;

          
        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;
       


        [Header("Animation Durations")] public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("Unity Events")] public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent buttonActivatedClicked;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered;

        public bool isSelectedAsPrimeTab;

        private Vector3 startScale;
        private Vector3 targetScale;

        public delegate void MyDelegateForButtonInteraction(CircularIconTabButton theTab);

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

        private void Reset()
        {
            myRectTransform = GetComponent<RectTransform>();
        }

        private void OnValidate()
        {
            if (myRectTransform == null)
                myRectTransform = GetComponent<RectTransform>();

            if (!Application.isPlaying)
                ApplyVisualsInstant();
        }

        private void Awake()
        {
            if (myRectTransform == null)
                myRectTransform = GetComponent<RectTransform>();

            ApplyVisualsInstant();
        }

        private void OnDisable()
        {
            SimpleTweenManager.UnregisterTween(this);
        }

        public void SetAsSelectedAsPrime(bool withAnimation)
        {
            isSelectedAsPrimeTab = true;
            UpdateBackgroundImages();

            if (withAnimation)
            {
                RefreshCurrentStateWithAnimation();
            }
            else
            {
                SimpleTweenManager.UnregisterTween(this);

                myInnerState = isHovered
                    ? InteractionState.hoverState
                    : InteractionState.exitNothingHappening;

                ApplyVisualsInstant();
            }
        }

        public void SetAsDeselected(bool withAnimation)
        {
            isSelectedAsPrimeTab = false;
            UpdateBackgroundImages();

            if (withAnimation)
            {
                RefreshCurrentStateWithAnimation();
            }
            else
            {
                SimpleTweenManager.UnregisterTween(this);

                myInnerState = isHovered
                    ? InteractionState.hoverState
                    : InteractionState.exitNothingHappening;

                ApplyVisualsInstant();
            }
        }

        public void SetSelected(bool selected, bool withAnimation)
        {
            if (selected)
                SetAsSelectedAsPrime(withAnimation);
            else
                SetAsDeselected(withAnimation);
        }

        private void RefreshCurrentStateWithAnimation()
        {
            if (isHovered && myInnerState != InteractionState.pointerDownState)
            {
                GoToHoverState();
            }
            else if (!isHovered)
            {
                GoToPointerExitState();
            }
        }

        private void UpdateBackgroundImages()
        {
            bool shouldUseActiveBackground =
                isSelectedAsPrimeTab ||
                isHovered ||
                myInnerState == InteractionState.pointerDownState;

            if (normalBackgroundImage != null)
                normalBackgroundImage.enabled = !shouldUseActiveBackground;

            if (activeBackgroundImage != null)
                activeBackgroundImage.enabled = shouldUseActiveBackground;
        }

        private Vector3 GetTargetScale()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return Vector3.one * clickDownScale;

            if (isSelectedAsPrimeTab && isHovered)
                return Vector3.one * selectedAndHoveredScale;

            if (isHovered)
                return Vector3.one * hoverScale;

            if (isSelectedAsPrimeTab)
                return Vector3.one * selectedScale;

            return Vector3.one * normalScale;
        }

        private void ApplyVisualsInstant()
        {
            UpdateBackgroundImages();

            if (myRectTransform != null)
                myRectTransform.localScale = GetTargetScale();
        }

        private void SetupTransition(InteractionState targetStateOfMouseInteraction)
        {
            myInnerState = targetStateOfMouseInteraction;
            elapsedTime = 0f;

            UpdateBackgroundImages();

            if (myRectTransform != null)
                startScale = myRectTransform.localScale;
            else
                startScale = Vector3.one;

            targetScale = GetTargetScale();

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

            buttonActivatedClicked?.Invoke();
            OnButtonActivatedClicked?.Invoke(this);

            GoToClickDownState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpEvent?.Invoke(this);
            onUnityPointerUp?.Invoke();

            if (isHovered)
                GoToHoverState();
            else
                GoToPointerExitState();
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
            else
            {
                UpdateBackgroundImages();
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
            else
            {
                UpdateBackgroundImages();
            }
        }

        public void CustomUpdate(float deltaTime)
        {
            switch (myInnerState)
            {
                case InteractionState.hoverState:
                    AnimateProperties(hoverAnimationDuration, deltaTime, EaseType.EaseOutQuad);
                    break;

                case InteractionState.exitNothingHappening:
                    AnimateProperties(pointerExitAnimationDuration, deltaTime, EaseType.EaseOutCubic);
                    break;

                case InteractionState.pointerDownState:
                    AnimateProperties(pointerDownAnimationDuration, deltaTime, EaseType.EaseOutQuart);
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

        private void AnimateProperties(float duration, float deltaTime, EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            if (myRectTransform != null)
                myRectTransform.localScale = Vector3.Lerp(startScale, targetScale, tEased);

            if (t >= 1f)
            {
                if (myRectTransform != null)
                    myRectTransform.localScale = targetScale;

                SimpleTweenManager.UnregisterTween(this);
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
