using Assets.Scripts.Domain.Enums;
using Project.Network.Models;
using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Project.Scripts.Modules.Map
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldMapIslandButtonController : MonoBehaviour
    {
        private const float FutureCitySiteButtonWorldSize = 0.55f;

        [SerializeField] private float _resourceWorldOffset = 0f;
        [SerializeField, Min(0.01f)] private float _iconButtonWorldSize = 1.75f;
        [SerializeField, Min(0f)] private float _layoutWorldTolerance = 0.001f;
        [SerializeField, Min(0f)] private float _orthographicSizeTolerance = 0.001f;

        private Button _button;
        private VisualElement _rootVisualElement;
        private Camera _mainCamera;
        private WorldIslandMapDTO _island;
        private string _buttonLabel = string.Empty;
        private string _buttonTooltip = "Open island overview";
        private string _buttonClass;
        private string _iconClass;
        private int _resourceSlotIndex;
        private float _currentWorldOffset;
        private bool _openIslandOnClick;
        private bool _hasClickAction;
        private bool _isFutureCitySiteGroup;
        private readonly List<Button> _futureCitySiteButtons = new();
        private readonly List<Vector3> _futureCitySiteAnchors = new();
        private Vector2 _lastPanelPosition = new(float.NaN, float.NaN);
        private Vector3 _lastAnchorPosition = new(float.NaN, float.NaN, float.NaN);
        private Vector3 _lastCameraPosition = new(float.NaN, float.NaN, float.NaN);
        private float _lastOrthographicSize = float.NaN;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        private void OnEnable()
        {
            _mainCamera = Camera.main;
            _rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            ResponsiveUiStateManager.RegisterRoot(_rootVisualElement);
            _rootVisualElement.pickingMode = PickingMode.Ignore;

            _button = _rootVisualElement.Q<Button>("island-button");
            if (_button != null)
            {
                ApplyButtonConfiguration();
            }
        }

        private void OnDisable()
        {
            ResponsiveUiStateManager.UnregisterRoot(_rootVisualElement);
            UnregisterClickHandler();
        }

        public void Initialize(WorldIslandMapDTO island)
        {
            _isFutureCitySiteGroup = false;
            _island = island;
            _buttonLabel = string.Empty;
            _buttonTooltip = "Open island overview";
            _buttonClass = "island-map-button--resource";
            _iconClass = "icon-island";
            _currentWorldOffset = 0f;
            _openIslandOnClick = true;
            _hasClickAction = true;
            InvalidateLayout();
            ApplyButtonConfiguration();
        }

        public void Initialize(WorldIslandMapDTO island, ExoticResourceTypeEnum resourceType, int slotIndex)
        {
            _isFutureCitySiteGroup = false;
            _island = island;
            _buttonLabel = string.Empty;
            _buttonTooltip = $"{resourceType.ToString().ToUpperInvariant()} - view tier and investment";
            _buttonClass = "island-map-button--resource";
            _iconClass = GetResourceIconClass(resourceType);
            _currentWorldOffset = _resourceWorldOffset;
            _resourceSlotIndex = slotIndex;
            _openIslandOnClick = false;
            _hasClickAction = true;
            InvalidateLayout();
            ApplyButtonConfiguration();
        }

        public void InitializeFutureCitySites(IReadOnlyList<Vector3> worldPositions)
        {
            _isFutureCitySiteGroup = true;
            _island = null;
            _currentWorldOffset = 0f;
            _hasClickAction = false;

            _futureCitySiteAnchors.Clear();
            _futureCitySiteAnchors.AddRange(worldPositions);
            CreateFutureCitySiteButtons();
            InvalidateLayout();
        }

        private void LateUpdate()
        {
            if (_isFutureCitySiteGroup)
            {
                UpdateFutureCitySitePositions();
                return;
            }

            UpdatePosition();
        }

        private void OpenIslandWindow()
        {
            if (_island != null && GlobalWindowManager.Instance != null)
            {
                GlobalWindowManager.Instance.OpenWindow(WindowTypeEnum.Island, _island.Id);
            }
        }

        private void OpenExoticResourceWindow()
        {
            if (_island != null && GlobalWindowManager.Instance != null)
            {
                GlobalWindowManager.Instance.OpenWindow(
                    WindowTypeEnum.ExoticResource,
                    new ExoticResourceWindowPayload(_island.Id, _resourceSlotIndex));
            }
        }

        private void ApplyButtonConfiguration()
        {
            if (_button == null)
            {
                return;
            }

            UnregisterClickHandler();

            _button.text = _buttonLabel;
            _button.tooltip = _buttonTooltip;

            if (!string.IsNullOrWhiteSpace(_buttonClass))
            {
                _button.AddToClassList(_buttonClass);
            }

            if (!string.IsNullOrWhiteSpace(_iconClass))
            {
                _button.AddToClassList(_iconClass);
            }

            if (!_hasClickAction)
            {
                return;
            }

            if (_openIslandOnClick)
            {
                _button.clicked += OpenIslandWindow;
            }
            else
            {
                _button.clicked += OpenExoticResourceWindow;
            }
        }

        private void UpdatePosition()
        {
            if (_button == null || _button.panel == null || _mainCamera == null)
            {
                return;
            }

            Vector3 anchor = transform.position + new Vector3(0f, _currentWorldOffset, 0f);
            if (!HasLayoutChanged(anchor))
            {
                return;
            }

            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(_button.panel, anchor, _mainCamera);
            Vector2 worldSize = Vector2.one * _iconButtonWorldSize;
            Vector2 panelSize = GetButtonPanelSize(anchor, panelPosition, worldSize);
            if (float.IsNaN(panelSize.x) || panelSize.x <= 0f || float.IsNaN(panelSize.y) || panelSize.y <= 0f)
            {
                return;
            }

            ApplyButtonSize(panelSize);

            Vector2 topLeft = new(panelPosition.x - panelSize.x * 0.5f, panelPosition.y - panelSize.y * 0.5f);
            if (float.IsNaN(_lastPanelPosition.x) || (topLeft - _lastPanelPosition).sqrMagnitude >= 0.01f)
            {
                _lastPanelPosition = topLeft;
                _button.style.position = Position.Absolute;
                _button.style.left = topLeft.x;
                _button.style.top = topLeft.y;
            }

            _lastAnchorPosition = anchor;
            _lastCameraPosition = _mainCamera.transform.position;
            _lastOrthographicSize = _mainCamera.orthographicSize;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void CreateFutureCitySiteButtons()
        {
            if (_rootVisualElement == null || _button == null)
            {
                return;
            }

            foreach (var existingButton in _futureCitySiteButtons)
            {
                if (existingButton != _button)
                {
                    existingButton.RemoveFromHierarchy();
                }
            }

            _futureCitySiteButtons.Clear();
            for (int index = 0; index < _futureCitySiteAnchors.Count; index++)
            {
                Button siteButton = index == 0 ? _button : new Button();
                siteButton.name = $"future-city-site-{index}";
                siteButton.text = string.Empty;
                siteButton.tooltip = "Future city site";
                siteButton.pickingMode = PickingMode.Ignore;
                siteButton.AddToClassList("island-map-button");
                siteButton.AddToClassList("island-map-button--future-city-site");
                siteButton.AddToClassList("icon-castle");

                if (index > 0)
                {
                    _rootVisualElement.Add(siteButton);
                }

                _futureCitySiteButtons.Add(siteButton);
            }
        }

        private void UpdateFutureCitySitePositions()
        {
            if (_futureCitySiteButtons.Count == 0
                || _futureCitySiteButtons[0].panel == null
                || _mainCamera == null)
            {
                return;
            }

            Vector3 firstAnchor = _futureCitySiteAnchors[0];
            if (!HasLayoutChanged(firstAnchor))
            {
                return;
            }

            Vector2 firstPanelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                _futureCitySiteButtons[0].panel,
                firstAnchor,
                _mainCamera);
            Vector2 panelSize = GetButtonPanelSize(
                firstAnchor,
                firstPanelPosition,
                Vector2.one * FutureCitySiteButtonWorldSize);
            if (float.IsNaN(panelSize.x) || panelSize.x <= 0f || float.IsNaN(panelSize.y) || panelSize.y <= 0f)
            {
                return;
            }

            float panelWidth = _rootVisualElement.resolvedStyle.width;
            float panelHeight = _rootVisualElement.resolvedStyle.height;
            if (float.IsNaN(panelWidth) || panelWidth <= 0f
                || float.IsNaN(panelHeight) || panelHeight <= 0f)
            {
                return;
            }

            var panel = _futureCitySiteButtons[0].panel;
            for (int index = 0; index < _futureCitySiteButtons.Count; index++)
            {
                Button siteButton = _futureCitySiteButtons[index];
                Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                    panel,
                    _futureCitySiteAnchors[index],
                    _mainCamera);
                bool isVisible = !float.IsNaN(panelPosition.x)
                    && !float.IsNaN(panelPosition.y)
                    && (panelPosition.x >= -panelSize.x
                        && panelPosition.x <= panelWidth + panelSize.x
                        && panelPosition.y >= -panelSize.y
                        && panelPosition.y <= panelHeight + panelSize.y);
                siteButton.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!isVisible)
                {
                    continue;
                }

                ApplyButtonSize(siteButton, panelSize, ensureTouchTarget: false);
                siteButton.style.position = Position.Absolute;
                siteButton.style.left = panelPosition.x - panelSize.x * 0.5f;
                siteButton.style.top = panelPosition.y - panelSize.y * 0.5f;
            }

            _lastAnchorPosition = firstAnchor;
            _lastCameraPosition = _mainCamera.transform.position;
            _lastOrthographicSize = _mainCamera.orthographicSize;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private bool HasLayoutChanged(Vector3 anchor)
        {
            float worldToleranceSquared = _layoutWorldTolerance * _layoutWorldTolerance;
            return float.IsNaN(_lastOrthographicSize)
                || (anchor - _lastAnchorPosition).sqrMagnitude > worldToleranceSquared
                || (_mainCamera.transform.position - _lastCameraPosition).sqrMagnitude > worldToleranceSquared
                || Mathf.Abs(_mainCamera.orthographicSize - _lastOrthographicSize) > _orthographicSizeTolerance
                || Screen.width != _lastScreenWidth
                || Screen.height != _lastScreenHeight;
        }

        private Vector2 GetButtonPanelSize(Vector3 anchor, Vector2 anchorPanelPosition, Vector2 worldSize)
        {
            Vector2 widthEdgePanelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                _button.panel,
                anchor + _mainCamera.transform.right * worldSize.x,
                _mainCamera);
            Vector2 heightEdgePanelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                _button.panel,
                anchor + _mainCamera.transform.up * worldSize.y,
                _mainCamera);
            return new Vector2(
                Vector2.Distance(anchorPanelPosition, widthEdgePanelPosition),
                Vector2.Distance(anchorPanelPosition, heightEdgePanelPosition));
        }

        private void ApplyButtonSize(Vector2 size)
        {
            ApplyButtonSize(_button, size, ensureTouchTarget: true);
        }

        private static void ApplyButtonSize(Button button, Vector2 size, bool ensureTouchTarget)
        {
            if (ensureTouchTarget && ResponsiveUiStateManager.IsPhoneLayout)
            {
                size = new Vector2(Mathf.Max(size.x, 44f), Mathf.Max(size.y, 44f));
            }

            button.style.width = size.x;
            button.style.minWidth = size.x;
            button.style.maxWidth = size.x;
            button.style.height = size.y;
            button.style.minHeight = size.y;
            button.style.maxHeight = size.y;

            float borderRadius = Mathf.Min(size.x, size.y) * 0.5f;
            button.style.borderTopLeftRadius = borderRadius;
            button.style.borderTopRightRadius = borderRadius;
            button.style.borderBottomLeftRadius = borderRadius;
            button.style.borderBottomRightRadius = borderRadius;
        }

        private void InvalidateLayout()
        {
            _lastPanelPosition = new Vector2(float.NaN, float.NaN);
            _lastAnchorPosition = new Vector3(float.NaN, float.NaN, float.NaN);
            _lastCameraPosition = new Vector3(float.NaN, float.NaN, float.NaN);
            _lastOrthographicSize = float.NaN;
        }

        private void UnregisterClickHandler()
        {
            if (_button != null)
            {
                _button.clicked -= OpenIslandWindow;
                _button.clicked -= OpenExoticResourceWindow;
            }
        }

        private static string GetResourceIconClass(ExoticResourceTypeEnum resourceType)
        {
            return resourceType switch
            {
                ExoticResourceTypeEnum.Cloth => "icon-cloth",
                ExoticResourceTypeEnum.Coal => "icon-coal",
                ExoticResourceTypeEnum.Copper => "icon-copper",
                ExoticResourceTypeEnum.Cotton => "icon-cotton",
                ExoticResourceTypeEnum.Diamond => "icon-diamond",
                ExoticResourceTypeEnum.Gold => "icon-gold",
                ExoticResourceTypeEnum.Ivory => "icon-ivory",
                ExoticResourceTypeEnum.Sand => "icon-sand",
                ExoticResourceTypeEnum.Silver => "icon-silver",
                ExoticResourceTypeEnum.Sulphur => "icon-sulphur",
                _ => "icon-cloth"
            };
        }
    }
}
