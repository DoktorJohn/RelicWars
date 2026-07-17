using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public enum FrontendLayoutMode
    {
        Desktop,
        Tablet,
        Phone
    }

    public readonly struct FrontendLayoutSnapshot : IEquatable<FrontendLayoutSnapshot>
    {
        public FrontendLayoutMode Mode { get; }
        public ScreenOrientation Orientation { get; }
        public Rect SafeArea { get; }

        public FrontendLayoutSnapshot(FrontendLayoutMode mode, ScreenOrientation orientation, Rect safeArea)
        {
            Mode = mode;
            Orientation = orientation;
            SafeArea = safeArea;
        }

        public bool Equals(FrontendLayoutSnapshot other)
        {
            return Mode == other.Mode
                && Orientation == other.Orientation
                && SafeArea.Equals(other.SafeArea);
        }

        public override bool Equals(object obj) => obj is FrontendLayoutSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Mode;
                hash = (hash * 397) ^ (int)Orientation;
                hash = (hash * 397) ^ SafeArea.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// Owns the device layout mode and applies the corresponding USS classes to UI Toolkit roots.
    /// It deliberately contains no gameplay state or window-specific presentation logic.
    /// </summary>
    public sealed class ResponsiveUiStateManager : MonoBehaviour
    {
        private const float DesktopPhoneBreakpoint = 600f;
        private const float DesktopTabletBreakpoint = 900f;
        private const float TabletShortestSideInches = 5f;

        private static readonly HashSet<VisualElement> RegisteredRoots = new();

        public static ResponsiveUiStateManager Instance { get; private set; }
        public static event Action<FrontendLayoutSnapshot> LayoutChanged;

        public static FrontendLayoutMode CurrentLayoutMode => EnsureInstance()._snapshot.Mode;
        public static bool IsPhoneLayout => CurrentLayoutMode == FrontendLayoutMode.Phone;
        public static FrontendLayoutSnapshot CurrentSnapshot => EnsureInstance()._snapshot;

        private FrontendLayoutSnapshot _snapshot;

        public static void RegisterRoot(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            var manager = EnsureInstance();
            RegisteredRoots.Add(root);
            manager.ApplyClasses(root);
        }

        public static void UnregisterRoot(VisualElement root)
        {
            if (root != null)
            {
                RegisteredRoots.Remove(root);
            }
        }

        public static Vector4 GetSafeAreaInsets()
        {
            Rect safeArea = CurrentSnapshot.SafeArea;
            return new Vector4(
                safeArea.xMin,
                Screen.height - safeArea.yMax,
                Screen.width - safeArea.xMax,
                safeArea.yMin);
        }

        private static ResponsiveUiStateManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existingManager = FindFirstObjectByType<ResponsiveUiStateManager>();
            if (existingManager != null)
            {
                return existingManager;
            }

            var managerObject = new GameObject(nameof(ResponsiveUiStateManager));
            return managerObject.AddComponent<ResponsiveUiStateManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshSnapshot(true);
        }

        private void Update()
        {
            RefreshSnapshot(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void RefreshSnapshot(bool force)
        {
            var nextSnapshot = new FrontendLayoutSnapshot(
                DetermineLayoutMode(),
                Screen.width >= Screen.height ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait,
                Screen.safeArea);

            if (!force && nextSnapshot.Equals(_snapshot))
            {
                return;
            }

            _snapshot = nextSnapshot;

            foreach (VisualElement root in RegisteredRoots)
            {
                ApplyClasses(root);
            }

            LayoutChanged?.Invoke(_snapshot);
        }

        private void ApplyClasses(VisualElement root)
        {
            root.RemoveFromClassList("layout-phone");
            root.RemoveFromClassList("layout-tablet");
            root.RemoveFromClassList("layout-desktop");
            root.RemoveFromClassList("orientation-portrait");
            root.RemoveFromClassList("orientation-landscape");

            root.AddToClassList(_snapshot.Mode switch
            {
                FrontendLayoutMode.Phone => "layout-phone",
                FrontendLayoutMode.Tablet => "layout-tablet",
                _ => "layout-desktop"
            });

            root.AddToClassList(_snapshot.Orientation == ScreenOrientation.Portrait
                ? "orientation-portrait"
                : "orientation-landscape");
        }

        private static FrontendLayoutMode DetermineLayoutMode()
        {
            float shortestSide = Mathf.Min(Screen.width, Screen.height);

            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                if (Screen.dpi > 0f && shortestSide / Screen.dpi >= TabletShortestSideInches)
                {
                    return FrontendLayoutMode.Tablet;
                }

                return FrontendLayoutMode.Phone;
            }

            if (shortestSide <= DesktopPhoneBreakpoint)
            {
                return FrontendLayoutMode.Phone;
            }

            return shortestSide <= DesktopTabletBreakpoint
                ? FrontendLayoutMode.Tablet
                : FrontendLayoutMode.Desktop;
        }
    }
}
