using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.Map
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldMapCityInteractionLabelController : MonoBehaviour
    {
        private VisualElement _labelContainer;
        private Label _cityNameLabel;
        private Label _cityPointsLabel;
        private Camera _mainCamera;

        [Header("Positionering")]
        [SerializeField] private float _verticalWorldOffset = 1.0f;

        private void OnEnable()
        {
            _mainCamera = Camera.main;
            SetupVisualElements();
        }

        private void SetupVisualElements()
        {
            UIDocument uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            _labelContainer = uiDocument.rootVisualElement.Q<VisualElement>("city-label-container");
            _cityNameLabel = uiDocument.rootVisualElement.Q<Label>("city-name-text");
            _cityPointsLabel = uiDocument.rootVisualElement.Q<Label>("city-points-text");

            uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;

            if (_labelContainer != null)
            {
                _labelContainer.style.position = Position.Absolute;
                _labelContainer.style.left = 0;
                _labelContainer.style.top = 0;
                _labelContainer.style.translate = StyleKeyword.None;
            }
        }

        public void InitializeCityInteractionLabel(string cityName, int cityPoints)
        {
            if (_cityNameLabel == null) SetupVisualElements();
            if (_cityNameLabel != null) _cityNameLabel.text = cityName.ToUpper();
            if (_cityPointsLabel != null) _cityPointsLabel.text = cityPoints.ToString();
            UpdatePosition();
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (_labelContainer == null || _mainCamera == null) return;

            Vector3 worldAnchorPoint = transform.position + new Vector3(0, _verticalWorldOffset, 0);

            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _labelContainer.panel,
                worldAnchorPoint,
                _mainCamera
            );

            float width = _labelContainer.resolvedStyle.width;
            float height = _labelContainer.resolvedStyle.height;

            if (float.IsNaN(width) || width <= 0) return;

            float centerX = panelPos.x - (width * 0.5f);
            float centerY = panelPos.y - (height * 0.5f);

            _labelContainer.transform.position = new Vector2(Mathf.Round(centerX), Mathf.Round(centerY));
        }
    }
}