using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiPremiumOverviewWindowController : MonoBehaviour, IUguiWindowPayloadReceiver
    {
        private const float ResolvingRefreshDelaySeconds = 3f;

        [Header("Authored assets")]
        [SerializeField] private GameObject miniUnitCardPrefab;
        [SerializeField] private UnitIconCatalog unitIconCatalog;
        [SerializeField] private Sprite attackCommandSprite;
        [SerializeField] private Sprite supportCommandSprite;

        private readonly List<GameObject> _renderedRows = new();
        private readonly List<RenderedMovement> _renderedMovements = new();
        private readonly HashSet<UnitTypeEnum> _missingIconWarnings = new();

        private GameObject _rowTemplate;
        private Transform _rowContainer;
        private Coroutine _requestCoroutine;
        private Coroutine _countdownCoroutine;
        private Coroutine _resolvingRefreshCoroutine;
        private int _requestVersion;
        private bool _isLoading;
        private bool _openReceived;

        private void Awake()
        {
            BindAuthoredView();
        }

        public void OnOpen(object payload)
        {
            _openReceived = true;
            if (_rowTemplate == null) BindAuthoredView();
            LoadMovements();
        }

        private void Start()
        {
            // The host normally delivers OnOpen immediately after Instantiate. This
            // fallback also makes the prefab safe when it is placed directly in a scene.
            if (!_openReceived) LoadMovements();
        }

        private void OnDisable()
        {
            _requestVersion++;
            _isLoading = false;
            StopRunningCoroutines();
        }

        private void BindAuthoredView()
        {
            Transform template = FindChild(transform, "CommandDataRow");
            if (template == null)
            {
                Debug.LogError("[PremiumOverview] CommandDataRow is missing from the prefab.", this);
                return;
            }

            _rowTemplate = template.gameObject;
            _rowContainer = template.parent;
            // This object is an authored cloning template, never a visible row.
            // Hide it before any other setup so later initialization failures
            // cannot leak sample data into Game view.
            _rowTemplate.SetActive(false);
            ConfigureTableColumns(_rowTemplate.transform);
            ConfigureHeaderColumns();

            foreach (Transform child in GetAllChildren(transform))
            {
                if (child.name == "ArrowUp Button" || child.name == "ArrowDownButton")
                    child.gameObject.SetActive(false);

                if (child.name.StartsWith("Placeholder", StringComparison.Ordinal))
                    DisableFutureTab(child.gameObject);
            }

            Transform commandTab = FindChild(transform, "CommandTab");
            commandTab?.GetComponent<FramedSpriteTabButton>()?.SetAsSelectedAsPrime(false);
        }

        private void ConfigureHeaderColumns()
        {
            SetColumnWidth(transform, "CommandHeader", 0.6f);
            SetColumnWidth(transform, "FromCityHeader", 1f);
            SetColumnWidth(transform, "ToCityHeader", 1f);
            SetColumnWidth(transform, "ArrivalHeader", 0.8f);
            SetColumnWidth(transform, "ContentHeader", 2f);
        }

        private static void ConfigureTableColumns(Transform row)
        {
            string[] names = { "CommandIcon", "FromText", "ToText", "ArrivalText", "Content" };
            float[] widths = { 0.6f, 1f, 1f, 0.8f, 2f };
            for (int index = 0; index < names.Length; index++)
            {
                Transform column = FindChild(row, names[index]);
                if (column == null) continue;
                column.gameObject.SetActive(true);
                column.SetSiblingIndex(index + 1); // Background is the ignored first child.
                SetColumnWidth(column, widths[index]);
            }
        }

        private static void SetColumnWidth(Transform root, string name, float width)
        {
            Transform column = FindChild(root, name);
            if (column != null) SetColumnWidth(column, width);
        }

        private static void SetColumnWidth(Transform column, float width)
        {
            LayoutElement layout = column.GetComponent<LayoutElement>()
                ?? column.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = false;
            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = width;
        }

        private static void DisableFutureTab(GameObject tab)
        {
            FramedSpriteTabButton button = tab.GetComponent<FramedSpriteTabButton>();
            if (button != null) button.enabled = false;

            // UnityEngine.Object has fake-null semantics. Do not use ?? here: a
            // destroyed/missing component wrapper is not CLR-null, but throws as
            // soon as one of its native properties is accessed.
            CanvasGroup canvasGroup = tab.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = tab.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0.55f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void LoadMovements()
        {
            if (_isLoading) return;

            StopTimers();
            int version = ++_requestVersion;
            _isLoading = true;
            ShowState("LOADING COMMANDS...");

            NetworkManager network = NetworkManager.Instance;
            if (network == null || network.UnitDeployment == null ||
                !Guid.TryParse(network.WorldPlayerId, out Guid worldPlayerId))
            {
                _isLoading = false;
                ShowState("NO ACTIVE WORLD PLAYER");
                return;
            }

            string requestError = null;
            _requestCoroutine = StartCoroutine(network.UnitDeployment.GetActiveDeployments(
                worldPlayerId,
                network.JwtToken,
                deployments =>
                {
                    if (!CanApply(version)) return;

                    _requestCoroutine = null;
                    _isLoading = false;
                    if (deployments == null)
                    {
                        string message = string.IsNullOrWhiteSpace(requestError)
                            ? "COMMANDS COULD NOT BE LOADED"
                            : requestError;
                        Debug.LogError($"[PremiumOverview] {message}", this);
                        ShowState(message.ToUpperInvariant());
                        return;
                    }

                    RenderMovements(deployments);
                },
                error =>
                {
                    if (CanApply(version)) requestError = error;
                }));
        }

        private bool CanApply(int version) =>
            isActiveAndEnabled && version == _requestVersion;

        private void RenderMovements(IEnumerable<UnitDeploymentDTO> deployments)
        {
            ClearRenderedRows();

            List<UnitDeploymentDTO> movements = deployments
                .Where(item => item != null &&
                    (item.Phase == UnitDeploymentPhaseEnum.Outbound ||
                     item.Phase == UnitDeploymentPhaseEnum.Returning))
                .ToList();

            if (movements.Count == 0)
            {
                ShowState("NO TROOP MOVEMENTS");
                return;
            }

            foreach (UnitDeploymentDTO movement in movements)
                AddMovementRow(movement);

            UpdateTimings();
            _countdownCoroutine = StartCoroutine(UpdateCountdownEverySecond());
        }

        private void AddMovementRow(UnitDeploymentDTO deployment)
        {
            GameObject row = CreateRow();
            if (row == null) return;

            ConfigureTableColumns(row.transform);

            bool returning = deployment.Phase == UnitDeploymentPhaseEnum.Returning;
            string originName = GetCityName(deployment.OriginLocation, deployment.OriginCity);
            string targetName = GetCityName(deployment.TargetLocation, deployment.TargetCity);

            SetNestedText(row.transform, "FromText", "Text", returning ? targetName : originName);
            SetNestedText(row.transform, "ToText", "Text", returning ? originName : targetName);

            Transform arrivalContainer = FindChild(row.transform, "ArrivalText");
            TMP_Text arrivalText = FindChild(arrivalContainer, "Arrival text")?.GetComponent<TMP_Text>();
            TMP_Text timeLeftText = FindChild(arrivalContainer, "Timeleft text")?.GetComponent<TMP_Text>();
            Image commandIcon = FindNamedImage(row.transform, "CommandIcon", "Icon");
            if (commandIcon != null)
            {
                commandIcon.sprite = deployment.Type == UnitDeploymentTypeEnum.Support
                    ? supportCommandSprite
                    : attackCommandSprite;
                commandIcon.enabled = commandIcon.sprite != null;
                commandIcon.preserveAspect = true;
            }

            PopulateUnitCards(row.transform, deployment.UnitStacks);
            _renderedMovements.Add(new RenderedMovement(deployment, arrivalText, timeLeftText));
            row.SetActive(true);
            RebuildTableLayout();
        }

        private void PopulateUnitCards(Transform row, IEnumerable<UnitStackDTO> stacks)
        {
            Transform content = FindChild(row, "Content");
            if (content == null) return;

            foreach (Transform child in content.Cast<Transform>().ToArray())
            {
                if (!child.name.StartsWith("MiniUnitCard", StringComparison.Ordinal)) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (miniUnitCardPrefab == null)
            {
                Debug.LogError("[PremiumOverview] MiniUnitCard prefab is not assigned.", this);
                return;
            }

            foreach (UnitStackDTO stack in stacks?.Where(item => item != null && item.Quantity > 0)
                         ?? Enumerable.Empty<UnitStackDTO>())
            {
                GameObject card = Instantiate(miniUnitCardPrefab, content, false);
                card.name = $"MiniUnitCard_{stack.Type}";

                LayoutElement cardLayout = card.GetComponent<LayoutElement>()
                    ?? card.AddComponent<LayoutElement>();
                cardLayout.ignoreLayout = false;
                cardLayout.minWidth = 60f;
                cardLayout.minHeight = 50f;
                cardLayout.preferredWidth = 60f;
                cardLayout.preferredHeight = 50f;
                cardLayout.flexibleWidth = 0f;
                cardLayout.flexibleHeight = 0f;

                Image icon = FindNamedImage(card.transform, card.name, "Icon");
                if (icon != null)
                {
                    Sprite sprite = ResolveUnitIcon(stack.Type);
                    icon.sprite = sprite;
                    icon.enabled = sprite != null;
                    icon.preserveAspect = true;
                }

                TMP_Text quantity = FindChild(card.transform, "Text (TMP)")?.GetComponent<TMP_Text>();
                if (quantity != null) quantity.text = stack.Quantity.ToString("N0");
            }
        }

        private Sprite ResolveUnitIcon(UnitTypeEnum unitType)
        {
            if (unitIconCatalog != null && unitIconCatalog.TryGetSprite(unitType, out Sprite sprite))
                return sprite;

            if (_missingIconWarnings.Add(unitType))
                Debug.LogWarning($"[PremiumOverview] UnitIconCatalog has no icon for {unitType}; using fallback.", this);

            return unitIconCatalog != null ? unitIconCatalog.FallbackSprite : null;
        }

        private void ShowState(string message)
        {
            ClearRenderedRows();
            GameObject row = CreateRow();
            if (row == null) return;

            SetNestedText(row.transform, "FromText", "Text", message);
            SetNestedText(row.transform, "ToText", "Text", string.Empty);
            SetNestedText(row.transform, "ArrivalText", "Arrival text", string.Empty);
            SetNestedText(row.transform, "ArrivalText", "Timeleft text", string.Empty);

            Transform command = FindChild(row.transform, "CommandIcon");
            if (command != null) command.gameObject.SetActive(false);
            Transform content = FindChild(row.transform, "Content");
            if (content != null) content.gameObject.SetActive(false);
            row.SetActive(true);
            RebuildTableLayout();
        }

        private GameObject CreateRow()
        {
            if (_rowTemplate == null || _rowContainer == null) return null;
            GameObject row = Instantiate(_rowTemplate, _rowContainer, false);
            row.name = "CommandDataRow_Runtime";
            _renderedRows.Add(row);
            return row;
        }

        private void RebuildTableLayout()
        {
            if (_rowContainer is RectTransform rowsRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowsRect);

            Transform header = FindChild(transform, "CommandHeader")?.parent;
            if (header is RectTransform headerRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(headerRect);
        }

        private void ClearRenderedRows()
        {
            foreach (GameObject row in _renderedRows)
            {
                if (row != null)
                {
                    row.SetActive(false);
                    Destroy(row);
                }
            }

            _renderedRows.Clear();
            _renderedMovements.Clear();
        }

        private IEnumerator UpdateCountdownEverySecond()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f);
                UpdateTimings();
            }
        }

        private void UpdateTimings()
        {
            bool resolving = false;
            DateTime utcNow = DateTime.UtcNow;
            foreach (RenderedMovement rendered in _renderedMovements)
            {
                if (rendered.ArrivalText == null || rendered.TimeLeftText == null) continue;
                if (!rendered.Deployment.ArrivalTime.HasValue)
                {
                    rendered.ArrivalText.text = "--";
                    rendered.TimeLeftText.text = "--";
                    continue;
                }

                DateTime arrivalUtc = AsUtc(rendered.Deployment.ArrivalTime.Value);
                rendered.ArrivalText.text = arrivalUtc.ToLocalTime().ToString("HH:mm:ss");
                TimeSpan remaining = arrivalUtc - utcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    rendered.TimeLeftText.text = "RESOLVING";
                    resolving = true;
                    continue;
                }

                rendered.TimeLeftText.text = FormatRemaining(remaining);
            }

            if (resolving && _resolvingRefreshCoroutine == null)
                _resolvingRefreshCoroutine = StartCoroutine(RefreshAfterWorkerDelay());
        }

        private IEnumerator RefreshAfterWorkerDelay()
        {
            yield return new WaitForSecondsRealtime(ResolvingRefreshDelaySeconds);
            _resolvingRefreshCoroutine = null;
            if (isActiveAndEnabled) LoadMovements();
        }

        private void StopRunningCoroutines()
        {
            if (_requestCoroutine != null) StopCoroutine(_requestCoroutine);
            _requestCoroutine = null;
            StopTimers();
        }

        private void StopTimers()
        {
            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
            if (_resolvingRefreshCoroutine != null) StopCoroutine(_resolvingRefreshCoroutine);
            _countdownCoroutine = null;
            _resolvingRefreshCoroutine = null;
        }

        private static string GetCityName(DeploymentLocationDTO location, CityDTO city)
        {
            if (!string.IsNullOrWhiteSpace(location?.CityName)) return location.CityName;
            if (!string.IsNullOrWhiteSpace(city?.CityName)) return city.CityName;
            return "UNKNOWN CITY";
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            int hours = remaining.Hours;
            return remaining.Days > 0
                ? $"{remaining.Days:00}d {hours:00}h {remaining.Minutes:00}m {remaining.Seconds:00}s"
                : $"{hours:00}h {remaining.Minutes:00}m {remaining.Seconds:00}s";
        }

        private static DateTime AsUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static void SetNestedText(Transform root, string containerName, string textName, string value)
        {
            Transform container = FindChild(root, containerName);
            TMP_Text text = FindChild(container, textName)?.GetComponent<TMP_Text>();
            if (text != null) text.text = value;
        }

        private static Image FindNamedImage(Transform root, string rootName, string imageName)
        {
            Transform scope = root.name == rootName ? root : FindChild(root, rootName);
            if (scope == null) return null;
            return scope.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image.name == imageName);
        }

        private static Transform FindChild(Transform root, string name) =>
            root == null ? null : GetAllChildren(root).FirstOrDefault(child => child.name == name);

        private static IEnumerable<Transform> GetAllChildren(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                yield return child;
        }

        private sealed class RenderedMovement
        {
            public RenderedMovement(UnitDeploymentDTO deployment, TMP_Text arrivalText, TMP_Text timeLeftText)
            {
                Deployment = deployment;
                ArrivalText = arrivalText;
                TimeLeftText = timeLeftText;
            }

            public UnitDeploymentDTO Deployment { get; }
            public TMP_Text ArrivalText { get; }
            public TMP_Text TimeLeftText { get; }
        }
    }
}
