using UnityEngine;
using UnityEngine.UIElements;
using Project.Modules.City;
using Assets.Scripts.Domain.State;
using System;

namespace Project.Modules.UI
{
    /// <summary>
    /// Controller der håndterer den øverste HUD-bar i byen.
    /// Lytter til CityResourceService for at opdatere visningen automatisk via events.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CityTopBarViewController : MonoBehaviour
    {
        private VisualElement _rootVisualElement;

        // Verbøse referencer til tekst-labels
        private Label _woodResourceAmountLabel;
        private Label _stoneResourceAmountLabel;
        private Label _metalResourceAmountLabel;
        private Label _silverResourceAmountLabel;
        private Label _populationAmountLabel;

        // NY REFERENCE: Kort Knappen
        private Button _mapButton;

        // Referencer til bue-tegnere (Painters)
        private WarehouseCapacityProgressPainter _woodWarehousePainter;
        private WarehouseCapacityProgressPainter _stoneWarehousePainter;
        private WarehouseCapacityProgressPainter _metalWarehousePainter;

        private void OnEnable()
        {
            Debug.Log("[CityTopBar] OnEnable: Initialiserer UI.");

            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeResourceLabels();
            InitializeButtons(); // NY INIT METODE
            InitializeWarehouseCapacityPainters();
            ValidateUserInterfaceReferences();
            RegisterButtonCallbacks(); // NY CALLBACK REGISTRERING

            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnResourceStateChanged += HandleResourceStateChanged;
            }
        }

        private void OnDisable()
        {
            UnregisterButtonCallbacks(); // NY CALLBACK AFREGISTRERING

            if (CityResourceService.Instance != null)
            {
                CityResourceService.Instance.OnResourceStateChanged -= HandleResourceStateChanged;
            }
        }

        private void InitializeResourceLabels()
        {
            _woodResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-WoodAmount");
            _stoneResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-StoneAmount");
            _metalResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-MetalAmount");
            _silverResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-SilverAmount");
            _populationAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-PopulationAmount");
        }

        // NY METODE TIL AT FINDE KNAPPER
        private void InitializeButtons()
        {
            _mapButton = _rootVisualElement.Q<Button>("City-TopBar-MapButton");
        }

        private void InitializeWarehouseCapacityPainters()
        {
            _woodWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Wood"));
            _stoneWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Stone"));
            _metalWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Metal"));
        }

        private void ValidateUserInterfaceReferences()
        {
            if (_woodResourceAmountLabel == null) Debug.LogError("[CityTopBar] WoodAmount Label ikke fundet.");
            if (_stoneResourceAmountLabel == null) Debug.LogError("[CityTopBar] StoneAmount Label ikke fundet.");
            if (_metalResourceAmountLabel == null) Debug.LogError("[CityTopBar] MetalAmount Label ikke fundet.");
            if (_silverResourceAmountLabel == null) Debug.LogError("[CityTopBar] SilverAmount Label ikke fundet.");
            if (_populationAmountLabel == null) Debug.LogError("[CityTopBar] Population Label ikke fundet.");
            // NY VALIDERING
            if (_mapButton == null) Debug.LogError("[CityTopBar] MapButton ikke fundet.");
        }

        // NYE METODER TIL EVENTS
        private void RegisterButtonCallbacks()
        {
            _mapButton?.RegisterCallback<ClickEvent>(OnMapButtonClicked);
        }

        private void UnregisterButtonCallbacks()
        {
            _mapButton?.UnregisterCallback<ClickEvent>(OnMapButtonClicked);
        }

        // NY KLIK-HÅNDTERING
        private void OnMapButtonClicked(ClickEvent clickEvent)
        {
            Debug.Log("<color=green>[CityTopBar]</color> Kort-knap klikket! Skifter til verdenskort...");
            // Her vil du typisk kalde en SceneManager for at skifte scene:
            // UnityEngine.SceneManagement.SceneManager.LoadScene("WorldMapScene");
        }


        /// <summary>
        /// Modtager den opdaterede state og sender data videre til UI-logikken.
        /// </summary>
        private void HandleResourceStateChanged(CityResourceState resourceState)
        {
            UpdateUserInterface(
                resourceState.WoodAmount, resourceState.WoodFillPercentage,
                resourceState.StoneAmount, resourceState.StoneFillPercentage,
                resourceState.MetalAmount, resourceState.MetalFillPercentage,
                resourceState.SilverAmount,
                resourceState.CurrentPopulationUsage,
                resourceState.MaxPopulationCapacity
            );
        }

        /// <summary>
        /// Opdaterer de visuelle elementer i HUD'en.
        /// </summary>
        private void UpdateUserInterface(
            double wood, float woodFill,
            double stone, float stoneFill,
            double metal, float metalFill,
            double silver,
            int currentPop, int maxPop)
        {
            // Ressource labels
            if (_woodResourceAmountLabel != null) _woodResourceAmountLabel.text = Math.Floor(wood).ToString("N0");
            if (_stoneResourceAmountLabel != null) _stoneResourceAmountLabel.text = Math.Floor(stone).ToString("N0");
            if (_metalResourceAmountLabel != null) _metalResourceAmountLabel.text = Math.Floor(metal).ToString("N0");
            if (_silverResourceAmountLabel != null) _silverResourceAmountLabel.text = Math.Floor(silver).ToString("N0");

            // Population label
            if (_populationAmountLabel != null)
            {
                _populationAmountLabel.text = $"{currentPop} / {maxPop}";
                bool isHousingFull = currentPop >= maxPop;
                _populationAmountLabel.style.color = isHousingFull ? Color.red : Color.white;
            }

            // Bue-painters
            _woodWarehousePainter?.UpdateFillAmount(woodFill);
            _stoneWarehousePainter?.UpdateFillAmount(stoneFill);
            _metalWarehousePainter?.UpdateFillAmount(metalFill);
        }

        // (Din WarehouseCapacityProgressPainter klasse er uændret herunder...)
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
                Vector2 arcCenterPoint = new Vector2(32f, 32f);
                float arcRadius = 28f;

                painter2D.lineWidth = 3.5f;
                painter2D.lineCap = LineCap.Round;

                painter2D.strokeColor = new Color(0.12f, 0.12f, 0.12f, 0.75f);
                painter2D.BeginPath();
                painter2D.Arc(arcCenterPoint, arcRadius, 135f, 405f, ArcDirection.Clockwise);
                painter2D.Stroke();

                Color progressColor = Color.Lerp(Color.white, Color.red, _currentFillPercentage);
                painter2D.strokeColor = progressColor;

                painter2D.BeginPath();
                float calculateEndAngle = 135f + (270f * _currentFillPercentage);
                painter2D.Arc(arcCenterPoint, arcRadius, 135f, calculateEndAngle, ArcDirection.Clockwise);
                painter2D.Stroke();
            }
        }
    }
}