using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.IdeologySelection
{
    public sealed class UguiIdeologyCardHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        private const float HoverScale = 1.03f;
        private const float PressedScale = 0.985f;
        private const float HoverDuration = 0.12f;
        private const float ExitDuration = 0.09f;
        private const float HoverTintStrength = 0.12f;
        private static readonly Color HoverTint = new Color(1f, 0.91f, 0.67f, 1f);

        private RectTransform _cardTransform;
        private Canvas _cardCanvas;
        private Image[] _images;
        private Color[] _baseColors;
        private Color[] _startColors;
        private Color[] _targetColors;
        private Vector3 _baseScale;
        private Vector3 _startScale;
        private Vector3 _targetScale;
        private float _elapsed;
        private float _duration;
        private bool _isHovered;
        private bool _isInteractable = true;
        private bool _isAnimating;
        private int _baseSortingOrder;

        public void Initialize(Image hitSurface)
        {
            _cardTransform = transform as RectTransform;
            _baseScale = _cardTransform != null ? _cardTransform.localScale : Vector3.one;

            Canvas parentCanvas = transform.parent != null
                ? transform.parent.GetComponentInParent<Canvas>()
                : null;
            _baseSortingOrder = parentCanvas != null ? parentCanvas.sortingOrder + 1 : 0;

            _cardCanvas = GetComponent<Canvas>();
            if (_cardCanvas == null)
            {
                _cardCanvas = gameObject.AddComponent<Canvas>();
            }

            _cardCanvas.overrideSorting = true;
            _cardCanvas.sortingOrder = _baseSortingOrder;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            Image[] allImages = GetComponentsInChildren<Image>(true);
            int visualImageCount = 0;
            foreach (Image image in allImages)
            {
                if (image != null && image != hitSurface)
                {
                    visualImageCount++;
                }
            }

            _images = new Image[visualImageCount];
            _baseColors = new Color[visualImageCount];
            _startColors = new Color[visualImageCount];
            _targetColors = new Color[visualImageCount];

            int targetIndex = 0;
            foreach (Image image in allImages)
            {
                if (image == null || image == hitSurface)
                {
                    continue;
                }

                _images[targetIndex] = image;
                _baseColors[targetIndex] = image.color;
                targetIndex++;
            }

            ApplyBaseVisuals();
        }

        public void SetInteractable(bool isInteractable)
        {
            _isInteractable = isInteractable;
            if (!isInteractable)
            {
                _isHovered = false;
                ApplyBaseVisuals();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            _isHovered = true;
            BeginTransition(HoverScale, true, HoverDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            BeginTransition(1f, false, ExitDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isInteractable)
            {
                BeginTransition(PressedScale, true, HoverDuration * 0.5f);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            BeginTransition(_isHovered ? HoverScale : 1f, _isHovered, HoverDuration * 0.65f);
        }

        private void Update()
        {
            if (!_isAnimating || _cardTransform == null)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);

            _cardTransform.localScale = Vector3.LerpUnclamped(_startScale, _targetScale, easedTime);
            for (int index = 0; index < _images.Length; index++)
            {
                if (_images[index] != null)
                {
                    _images[index].color = Color.LerpUnclamped(
                        _startColors[index],
                        _targetColors[index],
                        easedTime);
                }
            }

            if (normalizedTime >= 1f)
            {
                _isAnimating = false;
            }
        }

        private void BeginTransition(float scaleMultiplier, bool useHoverTint, float duration)
        {
            if (_cardTransform == null || _images == null)
            {
                return;
            }

            _startScale = _cardTransform.localScale;
            _targetScale = _baseScale * scaleMultiplier;
            _elapsed = 0f;
            _duration = duration;
            _isAnimating = true;

            if (_cardCanvas != null)
            {
                _cardCanvas.sortingOrder = useHoverTint
                    ? _baseSortingOrder + 1
                    : _baseSortingOrder;
            }

            for (int index = 0; index < _images.Length; index++)
            {
                _startColors[index] = _images[index] != null ? _images[index].color : _baseColors[index];
                _targetColors[index] = useHoverTint
                    ? BlendPreservingAlpha(_baseColors[index], HoverTint, HoverTintStrength)
                    : _baseColors[index];
            }
        }

        private void ApplyBaseVisuals()
        {
            _isAnimating = false;

            if (_cardTransform != null)
            {
                _cardTransform.localScale = _baseScale;
            }

            if (_cardCanvas != null)
            {
                _cardCanvas.sortingOrder = _baseSortingOrder;
            }

            if (_images == null)
            {
                return;
            }

            for (int index = 0; index < _images.Length; index++)
            {
                if (_images[index] != null)
                {
                    _images[index].color = _baseColors[index];
                }
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            ApplyBaseVisuals();
        }

        private static Color BlendPreservingAlpha(Color source, Color target, float strength)
        {
            Color result = Color.Lerp(source, target, strength);
            result.a = source.a;
            return result;
        }
    }
}
