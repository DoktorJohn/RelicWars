using System.Collections;
using Sunvale.Common.Sound;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    [DisallowMultipleComponent]
    public sealed class MapSceneNavigationButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        [SerializeField] private Image coreImage;
        [SerializeField] private Sprite hoverBackgroundSprite;
        [SerializeField] private Material hoverBackgroundMaterial;
        [SerializeField] private UISoundConfig clickSoundConfig;
        [SerializeField] private UISoundConfig hoverSoundConfig;
        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private float pressedScale = 0.97f;
        [SerializeField] private float hoverAnimationDuration = 0.08f;
        [SerializeField] private float pointerDownAnimationDuration = 0.04f;
        [SerializeField] private float pointerExitAnimationDuration = 0.075f;
        [SerializeField] private string worldMapSceneName = "WorldMapScene";
        [SerializeField] private string cityViewSceneName = "CityViewScene";

        private RectTransform _visual;
        private Vector3 _baseScale;
        private Coroutine _scaleRoutine;
        private bool _pointerInside;
        private bool _isLoading;
        private Sprite _normalBackgroundSprite;
        private Material _normalBackgroundMaterial;

        private void Awake()
        {
            coreImage ??= GetComponentInChildren<Image>(true);
            if (coreImage != null)
            {
                _normalBackgroundSprite = coreImage.sprite;
                _normalBackgroundMaterial = coreImage.material;
            }
            _visual = transform as RectTransform;
            _baseScale = _visual != null ? _visual.localScale : Vector3.one;
        }

        private void OnDisable()
        {
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = null;
            _pointerInside = false;
            _isLoading = false;
            SetHighlighted(false);
            if (_visual != null) _visual.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerInside = true;
            SetHighlighted(true);
            SimpleSoundManager.Play(hoverSoundConfig);
            AnimateScale(hoverScale, hoverAnimationDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            SetHighlighted(false);
            AnimateScale(1f, pointerExitAnimationDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                SetHighlighted(true);
                AnimateScale(pressedScale, pointerDownAnimationDuration);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                SetHighlighted(_pointerInside);
                AnimateScale(_pointerInside ? hoverScale : 1f, pointerDownAnimationDuration);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isLoading || eventData.button != PointerEventData.InputButton.Left) return;

            _isLoading = true;
            SimpleSoundManager.Play(clickSoundConfig);

            string activeSceneName = SceneManager.GetActiveScene().name;
            string destination = activeSceneName == worldMapSceneName
                ? cityViewSceneName
                : worldMapSceneName;
            SceneManager.LoadScene(destination);
        }

        private void AnimateScale(float multiplier, float duration)
        {
            if (_visual == null) return;
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(AnimateScaleRoutine(_baseScale * multiplier, duration));
        }

        private void SetHighlighted(bool highlighted)
        {
            if (coreImage == null) return;
            coreImage.sprite = highlighted && hoverBackgroundSprite != null
                ? hoverBackgroundSprite
                : _normalBackgroundSprite;
            coreImage.material = highlighted && hoverBackgroundMaterial != null
                ? hoverBackgroundMaterial
                : _normalBackgroundMaterial;
        }

        private IEnumerator AnimateScaleRoutine(Vector3 target, float duration)
        {
            Vector3 start = _visual.localScale;
            if (duration <= 0f)
            {
                _visual.localScale = target;
                _scaleRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - (1f - t) * (1f - t);
                _visual.localScale = Vector3.LerpUnclamped(start, target, t);
                yield return null;
            }

            _visual.localScale = target;
            _scaleRoutine = null;
        }
    }
}
