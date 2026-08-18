using Assets.Scripts.Domain.Enums;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public interface IUguiWindowPayloadReceiver
    {
        void OnOpen(object payload);
    }

    /// <summary>
    /// Shared host for authored uGUI windows. Launchers own prefab selection;
    /// this component only owns lifetime and placement.
    /// </summary>
    public sealed class UguiWindowHostController : MonoBehaviour
    {
        [SerializeField] private Transform windowParent;
        [SerializeField] private Transform foregroundTransform;

        public static UguiWindowHostController Instance { get; private set; }

        private GameObject _activeWindowInstance;
        private WindowTypeEnum _activeWindowType = WindowTypeEnum.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            windowParent ??= transform;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OpenWindow(WindowTypeEnum windowType, GameObject windowPrefab, object payload = null)
        {
            if (_activeWindowInstance != null && _activeWindowType == windowType)
            {
                if (payload != null)
                {
                    DeliverPayload(_activeWindowInstance, payload);
                    return;
                }

                CloseActiveWindow();
                return;
            }

            if (windowPrefab == null)
            {
                Debug.LogError($"[UguiWindowHostController] Failed to open {windowType}: its owner has not assigned a prefab.");
                return;
            }

            CloseActiveWindow();
            GlobalWindowManager.Instance?.CloseAllWindows();

            _activeWindowInstance = Instantiate(windowPrefab, windowParent, false);
            _activeWindowInstance.name = $"UguiWindow_{windowType}";
            _activeWindowType = windowType;
            EnsureWindowRaycastSurface(_activeWindowInstance);
            EnsureWindowChrome(_activeWindowInstance);
            DeliverPayload(_activeWindowInstance, payload);
            PlaceBelowForeground();
        }

        public void ReplaceActiveWindow(WindowTypeEnum windowType, GameObject replacementPrefab)
        {
            CloseActiveWindow();
            if (replacementPrefab == null) return;

            _activeWindowInstance = Instantiate(replacementPrefab, windowParent, false);
            _activeWindowInstance.name = $"UguiWindow_{windowType}";
            _activeWindowType = windowType;
            EnsureWindowRaycastSurface(_activeWindowInstance);
            EnsureWindowChrome(_activeWindowInstance);
            PlaceBelowForeground();
        }

        public void CloseActiveWindow()
        {
            if (_activeWindowInstance != null) Destroy(_activeWindowInstance);
            _activeWindowInstance = null;
            _activeWindowType = WindowTypeEnum.None;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (_activeWindowInstance == null ||
                !_activeWindowInstance.TryGetComponent(out RectTransform windowRect))
            {
                return false;
            }

            Canvas canvas = windowRect.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(windowRect, screenPosition, eventCamera);
        }

        private void PlaceBelowForeground()
        {
            if (foregroundTransform != null && foregroundTransform.parent == windowParent)
            {
                _activeWindowInstance.transform.SetSiblingIndex(foregroundTransform.GetSiblingIndex());
            }
        }

        private static void EnsureWindowRaycastSurface(GameObject windowInstance)
        {
            // The window root sits behind all authored controls. Making its complete
            // rect raycastable blocks only the world directly underneath the window,
            // while clicks outside the window remain available to the city view.
            Graphic surface = windowInstance.GetComponent<Graphic>();
            if (surface == null)
            {
                Image transparentSurface = windowInstance.AddComponent<Image>();
                transparentSurface.color = Color.clear;
                surface = transparentSurface;
            }

            surface.raycastTarget = true;
        }

        private static void EnsureWindowChrome(GameObject windowInstance)
        {
            RectTransform header = FindShallowestHeader(windowInstance.transform);
            if (header == null)
            {
                Debug.LogError($"[UguiWindowHostController] {windowInstance.name} has no Header for drag/close handling.", windowInstance);
                return;
            }

            UguiWindowChromeController chrome = header.GetComponent<UguiWindowChromeController>();
            if (chrome == null)
            {
                chrome = header.gameObject.AddComponent<UguiWindowChromeController>();
            }

            RectTransform windowRoot = windowInstance.transform as RectTransform;
            Transform closeTarget = FindCloseTarget(header);
            if (windowRoot == null || closeTarget == null)
            {
                Debug.LogError($"[UguiWindowHostController] {windowInstance.name} is missing its window root or X button.", windowInstance);
                return;
            }

            chrome.Configure(windowRoot, closeTarget);
        }

        private static void DeliverPayload(GameObject windowInstance, object payload)
        {
            if (windowInstance.GetComponent(typeof(IUguiWindowPayloadReceiver)) is IUguiWindowPayloadReceiver receiver)
                receiver.OnOpen(payload);
        }

        private static Transform FindCloseTarget(Transform windowRoot)
        {
            foreach (Transform candidate in windowRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name.Equals("X button", StringComparison.OrdinalIgnoreCase) ||
                    candidate.name.Contains("Close", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static RectTransform FindShallowestHeader(Transform windowRoot)
        {
            RectTransform bestMatch = null;
            int bestDepth = int.MaxValue;

            foreach (RectTransform candidate in windowRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (!candidate.name.Equals("Header", StringComparison.OrdinalIgnoreCase)) continue;

                int depth = 0;
                Transform current = candidate;
                while (current != null && current != windowRoot)
                {
                    depth++;
                    current = current.parent;
                }

                if (current == windowRoot && depth < bestDepth)
                {
                    bestMatch = candidate;
                    bestDepth = depth;
                }
            }

            return bestMatch;
        }
    }
}
