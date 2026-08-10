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
    [AddComponentMenu("Sunvale/AncientRomeUI/TextColorTabButton")]
    public class TextColorTabButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ITweenClient
    {
        [Header("References")]
        [SerializeField] private Image frameImage;
        [SerializeField] private TextMeshProUGUI tmpLabel;
        [SerializeField] private Image bottomRedLineImage;
        [SerializeField] private Image topRedLineImage;

        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;

        [Header("Frame Colors")]
        public Color baseFrameColor = Color.white;
        public Color hoverFrameColor = Color.white;
        public Color selectedFrameColor = Color.white;

        [Header("Font Vertex Colors")]
        public Color defaultFontVertexColor = Color.white;
        public Color hoverFontVertexColor = Color.white;
        public Color selectedFontVertexColor = Color.white;

        [Header("Font Character Spacing")]
        public float defaultFontCharacterSpacing = 0f;
        public float pointerDownFontCharacterSpacing = 8f;

        [Header("Extra Red Lines")]
        public bool useTopRedLine = false;
        public float hoverRedLineHeight = 2f;
        public float selectedRedLineHeight = 4f;

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
        private bool isHovered;

        public bool isSelectedAsPrimeTab;

        private Color startFrameColor;
        private Color targetFrameColor;

        private Color startFontVertexColor;
        private Color targetFontVertexColor;

        private float startFontCharacterSpacing;
        private float targetFontCharacterSpacing;

        private float startBottomRedLineHeight;
        private float targetBottomRedLineHeight;

        private float startTopRedLineHeight;
        private float targetTopRedLineHeight;

        public delegate void MyDelegateForButtonInteraction(TextColorTabButton theTab);

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
            ApplyVisualsInstant();
        }

        public void SetAsSelectedAsPrime(bool withAnimation)
        {
            isSelectedAsPrimeTab = true;

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

                myInnerState = isHovered
                    ? InteractionState.hoverState
                    : InteractionState.exitNothingHappening;

                ApplyVisualsInstant();
            }
        }

        public void SetAsDeselected(bool withAnimation)
        {
            isSelectedAsPrimeTab = false;

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

        private Color GetTargetFrameColor()
        {
            if (isHovered || myInnerState == InteractionState.pointerDownState)
                return hoverFrameColor;

            if (isSelectedAsPrimeTab)
                return selectedFrameColor;

            return baseFrameColor;
        }

        private Color GetTargetFontVertexColor()
        {
            if (isHovered || myInnerState == InteractionState.pointerDownState)
                return hoverFontVertexColor;

            if (isSelectedAsPrimeTab)
                return selectedFontVertexColor;

            return defaultFontVertexColor;
        }

        private float GetTargetFontCharacterSpacing()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return pointerDownFontCharacterSpacing;

            return defaultFontCharacterSpacing;
        }

        private float GetTargetRedLineHeight()
        {
            if (isSelectedAsPrimeTab)
                return selectedRedLineHeight;

            if (isHovered || myInnerState == InteractionState.pointerDownState)
                return hoverRedLineHeight;

            return 0f;
        }

        private void SetImageHeight(Image image, float height)
        {
            Vector2 sizeDelta = image.rectTransform.sizeDelta;
            sizeDelta.y = height;
            image.rectTransform.sizeDelta = sizeDelta;
            image.SetVerticesDirty();
        }

        private float GetImageHeight(Image image)
        {
            return image.rectTransform.sizeDelta.y;
        }

        private void ApplyVisualsInstant()
        {
            frameImage.color = GetTargetFrameColor();
            frameImage.SetVerticesDirty();

            tmpLabel.color = GetTargetFontVertexColor();
            tmpLabel.characterSpacing = GetTargetFontCharacterSpacing();
            tmpLabel.SetVerticesDirty();

            float targetRedLineHeight = GetTargetRedLineHeight();

            bottomRedLineImage.enabled = targetRedLineHeight > 0f;
            SetImageHeight(bottomRedLineImage, targetRedLineHeight);

            if (topRedLineImage != null)
            {
                topRedLineImage.enabled = useTopRedLine && targetRedLineHeight > 0f;
                SetImageHeight(topRedLineImage, useTopRedLine ? targetRedLineHeight : 0f);
            }
        }

        private void SetupTransition(InteractionState targetStateOfMouseInteraction)
        {
            myInnerState = targetStateOfMouseInteraction;
            elapsedTime = 0f;

            startFrameColor = frameImage.color;
            startFontVertexColor = tmpLabel.color;
            startFontCharacterSpacing = tmpLabel.characterSpacing;

            startBottomRedLineHeight = GetImageHeight(bottomRedLineImage);

            if (topRedLineImage != null)
                startTopRedLineHeight = GetImageHeight(topRedLineImage);
            else
                startTopRedLineHeight = 0f;

            targetFrameColor = GetTargetFrameColor();
            targetFontVertexColor = GetTargetFontVertexColor();
            targetFontCharacterSpacing = GetTargetFontCharacterSpacing();

            targetBottomRedLineHeight = GetTargetRedLineHeight();
            targetTopRedLineHeight = useTopRedLine ? targetBottomRedLineHeight : 0f;

            if (startBottomRedLineHeight > 0f || targetBottomRedLineHeight > 0f)
                bottomRedLineImage.enabled = true;

            if (topRedLineImage != null)
                topRedLineImage.enabled = useTopRedLine && (startTopRedLineHeight > 0f || targetTopRedLineHeight > 0f);

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
                GoToHoverState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            OnButtonPointerExitEvent?.Invoke(this);
            onUnityPointerExit?.Invoke();

            if (myInnerState != InteractionState.pointerDownState)
                GoToPointerExitState();
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

            frameImage.color = Color.Lerp(startFrameColor, targetFrameColor, tEased);
            frameImage.SetVerticesDirty();

            tmpLabel.color = Color.Lerp(startFontVertexColor, targetFontVertexColor, tEased);
            tmpLabel.characterSpacing = Mathf.Lerp(startFontCharacterSpacing, targetFontCharacterSpacing, tEased);
            tmpLabel.SetVerticesDirty();

            SetImageHeight(
                bottomRedLineImage,
                Mathf.Lerp(startBottomRedLineHeight, targetBottomRedLineHeight, tEased)
            );

            if (topRedLineImage != null)
            {
                SetImageHeight(
                    topRedLineImage,
                    Mathf.Lerp(startTopRedLineHeight, targetTopRedLineHeight, tEased)
                );
            }

            if (t >= 1f)
            {
                if (targetBottomRedLineHeight <= 0f)
                    bottomRedLineImage.enabled = false;

                if (topRedLineImage != null && targetTopRedLineHeight <= 0f)
                    topRedLineImage.enabled = false;

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
