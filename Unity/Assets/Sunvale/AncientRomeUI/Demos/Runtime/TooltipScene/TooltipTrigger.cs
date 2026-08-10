using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.AncientRomeUI.Demos.TooltipScene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public enum TooltipPlacement
        {
            Auto,
            Right,
            Left,
            Top,
            Bottom
        }

        [Header("Tooltip Prefab")] public TooltipWindowId tooltipPrefab;

        [Header("Spawn Source")]
        [SerializeField] private RectTransform targetRectTransform;

        [Header("Timing")]
        [SerializeField] private float hoverDelay = 0.35f;

        [Header("Placement")]
        [SerializeField] private TooltipPlacement preferredPlacement = TooltipPlacement.Auto;
        [SerializeField] private float spacing = 14f;

        private float hoverTimer;
        private bool hasRequestedTooltip;

        public bool IsPointerInside { get; private set; }
        public Vector2 LastPointerScreenPosition { get; private set; }

        public float HoverDelay => hoverDelay;
        public float Spacing => spacing;
        public TooltipPlacement PreferredPlacement => preferredPlacement;

        public RectTransform TargetRectTransform
        {
            get
            {
                if (targetRectTransform != null)
                    return targetRectTransform;

                return transform as RectTransform;
            }
        }

        private void Reset()
        {
            targetRectTransform = transform as RectTransform;
            hoverDelay = 0.35f;
            spacing = 14f;
            preferredPlacement = TooltipPlacement.Auto;
        }

        private void Update()
        {
            if (!IsPointerInside)
                return;

            if (hasRequestedTooltip)
                return;

            hoverTimer += Time.unscaledDeltaTime;

            if (hoverTimer < hoverDelay)
                return;

            hasRequestedTooltip = true;

            if (TooltipController.Instance != null)
                TooltipController.Instance.ShowTooltip(this, tooltipPrefab);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
            LastPointerScreenPosition = eventData.position;
            hoverTimer = 0f;

            hasRequestedTooltip = TooltipController.Instance != null &&
                                  TooltipController.Instance.IsShowingFor(this);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            LastPointerScreenPosition = eventData.position;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
            hoverTimer = 0f;
            hasRequestedTooltip = false;

            if (TooltipController.Instance != null)
                TooltipController.Instance.NotifySpawnerExit(this);
        }

        private void OnDisable()
        {
            IsPointerInside = false;
            hoverTimer = 0f;
            hasRequestedTooltip = false;

            if (TooltipController.Instance != null)
                TooltipController.Instance.NotifySpawnerDisabled(this);
        }
    }
}
