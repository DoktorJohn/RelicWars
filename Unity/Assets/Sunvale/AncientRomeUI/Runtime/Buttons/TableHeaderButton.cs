using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.UI;
using Sunvale.Common.Sound;
using Sunvale.Common.Tweening;


namespace Sunvale.AncientRomeUI.Buttons
{
    [AddComponentMenu("Sunvale/AncientRomeUI/TableHeaderButton")]
    public class TableHeaderButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, ITweenClient
    {
        [Header("References")]
        [SerializeField] public Image myBackgroundImage;

        [SerializeField] public SimpleButton arrowUp;
        [SerializeField] public SimpleButton arrowDown;
        
          
        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;
        


        [Header("Animation Durations")]
        public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("HSV Target Values")]
        public float hoverSaturationValue = 1.2f;
        public float hoverBrightnessValue = 1.2f;
        public float clickDownSaturationValue = 1.4f;
        public float clickDownBrightnessValue = 1.4f;

        [Header("Unity Events")]
        public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public TableHeaderClickEvent buttonActivatedClicked;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered = false;

        private float baseSaturation = 1f;
        private float baseBrightness = 1f;

        private float startSaturation;
        private float startBrightness;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBright = Shader.PropertyToID("_HsvBright");

        public delegate void MyDelegateForButtonInteraction(TableHeaderButton theButton);
        public delegate void MyDelegateForButtonInteractionWithClickData(
            TableHeaderButton theButton,
            TableHeaderClickSource clickData
        );

        public event MyDelegateForButtonInteraction OnPointerDownEvent;
        public event MyDelegateForButtonInteraction OnPointerUpEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerEnterEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerExitEvent;
        public event MyDelegateForButtonInteractionWithClickData OnButtonActivatedClicked;

        public enum TableHeaderClickSource
        {
            nothingJustButton,
            arrowUp,
            arrowDown
        }

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

        [Serializable]
        public class TableHeaderClickEvent : UnityEvent<TableHeaderClickSource>
        {
        }

        private void Awake()
        {
            myBackgroundImage.material = new Material(myBackgroundImage.material);

            //baseSaturation = myBackgroundImage.material.GetFloat(HsvSaturation);
            //baseBrightness = myBackgroundImage.material.GetFloat(HsvBright);

            if (arrowUp != null)
            {
                arrowUp.OnButtonActivatedClicked += OnArrowUpClicked;
            }

            if (arrowDown != null)
            {
                arrowDown.OnButtonActivatedClicked += OnArrowDownClicked;
            }
        }

        private void OnDestroy()
        {
            if (arrowUp != null)
            {
                arrowUp.OnButtonActivatedClicked -= OnArrowUpClicked;
            }

            if (arrowDown != null)
            {
                arrowDown.OnButtonActivatedClicked -= OnArrowDownClicked;
            }
        }

        private void SetupTransition(InteractionState targetState)
        {
            myInnerState = targetState;
            elapsedTime = 0f;

            startSaturation = myBackgroundImage.material.GetFloat(HsvSaturation);
            startBrightness = myBackgroundImage.material.GetFloat(HsvBright);

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

        private void PublishClicked(TableHeaderClickSource clickData)
        {
            buttonActivatedClicked?.Invoke(clickData);
            OnButtonActivatedClicked?.Invoke(this, clickData);
        }

        private void OnArrowUpClicked(SimpleButton theButton)
        {
            PublishClicked(TableHeaderClickSource.arrowUp);
        }

        private void OnArrowDownClicked(SimpleButton theButton)
        {
            PublishClicked(TableHeaderClickSource.arrowDown);
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
                PublishClicked(TableHeaderClickSource.nothingJustButton);
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

        private void AnimateProperties(float targetSat, float targetBright, float duration,
            float deltaTime, EaseType easeType)
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            float currentSat = Mathf.Lerp(startSaturation, targetSat, tEased);
            float currentBright = Mathf.Lerp(startBrightness, targetBright, tEased);

            myBackgroundImage.material.SetFloat(HsvSaturation, currentSat);
            myBackgroundImage.material.SetFloat(HsvBright, currentBright);

            if (t >= 1f)
            {
                SimpleTweenManager.UnregisterTween(this);
            }
        }

        private void HoverAnimation(float deltaTime)
        {
            AnimateProperties(hoverSaturationValue, hoverBrightnessValue,
                hoverAnimationDuration, deltaTime, EaseType.EaseOutQuad);
        }

        private void PointerExitAnimation(float deltaTime)
        {
            AnimateProperties(baseSaturation, baseBrightness,
                pointerExitAnimationDuration, deltaTime, EaseType.EaseOutCubic);
        }

        private void PointerDownAnimation(float deltaTime)
        {
            AnimateProperties(clickDownSaturationValue, clickDownBrightnessValue,
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
