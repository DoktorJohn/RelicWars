using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
using Project.Network.Manager;
using Project.Network.Models;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.Graphics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiExoticResourceWindowController : MonoBehaviour, IUguiWindowPayloadReceiver
    {
        [Header("Exotic resource icons")]
        [SerializeField] private Sprite clothIcon;
        [SerializeField] private Sprite coalIcon;
        [SerializeField] private Sprite copperIcon;
        [SerializeField] private Sprite cottonIcon;
        [SerializeField] private Sprite diamondIcon;
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private Sprite ivoryIcon;
        [SerializeField] private Sprite sandIcon;
        [SerializeField] private Sprite silverIcon;
        [SerializeField] private Sprite sulphurIcon;

        private readonly List<FramedSpriteTabButton> _tabs = new();
        private readonly List<WorldIslandResourceDTO> _resources = new();
        private readonly TMP_InputField[] _inputs = new TMP_InputField[4];
        private readonly TMP_Text[] _costs = new TMP_Text[4];

        private Image _resourceIcon;
        private TMP_Text _resourceName;
        private TMP_Text _tier;
        private TMP_Text _production;
        private TMP_Text _progressHeading;
        private TMP_Text _progressValue;
        private SimpleFillBar _progressFill;
        private CarvedPressButton _investButton;
        private Guid _islandId;
        private int _selectedSlotIndex;
        private int _loadVersion;
        private int _investmentVersion;
        private bool _requestInProgress;
        private bool _activeCityIsOnIsland;
        private WorldIslandDetailsDTO _details;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            BindControls();
        }

        private void OnDisable()
        {
            _loadVersion++;
            _investmentVersion++;
            _requestInProgress = false;
            StopAllCoroutines();
            UnbindControls();
        }

        public void OnOpen(object payload)
        {
            if (payload is not ExoticResourceWindowPayload resourcePayload || resourcePayload.IslandId == Guid.Empty)
            {
                Debug.LogError("[UguiExoticResourceWindowController] Invalid exotic resource payload.", this);
                return;
            }

            _islandId = resourcePayload.IslandId;
            _selectedSlotIndex = resourcePayload.SlotIndex;
            _investmentVersion++;
            _requestInProgress = false;
            ClearInputs();
            LoadIsland();
        }

        private void ResolveReferences()
        {
            _tabs.Clear();
            for (int index = 1; index <= 3; index++)
            {
                Transform root = FindDescendant(transform, $"Exotic Resource {index}");
                FramedSpriteTabButton tab = root?.GetComponent<FramedSpriteTabButton>();
                if (tab != null) _tabs.Add(tab);
            }

            Transform description = FindDescendant(transform, "Vertical description");
            _resourceIcon = FindDescendant(FindDescendant(transform, "ExoticContainer"), "Icon")?.GetComponent<Image>();
            _resourceName = FindDescendant(description, "Exotic Resource Name")?.GetComponent<TMP_Text>();
            _tier = FindDescendant(description, "Tier text")?.GetComponent<TMP_Text>();
            _production = FindDescendant(description, "Production text")?.GetComponent<TMP_Text>();
            _progressHeading = FindDescendant(transform, "Progress Text")?.GetComponent<TMP_Text>();
            Transform fillBar = FindDescendant(transform, "FillBar");
            _progressFill = fillBar?.GetComponent<SimpleFillBar>();
            _progressValue = FindDescendant(fillBar, "Text")?.GetComponent<TMP_Text>();

            ResolveInvestmentCard(0, "Wood");
            ResolveInvestmentCard(1, "Stone");
            ResolveInvestmentCard(2, "Metal");
            ResolveInvestmentCard(3, "Gold Coins");
            _investButton = FindDescendant(transform, "InvestContainer")?.GetComponentInChildren<CarvedPressButton>(true);
        }

        private void ResolveInvestmentCard(int index, string cardName)
        {
            Transform card = FindDescendant(FindDescendant(transform, "Basic Resources Container"), cardName);
            _inputs[index] = FindDescendant(card, "Invest input")?.GetComponent<TMP_InputField>();
            _costs[index] = FindDescendant(card, "Required")?.GetComponent<TMP_Text>();
        }

        private void BindControls()
        {
            UnbindControls();
            foreach (FramedSpriteTabButton tab in _tabs)
                tab.OnButtonActivatedClicked += OnTabClicked;
            if (_investButton != null)
                _investButton.OnButtonActivatedClicked += OnInvestClicked;
        }

        private void UnbindControls()
        {
            foreach (FramedSpriteTabButton tab in _tabs)
                tab.OnButtonActivatedClicked -= OnTabClicked;
            if (_investButton != null)
                _investButton.OnButtonActivatedClicked -= OnInvestClicked;
        }

        private void LoadIsland()
        {
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.World == null)
            {
                Debug.LogError("[UguiExoticResourceWindowController] Network is unavailable.", this);
                return;
            }

            int version = ++_loadVersion;
            SetControlsEnabled(false);
            StartCoroutine(network.World.GetIslandDetails(_islandId, network.JwtToken, details =>
            {
                if (!isActiveAndEnabled || version != _loadVersion) return;
                if (details?.ExoticResources == null || details.ExoticResources.Count == 0)
                {
                    Debug.LogError("[UguiExoticResourceWindowController] The island has no exotic resource data.", this);
                    return;
                }

                _details = details;
                _activeCityIsOnIsland = network.ActiveCityId.HasValue
                    && details.Cities != null
                    && details.Cities.Any(city => city.Id == network.ActiveCityId.Value);
                RenderTabs();
                SelectResource(_selectedSlotIndex);
            }));
        }

        private void RenderTabs()
        {
            _resources.Clear();
            _resources.AddRange(_details.ExoticResources.OrderBy(resource => resource.SlotIndex).Take(_tabs.Count));

            for (int index = 0; index < _tabs.Count; index++)
            {
                bool hasResource = index < _resources.Count;
                FramedSpriteTabButton tab = _tabs[index];
                tab.gameObject.SetActive(hasResource);
                if (!hasResource) continue;

                WorldIslandResourceDTO resource = _resources[index];
                TMP_Text label = FindDescendant(tab.transform, "Label")?.GetComponent<TMP_Text>();
                Image icon = FindDescendant(tab.transform, "Icon")?.GetComponent<Image>();
                if (label != null) label.text = ToDisplayName(resource.ResourceType);
                if (icon != null) icon.sprite = GetIcon(resource.ResourceType);
            }
        }

        private void OnTabClicked(FramedSpriteTabButton clickedTab)
        {
            if (_requestInProgress) return;
            int tabIndex = _tabs.IndexOf(clickedTab);
            if (tabIndex >= 0 && tabIndex < _resources.Count)
                SelectResource(_resources[tabIndex].SlotIndex);
        }

        private void SelectResource(int slotIndex)
        {
            WorldIslandResourceDTO resource = _resources.FirstOrDefault(item => item.SlotIndex == slotIndex);
            if (resource == null && _resources.Count > 0) resource = _resources[0];
            if (resource == null) return;

            _selectedSlotIndex = resource.SlotIndex;
            for (int index = 0; index < _tabs.Count; index++)
                _tabs[index].SetSelected(index < _resources.Count && _resources[index].SlotIndex == _selectedSlotIndex, false);

            if (_resourceIcon != null) _resourceIcon.sprite = GetIcon(resource.ResourceType);
            SetText(_resourceName, ToDisplayName(resource.ResourceType));
            SetText(_production, $"{resource.OutputPerHour:N2} per hour for every city on this island");

            if (_details.HasOwnedCity)
            {
                SetText(_tier, $"TIER {resource.Tier}/10");
                SetText(_progressHeading, resource.Tier >= 10
                    ? "MAXIMUM TIER"
                    : $"{resource.ProgressPercent:N1}% TO TIER {resource.Tier + 1}");
                SetText(_progressValue, $"{resource.ProgressPercent:N1}%");
                _progressFill?.SetNormalizedValue(Mathf.Clamp01((float)resource.ProgressPercent / 100f));
            }
            else
            {
                SetText(_tier, "Unknown tier");
                SetText(_progressHeading, "Unknown progress");
                SetText(_progressValue, "-");
                _progressFill?.SetNormalizedValue(0f);
            }

            SetCost(0, resource.WoodInvestment, resource.NextTierWoodCost);
            SetCost(1, resource.StoneInvestment, resource.NextTierStoneCost);
            SetCost(2, resource.MetalInvestment, resource.NextTierMetalCost);
            SetCost(3, resource.CoinInvestment, resource.NextTierCoinCost);
            ClearInputs();
            SetControlsEnabled(_activeCityIsOnIsland && resource.Tier < 10);
        }

        private void OnInvestClicked(CarvedPressButton _)
        {
            if (_requestInProgress || !_activeCityIsOnIsland || NetworkManager.Instance?.ActiveCityId == null) return;
            if (!TryReadAmount(_inputs[0], out double wood)
                || !TryReadAmount(_inputs[1], out double stone)
                || !TryReadAmount(_inputs[2], out double metal)
                || !TryReadAmount(_inputs[3], out double coins)
                || wood + stone + metal + coins <= 0d)
            {
                Debug.LogWarning("[UguiExoticResourceWindowController] Enter at least one non-negative contribution.", this);
                return;
            }

            if (!HasAvailableResources(wood, stone, metal, coins))
            {
                Debug.LogWarning("[UguiExoticResourceWindowController] The active city lacks the entered resources.", this);
                return;
            }

            var request = new ExoticResourceInvestmentRequestDTO
            {
                SlotIndex = _selectedSlotIndex,
                WoodAmount = wood,
                StoneAmount = stone,
                MetalAmount = metal,
                CoinAmount = coins
            };

            _requestInProgress = true;
            int version = ++_investmentVersion;
            Guid islandId = _islandId;
            SetControlsEnabled(false);
            NetworkManager network = NetworkManager.Instance;
            StartCoroutine(network.City.InvestInExoticResource(
                network.ActiveCityId.Value,
                request,
                network.JwtToken,
                response => HandleInvestmentResponse(response, version, islandId, wood, stone, metal, coins)));
        }

        private void HandleInvestmentResponse(ExoticResourceInvestmentResponseDTO response, int version,
            Guid islandId, double wood, double stone, double metal, double coins)
        {
            if (!isActiveAndEnabled || version != _investmentVersion || islandId != _islandId) return;
            _requestInProgress = false;
            if (response?.IslandExoticResources == null || response.IslandExoticResources.Count == 0)
            {
                Debug.LogError("[UguiExoticResourceWindowController] Investment failed.", this);
                SelectResource(_selectedSlotIndex);
                return;
            }

            _details.ExoticResources = response.IslandExoticResources;
            CityStateManager.Instance?.DeductResourcesLocally(wood, stone, metal);
            WorldPlayerStateManager.Instance?.DeductResourcesLocally(coins, 0, 0);
            NetworkManager network = NetworkManager.Instance;
            if (network.ActiveCityId.HasValue)
                CityStateManager.Instance?.RequestImmediateRefresh(network.ActiveCityId.Value);
            if (Guid.TryParse(network.WorldPlayerId, out Guid worldPlayerId))
                WorldPlayerStateManager.Instance?.InitiateEconomyRefresh(worldPlayerId);

            RenderTabs();
            SelectResource(_selectedSlotIndex);
        }

        private bool HasAvailableResources(double wood, double stone, double metal, double coins)
        {
            CityStateManager city = CityStateManager.Instance;
            WorldPlayerStateManager player = WorldPlayerStateManager.Instance;
            return city != null && player != null
                && wood <= city.CurrentResources.WoodAmount
                && stone <= city.CurrentResources.StoneAmount
                && metal <= city.CurrentResources.MetalAmount
                && coins <= player.CurrentEconomy.CoinsAmount;
        }

        private void SetControlsEnabled(bool enabled)
        {
            foreach (TMP_InputField input in _inputs)
                if (input != null) input.interactable = enabled;
            if (_investButton != null) _investButton.enabled = enabled;
        }

        private void ClearInputs()
        {
            foreach (TMP_InputField input in _inputs)
                if (input != null) input.text = string.Empty;
        }

        private void SetCost(int index, double invested, double required)
        {
            SetText(_costs[index], $"{invested:N0} / {required:N0}");
        }

        private static bool TryReadAmount(TMP_InputField input, out double amount)
        {
            string value = string.IsNullOrWhiteSpace(input?.text) ? "0" : input.text.Trim();
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount)
                && double.IsFinite(amount)
                && amount >= 0d;
        }

        private Sprite GetIcon(ExoticResourceTypeEnum type) => type switch
        {
            ExoticResourceTypeEnum.Cloth => clothIcon,
            ExoticResourceTypeEnum.Coal => coalIcon,
            ExoticResourceTypeEnum.Copper => copperIcon,
            ExoticResourceTypeEnum.Cotton => cottonIcon,
            ExoticResourceTypeEnum.Diamond => diamondIcon,
            ExoticResourceTypeEnum.Gold => goldIcon,
            ExoticResourceTypeEnum.Ivory => ivoryIcon,
            ExoticResourceTypeEnum.Sand => sandIcon,
            ExoticResourceTypeEnum.Silver => silverIcon,
            ExoticResourceTypeEnum.Sulphur => sulphurIcon,
            _ => null
        };

        private static string ToDisplayName(ExoticResourceTypeEnum type) => type.ToString();
        private static void SetText(TMP_Text label, string value) { if (label != null) label.text = value; }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null) return null;
            if (root.name.Equals(objectName, StringComparison.OrdinalIgnoreCase)) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), objectName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
