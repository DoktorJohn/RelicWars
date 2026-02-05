using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WorldMapUnitDeploymentLabelController : MonoBehaviour
{
    private VisualElement _labelContainer;
    private Label _unitOwnerLabel;
    private Label _unitStrengthLabel;
    private Camera _mainCamera;

    [Header("Positionering")]
    [SerializeField] private float _verticalWorldOffset = 1.2f;

    private void OnEnable()
    {
        _mainCamera = Camera.main;
        SetupVisualElements();
    }

    private void SetupVisualElements()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;

        _labelContainer = uiDocument.rootVisualElement.Q<VisualElement>("unit-label-container");
        _unitOwnerLabel = uiDocument.rootVisualElement.Q<Label>("unit-owner-text");
        _unitStrengthLabel = uiDocument.rootVisualElement.Q<Label>("unit-strength-text");

        uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;

        if (_labelContainer != null)
        {
            _labelContainer.style.position = Position.Absolute;
            _labelContainer.style.left = 0;
            _labelContainer.style.top = 20;
            // Vi fjerner translate herfra og gør det manuelt i matematikken for at sikre 100% centrering
            _labelContainer.style.translate = StyleKeyword.None;
        }
    }

    public void InitializeUnitDeploymentLabel(string ownerName, int totalUnitCount)
    {
        if (_unitOwnerLabel == null) SetupVisualElements();
        if (_unitOwnerLabel != null) _unitOwnerLabel.text = ownerName.ToUpper();
        if (_unitStrengthLabel != null) _unitStrengthLabel.text = totalUnitCount.ToString();
        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_labelContainer == null || _mainCamera == null) return;

        // 1. Find punktet i VERDEN. Ved at lægge offset til her, følger det med zoomet perfekt.
        Vector3 worldAnchorPoint = transform.position + new Vector3(0, _verticalWorldOffset, 0);

        // 2. Konverter til Panel-pixels (1:1 med skærm-pixels i dit setup)
        Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
            _labelContainer.panel,
            worldAnchorPoint,
            _mainCamera
        );

        // 3. MANUEL CENTRERING
        // Vi tager panel-positionen og trækker præcis halvdelen af labellens bredde/højde fra.
        // resolvedStyle sikrer at vi bruger den faktiske størrelse efter USS er anvendt.
        float width = _labelContainer.resolvedStyle.width;
        float height = _labelContainer.resolvedStyle.height;

        // Undgå NaN fejl hvis layout ikke er klar
        if (float.IsNaN(width) || width <= 0) return;

        float centerX = panelPos.x - (width * 0.5f);
        float centerY = panelPos.y - (height * 0.5f);

        // 4. Sæt positionen (GPU-baseret transform fjerner jitter)
        _labelContainer.transform.position = new Vector2(Mathf.Round(centerX), Mathf.Round(centerY));
    }
}