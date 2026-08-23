using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sunvale.AncientRomeUI.SkillTree;

namespace Project.Modules.UI
{
    /// <summary>
    /// Provides cursor-centred zoom and drag panning for one authored research tree.
    /// The content transform contains both the node and line containers, so they
    /// always move and scale together.
    /// </summary>
    public sealed class UguiResearchTreePanZoom : MonoBehaviour,
        IScrollHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField, Min(0.05f)] private float minZoom = 0.65f;
        [SerializeField, Min(0.05f)] private float maxZoom = 2.25f;
        [SerializeField, Min(0f)] private float wheelZoomStep = 0.12f;
        [SerializeField, Min(0f)] private float dragThreshold = 2f;
        [SerializeField, Min(0.05f)] private float initialZoom = 1f;

        private Canvas _canvas;
        private Camera _eventCamera;
        private bool _dragging;
        private Vector2 _lastPointerPosition;
        private float _zoom = 1f;

        public float Zoom => _zoom;

        private void Awake()
        {
            if (viewport == null) viewport = (RectTransform)transform;
            EnsureInputAndContent();
            _canvas = GetComponentInParent<Canvas>();
            _eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            SetZoom(Mathf.Clamp(initialZoom, minZoom, maxZoom), null, false);
        }

        private void EnsureInputAndContent()
        {
            Image hitTarget = GetComponent<Image>();
            if (hitTarget == null) hitTarget = gameObject.AddComponent<Image>();
            hitTarget.color = new Color(1f, 1f, 1f, 0f);
            hitTarget.raycastTarget = true;
            hitTarget.maskable = false;

            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();

            if (content != null && content != viewport) return;

            Transform existing = viewport.Find("Research Tree Zoom Content");
            if (existing != null)
            {
                content = existing as RectTransform;
                return;
            }

            SkillTreeConnectionBuilder builder = GetComponent<SkillTreeConnectionBuilder>();
            if (builder == null || builder.nodeContainer == null || builder.lineContainer == null)
            {
                Debug.LogError("[ResearchTreePanZoom] Tree is missing its node or line container.", this);
                content = null;
                return;
            }

            var contentObject = new GameObject("Research Tree Zoom Content", typeof(RectTransform));
            content = (RectTransform)contentObject.transform;
            content.SetParent(viewport, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.pivot = new Vector2(0.5f, 0.5f);

            builder.lineContainer.SetParent(content, true);
            builder.nodeContainer.SetParent(content, true);
            content.SetAsFirstSibling();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (content == null || Mathf.Abs(eventData.scrollDelta.y) < 0.01f) return;

            float direction = Mathf.Sign(eventData.scrollDelta.y);
            float nextZoom = _zoom * (1f + wheelZoomStep * direction);
            SetZoom(nextZoom, eventData.position, true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (content == null) return;
            _dragging = true;
            _lastPointerPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || content == null) return;

            Vector2 delta = eventData.position - _lastPointerPosition;
            if (delta.sqrMagnitude < dragThreshold * dragThreshold) return;

            float canvasScale = _canvas != null ? _canvas.scaleFactor : 1f;
            content.anchoredPosition += delta / Mathf.Max(canvasScale, 0.01f);
            _lastPointerPosition = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }

        public void ResetView()
        {
            if (content == null) return;
            content.anchoredPosition = Vector2.zero;
            SetZoom(Mathf.Clamp(initialZoom, minZoom, maxZoom), null, false);
        }

        private void SetZoom(float requestedZoom, Vector2? screenPosition, bool keepPointerStable)
        {
            if (content == null) return;

            float nextZoom = Mathf.Clamp(requestedZoom, minZoom, maxZoom);
            if (Mathf.Approximately(nextZoom, _zoom)) return;

            Vector3 pointerWorld = Vector3.zero;
            Vector3 contentPoint = Vector3.zero;
            bool hasPointer = keepPointerStable && screenPosition.HasValue &&
                              RectTransformUtility.ScreenPointToWorldPointInRectangle(
                                  viewport, screenPosition.Value, _eventCamera, out pointerWorld);
            if (hasPointer) contentPoint = content.InverseTransformPoint(pointerWorld);

            _zoom = nextZoom;
            content.localScale = Vector3.one * _zoom;

            if (hasPointer)
                content.position += pointerWorld - content.TransformPoint(contentPoint);
        }

#if UNITY_EDITOR
        public void Configure(RectTransform viewportTransform, RectTransform contentTransform)
        {
            viewport = viewportTransform;
            content = contentTransform;
            Image hitTarget = GetComponent<Image>();
            hitTarget.color = new Color(1f, 1f, 1f, 0f);
            hitTarget.raycastTarget = true;
            minZoom = Mathf.Clamp(minZoom, 0.05f, maxZoom);
            maxZoom = Mathf.Max(maxZoom, minZoom);
            initialZoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);
        }
#endif
    }
}
