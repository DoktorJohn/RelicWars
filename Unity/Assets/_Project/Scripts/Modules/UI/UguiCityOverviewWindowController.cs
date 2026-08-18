using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Scripts.Domain.State;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    /// <summary>
    /// Binds the authored uGUI CityOverviewWindow. This component never creates or
    /// rearranges UI; the prefab remains the single source of truth for its layout.
    /// </summary>
    public sealed class UguiCityOverviewWindowController : MonoBehaviour
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

        private ResourceCard _wood;
        private ResourceCard _stone;
        private ResourceCard _metal;
        private ResourceCard _coins;
        private ResourceCard[] _exoticCards;
        private TMP_Text _usedUnits;
        private TMP_Text _freePopulation;
        private TMP_Text _townHall;
        private TMP_Text _barracks;
        private TMP_Text _harbour;
        private TMP_Text _stable;
        private TMP_Text _workshop;
        private int _requestVersion;

        private void Awake()
        {
            _wood = BindCard("Wood");
            _stone = BindCard("Stone");
            _metal = BindCard("Metal");
            _coins = BindCard("Gold Coins");
            _exoticCards = new[] { BindCard("Exotic1"), BindCard("Exotic2"), BindCard("Exotic3") };

            Transform population = FindDescendant(transform, "Population");
            _usedUnits = FindAmount(population, "UsedUnits");
            _freePopulation = FindAmount(population, "FreePopulation");

            Transform activity = FindDescendant(transform, "CityActivity");
            _townHall = FindAmount(activity, "TownHall");
            _barracks = FindAmount(activity, "Barracks");
            _harbour = FindAmount(activity, "Harbour");
            _stable = FindAmount(activity, "Stable");
            _workshop = FindAmount(activity, "Workshop");
        }

        private void OnEnable()
        {
            SubscribeToCityState();
            if (CityStateManager.Instance != null)
            {
                RenderPopulation(CityStateManager.Instance.CurrentResources);
                RenderActivities();
            }

            LoadOverview();
        }

        private void OnDisable()
        {
            _requestVersion++;
            UnsubscribeFromCityState();
        }

        private void LoadOverview()
        {
            int version = ++_requestVersion;
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.City == null || !network.ActiveCityId.HasValue)
            {
                return;
            }

            StartCoroutine(network.City.GetCityOverviewHUD(
                network.ActiveCityId.Value,
                network.JwtToken,
                response =>
                {
                    if (!isActiveAndEnabled || version != _requestVersion || response == null)
                    {
                        return;
                    }

                    RenderOverview(response);
                }));
        }

        private void RenderOverview(CityOverviewHUDDTO overview)
        {
            _wood.Render("Wood", overview.Wood?.Production);
            _stone.Render("Stone", overview.Stone?.Production);
            _metal.Render("Metal", overview.Metal?.Production);
            _coins.RenderCoins(overview.CoinsProduction);

            List<CityExoticResourceProductionDTO> exotic = (overview.ExoticResourceProductions ?? new List<CityExoticResourceProductionDTO>())
                .OrderBy(item => item.SlotIndex)
                .Take(_exoticCards.Length)
                .ToList();

            for (int index = 0; index < _exoticCards.Length; index++)
            {
                bool hasResource = index < exotic.Count;
                _exoticCards[index].SetVisible(hasResource);
                if (hasResource)
                {
                    CityExoticResourceProductionDTO item = exotic[index];
                    _exoticCards[index].Render(ToDisplayName(item.ResourceType.ToString()), item.Production, GetExoticIcon(item.ResourceType.ToString()));
                }
            }

            if (CityStateManager.Instance != null)
            {
                RenderPopulation(CityStateManager.Instance.CurrentResources);
            }
            else if (overview.Population != null)
            {
                SetText(_usedUnits, overview.Population.InUse.ToString(CultureInfo.InvariantCulture));
                SetText(_freePopulation, overview.Population.Remaining.ToString(CultureInfo.InvariantCulture));
            }

            RenderActivities();
        }

        private void SubscribeToCityState()
        {
            CityStateManager state = CityStateManager.Instance;
            if (state == null) return;
            state.OnResourceStateChanged += RenderPopulation;
            state.OnBuildingQueueChanged += OnQueueChanged;
            state.OnBarracksQueueChanged += OnQueueChanged;
            state.OnHarborQueueChanged += OnQueueChanged;
            state.OnStableQueueChanged += OnQueueChanged;
            state.OnWorkshopQueueChanged += OnQueueChanged;
        }

        private void UnsubscribeFromCityState()
        {
            CityStateManager state = CityStateManager.Instance;
            if (state == null) return;
            state.OnResourceStateChanged -= RenderPopulation;
            state.OnBuildingQueueChanged -= OnQueueChanged;
            state.OnBarracksQueueChanged -= OnQueueChanged;
            state.OnHarborQueueChanged -= OnQueueChanged;
            state.OnStableQueueChanged -= OnQueueChanged;
            state.OnWorkshopQueueChanged -= OnQueueChanged;
        }

        private void OnQueueChanged<T>(List<T> _) => RenderActivities();

        private void RenderPopulation(CityResourceState state)
        {
            SetText(_usedUnits, state.CurrentPopulationUsage.ToString(CultureInfo.InvariantCulture));
            SetText(_freePopulation, state.FreePopulation.ToString(CultureInfo.InvariantCulture));
        }

        private void RenderActivities()
        {
            CityStateManager state = CityStateManager.Instance;
            if (state == null) return;

            List<BuildingDTO> builds = state.CurrentBuildingQueue;
            SetText(_townHall, FormatQueue(builds, item => item.Type));
            SetText(_barracks, FormatQueue(state.CurrentBarracksQueue, item => ToDisplayName(item.UnitType.ToString())));
            SetText(_harbour, FormatQueue(state.CurrentHarborQueue, item => ToDisplayName(item.UnitType.ToString())));
            SetText(_stable, FormatQueue(state.CurrentStableQueue, item => ToDisplayName(item.UnitType.ToString())));
            SetText(_workshop, FormatQueue(state.CurrentWorkshopQueue, item => ToDisplayName(item.UnitType.ToString())));
        }

        private ResourceCard BindCard(string cardName)
        {
            Transform root = FindDescendant(transform, cardName);
            if (root == null)
            {
                Debug.LogError($"[UguiCityOverviewWindowController] Missing authored card '{cardName}'.", this);
            }
            return new ResourceCard(root);
        }

        private Sprite GetExoticIcon(string resourceType)
        {
            return resourceType switch
            {
                "Cloth" => clothIcon,
                "Coal" => coalIcon,
                "Copper" => copperIcon,
                "Cotton" => cottonIcon,
                "Diamond" => diamondIcon,
                "Gold" => goldIcon,
                "Ivory" => ivoryIcon,
                "Sand" => sandIcon,
                "Silver" => silverIcon,
                "Sulphur" => sulphurIcon,
                _ => null
            };
        }

        private static TMP_Text FindAmount(Transform scope, string groupName)
        {
            Transform group = FindDescendant(scope, groupName);
            return FindDescendant(group, "Amount")?.GetComponent<TMP_Text>();
        }

        private static string FormatQueue<T>(IReadOnlyList<T> queue, Func<T, string> getName)
        {
            if (queue == null || queue.Count == 0) return "Idle";
            string suffix = queue.Count > 1 ? $" (+{queue.Count - 1})" : string.Empty;
            return getName(queue[0]) + suffix;
        }

        private static string ToDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(value, "(?<!^)([A-Z])", " $1");
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.name.Equals(name, StringComparison.Ordinal)) return child;
                Transform nested = FindDescendant(child, name);
                if (nested != null) return nested;
            }
            return null;
        }

        private sealed class ResourceCard
        {
            private readonly Transform _root;
            private readonly TMP_Text _title;
            private readonly Image _icon;
            private readonly TMP_Text _base;
            private readonly TMP_Text _flat;
            private readonly TMP_Text _multiplier;
            private readonly TMP_Text _unitUpkeep;
            private readonly TMP_Text _buildingUpkeep;
            private readonly TMP_Text _total;

            public ResourceCard(Transform root)
            {
                _root = root;
                Transform header = FindDescendant(root, "Header");
                _title = (FindDescendant(header, "TitleText") ?? FindDescendant(header, "Title"))?.GetComponent<TMP_Text>();
                _icon = FindDescendant(header, "Icon")?.GetComponent<Image>();
                _base = FindAmount(root, "BaseProduction");
                _flat = FindAmount(root, "Flat Bonus");
                _multiplier = FindAmount(root, "Multipliers");
                _unitUpkeep = FindAmount(root, "UnitUpkeep");
                _buildingUpkeep = FindAmount(root, "BuildingUpkeep");
                _total = FindAmount(root, "Total");
            }

            public void SetVisible(bool visible)
            {
                if (_root != null) _root.gameObject.SetActive(visible);
            }

            public void Render(string title, ProductionBreakdownDTO production, Sprite icon = null)
            {
                production ??= new ProductionBreakdownDTO();
                SetText(_title, title);
                if (_icon != null && icon != null) _icon.sprite = icon;
                SetText(_base, Format(production.BaseValue));
                SetText(_flat, FormatSigned(production.BuildingBonus));
                SetText(_multiplier, $"x{Format(production.GlobalModifierMultiplier, 2)}");
                SetText(_total, $"{Format(production.FinalValuePerHour)} / h");
            }

            public void RenderCoins(CoinsBreakdownDTO coins)
            {
                coins ??= new CoinsBreakdownDTO();
                SetText(_title, "Gold Coins");
                SetText(_base, Format(coins.BaseValue));
                SetText(_flat, FormatSigned(coins.BuildingBonus));
                SetText(_multiplier, $"x{Format(1d + coins.GlobalModifierMultiplier, 2)}");
                SetText(_unitUpkeep, $"-{Format(Math.Abs(coins.UnitUpkeepPerHour))}/hr");
                SetText(_buildingUpkeep, $"-{Format(Math.Abs(coins.BuildingUpkeepPerHour))}/hr");
                SetText(_total, $"{FormatSigned(coins.FinalValuePerHour - coins.Expenditure)} / h");
            }

            private static string Format(double value, int digits = 1) => value.ToString($"F{digits}", CultureInfo.InvariantCulture);
            private static string FormatSigned(double value, int digits = 1) => value >= 0 ? "+" + Format(value, digits) : Format(value, digits);
        }
    }
}
