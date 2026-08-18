using Assets.Scripts.Domain.Enums;
using Assets.Scripts.Domain.State;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using Sunvale.AncientRomeUI.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiWorkshopWindowController : MonoBehaviour
    {
        private sealed class TabBinding
        {
            public FramedSpriteTabButton Button;
            public TMP_Text Owned;
            public Image Icon;
            public WorkshopUnitInfoDTO Unit;
        }

        private readonly List<TabBinding> _tabs = new();
        private readonly List<GameObject> _queueCards = new();
        private readonly Dictionary<UnitTypeEnum, Sprite> _unitIcons = new();
        private WorkshopUnitInfoDTO _selected;
        private Slider _slider;
        private TMP_InputField _amountInput;
        private TMP_Text _amountText;
        private CarvedPressButton _recruitButton;
        private GameObject _queueTemplate;
        private Transform _queueRoot;
        private TMP_Text _queueAmount;
        private List<RecruitmentQueueItemDTO> _queue = new();
        private CityResourceState _resources;
        private int _requestVersion;
        private bool _synchronizingAmount;

        private void Awake()
        {
            BindTabs();
            _slider = FindComponent<Slider>(transform, "Options Slider");
            if (_slider != null) { _slider.minValue = 0; _slider.wholeNumbers = true; }
            BuildAmountInput();
            _recruitButton = FindComponent<CarvedPressButton>(transform, "RecruitBtnContent");
            _queueTemplate = FindTransform(transform, "UnitQueueCard")?.gameObject;
            _queueRoot = _queueTemplate != null ? _queueTemplate.transform.parent : FindTransform(transform, "Queue frame");
            Transform queueTitle = FindTransform(transform, "RecruitmentQueue title");
            _queueAmount = FindComponent<TMP_Text>(queueTitle, "Amount label");
            SetActive(_queueTemplate, false);
        }

        private void OnEnable()
        {
            if (_slider != null) _slider.onValueChanged.AddListener(OnSliderChanged);
            if (_amountInput != null) _amountInput.onEndEdit.AddListener(OnAmountEdited);
            if (_recruitButton != null) _recruitButton.OnButtonActivatedClicked += OnRecruitClicked;
            foreach (TabBinding tab in _tabs) tab.Button.OnButtonActivatedClicked += OnTabClicked;

            CityStateManager state = CityStateManager.Instance;
            if (state != null)
            {
                state.OnResourceStateChanged += OnResourcesChanged;
                state.OnWorkshopQueueChanged += OnQueueChanged;
                _resources = state.CurrentResources;
                OnQueueChanged(state.CurrentWorkshopQueue);
            }

            LoadOverview();
        }

        private void OnDisable()
        {
            _requestVersion++;
            StopAllCoroutines();
            if (_slider != null) _slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (_amountInput != null) _amountInput.onEndEdit.RemoveListener(OnAmountEdited);
            if (_recruitButton != null) _recruitButton.OnButtonActivatedClicked -= OnRecruitClicked;
            foreach (TabBinding tab in _tabs) tab.Button.OnButtonActivatedClicked -= OnTabClicked;
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnResourceStateChanged -= OnResourcesChanged;
                CityStateManager.Instance.OnWorkshopQueueChanged -= OnQueueChanged;
            }
            ClearQueueCards();
        }

        private void Update()
        {
            if (_queue.Count == 0) return;
            float elapsed = Time.deltaTime;
            foreach (RecruitmentQueueItemDTO item in _queue) item.TimeRemainingSeconds = Math.Max(0, item.TimeRemainingSeconds - elapsed);
            RefreshQueueCardProgress();
        }

        private void BindTabs()
        {
            string[] names = { "Ballista tab", "Catapult tab", "Engineer tab", "Cannon tab", "Trebuchet tab" };
            foreach (string name in names)
            {
                Transform root = FindTransform(transform, name);
                FramedSpriteTabButton button = root != null ? root.GetComponent<FramedSpriteTabButton>() : null;
                if (button == null) continue;
                _tabs.Add(new TabBinding
                {
                    Button = button,
                    Owned = FindComponent<TMP_Text>(root, "OwnedAmount"),
                    Icon = FindComponent<Image>(root, "Unit icon")
                });
            }
        }

        private void LoadOverview()
        {
            int version = ++_requestVersion;
            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.Workshop == null || !network.ActiveCityId.HasValue) return;
            StartCoroutine(network.Workshop.GetWorkshopOverviewInformation(network.ActiveCityId.Value, network.JwtToken, overview =>
            {
                if (!isActiveAndEnabled || version != _requestVersion || overview?.AvailableUnits == null) return;
                BindOverview(overview.AvailableUnits);
            }));
        }

        private void BindOverview(List<WorkshopUnitInfoDTO> units)
        {
            foreach (TabBinding tab in _tabs)
            {
                string tabKey = Normalize(tab.Button.name.Replace("tab", string.Empty));
                tab.Unit = units.FirstOrDefault(unit => Normalize(unit.UnitName) == tabKey || Normalize(unit.UnitType.ToString()) == tabKey);
                tab.Button.gameObject.SetActive(tab.Unit != null);
                if (tab.Unit == null) continue;
                SetText(tab.Owned, $"OWNED: {tab.Unit.AlreadyOwnedCount:N0}");
                if (tab.Icon != null && tab.Icon.sprite != null) _unitIcons[tab.Unit.UnitType] = tab.Icon.sprite;
            }

            TabBinding first = _tabs.FirstOrDefault(tab => tab.Unit != null);
            if (first != null) Select(first);
        }

        private void OnTabClicked(FramedSpriteTabButton button)
        {
            TabBinding tab = _tabs.FirstOrDefault(candidate => candidate.Button == button);
            if (tab?.Unit != null) Select(tab);
        }

        private void Select(TabBinding selectedTab)
        {
            _selected = selectedTab.Unit;
            foreach (TabBinding tab in _tabs) tab.Button.SetSelected(tab == selectedTab, true);
            Transform title = FindTransform(transform, "UnitIconandTitle");
            SetText(FindComponent<TMP_Text>(title, "UnitName label"), _selected.UnitName.ToUpperInvariant());
            Image mainIcon = FindComponent<Image>(title, "Unit icon");
            if (mainIcon != null && selectedTab.Icon != null) mainIcon.sprite = selectedTab.Icon.sprite;

            SetSection("Power Section", _selected.Power);
            SetSection("Armor Section", _selected.Armor);
            SetSection("Discipline Section", _selected.Discipline);
            SetSection("Mobility Section", _selected.Mobility);
            SetSection("Reach Section", _selected.Reach);
            SetSection("LootCapacity Section", _selected.LootCapacity);
            SetSection("Population Section", _selected.PopulationCost);
            SetSection("RecruitTime Section", FormatRecruitmentStat(_selected.RecruitmentTimeInSeconds));
            UpdateAffordableRange();
        }

        private void SetSection(string sectionName, object value)
        {
            Transform section = FindTransform(transform, sectionName);
            TMP_Text target = section?.GetComponentsInChildren<TMP_Text>(true).LastOrDefault();
            SetText(target, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private void OnResourcesChanged(CityResourceState resources) { _resources = resources; UpdateAffordableRange(); }

        private void UpdateAffordableRange()
        {
            if (_selected == null || _slider == null) return;
            int maximum = 100;
            maximum = Affordable(maximum, _resources.WoodAmount, _selected.CostWood);
            maximum = Affordable(maximum, _resources.StoneAmount, _selected.CostStone);
            maximum = Affordable(maximum, _resources.MetalAmount, _selected.CostMetal);
            maximum = Affordable(maximum, _resources.FreePopulation, _selected.PopulationCost);
            _slider.minValue = maximum > 0 ? 1 : 0;
            _slider.maxValue = Math.Max(0, maximum);
            int amount = maximum > 0 ? Math.Max(1, Mathf.RoundToInt(_slider.value)) : 0;
            SetAmount(Mathf.Clamp(amount, 0, maximum));
        }

        private static int Affordable(int current, double available, int cost) => cost > 0 ? Math.Min(current, (int)Math.Floor(available / cost)) : current;
        private void OnSliderChanged(float value) { if (!_synchronizingAmount) SetAmount(Mathf.RoundToInt(value)); }
        private void OnAmountEdited(string value) { SetAmount(int.TryParse(value, out int parsed) ? parsed : 0); }

        private void SetAmount(int amount)
        {
            if (_slider == null) return;
            amount = Mathf.Clamp(amount, 0, Mathf.RoundToInt(_slider.maxValue));
            _synchronizingAmount = true;
            _slider.SetValueWithoutNotify(amount);
            if (_amountInput != null) _amountInput.SetTextWithoutNotify(amount.ToString(CultureInfo.InvariantCulture));
            else SetText(_amountText, amount.ToString(CultureInfo.InvariantCulture));
            _synchronizingAmount = false;

            if (_selected != null)
            {
                SetText(FindComponent<TMP_Text>(transform, "Wood amount"), (_selected.CostWood * amount).ToString("N0"));
                SetText(FindComponent<TMP_Text>(transform, "Stone amount"), (_selected.CostStone * amount).ToString("N0"));
                SetText(FindComponent<TMP_Text>(transform, "Metal amount"), (_selected.CostMetal * amount).ToString("N0"));
                SetText(FindComponent<TMP_Text>(transform, "Population amount"), (_selected.PopulationCost * amount).ToString("N0"));
                int seconds = _selected.RecruitmentTimeInSeconds * amount;
                SetText(FindComponent<TMP_Text>(FindTransform(transform, "Recruitmenttime"), "Time label"), FormatDuration(seconds));
                SetText(FindComponent<TMP_Text>(FindTransform(transform, "Completiontime"), "Completion data label"), amount > 0 ? DateTime.UtcNow.AddSeconds(seconds).ToString("dd/MM HH:mm:ss 'UTC'") : "-");
                _recruitButton?.SetTextOnLabel(amount > 0 ? $"RECRUIT {amount:N0}" : "RECRUIT 0");
            }
        }

        private void OnRecruitClicked(CarvedPressButton _)
        {
            int amount = _slider != null ? Mathf.RoundToInt(_slider.value) : 0;
            NetworkManager network = NetworkManager.Instance;
            if (_selected == null || amount <= 0 || network?.Workshop == null || !network.ActiveCityId.HasValue) return;
            StartCoroutine(network.Workshop.RecruitUnits(network.ActiveCityId.Value, _selected.UnitType, amount, network.JwtToken, result =>
            {
                if (result?.Success != true) return;
                SetAmount(_slider.maxValue >= 1 ? 1 : 0);
                CityStateManager.Instance?.RequestImmediateRefresh(network.ActiveCityId.Value);
                LoadOverview();
            }));
        }

        private void OnQueueChanged(List<RecruitmentQueueItemDTO> queue)
        {
            _queue = queue?.ToList() ?? new List<RecruitmentQueueItemDTO>();
            RenderQueue();
        }

        private void RenderQueue()
        {
            ClearQueueCards();
            SetText(_queueAmount, $"{_queue.Count}/5");
            if (_queueTemplate == null || _queueRoot == null) return;
            for (int index = 0; index < _queue.Count; index++)
            {
                RecruitmentQueueItemDTO item = _queue[index];
                GameObject card = Instantiate(_queueTemplate, _queueRoot, false);
                card.name = _queueTemplate.name;
                SetText(FindComponent<TMP_Text>(card.transform, "UnitName label"), Humanize(item.UnitType.ToString()).ToUpperInvariant());
                SetText(FindComponent<TMP_Text>(card.transform, "Amount label"), $"x{item.Amount:N0}");
                Image icon = FindComponent<Image>(card.transform, "Unit icon");
                if (icon != null && _unitIcons.TryGetValue(item.UnitType, out Sprite sprite)) icon.sprite = sprite;
                bool isLast = index == _queue.Count - 1;
                CarvedPressButton cancel = FindComponent<CarvedPressButton>(card.transform, "CancelBtn");
                SetActive(cancel?.gameObject, isLast);
                if (cancel != null && isLast) cancel.OnButtonActivatedClicked += _ => Cancel(item.QueueId, cancel);
                card.SetActive(true);
                _queueCards.Add(card);
            }
            RefreshQueueCardProgress();
            SetActive(_queueTemplate, false);
        }

        private void RefreshQueueCardProgress()
        {
            for (int i = 0; i < Math.Min(_queue.Count, _queueCards.Count); i++)
            {
                RecruitmentQueueItemDTO item = _queue[i];
                GameObject card = _queueCards[i];
                float remaining = item.TotalDurationSeconds > 0 ? Mathf.Clamp01((float)(item.TimeRemainingSeconds / item.TotalDurationSeconds)) : 0;
                SimpleFillBar fill = FindComponent<SimpleFillBar>(card.transform, "FillBar");
                if (fill != null) fill.SetNormalizedValue(remaining);
                SetText(FindComponent<TMP_Text>(card.transform, "Amount label"), $"x{CalculateRemainingAmount(item):N0}");
                SetText(FindComponent<TMP_Text>(card.transform, "ProgressText"), FormatDuration((int)Math.Ceiling(item.TimeRemainingSeconds)));
            }
        }

        private static int CalculateRemainingAmount(RecruitmentQueueItemDTO item)
        {
            if (item == null || item.Amount <= 0 || item.TimeRemainingSeconds <= 0) return 0;
            if (item.SecondsPerUnit <= 0) return item.Amount;
            return Mathf.Clamp((int)Math.Ceiling(item.TimeRemainingSeconds / item.SecondsPerUnit), 0, item.Amount);
        }

        private void Cancel(Guid queueId, CarvedPressButton button)
        {
            NetworkManager network = NetworkManager.Instance;
            if (network?.Workshop == null || !network.ActiveCityId.HasValue) return;
            button.enabled = false;
            StartCoroutine(network.Workshop.CancelRecruitment(network.ActiveCityId.Value, queueId, network.JwtToken, result =>
            {
                if (button != null) button.enabled = true;
                if (result?.Success == true) CityStateManager.Instance?.RequestImmediateRefresh(network.ActiveCityId.Value);
            }));
        }

        private void BuildAmountInput()
        {
            Transform root = FindTransform(transform, "AmountContent");
            if (root == null) return;
            _amountText = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
            _amountInput = root.GetComponent<TMP_InputField>() ?? root.gameObject.AddComponent<TMP_InputField>();
            _amountInput.textViewport = root as RectTransform;
            _amountInput.textComponent = _amountText as TextMeshProUGUI;
            _amountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _amountInput.lineType = TMP_InputField.LineType.SingleLine;
            _amountInput.characterLimit = 6;
            _amountInput.targetGraphic = root.GetComponentInChildren<Image>(true);
        }

        private void ClearQueueCards() { foreach (GameObject card in _queueCards) if (card != null) Destroy(card); _queueCards.Clear(); SetActive(_queueTemplate, false); }
        private static string FormatRecruitmentStat(int seconds) { seconds = Math.Max(0, seconds); int minutes = seconds / 60; int remainingSeconds = seconds % 60; return minutes > 0 ? $"{minutes}m {remainingSeconds}s" : $"{remainingSeconds}s"; }
        private static string FormatDuration(int seconds) { TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds)); return time.Days > 0 ? $"{time.Days}d {time:hh\\:mm\\:ss}" : time.ToString(@"hh\:mm\:ss"); }
        private static string Normalize(string value) => new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        private static string Humanize(string value) => string.Concat((value ?? string.Empty).Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
        private static T FindComponent<T>(Transform root, string name) where T : Component { Transform target = FindTransform(root, name); return target == null ? null : target.GetComponent<T>() ?? target.GetComponentInChildren<T>(true); }
        private static Transform FindTransform(Transform root, string name) { if (root == null) return null; if (root.name.Equals(name, StringComparison.Ordinal)) return root; for (int i = 0; i < root.childCount; i++) { Transform found = FindTransform(root.GetChild(i), name); if (found != null) return found; } return null; }
        private static void SetText(TMP_Text target, string value) { if (target != null) target.text = value ?? string.Empty; }
        private static void SetActive(GameObject target, bool active) { if (target != null) target.SetActive(active); }
    }
}
