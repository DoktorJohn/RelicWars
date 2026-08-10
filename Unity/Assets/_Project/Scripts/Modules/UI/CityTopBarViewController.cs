using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const string WorldMapSceneName = "WorldMapScene";
        private const string CityViewSceneName = "CityViewScene";
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
        [SerializeField] private IconTextSidebarButton mapButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button administrationButton;
        [SerializeField] private IconTextSidebarButton logoutButton;

        [Header("Status")]
        [SerializeField] private TMP_Text serverTimeLabel;
        [SerializeField] private CityTopBarResourceView[] resourceViews;

        private readonly Dictionary<CityTopBarResourceType, CityTopBarResourceView> _resourceViewLookup = new();
        private Coroutine _timeUpdateCoroutine;
        private List<CityDTO> _playerCities = new();

        private void Awake()
        {
            BindAuthoredTopSection();
        }

        private void OnEnable()
        {
            CacheResourceViews();
            BindViewEvents();
            InitializeCitySelector();
            InitializeResourceTooltips();

            ResponsiveUiStateManager.LayoutChanged += ApplyLayout;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            ApplyLayout(ResponsiveUiStateManager.CurrentSnapshot);

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged += HandleCityResourceStateChanged;
                CityStateManager.Instance.OnCityNameChanged += HandleCityNameChanged;
                UpdateCityUserInterfaceLabels(CityStateManager.Instance.CurrentResources);
                UpdateCapacityIndicators(CityStateManager.Instance.CurrentResources);

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
                    HandleWorldPlayerEconomyStateChanged(WorldPlayerStateManager.Instance.CurrentEconomy);
                }
            }

            if (_timeUpdateCoroutine != null) StopCoroutine(_timeUpdateCoroutine);
            _timeUpdateCoroutine = StartCoroutine(UpdateServerTimeRoutine());
        }

        private void BindAuthoredTopSection()
        {
            canvas ??= GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                    .FirstOrDefault();
            }

            if (canvas == null) return;

            RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
            desktopRoot ??= rects.FirstOrDefault(rect => rect.name == "Top Section");
            safeAreaRoot ??= canvas != null ? canvas.transform as RectTransform : null;
            primaryRow ??= desktopRoot;
            resourceStrip ??= rects.FirstOrDefault(rect => rect.name == "Horizontal Box");
            mapButton ??= canvas.GetComponentsInChildren<IconTextSidebarButton>(true)
                .FirstOrDefault(button => button.name == "Worldmap");
            logoutButton ??= canvas.GetComponentsInChildren<IconTextSidebarButton>(true)
                .FirstOrDefault(button => button.name == "Logout");

            if (serverTimeLabel == null)
            {
                serverTimeLabel = GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(label => label.name == "Turn TMP");
            }

            if (resourceViews == null || resourceViews.Length == 0)
            {
                BindAuthoredResourceViews(rects);
            }
        }

        private void BindAuthoredResourceViews(IEnumerable<RectTransform> rects)
        {
            Dictionary<string, CityTopBarResourceType> types = new(StringComparer.Ordinal)
            {
                ["Gold coins"] = CityTopBarResourceType.Coins,
                ["Population"] = CityTopBarResourceType.Population,
                ["Wood"] = CityTopBarResourceType.Wood,
                ["Stone"] = CityTopBarResourceType.Stone,
                ["Metal"] = CityTopBarResourceType.Metal,
                ["Research"] = CityTopBarResourceType.Research,
                ["Focus"] = CityTopBarResourceType.Ideology,
                ["Exotic resources"] = CityTopBarResourceType.Exotic
            };

            RectTransform[] fields = rects
                .Where(rect => types.ContainsKey(rect.name))
                .OrderBy(rect => rect.GetSiblingIndex())
                .ToArray();

            resourceViews = fields.Select(field =>
            {
                CityTopBarResourceView view = field.GetComponent<CityTopBarResourceView>()
                    ?? field.gameObject.AddComponent<CityTopBarResourceView>();
                Image icon = field.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(image => image.name == "Icon");
                TMP_Text[] labels = field.GetComponentsInChildren<TMP_Text>(true);
                TMP_Text amount = labels.FirstOrDefault(label => label.name == "Text (TMP)");
                TMP_Text production = labels.FirstOrDefault(label => label.name == "Text (TMP) (1)");
                view.Configure(types[field.name], icon, amount, production);
                return view;
            }).ToArray();
        }

        private void OnDisable()
        {
            ResponsiveUiStateManager.LayoutChanged -= ApplyLayout;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged -= HandleCityResourceStateChanged;
                CityStateManager.Instance.OnCityNameChanged -= HandleCityNameChanged;
            }

            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.OnEconomyStateChanged -= HandleWorldPlayerEconomyStateChanged;
            }

            UnbindViewEvents();
            CleanupCitySelector();
            CleanupResourceTooltips();
            CloseAllPopups();

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
            if (mapButton != null) mapButton.OnButtonActivatedClicked += HandleContextualNavigationRequested;
            if (administrationButton != null) administrationButton.onClick.AddListener(HandleAdministrationRequested);
            if (logoutButton != null) logoutButton.OnButtonActivatedClicked += HandleLogoutRequested;
            if (popupBackdrop != null) popupBackdrop.onClick.AddListener(CloseAllPopups);
        }

        private void UnbindViewEvents()
        {
            if (mapButton != null) mapButton.OnButtonActivatedClicked -= HandleContextualNavigationRequested;
            if (administrationButton != null) administrationButton.onClick.RemoveListener(HandleAdministrationRequested);
            if (logoutButton != null) logoutButton.OnButtonActivatedClicked -= HandleLogoutRequested;
            if (popupBackdrop != null) popupBackdrop.onClick.RemoveListener(CloseAllPopups);
        }

        private void HandleAdministrationRequested()
        {
            CloseAllPopups();
            GlobalWindowManager.Instance?.OpenWindow(Assets.Scripts.Domain.Enums.WindowTypeEnum.Administration);
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
            SetResourceAmount(CityTopBarResourceType.Research, Math.Floor(state.ResearchPointsAmount).ToString("N0"));
            SetResourceProduction(CityTopBarResourceType.Research, state.ResearchPointsProductionPerHour);
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
