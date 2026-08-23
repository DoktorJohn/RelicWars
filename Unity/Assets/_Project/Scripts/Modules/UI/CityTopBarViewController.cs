using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Domain.State;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public partial class CityTopBarViewController : MonoBehaviour
    {
        private const string LoginSceneName = "LoginScene";

        [Header("Canvas and responsive roots")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private RectTransform desktopRoot;
        [SerializeField] private RectTransform phoneRoot;
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private Button popupBackdrop;
        [SerializeField] private RectTransform primaryRow;
        [SerializeField] private RectTransform resourceStrip;
        [SerializeField] private GameObject serverTimeSection;

        [Header("Actions")]
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button administrationButton;
        [SerializeField] private Button premiumButton;
        [SerializeField] private GameObject premiumOverviewPrefab;
        [SerializeField] private IconTextSidebarButton logoutButton;

        [Header("Status")]
        [SerializeField] private TMP_Text serverTimeLabel;
        [SerializeField] private CityTopBarResourceView[] resourceViews;

        private readonly Dictionary<CityTopBarResourceType, CityTopBarResourceView> _resourceViewLookup = new();
        private Coroutine _timeUpdateCoroutine;
        private Coroutine _stateManagerBindingCoroutine;
        private List<CityDTO> _playerCities = new();
        private CityStateManager _boundCityStateManager;
        private WorldPlayerStateManager _boundWorldPlayerStateManager;

        private void OnEnable()
        {
            CacheResourceViews();
            BindViewEvents();
            InitializeCitySelector();
            InitializeResourceTooltips();

            ResponsiveUiStateManager.LayoutChanged += ApplyLayout;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            ApplyLayout(ResponsiveUiStateManager.CurrentSnapshot);

            RefreshStateManagerBindings();
            _stateManagerBindingCoroutine = StartCoroutine(MonitorStateManagerBindings());

            if (_timeUpdateCoroutine != null) StopCoroutine(_timeUpdateCoroutine);
            _timeUpdateCoroutine = StartCoroutine(UpdateServerTimeRoutine());
        }

        private IEnumerator MonitorStateManagerBindings()
        {
            var waitInstruction = new WaitForSecondsRealtime(0.25f);
            while (true)
            {
                RefreshStateManagerBindings();
                yield return waitInstruction;
            }
        }

        private void RefreshStateManagerBindings()
        {
            CityStateManager cityStateManager = CityStateManager.Instance;
            if (_boundCityStateManager != cityStateManager)
            {
                if (_boundCityStateManager != null)
                {
                    _boundCityStateManager.OnResourceStateChanged -= HandleCityResourceStateChanged;
                    _boundCityStateManager.OnCityNameChanged -= HandleCityNameChanged;
                }

                _boundCityStateManager = cityStateManager;

                if (_boundCityStateManager != null)
                {
                    _boundCityStateManager.OnResourceStateChanged += HandleCityResourceStateChanged;
                    _boundCityStateManager.OnCityNameChanged += HandleCityNameChanged;
                    UpdateCityUserInterfaceLabels(_boundCityStateManager.CurrentResources);
                    UpdateCapacityIndicators(_boundCityStateManager.CurrentResources);

                    if (!string.IsNullOrEmpty(_boundCityStateManager.CurrentCityName))
                    {
                        UpdateCitySelectorLabel(_boundCityStateManager.CurrentCityName);
                    }
                }
            }

            WorldPlayerStateManager worldPlayerStateManager = WorldPlayerStateManager.Instance;
            if (_boundWorldPlayerStateManager != worldPlayerStateManager)
            {
                if (_boundWorldPlayerStateManager != null)
                {
                    _boundWorldPlayerStateManager.OnEconomyStateChanged -= HandleWorldPlayerEconomyStateChanged;
                }

                _boundWorldPlayerStateManager = worldPlayerStateManager;

                if (_boundWorldPlayerStateManager != null)
                {
                    _boundWorldPlayerStateManager.OnEconomyStateChanged += HandleWorldPlayerEconomyStateChanged;
                    if (_boundWorldPlayerStateManager.CurrentEconomy != null)
                    {
                        HandleWorldPlayerEconomyStateChanged(_boundWorldPlayerStateManager.CurrentEconomy);
                    }
                }
            }
        }

        private void OnDisable()
        {
            ResponsiveUiStateManager.LayoutChanged -= ApplyLayout;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            if (_boundCityStateManager != null)
            {
                _boundCityStateManager.OnResourceStateChanged -= HandleCityResourceStateChanged;
                _boundCityStateManager.OnCityNameChanged -= HandleCityNameChanged;
                _boundCityStateManager = null;
            }

            if (_boundWorldPlayerStateManager != null)
            {
                _boundWorldPlayerStateManager.OnEconomyStateChanged -= HandleWorldPlayerEconomyStateChanged;
                _boundWorldPlayerStateManager = null;
            }

            UnbindViewEvents();
            CleanupCitySelector();
            CleanupResourceTooltips();
            CloseAllPopups();

            if (_stateManagerBindingCoroutine != null)
            {
                StopCoroutine(_stateManagerBindingCoroutine);
                _stateManagerBindingCoroutine = null;
            }

            if (_timeUpdateCoroutine != null)
            {
                StopCoroutine(_timeUpdateCoroutine);
                _timeUpdateCoroutine = null;
            }
        }

        private void CacheResourceViews()
        {
            _resourceViewLookup.Clear();
            if (resourceViews == null) return;

            foreach (CityTopBarResourceView view in resourceViews)
            {
                if (view != null) _resourceViewLookup[view.ResourceType] = view;
            }
        }

        private void BindViewEvents()
        {
            if (administrationButton != null) administrationButton.onClick.AddListener(HandleAdministrationRequested);
            if (premiumButton != null) premiumButton.onClick.AddListener(HandlePremiumRequested);
            if (logoutButton != null) logoutButton.OnButtonActivatedClicked += HandleLogoutRequested;
            if (popupBackdrop != null) popupBackdrop.onClick.AddListener(CloseAllPopups);
        }

        private void UnbindViewEvents()
        {
            if (administrationButton != null) administrationButton.onClick.RemoveListener(HandleAdministrationRequested);
            if (premiumButton != null) premiumButton.onClick.RemoveListener(HandlePremiumRequested);
            if (logoutButton != null) logoutButton.OnButtonActivatedClicked -= HandleLogoutRequested;
            if (popupBackdrop != null) popupBackdrop.onClick.RemoveListener(CloseAllPopups);
        }

        private void HandleAdministrationRequested()
        {
            CloseAllPopups();
            GlobalWindowManager.Instance?.OpenWindow(Assets.Scripts.Domain.Enums.WindowTypeEnum.Administration);
        }

        private void HandlePremiumRequested()
        {
            CloseAllPopups();
            UguiWindowHostController.Instance?.OpenWindow(
                Assets.Scripts.Domain.Enums.WindowTypeEnum.PremiumOverview,
                premiumOverviewPrefab);
        }

        private void HandleLogoutRequested(IconTextSidebarButton _)
        {
            CloseAllPopups();
            CityStateManager.Instance?.ResetForLogout();
            WorldPlayerStateManager.Instance?.ResetForLogout();
            WorldMapStateManager.Instance?.ResetForLogout();
            GlobalWindowManager.Instance?.CloseAllWindows();
            NetworkManager.Instance?.ClearSession();
            SceneManager.LoadScene(LoginSceneName);
        }

        private void HandleCityResourceStateChanged(CityResourceState state)
        {
            UpdateCityUserInterfaceLabels(state);
            UpdateCapacityIndicators(state);
            RefreshExoticResourcesSection();
            RefreshVisibleResourceTooltip();
        }

        private void HandleCityNameChanged(string cityName) => UpdateCitySelectorLabel(cityName);

        private void HandleWorldPlayerEconomyStateChanged(WorldPlayerState state)
        {
            UpdateWorldPlayerUserInterfaceLabels(state);
            RefreshVisibleResourceTooltip();

            if (state.PlayerCities == null) return;

            bool cityListChanged = !ReferenceEquals(_playerCities, state.PlayerCities);
            _playerCities = state.PlayerCities;
            if (cityListChanged) PopulateCitySelectorPopup();
            UpdateNavigationButtonsState();
        }

        private void UpdateCityUserInterfaceLabels(CityResourceState state)
        {
            SetResourceAmount(CityTopBarResourceType.Wood, Math.Floor(state.WoodAmount).ToString("N0"));
            SetResourceProduction(CityTopBarResourceType.Wood, state.WoodProductionPerHour);
            SetResourceAmount(CityTopBarResourceType.Stone, Math.Floor(state.StoneAmount).ToString("N0"));
            SetResourceProduction(CityTopBarResourceType.Stone, state.StoneProductionPerHour);
            SetResourceAmount(CityTopBarResourceType.Metal, Math.Floor(state.MetalAmount).ToString("N0"));
            SetResourceProduction(CityTopBarResourceType.Metal, state.MetalProductionPerHour);
            SetResourceAmount(CityTopBarResourceType.Population, state.RemainingPopulation.ToString("N0"), state.RemainingPopulation <= 0);
            SetResourceDetail(
                CityTopBarResourceType.Population,
                $"{state.CurrentPopulationUsage:N0} / {state.MaxPopulationCapacity:N0}",
                state.RemainingPopulation <= 0);
        }

        private void UpdateWorldPlayerUserInterfaceLabels(WorldPlayerState state)
        {
            SetResourceAmount(CityTopBarResourceType.Coins, Math.Floor(state.CoinsAmount).ToString("N0"));
            SetResourceProduction(CityTopBarResourceType.Coins, state.CoinsProductionPerHour);
            SetResourceAmount(CityTopBarResourceType.Research, state.EffectiveResearchPower.ToString("F2"));
            SetResourceDetail(CityTopBarResourceType.Research, $"{state.ResearchSpeedMultiplier:F2}x");
            SetResourceAmount(CityTopBarResourceType.Ideology, Math.Floor(state.IdeologyFocusPointsAmount).ToString("N0"));
            SetResourceProduction(CityTopBarResourceType.Ideology, state.IdeologyFocusPointsProductionPerHour);
        }

        private void SetResourceAmount(CityTopBarResourceType type, string value, bool isNegative = false)
        {
            if (_resourceViewLookup.TryGetValue(type, out CityTopBarResourceView view))
            {
                view.SetAmount(value, isNegative);
            }
        }

        private void SetResourceProduction(CityTopBarResourceType type, double productionPerHour)
        {
            if (_resourceViewLookup.TryGetValue(type, out CityTopBarResourceView view))
            {
                view.SetProduction(productionPerHour);
            }
        }

        private void SetResourceDetail(CityTopBarResourceType type, string value, bool isNegative = false)
        {
            if (_resourceViewLookup.TryGetValue(type, out CityTopBarResourceView view))
            {
                view.SetDetail(value, isNegative);
            }
        }

        private void UpdateCapacityIndicators(CityResourceState state)
        {
            SetResourceFill(CityTopBarResourceType.Wood, state.WoodFillPercentage);
            SetResourceFill(CityTopBarResourceType.Stone, state.StoneFillPercentage);
            SetResourceFill(CityTopBarResourceType.Metal, state.MetalFillPercentage);
            SetResourceFill(
                CityTopBarResourceType.Population,
                state.MaxPopulationCapacity > 0 ? (float)state.CurrentPopulationUsage / state.MaxPopulationCapacity : 0f);
        }

        private void SetResourceFill(CityTopBarResourceType type, float fill)
        {
            if (_resourceViewLookup.TryGetValue(type, out CityTopBarResourceView view))
            {
                view.SetCapacityFill(fill);
            }
        }

        private IEnumerator UpdateServerTimeRoutine()
        {
            var waitInstruction = new WaitForSecondsRealtime(1f);
            while (true)
            {
                if (serverTimeLabel != null) serverTimeLabel.text = DateTime.UtcNow.ToString("HH:mm:ss");
                yield return waitInstruction;
            }
        }

        private void ApplyLayout(FrontendLayoutSnapshot snapshot)
        {
            // The authored Top Section prefab owns all layout and scaling.
        }

        private void HandleActiveSceneChanged(Scene previous, Scene next) => CloseAllPopups();

        private void CloseAllPopups()
        {
            HideCitySelectorPopup();
            HideResourceTooltips();
            if (popupBackdrop != null) popupBackdrop.gameObject.SetActive(false);
        }

        private void SetBackdropVisible(bool visible)
        {
            if (popupBackdrop != null) popupBackdrop.gameObject.SetActive(visible);
        }
    }
}
