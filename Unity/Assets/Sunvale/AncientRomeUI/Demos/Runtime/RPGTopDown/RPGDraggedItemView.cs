using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;
using Sunvale.Common.Tweening;


namespace Sunvale.AncientRomeUI.Demos.RPGTopDown
{
    public class RPGDraggedItemView : MonoBehaviour, ITweenClient
    {
        [Header("References")]
        public Image itemImage;
        public RectTransform myRectTransform;
        public CanvasGroup canvasGroup;

        [Header("Sizing")]
        public float baseShortSideSize = 100f;
        public float maxLongSideSize = 230f;

        [Header("Animation Durations")]
        public float normalAnimationDuration = 0.08f;
        public float deleteAnimationDuration = 0.08f;

        [Header("Visual Values")]
        public float normalAlpha = 0.95f;
        public float deleteAlpha = 0.65f;
        public float normalScale = 1f;
        public float deleteScale = 0.86f;
        public Color normalColor = Color.white;
        public Color deleteColor = new Color(1f, 0.35f, 0.35f, 0.85f);

        private int myTweenNumber;
        private float elapsedTime;

        private bool isDeleteCandidate;

        private Color startColor;
        private float startAlpha;
        private float startScale;

        private VisualState currentVisualState;

        private enum VisualState
        {
            normal,
            deleteCandidate
        }

        private enum EaseType
        {
            EaseOutQuad,
            EaseOutCubic
        }

        private void Awake()
        {
            AutoWireReferences();

            itemImage.raycastTarget = false;
            itemImage.preserveAspect = true;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public void AutoWireReferences()
        {
            if (myRectTransform == null)
                myRectTransform = GetComponent<RectTransform>();

            if (itemImage == null)
                itemImage = GetComponent<Image>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show(RPGItemDefinitionSO item)
        {
            itemImage.sprite = item.itemSprite;
            itemImage.enabled = true;
            itemImage.color = normalColor;

            canvasGroup.alpha = normalAlpha;

            isDeleteCandidate = false;
            currentVisualState = VisualState.normal;

            ApplySizeFromSprite(item.itemSprite);

            itemImage.transform.localScale = new Vector3(normalScale, normalScale, normalScale);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            SimpleTweenManager.UnregisterTween(this);
            gameObject.SetActive(false);
        }

        public void SetDeleteCandidateVisual(bool value)
        {
            if (isDeleteCandidate == value)
                return;

            isDeleteCandidate = value;

            SetupTransition(isDeleteCandidate ? VisualState.deleteCandidate : VisualState.normal);
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            myRectTransform.anchoredPosition = anchoredPosition;
        }

        private void ApplySizeFromSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                SetSize(baseShortSideSize, baseShortSideSize);
                return;
            }

            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;

            if (spriteWidth <= 0f || spriteHeight <= 0f)
            {
                SetSize(baseShortSideSize, baseShortSideSize);
                return;
            }

            float width;
            float height;

            float aspect = spriteWidth / spriteHeight;

            if (aspect >= 1f)
            {
                height = baseShortSideSize;
                width = baseShortSideSize * aspect;
            }
            else
            {
                width = baseShortSideSize;
                height = baseShortSideSize / aspect;
            }

            float longSide = Mathf.Max(width, height);

            if (maxLongSideSize > 0f && longSide > maxLongSideSize)
            {
                float scale = maxLongSideSize / longSide;
                width *= scale;
                height *= scale;
            }

            SetSize(width, height);
        }

        private void SetSize(float width, float height)
        {
            myRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            myRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void SetupTransition(VisualState targetState)
        {
            currentVisualState = targetState;
            elapsedTime = 0f;

            startColor = itemImage.color;
            startAlpha = canvasGroup.alpha;
            startScale = itemImage.transform.localScale.x;

            SimpleTweenManager.RegisterTween(this);
        }

        public void CustomUpdate(float deltaTime)
        {
            switch (currentVisualState)
            {
                case VisualState.normal:
                    AnimateProperties(
                        normalColor,
                        normalAlpha,
                        normalScale,
                        normalAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutCubic
                    );
                    break;

                case VisualState.deleteCandidate:
                    AnimateProperties(
                        deleteColor,
                        deleteAlpha,
                        deleteScale,
                        deleteAnimationDuration,
                        deltaTime,
                        EaseType.EaseOutQuad
                    );
                    break;
            }
        }

        private void AnimateProperties(
            Color targetColor,
            float targetAlpha,
            float targetScale,
            float duration,
            float deltaTime,
            EaseType easeType
        )
        {
            elapsedTime += deltaTime;

            float safeDuration = Mathf.Max(0.0001f, duration);
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            float tEased = GetEasedTime(t, easeType);

            itemImage.color = Color.Lerp(startColor, targetColor, tEased);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, tEased);

            float currentScale = Mathf.Lerp(startScale, targetScale, tEased);
            itemImage.transform.localScale = new Vector3(currentScale, currentScale, currentScale);

            if (t >= 1f)
                SimpleTweenManager.UnregisterTween(this);
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

                default:
                    return t;
            }
        }

        private void OnDisable()
        {
            SimpleTweenManager.UnregisterTween(this);
        }

        public void SetIndexNumber(int number)
        {
            myTweenNumber = number;
        }

        public int GetIndexNumber()
        {
            return myTweenNumber;
        }

        private void Reset()
        {
            myRectTransform = GetComponent<RectTransform>();
            itemImage = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
