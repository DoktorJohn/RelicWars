using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    /// <summary>
    /// Manipulator der gør det muligt at flytte et VisualElement ved at trække i et mål-element.
    /// </summary>
    public class CityUserInterfaceWindowDragManipulator : PointerManipulator
    {
        private Vector2 _pointerStartPosition;
        private bool _isCurrentlyDragging;
        private readonly VisualElement _targetWindowToMove;

        public CityUserInterfaceWindowDragManipulator(VisualElement targetWindowToMove)
        {
            _targetWindowToMove = targetWindowToMove;
            _isCurrentlyDragging = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            _pointerStartPosition = pointerEvent.localPosition;
            _isCurrentlyDragging = true;
            target.CapturePointer(pointerEvent.pointerId);

            // Bring vinduet forrest i UI-stakken
            _targetWindowToMove.BringToFront();
            pointerEvent.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (!_isCurrentlyDragging || !target.HasPointerCapture(pointerEvent.pointerId)) return;

            Vector2 pointerDelta = (Vector2)pointerEvent.localPosition - _pointerStartPosition;

            _targetWindowToMove.style.left = _targetWindowToMove.resolvedStyle.left + pointerDelta.x;
            _targetWindowToMove.style.top = _targetWindowToMove.resolvedStyle.top + pointerDelta.y;
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (!_isCurrentlyDragging || !target.HasPointerCapture(pointerEvent.pointerId)) return;

            _isCurrentlyDragging = false;
            target.ReleasePointer(pointerEvent.pointerId);
            pointerEvent.StopPropagation();
        }
    }
}