using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    /// <summary>
    /// Manipulator der håndterer flytning af UI-vinduer.
    /// Bruger Panel-koordinater i stedet for Local-koordinater for at undgå offset-fejl ved skalering.
    /// </summary>
    public class CityUserInterfaceWindowDragManipulator : PointerManipulator
    {
        private Vector2 _pointerStartGlobalPosition;
        private Vector2 _initialWindowPosition;
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
            if (_targetWindowToMove == null) return;

            // OBJEKTIV FIX: Vi bruger 'position' (Panel-space) i stedet for 'localPosition'.
            // Dette sikrer at musens startpunkt er fastlåst til skærmen, ikke til headeren.
            _pointerStartGlobalPosition = pointerEvent.position;

            // Vi gemmer vinduets nuværende position fra layout-motoren.
            _initialWindowPosition = new Vector2(
                _targetWindowToMove.resolvedStyle.left,
                _targetWindowToMove.resolvedStyle.top
            );

            _isCurrentlyDragging = true;
            target.CapturePointer(pointerEvent.pointerId);

            // Sørg for at vinduet ikke kæmper mod Flexbox centrering (vigtigt!)
            ResetFlexboxAlignmentToManualPosition();

            _targetWindowToMove.BringToFront();
            pointerEvent.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (!_isCurrentlyDragging || !target.HasPointerCapture(pointerEvent.pointerId)) return;

            // Beregn afstanden musen har flyttet sig globalt
            Vector2 currentPointerGlobalPosition = pointerEvent.position;
            Vector2 movementDelta = currentPointerGlobalPosition - _pointerStartGlobalPosition;

            // Sæt den nye position baseret på den oprindelige position + musens bevægelse
            _targetWindowToMove.style.left = _initialWindowPosition.x + movementDelta.x;
            _targetWindowToMove.style.top = _initialWindowPosition.y + movementDelta.y;
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (!_isCurrentlyDragging || !target.HasPointerCapture(pointerEvent.pointerId)) return;

            _isCurrentlyDragging = false;
            target.ReleasePointer(pointerEvent.pointerId);
            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Når man begynder at trække i et vindue der er centreret via 'justify-content' 
        /// eller 'align-items', skal vi deaktivere de properties der tvinger det mod midten.
        /// </summary>
        private void ResetFlexboxAlignmentToManualPosition()
        {
            if (_targetWindowToMove == null) return;

            // Hvis vinduet var centreret via margin: auto eller translate, nulstiller vi det her.
            _targetWindowToMove.style.right = StyleKeyword.Auto;
            _targetWindowToMove.style.bottom = StyleKeyword.Auto;

            // Hvis I bruger transform: translate(-50%, -50%) til centrering, skal det fjernes her:
            _targetWindowToMove.style.translate = new StyleTranslate(new Translate(0, 0, 0));
        }
    }
}