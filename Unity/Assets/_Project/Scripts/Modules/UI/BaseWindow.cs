using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Domain.Enums;
using System.Collections.Generic;
using System.Text;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class BaseWindow : MonoBehaviour
    {
        protected VisualElement Root;
        protected VisualElement MainContainer;
        protected UIDocument MyUiDocument;

        // Abstract: Every window must define its visual names
        protected abstract string WindowName { get; } // e.g., "TownHall"
        protected abstract string VisualContainerName { get; } // e.g., "TownHall-MainContainer"
        protected abstract string HeaderName { get; } // e.g., "TownHall-Header"

        public WindowTypeEnum Type { get; private set; }
        private GlobalWindowManager _manager;
        private bool _isReadyToShow;
        private bool _pendingFocus;
        private int _openSequence;
        private VisualElement _windowTooltip;
        private bool _isWindowTooltipPinned;

        public void Initialize(GlobalWindowManager manager, WindowTypeEnum type)
        {
            _manager = manager;
            Type = type;

            MyUiDocument = GetComponent<UIDocument>();
            Root = MyUiDocument.rootVisualElement;
            MainContainer = Root.Q<VisualElement>(VisualContainerName);
            SetWindowVisibility(false);

            // 1. Setup Dragging
            var header = Root.Q<VisualElement>(HeaderName);
            if (header != null && MainContainer != null)
            {
                var dragger = new CityUserInterfaceWindowDragManipulator(MainContainer);
                header.AddManipulator(dragger);
            }

            SetupWindowTooltip(header);

            // 2. Setup Close Button (Standardized naming recommended)
            var closeBtn = Root.Q<Button>($"{WindowName}-Close-Button");
            if (closeBtn != null) closeBtn.clicked += Close;

            // Capture pointer interaction before child controls can stop propagation.
            MainContainer?.RegisterCallback<PointerDownEvent>(OnWindowPointerDown, TrickleDown.TrickleDown);
        }

        public void Focus()
        {
            if (!_isReadyToShow)
            {
                _pendingFocus = true;
                return;
            }

            ApplyFocus();
        }

        private void OnWindowPointerDown(PointerDownEvent _)
        {
            Focus();
        }

        private void SetupWindowTooltip(VisualElement header)
        {
            if (header == null || MainContainer == null)
            {
                return;
            }

            if (!WindowTooltipCatalog.TryGet(Type, out string tooltipText))
            {
                Debug.LogWarning($"[BaseWindow] No tooltip text configured for window type: {Type}");
                return;
            }

            var tooltipButton = new Button();
            tooltipButton.name = $"{WindowName}-Info-Button";
            tooltipButton.AddToClassList("window-info-button");
            tooltipButton.clicked += ToggleWindowTooltip;
            tooltipButton.RegisterCallback<MouseEnterEvent>(ShowWindowTooltip);
            tooltipButton.RegisterCallback<MouseLeaveEvent>(HideUnpinnedWindowTooltip);
            header.Insert(0, tooltipButton);

            _windowTooltip = new VisualElement { pickingMode = PickingMode.Ignore };
            _windowTooltip.name = $"{WindowName}-Info-Tooltip";
            _windowTooltip.AddToClassList("window-info-tooltip");
            _windowTooltip.style.display = DisplayStyle.None;

            var title = new Label("ABOUT THIS WINDOW");
            title.AddToClassList("window-info-tooltip__title");
            _windowTooltip.Add(title);

            var body = new Label(tooltipText);
            body.AddToClassList("window-info-tooltip__body");
            _windowTooltip.Add(body);

            MainContainer.Add(_windowTooltip);
        }

        private void ShowWindowTooltip(MouseEnterEvent _)
        {
            SetWindowTooltipVisible(true);
        }

        private void HideUnpinnedWindowTooltip(MouseLeaveEvent _)
        {
            if (!_isWindowTooltipPinned)
            {
                SetWindowTooltipVisible(false);
            }
        }

        private void ToggleWindowTooltip()
        {
            _isWindowTooltipPinned = !_isWindowTooltipPinned;
            SetWindowTooltipVisible(_isWindowTooltipPinned);
        }

        private void SetWindowTooltipVisible(bool isVisible)
        {
            if (_windowTooltip == null)
            {
                return;
            }

            _windowTooltip.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (isVisible)
            {
                _windowTooltip.BringToFront();
            }
        }

        protected int BeginDeferredOpen()
        {
            _openSequence++;
            _isReadyToShow = false;
            SetWindowVisibility(false);
            return _openSequence;
        }

        protected bool IsDeferredOpenCurrent(int sequence)
        {
            return sequence == _openSequence;
        }

        protected void CompleteDeferredOpen(int sequence, bool bringToFront = true)
        {
            if (!IsDeferredOpenCurrent(sequence))
            {
                return;
            }

            _isReadyToShow = true;
            SetWindowVisibility(true);

            if (bringToFront || _pendingFocus)
            {
                ApplyFocus();
            }
        }

        protected void InvalidateDeferredOpen()
        {
            _openSequence++;
            _pendingFocus = false;
            _isReadyToShow = false;
            SetWindowVisibility(false);
        }

        public void Close()
        {
            _manager.CloseWindow(Type);
            Destroy(gameObject); // We destroy the GameObject to clean up
        }

        // Child classes implement this to receive data (e.g., CityId, EnemyId)
        public abstract void OnOpen(object dataPayload);

        private void ApplyFocus()
        {
            _pendingFocus = false;

            if (MyUiDocument != null && _manager != null)
            {
                _manager.NotifyWindowFocused(this);
                // We change the 'Sort Order' on the UIDocument component to bring it visually to the front
                MyUiDocument.sortingOrder = _manager.GetNextSortingOrder();
            }
        }

        private void SetWindowVisibility(bool isVisible)
        {
            if (Root == null)
            {
                return;
            }

            Root.style.visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }
    }

    internal static class WindowTooltipCatalog
    {
        private const string ResourcePath = "UI/WindowTooltips";
        private static Dictionary<WindowTypeEnum, string> _tooltips;

        public static bool TryGet(WindowTypeEnum windowType, out string tooltip)
        {
            EnsureLoaded();
            return _tooltips.TryGetValue(windowType, out tooltip);
        }

        private static void EnsureLoaded()
        {
            if (_tooltips != null)
            {
                return;
            }

            _tooltips = new Dictionary<WindowTypeEnum, string>();
            var tooltipFile = Resources.Load<TextAsset>(ResourcePath);
            if (tooltipFile == null)
            {
                Debug.LogError($"[WindowTooltipCatalog] Missing Resources/{ResourcePath}.txt");
                return;
            }

            Parse(tooltipFile.text);
        }

        private static void Parse(string fileContents)
        {
            WindowTypeEnum currentWindowType = WindowTypeEnum.None;
            var currentText = new StringBuilder();
            string[] lines = fileContents.Replace("\r\n", "\n").Split('\n');

            foreach (string line in lines)
            {
                if (line.StartsWith("## "))
                {
                    AddEntry(currentWindowType, currentText);
                    string windowTypeName = line.Substring(3).Trim();
                    if (!System.Enum.TryParse(windowTypeName, out currentWindowType) || currentWindowType == WindowTypeEnum.None)
                    {
                        Debug.LogWarning($"[WindowTooltipCatalog] Unknown window type: {windowTypeName}");
                        currentWindowType = WindowTypeEnum.None;
                    }

                    currentText.Clear();
                    continue;
                }

                if (currentWindowType != WindowTypeEnum.None)
                {
                    currentText.AppendLine(line);
                }
            }

            AddEntry(currentWindowType, currentText);
        }

        private static void AddEntry(WindowTypeEnum windowType, StringBuilder text)
        {
            if (windowType == WindowTypeEnum.None)
            {
                return;
            }

            string tooltip = text.ToString().Trim();
            if (string.IsNullOrEmpty(tooltip))
            {
                Debug.LogWarning($"[WindowTooltipCatalog] Empty tooltip for window type: {windowType}");
                return;
            }

            _tooltips[windowType] = tooltip;
        }
    }

    public static class WindowAsyncStateHelper
    {
        private const string StateContainerClass = "ui-window-state";
        private const string MessageClass = "ui-window-state__message";
        private const string RetryClass = "ui-window-state__retry";

        public static void ShowLoading(VisualElement container, string message = "Loading...")
        {
            ShowState(container, message, "ui-window-state--loading");
        }

        public static void ShowEmpty(VisualElement container, string message = "No data available.")
        {
            ShowState(container, message, "ui-window-state--empty");
        }

        public static void ShowError(VisualElement container, string message, System.Action retry = null)
        {
            if (container == null) return;

            container.Clear();

            var wrapper = new VisualElement();
            wrapper.AddToClassList(StateContainerClass);
            wrapper.AddToClassList("ui-window-state--error");
            ApplyInlineStateStyle(wrapper, true);

            var label = new Label(message ?? "Something went wrong.");
            label.AddToClassList(MessageClass);
            wrapper.Add(label);

            if (retry != null)
            {
                var retryButton = new Button(() => retry.Invoke()) { text = "RETRY" };
                retryButton.AddToClassList("btn-global-base");
                retryButton.AddToClassList("btn-imperial-primary");
                retryButton.AddToClassList(RetryClass);
                wrapper.Add(retryButton);
            }

            container.Add(wrapper);
        }

        public static void Clear(VisualElement container)
        {
            container?.Clear();
        }

        public static void SetButtonsEnabled(System.Collections.Generic.IEnumerable<Button> buttons, bool enabled)
        {
            if (buttons == null) return;

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.SetEnabled(enabled);
                }
            }
        }

        private static void ShowState(VisualElement container, string message, string className)
        {
            if (container == null) return;

            container.Clear();

            var wrapper = new VisualElement();
            wrapper.AddToClassList(StateContainerClass);
            wrapper.AddToClassList(className);
            ApplyInlineStateStyle(wrapper, false);

            var label = new Label(message ?? string.Empty);
            label.AddToClassList(MessageClass);
            wrapper.Add(label);

            container.Add(wrapper);
        }

        private static void ApplyInlineStateStyle(VisualElement wrapper, bool isError)
        {
            wrapper.style.width = Length.Percent(100);
            wrapper.style.minHeight = 80;
            wrapper.style.justifyContent = Justify.Center;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.paddingLeft = 16;
            wrapper.style.paddingRight = 16;
            wrapper.style.paddingTop = 16;
            wrapper.style.paddingBottom = 16;
            wrapper.style.marginTop = 8;
            wrapper.style.marginBottom = 8;
            wrapper.style.borderTopWidth = 1;
            wrapper.style.borderRightWidth = 1;
            wrapper.style.borderBottomWidth = 1;
            wrapper.style.borderLeftWidth = 1;
            wrapper.style.borderTopLeftRadius = 6;
            wrapper.style.borderTopRightRadius = 6;
            wrapper.style.borderBottomLeftRadius = 6;
            wrapper.style.borderBottomRightRadius = 6;
            wrapper.style.backgroundColor = isError
                ? new Color(0.47f, 0.12f, 0.08f, 0.10f)
                : new Color(0f, 0f, 0f, 0.06f);
            var borderColor = isError
                ? new Color(0.47f, 0.12f, 0.08f, 0.45f)
                : new Color(0.65f, 0.49f, 0.2f, 0.35f);
            wrapper.style.borderTopColor = borderColor;
            wrapper.style.borderRightColor = borderColor;
            wrapper.style.borderBottomColor = borderColor;
            wrapper.style.borderLeftColor = borderColor;
        }
    }
}
