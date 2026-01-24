using UnityEngine;
using UnityEngine.UIElements;
using Project.Modules.City;
using Assets.Scripts.Domain.State;
using System;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class CityTopBarViewController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

        private Label _woodResourceAmountLabel;
        private Label _stoneResourceAmountLabel;
        private Label _metalResourceAmountLabel;
        private Label _silverResourceAmountLabel;
        private Label _populationAmountLabel;
        private Label _researchAmountLabel;
        private Label _ideologyFocusPointsAmountLabel;

        private Button _mapButton;

        private WarehouseCapacityProgressPainter _woodWarehousePainter;
        private WarehouseCapacityProgressPainter _stoneWarehousePainter;
        private WarehouseCapacityProgressPainter _metalWarehousePainter;

        private void OnEnable()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeUserInterfaceResourceLabels();
            InitializeNavigationButtons();
            InitializeWarehouseCapacityPainters();

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged += HandleResourceStateCalculated;
            }
        }

        private void OnDisable()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged -= HandleResourceStateCalculated;
            }
        }

        private void InitializeUserInterfaceResourceLabels()
        {
            _woodResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-WoodAmount");
            _stoneResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-StoneAmount");
            _metalResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-MetalAmount");
            _silverResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-SilverAmount");
            _populationAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-PopulationAmount");
            _researchAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-ResearchAmount");
            _ideologyFocusPointsAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-IdeologyAmount");
        }

        private void InitializeNavigationButtons()
        {
            _mapButton = _rootVisualElement.Q<Button>("City-TopBar-MapButton");
            if (_mapButton != null)
            {
                _mapButton.clicked += HandleMapNavigationRequested;
            }
        }

        private void InitializeWarehouseCapacityPainters()
        {
            _woodWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Wood"));
            _stoneWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Stone"));
            _metalWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Metal"));
        }

        private void HandleMapNavigationRequested()
        {
            Debug.Log("[CityTopBar] Navigation to the world map requested.");
        }

        private void HandleResourceStateCalculated(CityResourceState currentResourceState)
        {
            UpdateUserInterfaceLabels(currentResourceState);
            UpdateWarehouseVisuals(currentResourceState);
        }

        private void UpdateUserInterfaceLabels(CityResourceState state)
        {
            if (_woodResourceAmountLabel != null)
                _woodResourceAmountLabel.text = Math.Floor(state.WoodAmount).ToString("N0");

            if (_stoneResourceAmountLabel != null)
                _stoneResourceAmountLabel.text = Math.Floor(state.StoneAmount).ToString("N0");

            if (_metalResourceAmountLabel != null)
                _metalResourceAmountLabel.text = Math.Floor(state.MetalAmount).ToString("N0");

            if (_silverResourceAmountLabel != null)
                _silverResourceAmountLabel.text = Math.Floor(state.SilverAmount).ToString("N0");

            if (_researchAmountLabel != null)
                _researchAmountLabel.text = Math.Floor(state.ResearchPointsAmount).ToString("N0");

            if (_ideologyFocusPointsAmountLabel != null)
                _ideologyFocusPointsAmountLabel.text = Math.Floor(state.IdeologyFocusPointsAmount).ToString("N0");

            if (_populationAmountLabel != null)
            {
                int freePopulation = state.MaxPopulationCapacity - state.CurrentPopulationUsage;
                _populationAmountLabel.text = Math.Max(0, freePopulation).ToString("N0");
                _populationAmountLabel.style.color = (freePopulation <= 0) ? Color.red : new Color(0.92f, 0.9f, 0.86f);
            }
        }

        private void UpdateWarehouseVisuals(CityResourceState state)
        {
            _woodWarehousePainter?.UpdateFillAmount(state.WoodFillPercentage);
            _stoneWarehousePainter?.UpdateFillAmount(state.StoneFillPercentage);
            _metalWarehousePainter?.UpdateFillAmount(state.MetalFillPercentage);
        }

        private class WarehouseCapacityProgressPainter
        {
            private readonly VisualElement _targetVisualElement;
            private float _currentFillPercentage;

            public WarehouseCapacityProgressPainter(VisualElement targetElement)
            {
                _targetVisualElement = targetElement;
                if (_targetVisualElement != null)
                    _targetVisualElement.generateVisualContent += OnGenerateVisualContent;
            }

            public void UpdateFillAmount(float percentage)
            {
                _currentFillPercentage = Mathf.Clamp01(percentage);
                _targetVisualElement?.MarkDirtyRepaint();
            }

            private void OnGenerateVisualContent(MeshGenerationContext context)
            {
                var painter2D = context.painter2D;

                // PUNKT 2 FIX: Elementet er 56x56. Center er (28, 28).
                Vector2 arcCenterPoint = new Vector2(24f, 24f);

                // Radius sat til 25f for at give plads til linjetykkelsen på 3.5f indeni de 56px
                float arcRadius = 21f;

                painter2D.lineWidth = 3.2f;
                painter2D.lineCap = LineCap.Round;

                // Baggrundsbue (Semi-transparent mørk)
                painter2D.strokeColor = new Color(0.12f, 0.12f, 0.12f, 0.5f);
                painter2D.BeginPath();
                painter2D.Arc(arcCenterPoint, arcRadius, 135f, 405f, ArcDirection.Clockwise);
                painter2D.Stroke();

                // Fremskridtsbue (Fra hvid/beige til rød)
                Color progressColor = Color.Lerp(new Color(0.9f, 0.9f, 0.85f), Color.red, _currentFillPercentage);
                painter2D.strokeColor = progressColor;

                painter2D.BeginPath();
                float calculateEndAngle = 135f + (270f * _currentFillPercentage);
                painter2D.Arc(arcCenterPoint, arcRadius, 135f, calculateEndAngle, ArcDirection.Clockwise);
                painter2D.Stroke();
            }
        }
    }
}