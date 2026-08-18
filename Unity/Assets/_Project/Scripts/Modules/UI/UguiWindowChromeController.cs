using Sunvale.AncientRomeUI.Buttons;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Project.Modules.UI
{
    public sealed class UguiWindowChromeController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform windowRoot;
        [SerializeField] private CarvedPressButton closeButton;

        private RectTransform _windowParent;
        private Transform _closeTarget;
        private EventTrigger.Entry _closeReleaseEntry;
        private Vector2 _pointerOffset;
        private bool _isDragging;
        private bool _isClosing;

        public void Configure(RectTransform configuredWindowRoot, Transform configuredCloseTarget)
        {
            if (isActiveAndEnabled && closeButton != null)
            {
                closeButton.OnButtonActivatedClicked -= OnCloseClicked;
            }

            windowRoot = configuredWindowRoot;
            _closeTarget = configuredCloseTarget;
            closeButton = _closeTarget != null
                ? _closeTarget.GetComponent<CarvedPressButton>()
                : null;
            WireCloseRelease();

            if (isActiveAndEnabled && closeButton != null)
            {
                closeButton.OnButtonActivatedClicked -= OnCloseClicked;
                closeButton.OnButtonActivatedClicked += OnCloseClicked;
            }
        }

        private void Awake()
        {
            // Shared uGUI window chrome contract: put this component on the authored Header.
            // Explicit inspector references remain supported, but standard windows require no wiring.
            windowRoot ??= transform.parent as RectTransform;

            Transform closeSearchRoot = windowRoot != null ? windowRoot : transform;
            _closeTarget ??= FindDescendant(closeSearchRoot, "X button");
            closeButton ??= _closeTarget != null
                ? _closeTarget.GetComponent<CarvedPressButton>()
                : null;
            WireCloseRelease();
        }

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.OnButtonActivatedClicked += OnCloseClicked;
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.OnButtonActivatedClicked -= OnCloseClicked;
            }

            _isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left ||
                windowRoot == null ||
                IsCloseButtonInteraction(eventData))
            {
                return;
            }

            _windowParent = windowRoot.parent as RectTransform;
            if (_windowParent == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _windowParent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 pointerPosition))
            {
                return;
            }

            _pointerOffset = windowRoot.anchoredPosition - pointerPosition;
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _windowParent == null || windowRoot == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _windowParent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 pointerPosition))
            {
                windowRoot.anchoredPosition = pointerPosition + _pointerOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left &&
                IsCloseButtonInteraction(eventData))
            {
                CloseWindow();
            }
        }

        private void OnCloseClicked(CarvedPressButton _)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            if (_isClosing) return;
            _isClosing = true;

            if (windowRoot != null)
            {
                Destroy(windowRoot.gameObject);
            }
        }

        private void WireCloseRelease()
        {
            if (_closeTarget == null) return;

            EventTrigger trigger = _closeTarget.GetComponent<EventTrigger>() ??
                                   _closeTarget.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();

            if (_closeReleaseEntry != null)
            {
                trigger.triggers.Remove(_closeReleaseEntry);
            }

            _closeReleaseEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            _closeReleaseEntry.callback.AddListener(_ => CloseWindow());
            trigger.triggers.Add(_closeReleaseEntry);
        }

        private bool IsCloseButtonInteraction(PointerEventData eventData)
        {
            return _closeTarget != null &&
                   ((eventData.pointerPress != null && eventData.pointerPress.transform.IsChildOf(_closeTarget)) ||
                    (eventData.pointerCurrentRaycast.gameObject != null &&
                     eventData.pointerCurrentRaycast.gameObject.transform.IsChildOf(_closeTarget)));
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null) return null;
            if (root.name.Equals(objectName, StringComparison.OrdinalIgnoreCase)) return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), objectName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
