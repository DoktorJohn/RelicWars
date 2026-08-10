using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Sunvale.Common.Sound;
using Sunvale.Common.Tweening;

namespace Sunvale.Common.UI
{
    [AddComponentMenu("Sunvale/Common/TMPDropdownSoundCompanion")]
    public class TMPDropdownSoundCompanion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, ITweenClient
    {
        public TMP_Dropdown dropDown;

        [Header("Sounds")]
        public UISoundConfig buttonHoverConfig;
        public UISoundConfig buttonClickConfig;
        public UISoundConfig tabSwitchConfig;

        private bool mouseInside;
        private int tweenIndex;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static bool inputSystemReflectionInitialized;
        private static PropertyInfo mouseCurrentProperty;
        private static PropertyInfo mousePositionProperty;
        private static MethodInfo positionReadValueMethod;
#endif

        private void Awake()
        {
            if (dropDown == null)
                dropDown = GetComponent<TMP_Dropdown>();

            if (dropDown != null)
                dropDown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void OnEnable()
        {
            StartWatching();
        }

        private void OnDisable()
        {
            StopWatching();
        }

        private void OnDestroy()
        {
            if (dropDown != null)
                dropDown.onValueChanged.RemoveListener(OnDropdownValueChanged);

            StopWatching();
        }

        public void PlayHoverSound()
        {
            if (dropDown != null && dropDown.IsExpanded)
                return;

            SimpleSoundManager.Play(buttonHoverConfig);
        }

        public void PlayClickSound()
        {
            SimpleSoundManager.Play(buttonClickConfig);
            SimpleSoundManager.Play(tabSwitchConfig);
        }

        private void OnDropdownValueChanged(int newValue)
        {
            SimpleSoundManager.Play(tabSwitchConfig);
            StartWatching();
        }

        // --- ITweenClient Interface ---

        public void SetIndexNumber(int number)
        {
            tweenIndex = number;
        }

        public int GetIndexNumber()
        {
            return tweenIndex;
        }

        public void CustomUpdate(float deltaTime)
        {
            if (dropDown == null || EventSystem.current == null)
            {
                StopWatching();
                return;
            }

            bool physicallyOver = IsPointerOverDropdown();
            bool isSelected = EventSystem.current.currentSelectedGameObject == dropDown.gameObject;

            if (!dropDown.IsExpanded)
            {
                if (!physicallyOver)
                {
                    if (isSelected || mouseInside)
                    {
                        mouseInside = false;
                        ForceDropdownPointerExitAndDeselect();
                        isSelected = false;
                    }
                }
            }

            if (!dropDown.IsExpanded && !mouseInside && !isSelected)
            {
                StopWatching();
            }
        }

        private void StartWatching()
        {
            if (tweenIndex == 0)
                SimpleTweenManager.RegisterTween(this);
        }

        private void StopWatching()
        {
            if (tweenIndex != 0)
            {
                SimpleTweenManager.UnregisterTween(this);
                tweenIndex = 0;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            mouseInside = true;
            PlayHoverSound();
            StartWatching();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            mouseInside = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PlayClickSound();
            StartWatching();
        }

        private bool IsPointerOverDropdown()
        {
            if (dropDown == null)
                return false;

            RectTransform rectTransform = dropDown.transform as RectTransform;

            if (rectTransform == null)
                return false;

            if (!TryGetPointerScreenPosition(out Vector2 pointerPosition))
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform,
                pointerPosition,
                GetEventCamera()
            );
        }

        private bool TryGetPointerScreenPosition(out Vector2 pointerPosition)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return TryGetPointerScreenPositionFromNewInputSystem(out pointerPosition);
#else
            pointerPosition = Input.mousePosition;
            return true;
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static bool TryGetPointerScreenPositionFromNewInputSystem(out Vector2 pointerPosition)
        {
            pointerPosition = Vector2.zero;

            InitializeInputSystemReflection();

            if (mouseCurrentProperty == null || mousePositionProperty == null)
                return false;

            object mouse = mouseCurrentProperty.GetValue(null, null);

            if (mouse == null)
                return false;

            object positionControl = mousePositionProperty.GetValue(mouse, null);

            if (positionControl == null)
                return false;

            if (positionReadValueMethod == null)
            {
                positionReadValueMethod = positionControl
                    .GetType()
                    .GetMethod(
                        "ReadValue",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        Type.EmptyTypes,
                        null
                    );
            }

            if (positionReadValueMethod == null)
                return false;

            object value = positionReadValueMethod.Invoke(positionControl, null);

            if (value is Vector2 position)
            {
                pointerPosition = position;
                return true;
            }

            return false;
        }

        private static void InitializeInputSystemReflection()
        {
            if (inputSystemReflectionInitialized)
                return;

            inputSystemReflectionInitialized = true;

            Type mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");

            if (mouseType == null)
                return;

            mouseCurrentProperty = mouseType.GetProperty(
                "current",
                BindingFlags.Public | BindingFlags.Static
            );

            mousePositionProperty = mouseType.GetProperty(
                "position",
                BindingFlags.Public | BindingFlags.Instance
            );
        }
#endif

        private Camera GetEventCamera()
        {
            Canvas canvas = dropDown.GetComponentInParent<Canvas>();

            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private void ForceDropdownPointerExitAndDeselect()
        {
            if (EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject == dropDown.gameObject)
                EventSystem.current.SetSelectedGameObject(null);

            if (!TryGetPointerScreenPosition(out Vector2 pointerPosition))
                return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = pointerPosition
            };

            ExecuteEvents.Execute<IPointerExitHandler>(
                dropDown.gameObject,
                pointerData,
                ExecuteEvents.pointerExitHandler
            );
        }

        private void Reset()
        {
            dropDown = GetComponent<TMP_Dropdown>();
        }
    }
}