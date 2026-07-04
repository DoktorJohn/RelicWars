using Project.Modules.WorldPlayer;
using Assets.Scripts.Domain.State;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Modules.City;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI
{
    [RequireComponent(typeof(UIDocument))]
    public partial class CityTopBarViewController : MonoBehaviour
    {
        private const string WorldMapSceneName = "WorldMapScene";
        private const string CityViewSceneName = "CityViewScene";
        private const string LoginSceneName = "LoginScene";

        private VisualElement _rootVisualElement;

        private Label _woodResourceAmountLabel;
        private Label _stoneResourceAmountLabel;
        private Label _metalResourceAmountLabel;
        private Label _coinsResourceAmountLabel;
        private Label _populationAmountLabel;
        private Label _researchAmountLabel;
        private Label _ideologyFocusPointsAmountLabel;
        private Label _exoticResourcesTotalLabel;
        private VisualElement _exoticResourcesTrigger;
        private VisualElement _exoticResourcesTooltip;
        private VisualElement _exoticResourcesTooltipGrid;
        private Label _exoticResourcesTooltipTitle;

        // Server Time Label
        private Label _serverTimeLabel;

        // City Selector Elements
        private VisualElement _citySelectorSection;
        private Label _citySelectorCurrentCityLabel;
        private TextField _citySelectorRenameInput;
        private VisualElement _citySelectorDropdownContainer;
        private ScrollView _citySelectorDropdownScroll;

        private Button _previousCityButton;
        private Button _nextCityButton;

        private Button _navigationButton;
        private Button _logoutButton;

        private WarehouseCapacityProgressPainter _woodWarehousePainter;
        private WarehouseCapacityProgressPainter _stoneWarehousePainter;
        private WarehouseCapacityProgressPainter _metalWarehousePainter;
        private WarehouseCapacityProgressPainter _populationUsagePainter;
        private WarehouseCapacityProgressPainter _ideologyPainter;

        // Coroutine reference
        private Coroutine _timeUpdateCoroutine;

        // Data
        private List<CityDTO> _playerCities = new List<CityDTO>();

        private void OnEnable()
        {
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeUserInterfaceResourceLabels();
            InitializeCitySelector();
            InitializeNavigationButtons();
            InitializeLogoutButton();
            InitializeWarehouseCapacityPainters();
            InitializeExoticResourcesSection();
            
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged += HandleCityResourceStateChanged;
                CityStateManager.Instance.OnCityNameChanged += HandleCityNameChanged;

                UpdateCityUserInterfaceLabels(CityStateManager.Instance.CurrentResources);
                UpdateWarehouseVisuals(CityStateManager.Instance.CurrentResources);
                
                if (!string.IsNullOrEmpty(CityStateManager.Instance.CurrentCityName))
                {
                    UpdateCitySelectorLabel(CityStateManager.Instance.CurrentCityName);
                }
            }

            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.OnEconomyStateChanged += HandleWorldPlayerEconomyStateChanged;
                
                if (WorldPlayerStateManager.Instance.CurrentEconomy != null)
                {
                    UpdateWorldPlayerUserInterfaceLabels(WorldPlayerStateManager.Instance.CurrentEconomy);
                    if (WorldPlayerStateManager.Instance.CurrentEconomy.PlayerCities != null)
                    {
                        _playerCities = WorldPlayerStateManager.Instance.CurrentEconomy.PlayerCities;
                        PopulateCityDropdown();
                        UpdateNavigationButtonsState();
                    }
                }
            }

            if (_timeUpdateCoroutine != null) StopCoroutine(_timeUpdateCoroutine);
            _timeUpdateCoroutine = StartCoroutine(UpdateServerTimeRoutine());
        }

        private void OnDisable()
        {
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged -= HandleCityResourceStateChanged;
                CityStateManager.Instance.OnCityNameChanged -= HandleCityNameChanged;
            }

            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.OnEconomyStateChanged -= HandleWorldPlayerEconomyStateChanged;
            }

            HideExoticResourceTooltip();
            CleanupExoticResourcesSection();

            if (_logoutButton != null)
            {
                _logoutButton.clicked -= HandleLogoutRequested;
            }

            if (_timeUpdateCoroutine != null) StopCoroutine(_timeUpdateCoroutine);
        }

        private void InitializeLogoutButton()
        {
            _logoutButton = _rootVisualElement.Q<Button>("City-TopBar-LogoutButton");
            if (_logoutButton == null)
            {
                return;
            }

            _logoutButton.clicked -= HandleLogoutRequested;
            _logoutButton.clicked += HandleLogoutRequested;
        }

        private void HandleLogoutRequested()
        {
            CityStateManager.Instance?.ResetForLogout();
            WorldPlayerStateManager.Instance?.ResetForLogout();
            WorldMapStateManager.Instance?.ResetForLogout();
            GlobalWindowManager.Instance?.CloseAllWindows();
            NetworkManager.Instance?.ClearSession();
            SceneManager.LoadScene(LoginSceneName);
        }

        private void InitializeUserInterfaceResourceLabels()
        {
            _woodResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-WoodAmount");
            _stoneResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-StoneAmount");
            _metalResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-MetalAmount");
            _coinsResourceAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-CoinsAmount");
            _populationAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-PopulationAmount");
            _researchAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-ResearchAmount");
            _ideologyFocusPointsAmountLabel = _rootVisualElement.Q<Label>("City-ResourceLabel-IdeologyAmount");

            _serverTimeLabel = _rootVisualElement.Q<Label>("City-ServerTime-Label");
        }

        private void InitializeWarehouseCapacityPainters()
        {
            _woodWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Wood"));
            _stoneWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Stone"));
            _metalWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Metal"));
            _populationUsagePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Population"));
            _ideologyPainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Ideology"));
        }

        private void HandleCityResourceStateChanged(CityResourceState currentResourceState)
        {
            UpdateCityUserInterfaceLabels(currentResourceState);
            UpdateWarehouseVisuals(currentResourceState);
            RefreshExoticResourcesSection();
        }

        private void HandleCityNameChanged(string newCityName)
        {
            UpdateCitySelectorLabel(newCityName);
        }

        private void HandleWorldPlayerEconomyStateChanged(WorldPlayerState economyState)
        {
            UpdateWorldPlayerUserInterfaceLabels(economyState);

            if (economyState.PlayerCities != null && !ReferenceEquals(_playerCities, economyState.PlayerCities))
            {
                _playerCities = economyState.PlayerCities;
                PopulateCityDropdown();
                UpdateNavigationButtonsState();
            }
        }

        private void UpdateCityUserInterfaceLabels(CityResourceState state)
        {
            if (_woodResourceAmountLabel != null)
                _woodResourceAmountLabel.text = Math.Floor(state.WoodAmount).ToString("N0");

            if (_stoneResourceAmountLabel != null)
                _stoneResourceAmountLabel.text = Math.Floor(state.StoneAmount).ToString("N0");

            if (_metalResourceAmountLabel != null)
                _metalResourceAmountLabel.text = Math.Floor(state.MetalAmount).ToString("N0");

            if (_populationAmountLabel != null)
            {
                int freePopulation = state.MaxPopulationCapacity - state.CurrentPopulationUsage;
                _populationAmountLabel.text = Math.Max(0, freePopulation).ToString("N0");
                _populationAmountLabel.style.color = (freePopulation <= 0) ? Color.red : new Color(0.92f, 0.9f, 0.86f);
            }
        }

        private void UpdateWorldPlayerUserInterfaceLabels(WorldPlayerState state)
        {
            if (_coinsResourceAmountLabel != null)
                _coinsResourceAmountLabel.text = Math.Floor(state.CoinsAmount).ToString("N0");

            if (_researchAmountLabel != null)
                _researchAmountLabel.text = Math.Floor(state.ResearchPointsAmount).ToString("N0");

            if (_ideologyFocusPointsAmountLabel != null)
                _ideologyFocusPointsAmountLabel.text = Math.Floor(state.IdeologyFocusPointsAmount).ToString("N0");
        }

        private void UpdateWarehouseVisuals(CityResourceState state)
        {
            _woodWarehousePainter?.UpdateFillAmount(state.WoodFillPercentage);
            _stoneWarehousePainter?.UpdateFillAmount(state.StoneFillPercentage);
            _metalWarehousePainter?.UpdateFillAmount(state.MetalFillPercentage);

            float populationFill = state.MaxPopulationCapacity > 0
                ? (float)state.CurrentPopulationUsage / state.MaxPopulationCapacity
                : 0f;
            _populationUsagePainter?.UpdateFillAmount(populationFill);

            _ideologyPainter?.UpdateFillAmount(0f);
        }

        // ===============================================
        // SERVER TIME ROUTINE
        // ===============================================
        private IEnumerator UpdateServerTimeRoutine()
        {
            var waitInstruction = new WaitForSeconds(1f);
            while (true)
            {
                if (_serverTimeLabel != null)
                {
                    // Formatterer tiden som DD.MM.YYYY HH:MM:SS
                    _serverTimeLabel.text = DateTime.UtcNow.ToString("HH:mm:ss");
                }
                yield return waitInstruction;
            }
        }

        private class WarehouseCapacityProgressPainter
        {
            private readonly VisualElement _targetVisualElement;
            private float _currentFillPercentage;

            private readonly Color _colorBaseBeige = new Color(0.9f, 0.9f, 0.85f);
            private readonly Color _colorWarningGold = new Color(1.0f, 0.8f, 0.2f);
            private readonly Color _colorDangerRed = Color.red;

            private const float _dangerThresholdPercentage = 0.95f;

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
                Vector2 arcCenterPoint = new Vector2(24f, 24f);
                float arcRadius = 21f;

                painter2D.lineWidth = 3.2f;
                painter2D.lineCap = LineCap.Round;

                painter2D.strokeColor = new Color(0.12f, 0.12f, 0.12f, 0.5f);
                painter2D.BeginPath();
                painter2D.Arc(arcCenterPoint, arcRadius, 135f, 405f, ArcDirection.Clockwise);
                painter2D.Stroke();

                Color progressStrokeColor;
                if (_currentFillPercentage < _dangerThresholdPercentage)
                {
                    float normalizedGoldStep = _currentFillPercentage / _dangerThresholdPercentage;
                    progressStrokeColor = Color.Lerp(_colorBaseBeige, _colorWarningGold, normalizedGoldStep);
                }
                else
                {
                    float normalizedRedStep = (_currentFillPercentage - _dangerThresholdPercentage) / (1.0f - _dangerThresholdPercentage);
                    progressStrokeColor = Color.Lerp(_colorWarningGold, _colorDangerRed, normalizedRedStep);
                }

                painter2D.strokeColor = progressStrokeColor;
                painter2D.BeginPath();
                float calculateEndAngle = 135f + (270f * _currentFillPercentage);
                painter2D.Arc(arcCenterPoint, arcRadius, 135f, calculateEndAngle, ArcDirection.Clockwise);
                painter2D.Stroke();
            }
        }
    }
}
