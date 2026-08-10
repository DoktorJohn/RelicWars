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
    [AddComponentMenu("Sunvale/AncientRomeUI/IconTextSidebarButton")]
    public class IconTextSidebarButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ITweenClient
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI tmpLabel;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image leftSideBarImage;
        [SerializeField] private Image rightSideBarImage;

        [Header("Sounds")]
        [SerializeField] public UISoundConfig clickSoundConfig;
        [SerializeField] public UISoundConfig hoverSoundConfig;

        [Header("Font Vertex Colors")]
        public Color defaultFontVertexColor = Color.white;
        public Color hoverFontVertexColor = Color.white;
        public Color pointerDownFontVertexColor = Color.white;

        [Header("Icon Colors")]
        public Color defaultIconColor = Color.white;
        public Color hoverIconColor = Color.white;
        public Color pointerDownIconColor = Color.white;

        [Header("Icon Pointer Down Transform")]
        public float pointerDownIconXOffset = 2f;
        public float pointerDownIconScale = 0.92f;

        [Header("Side Bar Colors")]
        public Color defaultSideBarColor = Color.white;
        public Color hoverSideBarColor = Color.white;
        public Color pointerDownSideBarColor = Color.white;

        [Header("Font Character Spacing")]
        public float defaultFontCharacterSpacing = 0f;
        public float pointerDownFontCharacterSpacing = 8f;

        [Header("Side Bars")]
        public bool useLeftSideBar = true;
        public bool useRightSideBar = true;

        public float defaultSideBarWidth = 0f;
        public float hoverSideBarWidth = 2f;
        public float pointerDownSideBarWidth = 4f;

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

        private RectTransform iconRectTransform;
        private Vector2 defaultIconAnchoredPosition;
        private Vector3 defaultIconLocalScale;

        private Color startFontVertexColor;
        private Color targetFontVertexColor;

        private Color startIconColor;
        private Color targetIconColor;

        private Vector2 startIconAnchoredPosition;
        private Vector2 targetIconAnchoredPosition;

        private Vector3 startIconLocalScale;
        private Vector3 targetIconLocalScale;

        private Color startLeftSideBarColor;
        private Color targetLeftSideBarColor;

        private Color startRightSideBarColor;
        private Color targetRightSideBarColor;

        private float startFontCharacterSpacing;
        private float targetFontCharacterSpacing;

        private float startLeftSideBarWidth;
        private float targetLeftSideBarWidth;

        private float startRightSideBarWidth;
        private float targetRightSideBarWidth;

        public delegate void MyDelegateForButtonInteraction(IconTextSidebarButton theButton);

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
            if (iconImage != null)
            {
                iconRectTransform = iconImage.rectTransform;
                defaultIconAnchoredPosition = iconRectTransform.anchoredPosition;
                defaultIconLocalScale = iconRectTransform.localScale;
            }

            ApplyVisualsInstant();
        }

        private Color GetTargetFontVertexColor()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return pointerDownFontVertexColor;

            if (isHovered)
                return hoverFontVertexColor;

            return defaultFontVertexColor;
        }

        private Color GetTargetIconColor()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return pointerDownIconColor;

            if (isHovered)
                return hoverIconColor;

            return defaultIconColor;
        }

        private Vector2 GetTargetIconAnchoredPosition()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return defaultIconAnchoredPosition + new Vector2(pointerDownIconXOffset, 0f);

            return defaultIconAnchoredPosition;
        }

        private Vector3 GetTargetIconLocalScale()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return defaultIconLocalScale * pointerDownIconScale;

            return defaultIconLocalScale;
        }

        private Color GetTargetSideBarColor()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return pointerDownSideBarColor;

            if (isHovered)
                return hoverSideBarColor;

            return defaultSideBarColor;
        }

        private float GetTargetFontCharacterSpacing()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return pointerDownFontCharacterSpacing;

            return defaultFontCharacterSpacing;
        }

        private float GetTargetSideBarWidth()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return pointerDownSideBarWidth;

            if (isHovered)
                return hoverSideBarWidth;

            return defaultSideBarWidth;
        }

        private void SetImageWidth(Image image, float width)
        {
            if (image == null)
                return;

            Vector2 sizeDelta = image.rectTransform.sizeDelta;
            sizeDelta.x = width;
            image.rectTransform.sizeDelta = sizeDelta;
            image.SetVerticesDirty();
        }

        private float GetImageWidth(Image image)
        {
            if (image == null)
                return 0f;

            return image.rectTransform.sizeDelta.x;
        }

        private void SetImageColor(Image image, Color color)
        {
            if (image == null)
                return;

            image.color = color;
            image.SetVerticesDirty();
        }

        private void ApplyVisualsInstant()
        {
            Color targetFontColor = GetTargetFontVertexColor();
            Color targetIconColor = GetTargetIconColor();
            Color targetSideBarColor = GetTargetSideBarColor();

            if (tmpLabel != null)
            {
                tmpLabel.color = targetFontColor;
                tmpLabel.characterSpacing = GetTargetFontCharacterSpacing();
                tmpLabel.SetVerticesDirty();
            }

            SetImageColor(iconImage, targetIconColor);

            if (iconRectTransform != null)
            {
                iconRectTransform.anchoredPosition = GetTargetIconAnchoredPosition();
                iconRectTransform.localScale = GetTargetIconLocalScale();
            }

            float targetSideBarWidth = GetTargetSideBarWidth();

            if (leftSideBarImage != null)
            {
                leftSideBarImage.enabled = useLeftSideBar && targetSideBarWidth > 0f;
                SetImageColor(leftSideBarImage, targetSideBarColor);
                SetImageWidth(leftSideBarImage, useLeftSideBar ? targetSideBarWidth : 0f);
            }

            if (rightSideBarImage != null)
            {
                rightSideBarImage.enabled = useRightSideBar && targetSideBarWidth > 0f;
                SetImageColor(rightSideBarImage, targetSideBarColor);
                SetImageWidth(rightSideBarImage, useRightSideBar ? targetSideBarWidth : 0f);
            }
        }

        private void SetupTransition(InteractionState targetStateOfMouseInteraction)
        {
            myInnerState = targetStateOfMouseInteraction;
            elapsedTime = 0f;

            startFontVertexColor = tmpLabel != null ? tmpLabel.color : Color.white;
            startFontCharacterSpacing = tmpLabel != null ? tmpLabel.characterSpacing : defaultFontCharacterSpacing;

            startIconColor = iconImage != null ? iconImage.color : Color.white;

            if (iconRectTransform != null)
            {
                startIconAnchoredPosition = iconRectTransform.anchoredPosition;
                startIconLocalScale = iconRectTransform.localScale;
            }
            else
            {
                startIconAnchoredPosition = Vector2.zero;
                startIconLocalScale = Vector3.one;
            }

            startLeftSideBarColor = leftSideBarImage != null ? leftSideBarImage.color : Color.white;
            startRightSideBarColor = rightSideBarImage != null ? rightSideBarImage.color : Color.white;

            startLeftSideBarWidth = GetImageWidth(leftSideBarImage);
            startRightSideBarWidth = GetImageWidth(rightSideBarImage);

            targetFontVertexColor = GetTargetFontVertexColor();
            targetFontCharacterSpacing = GetTargetFontCharacterSpacing();

            targetIconColor = GetTargetIconColor();
            targetIconAnchoredPosition = GetTargetIconAnchoredPosition();
            targetIconLocalScale = GetTargetIconLocalScale();

            targetLeftSideBarColor = GetTargetSideBarColor();
            targetRightSideBarColor = GetTargetSideBarColor();

            float targetSideBarWidth = GetTargetSideBarWidth();

            targetLeftSideBarWidth = useLeftSideBar ? targetSideBarWidth : 0f;
            targetRightSideBarWidth = useRightSideBar ? targetSideBarWidth : 0f;

            if (leftSideBarImage != null)
                leftSideBarImage.enabled = useLeftSideBar && (startLeftSideBarWidth > 0f || targetLeftSideBarWidth > 0f);

            if (rightSideBarImage != null)
                rightSideBarImage.enabled = useRightSideBar && (startRightSideBarWidth > 0f || targetRightSideBarWidth > 0f);

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

            if (tmpLabel != null)
            {
                tmpLabel.color = Color.Lerp(startFontVertexColor, targetFontVertexColor, tEased);
                tmpLabel.characterSpacing = Mathf.Lerp(startFontCharacterSpacing, targetFontCharacterSpacing, tEased);
                tmpLabel.SetVerticesDirty();
            }

            SetImageColor(iconImage, Color.Lerp(startIconColor, targetIconColor, tEased));

            if (iconRectTransform != null)
            {
                iconRectTransform.anchoredPosition = Vector2.Lerp(startIconAnchoredPosition, targetIconAnchoredPosition, tEased);
                iconRectTransform.localScale = Vector3.Lerp(startIconLocalScale, targetIconLocalScale, tEased);
            }

            if (leftSideBarImage != null)
            {
                SetImageColor(leftSideBarImage, Color.Lerp(startLeftSideBarColor, targetLeftSideBarColor, tEased));
                SetImageWidth(leftSideBarImage, Mathf.Lerp(startLeftSideBarWidth, targetLeftSideBarWidth, tEased));
            }

            if (rightSideBarImage != null)
            {
                SetImageColor(rightSideBarImage, Color.Lerp(startRightSideBarColor, targetRightSideBarColor, tEased));
                SetImageWidth(rightSideBarImage, Mathf.Lerp(startRightSideBarWidth, targetRightSideBarWidth, tEased));
            }

            if (t >= 1f)
            {
                if (leftSideBarImage != null && targetLeftSideBarWidth <= 0f)
                    leftSideBarImage.enabled = false;

                if (rightSideBarImage != null && targetRightSideBarWidth <= 0f)
                    rightSideBarImage.enabled = false;

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
