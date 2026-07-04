using Assets.Scripts.Domain.Enums;
using Project.Network.Models;
using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.Map
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldMapIslandButtonController : MonoBehaviour
    {
        [SerializeField] private float _resourceWorldOffset = 0f;
        [SerializeField, Min(0.01f)] private float _iconButtonWorldSize = 1.75f;
        [SerializeField, Min(0f)] private float _layoutWorldTolerance = 0.001f;
        [SerializeField, Min(0f)] private float _orthographicSizeTolerance = 0.001f;

        private Button _button;
        private Camera _mainCamera;
        private WorldIslandMapDTO _island;
        private string _buttonLabel = string.Empty;
        private string _buttonTooltip = "Open island overview";
        private string _buttonClass;
        private string _iconClass;
        private int _resourceSlotIndex;
        private float _currentWorldOffset;
        private bool _openIslandOnClick;
        private Vector2 _lastPanelPosition = new(float.NaN, float.NaN);
        private Vector3 _lastAnchorPosition = new(float.NaN, float.NaN, float.NaN);
        private Vector3 _lastCameraPosition = new(float.NaN, float.NaN, float.NaN);
        private float _lastOrthographicSize = float.NaN;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        private void OnEnable()
        {
            _mainCamera = Camera.main;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            _button = root.Q<Button>("island-button");
            if (_button != null)
            {
                ApplyButtonConfiguration();
            }
        }

        private void OnDisable()
        {
            UnregisterClickHandler();
        }

        public void Initialize(WorldIslandMapDTO island)
        {
            _island = island;
            _buttonLabel = string.Empty;
            _buttonTooltip = "Open island overview";
            _buttonClass = "island-map-button--resource";
            _iconClass = "icon-island";
            _currentWorldOffset = 0f;
            _openIslandOnClick = true;
            InvalidateLayout();
            ApplyButtonConfiguration();
        }

        public void Initialize(WorldIslandMapDTO island, ExoticResourceTypeEnum resourceType, int slotIndex)
        {
            _island = island;
            _buttonLabel = string.Empty;
            _buttonTooltip = $"{resourceType.ToString().ToUpperInvariant()} - view tier and investment";
            _buttonClass = "island-map-button--resource";
            _iconClass = GetResourceIconClass(resourceType);
            _currentWorldOffset = _resourceWorldOffset;
            _resourceSlotIndex = slotIndex;
            _openIslandOnClick = false;
            InvalidateLayout();
            ApplyButtonConfiguration();
        }

        private void LateUpdate() => UpdatePosition();

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
            _button.style.width = size.x;
            _button.style.minWidth = size.x;
            _button.style.maxWidth = size.x;
            _button.style.height = size.y;
            _button.style.minHeight = size.y;
            _button.style.maxHeight = size.y;

            float borderRadius = Mathf.Min(size.x, size.y) * 0.5f;
            _button.style.borderTopLeftRadius = borderRadius;
            _button.style.borderTopRightRadius = borderRadius;
            _button.style.borderBottomLeftRadius = borderRadius;
            _button.style.borderBottomRightRadius = borderRadius;
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
