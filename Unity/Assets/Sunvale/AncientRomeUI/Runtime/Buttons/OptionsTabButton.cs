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
    [AddComponentMenu("Sunvale/AncientRomeUI/OptionsTabButton")]
    public class OptionsTabButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, ITweenClient
    {
        [SerializeField] public RectTransform myRectTransform;
        [SerializeField] public Image mainBackgroundImage;

        [SerializeField] public Sprite normalSprite;
        [SerializeField] public Sprite highlightHoverAndSelectedSprite;

        [SerializeField] public TextMeshProUGUI tmpLabel;
        
         
        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;
       

        
        [Header("Animation Durations")] 
        public float hoverAnimationDuration = 0.12f;
        public float pointerDownAnimationDuration = 0.06f;
        public float pointerExitAnimationDuration = 0.075f;

        [Header("Unity Events")] 
        public UnityEvent onUnityPointerDown;
        public UnityEvent onUnityPointerUp;
        public UnityEvent onUnityPointerEnter;
        public UnityEvent onUnityPointerExit;
        public UnityEvent buttonActivatedClicked;

        private InteractionState myInnerState;
        private int myTweenNumber;
        private float elapsedTime;
        private bool isHovered = false;

        // Base values to return to when exiting
        public float baseSaturation = 1f;
        public float baseBrightness = 1f;
        
        public float baseSaturationWhenSelectedAsPrime = 1.05f;
        public float baseBrightnessWhenSelectedAsPrime = 1.05f;

        public float hoverSaturationWhenNotSelectedAsPrime = 0.9f;
        public float hoverBrightnessWhenNotSelectedAsPrime = 0.9f;
        
        public float hoverSaturationAddWhenSelectedAsPrime = 1.15f;
        public float hoverBrightnessAddWhenSelectedAsPrime = 1.15f;

        public float baseBevelSize = 4f;
        public float clickBevelSize = 0f;

        public Color topEdgeHighlightColorNormal = new Color(1.0f, 0.6f, 0.6f, 0.53f);
        public Color topEdgeHighlightColorHoverAndSelected = new Color(0.990566f, 0.9634355f, 0.9204788f, 0.85f);
        
        // Start values for smooth interrupted transitions
        private float startSaturation;
        private float startBrightness;
        private float startBevelSize;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBright = Shader.PropertyToID("_HsvBright");
        private static readonly int BevelSize = Shader.PropertyToID("_BevelSize");
        private static readonly int TopEdgeHighlightColor = Shader.PropertyToID("_HighlightColor");
        

        public delegate void MyDelegateForButtonInteraction(OptionsTabButton theTab);

        public event MyDelegateForButtonInteraction OnPointerDownEvent;
        public event MyDelegateForButtonInteraction OnPointerUpEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerEnterEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerExitEvent;
        public event MyDelegateForButtonInteraction OnButtonActivatedClicked;

        public bool isSelectedAsPrimeTab;
        
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
            // Instantiate material to avoid modifying the shared asset
            mainBackgroundImage.material = new Material(mainBackgroundImage.material);
        }

        public void SetTextOnLabel(string s)
        {
            tmpLabel.SetText(s);
        }

        public void SetAsSelectedAsPrime(bool withAnimation)
        {
            isSelectedAsPrimeTab = true;
            UpdateSprite();

            if (withAnimation)
            {
                if (isHovered && myInnerState != InteractionState.pointerDownState)
                    GoToHoverState();
                else if (!isHovered)
                    GoToPointerExitState();
            }
            else
            {
                SimpleTweenManager.UnregisterTween(this);
                myInnerState = isHovered ? InteractionState.hoverState : InteractionState.exitNothingHappening;

                float targetSat = isHovered ? hoverSaturationAddWhenSelectedAsPrime : baseSaturationWhenSelectedAsPrime;
                float targetBri = isHovered ? hoverBrightnessAddWhenSelectedAsPrime : baseBrightnessWhenSelectedAsPrime;

                mainBackgroundImage.material.SetFloat(HsvSaturation, targetSat);
                mainBackgroundImage.material.SetFloat(HsvBright, targetBri);
                mainBackgroundImage.material.SetFloat(BevelSize, baseBevelSize);
                mainBackgroundImage.material.SetColor(TopEdgeHighlightColor, topEdgeHighlightColorHoverAndSelected);
            }
        }

        public void SetAsDeselected(bool withAnimation)
        {
            isSelectedAsPrimeTab = false;
            UpdateSprite();

            if (withAnimation)
            {
                if (isHovered && myInnerState != InteractionState.pointerDownState)
                    GoToHoverState();
                else if (!isHovered)
                    GoToPointerExitState();
            }
            else
            {
                SimpleTweenManager.UnregisterTween(this);
                myInnerState = isHovered ? InteractionState.hoverState : InteractionState.exitNothingHappening;

                float targetSat = isHovered ? hoverSaturationWhenNotSelectedAsPrime : baseSaturation;
                float targetBri = isHovered ? hoverBrightnessWhenNotSelectedAsPrime : baseBrightness;

                mainBackgroundImage.material.SetFloat(HsvSaturation, targetSat);
                mainBackgroundImage.material.SetFloat(HsvBright, targetBri);
                mainBackgroundImage.material.SetFloat(BevelSize, baseBevelSize);
                mainBackgroundImage.material.SetColor(TopEdgeHighlightColor, topEdgeHighlightColorNormal);
            }
        }

        private void UpdateSprite()
        {
            if (isSelectedAsPrimeTab || isHovered)
            {
                mainBackgroundImage.sprite = highlightHoverAndSelectedSprite;
            }
            else
            {
                mainBackgroundImage.sprite = normalSprite;
            }
        }

        private void UpdateTopEdgeColor()
        {
            if (isSelectedAsPrimeTab || isHovered)
            {
                mainBackgroundImage.material.SetColor(TopEdgeHighlightColor, topEdgeHighlightColorHoverAndSelected);
            }
            else
            {
                mainBackgroundImage.material.SetColor(TopEdgeHighlightColor, topEdgeHighlightColorNormal);
            }
        }

        private void SetupTransition(InteractionState targetStateOfMouseInteraction)
        {
            myInnerState = targetStateOfMouseInteraction;
            elapsedTime = 0f;

            // Update visual graphic state
            UpdateSprite();
            UpdateTopEdgeColor();

            // Grab current material values so interrupted animations are perfectly smooth
            startSaturation = mainBackgroundImage.material.GetFloat(HsvSaturation);
            startBrightness = mainBackgroundImage.material.GetFloat(HsvBright);
            startBevelSize = mainBackgroundImage.material.GetFloat(BevelSize);

            // Register to our custom update loop
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

            // If the pointer is still inside the button bounds after releasing, go back to hover state
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
            
            SimpleSoundManager.Play(hoverSoundConfig);

            // Don't override the down state if we drag back into the button while holding click
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

            // Keep the click animation if we drag out while holding the button down
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

        // --- Custom Easing Math ---
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

        private void AnimateProperties(float targetSat, float targetBright, float targetBevel, float duration, float deltaTime, EaseType easeType)
        {
            elapsedTime += deltaTime;
            
            // Prevent division by zero
            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);

            // Apply custom Easing
            float tEased = GetEasedTime(t, easeType);

            // Interpolate Values
            float currentSat = Mathf.Lerp(startSaturation, targetSat, tEased);
            float currentBright = Mathf.Lerp(startBrightness, targetBright, tEased);
            float currentBevel = Mathf.Lerp(startBevelSize, targetBevel, tEased);

            // Apply to Material
            mainBackgroundImage.material.SetFloat(HsvSaturation, currentSat);
            mainBackgroundImage.material.SetFloat(HsvBright, currentBright);
            mainBackgroundImage.material.SetFloat(BevelSize, currentBevel);

            // Stop animating when we reach the target
            if (t >= 1f)
            {
                SimpleTweenManager.UnregisterTween(this);
            }
        }

        private void HoverAnimation(float deltaTime)
        {
            float targetSat = isSelectedAsPrimeTab ? hoverSaturationAddWhenSelectedAsPrime : hoverSaturationWhenNotSelectedAsPrime;
            float targetBri = isSelectedAsPrimeTab ? hoverBrightnessAddWhenSelectedAsPrime : hoverBrightnessWhenNotSelectedAsPrime;

            AnimateProperties(targetSat, targetBri, baseBevelSize, hoverAnimationDuration, deltaTime, EaseType.EaseOutQuad);
        }

        private void PointerExitAnimation(float deltaTime)
        {
            float targetSat = isSelectedAsPrimeTab ? baseSaturationWhenSelectedAsPrime : baseSaturation;
            float targetBri = isSelectedAsPrimeTab ? baseBrightnessWhenSelectedAsPrime : baseBrightness;

            AnimateProperties(targetSat, targetBri, baseBevelSize, pointerExitAnimationDuration, deltaTime, EaseType.EaseOutCubic);
        }

        private void PointerDownAnimation(float deltaTime)
        {
            // Saturation & Brightness maintain their hover equivalents, bevel collapses on click
            float targetSat = isSelectedAsPrimeTab ? hoverSaturationAddWhenSelectedAsPrime : hoverSaturationWhenNotSelectedAsPrime;
            float targetBri = isSelectedAsPrimeTab ? hoverBrightnessAddWhenSelectedAsPrime : hoverBrightnessWhenNotSelectedAsPrime;

            AnimateProperties(targetSat, targetBri, clickBevelSize, pointerDownAnimationDuration, deltaTime, EaseType.EaseOutQuart);
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
