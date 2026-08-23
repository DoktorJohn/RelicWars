using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiPremiumButtonHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        private const float HoverScale = 1.06f;
        private const float PressedScale = 0.96f;
        private const float HoverDuration = 0.12f;
        private const float ExitDuration = 0.075f;
        private const float TintStrength = 0.14f;
        private static readonly Color HoverTint = new Color(1f, 0.86f, 0.48f, 1f);

        private RectTransform _target;
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
        private bool _isAnimating;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            _target = transform as RectTransform;
            _baseScale = _target != null ? _target.localScale : Vector3.one;
            _images = GetComponentsInChildren<Image>(true);
            _baseColors = new Color[_images.Length];
            _startColors = new Color[_images.Length];
            _targetColors = new Color[_images.Length];

            for (int index = 0; index < _images.Length; index++)
            {
                _baseColors[index] = _images[index].color;
            }

            ApplyBaseVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
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
            BeginTransition(PressedScale, true, HoverDuration * 0.5f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            BeginTransition(_isHovered ? HoverScale : 1f, _isHovered, HoverDuration * 0.65f);
        }

        private void Update()
        {
            if (!_isAnimating || _target == null)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);
            _target.localScale = Vector3.LerpUnclamped(_startScale, _targetScale, easedTime);

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
            if (_target == null || _images == null)
            {
                return;
            }

            _startScale = _target.localScale;
            _targetScale = _baseScale * scaleMultiplier;
            _elapsed = 0f;
            _duration = duration;
            _isAnimating = true;

            for (int index = 0; index < _images.Length; index++)
            {
                _startColors[index] = _images[index] != null ? _images[index].color : _baseColors[index];
                _targetColors[index] = useHoverTint
                    ? BlendPreservingAlpha(_baseColors[index], HoverTint, TintStrength)
                    : _baseColors[index];
            }
        }

        private void ApplyBaseVisuals()
        {
            _isAnimating = false;
            if (_target != null)
            {
                _target.localScale = _baseScale;
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
