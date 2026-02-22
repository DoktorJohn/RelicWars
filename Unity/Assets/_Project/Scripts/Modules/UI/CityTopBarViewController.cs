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

        private WarehouseCapacityProgressPainter _woodWarehousePainter;
        private WarehouseCapacityProgressPainter _stoneWarehousePainter;
        private WarehouseCapacityProgressPainter _metalWarehousePainter;
        private WarehouseCapacityProgressPainter _populationUsagePainter;
        private WarehouseCapacityProgressPainter _ideologyPainter;

        // Coroutine reference
        private Coroutine _timeUpdateCoroutine;

        // Data
        private List<CityDTO> _playerCities = new List<CityDTO>();
        private bool _isFetchingCities = false;

        private void OnEnable()
        {
            Debug.Log("[CityTopBar_DEBUG] OnEnable started.");
            var uiDocumentComponent = GetComponent<UIDocument>();
            if (uiDocumentComponent == null) return;

            _rootVisualElement = uiDocumentComponent.rootVisualElement;

            InitializeUserInterfaceResourceLabels();
            InitializeCitySelector();
            InitializeNavigationButtons();
            InitializeWarehouseCapacityPainters();

            if (CityStateManager.Instance != null)
            {
                Debug.Log($"[CityTopBar] OnEnable - CityStateManager found. Current City: {CityStateManager.Instance.CurrentCityName}");
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
                
                // Initial update from state if available
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

            // Start Uret
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

            if (_timeUpdateCoroutine != null) StopCoroutine(_timeUpdateCoroutine);
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

            _serverTimeLabel = _rootVisualElement.Q<Label>("City-ServerTime-Label");
        }

        private void InitializeCitySelector()
        {
            _citySelectorSection = _rootVisualElement.Q<VisualElement>("City-Selector-Section");
            _citySelectorCurrentCityLabel = _rootVisualElement.Q<Label>("City-Selector-CurrentCity-Label");
            _citySelectorRenameInput = _rootVisualElement.Q<TextField>("City-Selector-Rename-Input");
            _citySelectorDropdownContainer = _rootVisualElement.Q<VisualElement>("City-Selector-Dropdown-Container");
            _citySelectorDropdownScroll = _rootVisualElement.Q<ScrollView>("City-Selector-Dropdown-Scroll");

            _previousCityButton = _rootVisualElement.Q<Button>("City-Selector-Arrow-Left");
            _nextCityButton = _rootVisualElement.Q<Button>("City-Selector-Arrow-Right");

            if (_citySelectorCurrentCityLabel != null)
            {
                _citySelectorCurrentCityLabel.RegisterCallback<ClickEvent>(OnCityLabelClicked);
            }

            if (_citySelectorRenameInput != null)
            {
                _citySelectorRenameInput.RegisterCallback<FocusOutEvent>(OnRenameInputFocusOut);
                _citySelectorRenameInput.RegisterCallback<KeyDownEvent>(OnRenameInputKeyDown);
            }

            if (_previousCityButton != null) _previousCityButton.clicked += OnPreviousCityClicked;
            if (_nextCityButton != null) _nextCityButton.clicked += OnNextCityClicked;
        }

        private void InitializeNavigationButtons()
        {
            _navigationButton = _rootVisualElement.Q<Button>("City-TopBar-MapButton");
            if (_navigationButton != null)
            {
                _navigationButton.clicked -= HandleContextualNavigationRequested;
                _navigationButton.clicked += HandleContextualNavigationRequested;
            }
        }

        private void InitializeWarehouseCapacityPainters()
        {
            _woodWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Wood"));
            _stoneWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Stone"));
            _metalWarehousePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Metal"));
            _populationUsagePainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Population"));
            _ideologyPainter = new WarehouseCapacityProgressPainter(_rootVisualElement.Q<VisualElement>("City-WarehouseBar-Ideology"));
        }

        private IEnumerator FetchPlayerCities(Guid currentCityId)
        {
            if (NetworkManager.Instance == null) yield break;

            yield return NetworkManager.Instance.City.GetPlayerCities(currentCityId, NetworkManager.Instance.JwtToken, (cities) =>
            {
                if (cities != null)
                {
                    _playerCities = cities;
                    PopulateCityDropdown();
                    UpdateCitySelectorLabel(currentCityId.ToString());
                }
            });
        }

        private void PopulateCityDropdown()
        {
            if (_citySelectorDropdownScroll == null) return;

            _citySelectorDropdownScroll.Clear();

            foreach (var city in _playerCities)
            {
                var cityLabel = new Label(city.CityName);
                cityLabel.AddToClassList("city-selector-item");
                cityLabel.RegisterCallback<ClickEvent>(evt => OnCityDropdownItemClicked(city));
                _citySelectorDropdownScroll.Add(cityLabel);
            }
        }

        private void UpdateCitySelectorLabel(string cityName)
        {
            if (_citySelectorCurrentCityLabel == null) return;
            
            if (!string.IsNullOrEmpty(cityName))
            {
                _citySelectorCurrentCityLabel.text = cityName;
            }
        }

        // --- Interaction Handlers ---

        private void OnCityLabelClicked(ClickEvent evt)
        {
            if (evt.clickCount == 2)
            {
                // Double click -> Rename
                EnableRenameMode();
            }
            else
            {
                // Single click -> Toggle Dropdown
                ToggleDropdown();
            }
        }

        private void ToggleDropdown()
        {
            if (_citySelectorDropdownContainer == null) return;

            bool isVisible = _citySelectorDropdownContainer.style.display == DisplayStyle.Flex;
            _citySelectorDropdownContainer.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnCityDropdownItemClicked(CityDTO city)
        {
            Debug.Log($"[CityTopBar] Player selected city: {city.CityName} ({city.Id}). Switching state is ignored for now.");
            // Hide dropdown
            if (_citySelectorDropdownContainer != null)
                _citySelectorDropdownContainer.style.display = DisplayStyle.None;
        }

        private void EnableRenameMode()
        {
            if (_citySelectorCurrentCityLabel == null || _citySelectorRenameInput == null) return;

            _citySelectorCurrentCityLabel.style.display = DisplayStyle.None;
            _citySelectorDropdownContainer.style.display = DisplayStyle.None; // Ensure dropdown is closed

            _citySelectorRenameInput.style.display = DisplayStyle.Flex;
            _citySelectorRenameInput.value = _citySelectorCurrentCityLabel.text;
            _citySelectorRenameInput.Focus();
        }

        private void OnRenameInputFocusOut(FocusOutEvent evt)
        {
            CommitRename();
        }

        private void OnRenameInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                CommitRename();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                CancelRename();
            }
        }

        private void CancelRename()
        {
             if (_citySelectorCurrentCityLabel == null || _citySelectorRenameInput == null) return;

            _citySelectorRenameInput.style.display = DisplayStyle.None;
            _citySelectorCurrentCityLabel.style.display = DisplayStyle.Flex;
        }

        private void CommitRename()
        {
            if (_citySelectorRenameInput == null || _citySelectorCurrentCityLabel == null) return;
            
            // Avoid double commit (e.g. enter then focus out) if already hidden
            if (_citySelectorRenameInput.style.display == DisplayStyle.None) return;

            string newName = _citySelectorRenameInput.value;
            
            if (string.IsNullOrWhiteSpace(newName) || newName.Length < 3)
            {
                Debug.LogWarning("[CityTopBar] Name too short.");
                CancelRename(); // Or keep focus? For now just cancel.
                return;
            }

            Guid currentCityId = CityStateManager.Instance.CityId;
            if (currentCityId == Guid.Empty)
            {
                CancelRename();
                return;
            }

            StartCoroutine(NetworkManager.Instance.City.ChangeCityName(currentCityId, newName, NetworkManager.Instance.JwtToken, (response) =>
            {
                if (response.Success)
                {
                    _citySelectorCurrentCityLabel.text = response.CityName;
                    
                    // Update local list
                    var cityInList = _playerCities.Find(c => c.Id == response.CityId);
                    if (cityInList != null) cityInList.CityName = response.CityName;
                    PopulateCityDropdown(); // Refresh list names
                }
                else
                {
                    Debug.LogError($"[CityTopBar] Rename failed: {response.Message}");
                    // Optionally show error to user
                }
                CancelRename(); // Go back to label mode
            }));
        }

        private void OnPreviousCityClicked()
        {
            if (_playerCities == null || _playerCities.Count <= 1) return;

            var currentCityId = CityStateManager.Instance.CityId;
            var currentIndex = _playerCities.FindIndex(c => c.Id == currentCityId);

            if (currentIndex == -1) currentIndex = 0; // Default if not found

            // Cycle backwards
            int newIndex = currentIndex - 1;
            if (newIndex < 0) newIndex = _playerCities.Count - 1;

            var newCity = _playerCities[newIndex];
            OnCityDropdownItemClicked(newCity);
            UpdateCitySelectorLabel(newCity.CityName);
        }

        private void OnNextCityClicked()
        {
            if (_playerCities == null || _playerCities.Count <= 1) return;

            var currentCityId = CityStateManager.Instance.CityId;
            var currentIndex = _playerCities.FindIndex(c => c.Id == currentCityId);

            if (currentIndex == -1) currentIndex = 0; // Default if not found

            // Cycle forwards
            int newIndex = currentIndex + 1;
            if (newIndex >= _playerCities.Count) newIndex = 0;

            var newCity = _playerCities[newIndex];
            OnCityDropdownItemClicked(newCity);
            UpdateCitySelectorLabel(newCity.CityName);
        }

        private void UpdateNavigationButtonsState()
        {
            bool hasMultipleCities = _playerCities != null && _playerCities.Count > 1;
            
            if (_previousCityButton != null) _previousCityButton.SetEnabled(hasMultipleCities);
            if (_nextCityButton != null) _nextCityButton.SetEnabled(hasMultipleCities);
        }

        // --- Standard Logic ---

        private void HandleContextualNavigationRequested()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "WorldMapScene")
            {
                Debug.Log("[CityTopBar] Navigation back to City requested.");
                SceneManager.LoadScene("CityViewScene");
            }
            else
            {
                Debug.Log("[CityTopBar] Navigation to World Map requested.");
                SceneManager.LoadScene("WorldMapScene");
            }
        }

        private void HandleCityResourceStateChanged(CityResourceState currentResourceState)
        {
            UpdateCityUserInterfaceLabels(currentResourceState);
            UpdateWarehouseVisuals(currentResourceState);
        }

        private void HandleCityNameChanged(string newCityName)
        {
            UpdateCitySelectorLabel(newCityName);
        }

        private void HandleWorldPlayerEconomyStateChanged(WorldPlayerState economyState)
        {
            UpdateWorldPlayerUserInterfaceLabels(economyState);

            if (economyState.PlayerCities != null)
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
            if (_silverResourceAmountLabel != null)
                _silverResourceAmountLabel.text = Math.Floor(state.SilverAmount).ToString("N0");

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