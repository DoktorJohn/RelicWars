using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(LayoutElement))]
    public sealed class ProfileImageLayout : MonoBehaviour
    {
        [SerializeField] private Image profileImage;
        [SerializeField] private Vector2 maxSize = new Vector2(240f, 180f);

        private LayoutElement _layoutElement;
        private RectTransform _rectTransform;
        private Sprite _lastSprite;

        private void OnEnable() { CacheReferences(); Refresh(); }

        private void OnValidate()
        {
            maxSize.x = Mathf.Max(1f, maxSize.x);
            maxSize.y = Mathf.Max(1f, maxSize.y);
            CacheReferences();
            Refresh();
        }

        private void LateUpdate()
        {
            if (profileImage != null && profileImage.sprite != _lastSprite) Refresh();
        }

        private void CacheReferences()
        {
            if (_layoutElement == null) _layoutElement = GetComponent<LayoutElement>();
            if (_rectTransform == null) _rectTransform = (RectTransform)transform;
            if (profileImage == null)
            {
                var icon = transform.Find("Icon");
                if (icon != null) profileImage = icon.GetComponent<Image>();
            }
        }

        private void Refresh()
        {
            if (profileImage == null || profileImage.sprite == null || _layoutElement == null || _rectTransform == null) return;

            _lastSprite = profileImage.sprite;
            var spriteSize = _lastSprite.rect.size;
            var scale = Mathf.Min(maxSize.x / spriteSize.x, maxSize.y / spriteSize.y);
            var fittedSize = spriteSize * scale;

            _layoutElement.preferredWidth = fittedSize.x;
            _layoutElement.preferredHeight = fittedSize.y;
            _layoutElement.flexibleWidth = 0f;
            _layoutElement.flexibleHeight = 0f;
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fittedSize.x);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fittedSize.y);
        }
    }
}
