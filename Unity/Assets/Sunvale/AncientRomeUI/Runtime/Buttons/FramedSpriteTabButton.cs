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
    [AddComponentMenu("Sunvale/AncientRomeUI/FramedSpriteTabButton")]
    public class FramedSpriteTabButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ITweenClient
    {
        [SerializeField] public RectTransform myRectTransform;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private TextMeshProUGUI tmpLabel;
        [SerializeField] private Image iconImage;

        [Header("Background Sprites")]
        [SerializeField] public Sprite baseBackgroundSprite;
        [SerializeField] public Sprite hoverAndSelectedBackgroundSprite;

        
          
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

        [Header("Icon HSV")]
        public float baseIconHsvSaturation = 1f;
        public float hoverAndSelectedIconHsvSaturation = 1f;
        public float baseIconHsvBright = 1f;
        public float hoverAndSelectedIconHsvBright = 1f;

        [Header("Frame Pixels Per Unit")]
        public float baseFramePixelsPerUnit = 1f;
        public float hoverFramePixelsPerUnit = 1.25f;

        [Header("Scale")]
        public float normalScale = 1f;
        public float pointerDownScale = 0.96f;
        public bool remainSunkendOnSelect = false;
        public float selectedSunkenScale = 0.98f;

        [Header("Sound")]
        public bool playHoverSound = true;
        public bool playClickSound = true;

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

        private float startIconHsvSaturation;
        private float targetIconHsvSaturation;

        private float startIconHsvBright;
        private float targetIconHsvBright;

        private float startFramePixelsPerUnit;
        private float targetFramePixelsPerUnit;

        private Vector3 startScale;
        private Vector3 targetScale;

        private Material iconMaterialInstance;

        public delegate void MyDelegateForButtonInteraction(FramedSpriteTabButton theTab);

        public event MyDelegateForButtonInteraction OnPointerDownEvent;
        public event MyDelegateForButtonInteraction OnPointerUpEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerEnterEvent;
        public event MyDelegateForButtonInteraction OnButtonPointerExitEvent;
        public event MyDelegateForButtonInteraction OnButtonActivatedClicked;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBright = Shader.PropertyToID("_HsvBright");

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
            EnsureIconMaterialInstance();
            ApplyVisualsInstant();
        }

        private void OnDestroy()
        {
            if (iconMaterialInstance == null)
                return;

            if (Application.isPlaying)
                Destroy(iconMaterialInstance);
            else
                DestroyImmediate(iconMaterialInstance);

            iconMaterialInstance = null;
        }

        public void SetAsSelectedAsPrime(bool withAnimation)
        {
            isSelectedAsPrimeTab = true;
            UpdateBackgroundSprite();

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
            UpdateBackgroundSprite();

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

        private void UpdateBackgroundSprite()
        {
            if (isSelectedAsPrimeTab || isHovered)
                backgroundImage.sprite = hoverAndSelectedBackgroundSprite;
            else
                backgroundImage.sprite = baseBackgroundSprite;
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

        private float GetTargetIconHsvSaturation()
        {
            if (isHovered || isSelectedAsPrimeTab || myInnerState == InteractionState.pointerDownState)
                return hoverAndSelectedIconHsvSaturation;

            return baseIconHsvSaturation;
        }

        private float GetTargetIconHsvBright()
        {
            if (isHovered || isSelectedAsPrimeTab || myInnerState == InteractionState.pointerDownState)
                return hoverAndSelectedIconHsvBright;

            return baseIconHsvBright;
        }

        private float GetTargetFramePixelsPerUnit()
        {
            if (isHovered || myInnerState == InteractionState.pointerDownState)
                return hoverFramePixelsPerUnit;

            return baseFramePixelsPerUnit;
        }

        private Vector3 GetTargetScale()
        {
            if (myInnerState == InteractionState.pointerDownState)
                return Vector3.one * pointerDownScale;

            if (remainSunkendOnSelect && isSelectedAsPrimeTab)
                return Vector3.one * selectedSunkenScale;

            return Vector3.one * normalScale;
        }

        private bool EnsureIconMaterialInstance()
        {
            if (iconImage == null)
                return false;

            if (iconMaterialInstance != null)
                return true;

            Material sourceMaterial = iconImage.material;

            if (sourceMaterial == null)
                return false;

            iconMaterialInstance = new Material(sourceMaterial);
            iconImage.material = iconMaterialInstance;

            return true;
        }

        private bool HasValidIconMaterial()
        {
            return EnsureIconMaterialInstance()
                   && iconMaterialInstance.HasProperty(HsvSaturation)
                   && iconMaterialInstance.HasProperty(HsvBright);
        }

        private void ApplyIconHsv(float saturation, float brightness)
        {
            if (!HasValidIconMaterial())
                return;

            iconMaterialInstance.SetFloat(HsvSaturation, saturation);
            iconMaterialInstance.SetFloat(HsvBright, brightness);

            iconImage.SetMaterialDirty();
        }

        private void ApplyVisualsInstant()
        {
            UpdateBackgroundSprite();

            frameImage.color = GetTargetFrameColor();
            frameImage.pixelsPerUnitMultiplier = GetTargetFramePixelsPerUnit();
            frameImage.SetVerticesDirty();

            tmpLabel.color = GetTargetFontVertexColor();

            ApplyIconHsv(GetTargetIconHsvSaturation(), GetTargetIconHsvBright());

            myRectTransform.localScale = GetTargetScale();
        }

        private void SetupTransition(InteractionState targetStateOfMouseInteraction)
        {
            myInnerState = targetStateOfMouseInteraction;
            elapsedTime = 0f;

            UpdateBackgroundSprite();

            startFrameColor = frameImage.color;
            startFramePixelsPerUnit = frameImage.pixelsPerUnitMultiplier;
            startScale = myRectTransform.localScale;
            startFontVertexColor = tmpLabel.color;

            if (HasValidIconMaterial())
            {
                startIconHsvSaturation = iconMaterialInstance.GetFloat(HsvSaturation);
                startIconHsvBright = iconMaterialInstance.GetFloat(HsvBright);
            }
            else
            {
                startIconHsvSaturation = GetTargetIconHsvSaturation();
                startIconHsvBright = GetTargetIconHsvBright();
            }

            targetFrameColor = GetTargetFrameColor();
            targetFramePixelsPerUnit = GetTargetFramePixelsPerUnit();
            targetScale = GetTargetScale();
            targetFontVertexColor = GetTargetFontVertexColor();

            targetIconHsvSaturation = GetTargetIconHsvSaturation();
            targetIconHsvBright = GetTargetIconHsvBright();

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
            frameImage.pixelsPerUnitMultiplier = Mathf.Lerp(
                startFramePixelsPerUnit,
                targetFramePixelsPerUnit,
                tEased
            );
            frameImage.SetVerticesDirty();

            tmpLabel.color = Color.Lerp(startFontVertexColor, targetFontVertexColor, tEased);

            ApplyIconHsv(
                Mathf.Lerp(startIconHsvSaturation, targetIconHsvSaturation, tEased),
                Mathf.Lerp(startIconHsvBright, targetIconHsvBright, tEased)
            );

            myRectTransform.localScale = Vector3.Lerp(startScale, targetScale, tEased);

            if (t >= 1f)
            {
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
