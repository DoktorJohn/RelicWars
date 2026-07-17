using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public class AdministrationWindowController : BaseWindow
    {
        private const float ResolvingRefreshDelaySeconds = 3f;

        protected override string WindowName => "AdministrationWindow";
        protected override string VisualContainerName => "Administration-Window-MainContainer";
        protected override string HeaderName => "Administration-Window-Header";

        [Header("Deployment Row Configuration")]
        [SerializeField] private VisualTreeAsset _deploymentRowTemplate;

        private VisualElement _loadState;
        private VisualElement _content;
        private VisualElement _movementsPanel;
        private VisualElement _deploymentsPanel;
        private VisualElement _movementRows;
        private VisualElement _deploymentRows;
        private VisualElement _movementEmptyState;
        private VisualElement _deploymentEmptyState;
        private ScrollView _movementScroll;
        private ScrollView _deploymentScroll;
        private Button _movementsTab;
        private Button _deploymentsTab;
        private readonly List<RenderedMovement> _renderedMovements = new();
        private List<UnitDeploymentDTO> _loadedDeployments = new();
        private Coroutine _countdownCoroutine;
        private Coroutine _resolvingRefreshCoroutine;
        private int _loadVersion;
        private int _openSequence;
        private AdministrationTab _selectedTab = AdministrationTab.Movements;

        public override void OnOpen(object dataPayload)
        {
            _openSequence = BeginDeferredOpen();
            CacheElements();
            BindButtons();
            ShowTab(_selectedTab);
            LoadDeployments(_openSequence);
        }

        private void OnDisable()
        {
            _loadVersion++;
            StopTimers();
            UnbindButtons();
            InvalidateDeferredOpen();
        }

        private void CacheElements()
        {
            _loadState = Root.Q<VisualElement>("Administration-Load-State");
            _content = Root.Q<VisualElement>("Administration-Content");
            _movementsPanel = Root.Q<VisualElement>("Administration-Movements-Panel");
            _deploymentsPanel = Root.Q<VisualElement>("Administration-Deployments-Panel");
            _movementRows = Root.Q<VisualElement>("Administration-Movement-Rows");
            _deploymentRows = Root.Q<VisualElement>("Administration-Stationed-Rows");
            _movementEmptyState = Root.Q<VisualElement>("Administration-Movement-Empty");
            _deploymentEmptyState = Root.Q<VisualElement>("Administration-Deployment-Empty");
            _movementScroll = Root.Q<ScrollView>("Administration-Movement-Scroll");
            _deploymentScroll = Root.Q<ScrollView>("Administration-Deployment-Scroll");
            _movementsTab = Root.Q<Button>("Administration-Movements-Tab");
            _deploymentsTab = Root.Q<Button>("Administration-Deployments-Tab");
        }

        private void BindButtons()
        {
            UnbindButtons();
            if (_movementsTab != null) _movementsTab.clicked += HandleMovementsTabClicked;
            if (_deploymentsTab != null) _deploymentsTab.clicked += HandleDeploymentsTabClicked;
        }

        private void UnbindButtons()
        {
            if (_movementsTab != null) _movementsTab.clicked -= HandleMovementsTabClicked;
            if (_deploymentsTab != null) _deploymentsTab.clicked -= HandleDeploymentsTabClicked;
        }

        private void HandleMovementsTabClicked() => ShowTab(AdministrationTab.Movements);
        private void HandleDeploymentsTabClicked() => ShowTab(AdministrationTab.Deployments);

        private void ShowTab(AdministrationTab tab)
        {
            _selectedTab = tab;
            bool showMovements = tab == AdministrationTab.Movements;
            _movementsPanel?.EnableInClassList("hidden", !showMovements);
            _deploymentsPanel?.EnableInClassList("hidden", showMovements);
            _movementsTab?.EnableInClassList("window-tab-active", showMovements);
            _deploymentsTab?.EnableInClassList("window-tab-active", !showMovements);
        }

        private void LoadDeployments(int openSequence)
        {
            StopTimers();
            int loadVersion = ++_loadVersion;
            _content?.AddToClassList("hidden");
            _loadState?.RemoveFromClassList("hidden");
            WindowAsyncStateHelper.ShowLoading(_loadState, "Loading troop administration...");

            if (NetworkManager.Instance == null || !Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid worldPlayerId))
            {
                ShowError(openSequence, loadVersion, "No active world player is available.");
                return;
            }

            string loadError = null;
            StartCoroutine(NetworkManager.Instance.UnitDeployment.GetActiveDeployments(
                worldPlayerId,
                NetworkManager.Instance.JwtToken,
                deployments =>
                {
                    if (!CanApply(openSequence, loadVersion)) return;
                    if (deployments == null)
                    {
                        ShowError(openSequence, loadVersion, string.IsNullOrWhiteSpace(loadError)
                            ? "Troop administration could not be loaded."
                            : loadError);
                        return;
                    }

                    _loadedDeployments = deployments;
                    RenderTables();
                    CompleteDeferredOpen(openSequence);
                },
                error =>
                {
                    if (CanApply(openSequence, loadVersion)) loadError = error;
                }));
        }

        private void ShowError(int openSequence, int loadVersion, string message)
        {
            if (!CanApply(openSequence, loadVersion)) return;
            _loadState?.RemoveFromClassList("hidden");
            WindowAsyncStateHelper.ShowError(_loadState, message, () => LoadDeployments(_openSequence));
            CompleteDeferredOpen(openSequence);
        }

        private bool CanApply(int openSequence, int loadVersion) =>
            isActiveAndEnabled && IsDeferredOpenCurrent(openSequence) && loadVersion == _loadVersion;

        private void RenderTables()
        {
            _movementRows?.Clear();
            _deploymentRows?.Clear();
            _renderedMovements.Clear();
            WindowAsyncStateHelper.Clear(_loadState);
            _loadState?.AddToClassList("hidden");
            _content?.RemoveFromClassList("hidden");

            var movements = _loadedDeployments
                .Where(item => item.Phase != UnitDeploymentPhaseEnum.Stationed)
                .OrderBy(item => item.ArrivalTime ?? DateTime.MaxValue)
                .ThenBy(item => item.DepartureTime)
                .ThenBy(item => item.Id)
                .ToList();
            var stationedSupports = _loadedDeployments
                .Where(item => item.Phase == UnitDeploymentPhaseEnum.Stationed && item.Type == UnitDeploymentTypeEnum.Support)
                .OrderByDescending(item => item.StationedAt)
                .ThenBy(item => item.DepartureTime)
                .ThenBy(item => item.Id)
                .ToList();

            RenderTable(movements, _movementRows, _movementScroll, _movementEmptyState, false);
            RenderTable(stationedSupports, _deploymentRows, _deploymentScroll, _deploymentEmptyState, true);
            ShowTab(_selectedTab);

            UpdateTimings();
            if (movements.Count > 0)
            {
                _countdownCoroutine = StartCoroutine(UpdateCountdownEverySecond());
            }
        }

        private void RenderTable(
            List<UnitDeploymentDTO> deployments,
            VisualElement rows,
            ScrollView scroll,
            VisualElement emptyState,
            bool isStationedTable)
        {
            bool isEmpty = deployments.Count == 0;
            scroll?.EnableInClassList("hidden", isEmpty);
            emptyState?.EnableInClassList("hidden", !isEmpty);
            if (isEmpty)
            {
                WindowAsyncStateHelper.ShowEmpty(emptyState, isStationedTable
                    ? "No stationed support deployments."
                    : "No troop movements.");
                return;
            }

            WindowAsyncStateHelper.Clear(emptyState);
            for (int index = 0; index < deployments.Count; index++)
            {
                AddDeploymentRow(rows, deployments[index], isStationedTable, index % 2 != 0);
            }
        }

        private void AddDeploymentRow(
            VisualElement rows,
            UnitDeploymentDTO deployment,
            bool isStationedTable,
            bool useAlternateSurface)
        {
            if (rows == null || _deploymentRowTemplate == null) return;
            TemplateContainer instance = _deploymentRowTemplate.Instantiate();
            VisualElement row = instance.Q<VisualElement>("Administration-Deployment-Row");
            if (row == null) return;
            if (useAlternateSurface) row.AddToClassList("administration-table-row-alternate");

            Label action = row.Q<Label>("Administration-Row-Action");
            Label phase = row.Q<Label>("Administration-Row-Phase");
            VisualElement from = row.Q<VisualElement>("Administration-Row-From");
            VisualElement to = row.Q<VisualElement>("Administration-Row-To");
            Label troops = row.Q<Label>("Administration-Row-Troops");
            Label timing = row.Q<Label>("Administration-Row-Timing");

            SetText(action, deployment.Type.ToString().ToUpperInvariant());
            action?.AddToClassList(deployment.Type == UnitDeploymentTypeEnum.Attack
                ? "administration-action-attack"
                : "administration-action-support");
            SetText(phase, deployment.Phase.ToString().ToUpperInvariant());

            bool returning = deployment.Phase == UnitDeploymentPhaseEnum.Returning;
            DeploymentLocationDTO origin = GetLocation(deployment.OriginLocation, deployment.OriginCity, deployment.WorldPlayerId, deployment.WorldPlayerUserName);
            DeploymentLocationDTO target = GetLocation(deployment.TargetLocation, deployment.TargetCity, null, null);
            PopulateLocation(from, returning ? target : origin);
            PopulateLocation(to, returning ? origin : target);
            SetText(troops, FormatTroops(deployment));

            if (isStationedTable)
            {
                row.AddToClassList("administration-stationed-row");
                phase?.AddToClassList("hidden");
                SetText(timing, deployment.StationedAt.HasValue
                    ? AsUtc(deployment.StationedAt.Value).ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                    : "--");
            }
            else
            {
                _renderedMovements.Add(new RenderedMovement(deployment, timing));
            }

            rows.Add(row);
        }

        private static DeploymentLocationDTO GetLocation(
            DeploymentLocationDTO location,
            CityDTO city,
            Guid? worldPlayerId,
            string worldPlayerName)
        {
            if (location != null) return location;
            return new DeploymentLocationDTO
            {
                CityId = city?.Id ?? Guid.Empty,
                CityName = city?.CityName ?? "UNKNOWN CITY",
                X = city?.X ?? 0,
                Y = city?.Y ?? 0,
                IsNPC = city?.IsNPC ?? false,
                WorldPlayerId = worldPlayerId,
                WorldPlayerName = worldPlayerName
            };
        }

        private static void PopulateLocation(VisualElement container, DeploymentLocationDTO location)
        {
            if (container == null) return;
            container.Clear();
            if (location == null)
            {
                container.Add(CreatePlainEntityLine("UNKNOWN CITY"));
                container.Add(CreatePlainEntityLine("UNKNOWN PLAYER"));
                container.Add(CreatePlainEntityLine("NO ALLIANCE"));
                return;
            }

            string cityName = string.IsNullOrWhiteSpace(location.CityName) ? "UNKNOWN CITY" : location.CityName;
            if (location.CityId != Guid.Empty)
            {
                container.Add(WindowNavigationHelper.CreateLinkButton(
                    cityName,
                    () => WindowNavigationHelper.OpenCityInspection(location.CityId, location.X, location.Y),
                    "administration-entity-line"));
            }
            else
            {
                container.Add(CreatePlainEntityLine(cityName));
            }

            if (location.IsNPC)
            {
                container.Add(CreatePlainEntityLine("NPC VILLAGE"));
            }
            else if (location.WorldPlayerId.HasValue)
            {
                container.Add(WindowNavigationHelper.CreateLinkButton(
                    string.IsNullOrWhiteSpace(location.WorldPlayerName) ? "UNKNOWN PLAYER" : location.WorldPlayerName,
                    () => WindowNavigationHelper.OpenProfile(location.WorldPlayerId.Value),
                    "administration-entity-line"));
            }
            else
            {
                container.Add(CreatePlainEntityLine("UNKNOWN PLAYER"));
            }

            if (location.AllianceId.HasValue)
            {
                string alliance = string.IsNullOrWhiteSpace(location.AllianceTag)
                    ? location.AllianceName
                    : $"[{location.AllianceTag}] {location.AllianceName}";
                container.Add(WindowNavigationHelper.CreateLinkButton(
                    string.IsNullOrWhiteSpace(alliance) ? "UNKNOWN ALLIANCE" : alliance,
                    () => WindowNavigationHelper.OpenAlliance(location.AllianceId.Value),
                    "administration-entity-line"));
            }
            else
            {
                container.Add(CreatePlainEntityLine("NO ALLIANCE"));
            }
        }

        private static Label CreatePlainEntityLine(string text)
        {
            var label = new Label(text);
            label.AddToClassList("administration-entity-line");
            label.AddToClassList("administration-entity-plain");
            return label;
        }

        private static string FormatTroops(UnitDeploymentDTO deployment)
        {
            if (deployment.UnitStacks == null) return "--";
            string manifest = string.Join(" · ", deployment.UnitStacks
                .Where(stack => stack.Quantity > 0)
                .Select(stack => $"{stack.Type} {stack.Quantity:N0}"));
            return string.IsNullOrWhiteSpace(manifest) ? "--" : manifest;
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
            bool hasResolvingMovement = false;
            DateTime utcNow = DateTime.UtcNow;
            foreach (RenderedMovement rendered in _renderedMovements)
            {
                if (rendered.TimingLabel == null) continue;
                if (!rendered.Deployment.ArrivalTime.HasValue)
                {
                    rendered.TimingLabel.text = "--";
                    continue;
                }

                TimeSpan remaining = AsUtc(rendered.Deployment.ArrivalTime.Value) - utcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    rendered.TimingLabel.text = "RESOLVING";
                    rendered.TimingLabel.AddToClassList("administration-timing-resolving");
                    hasResolvingMovement = true;
                    continue;
                }

                rendered.TimingLabel.RemoveFromClassList("administration-timing-resolving");
                int days = (int)remaining.TotalDays;
                rendered.TimingLabel.text = $"{days:00}:{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
            }

            if (hasResolvingMovement && _resolvingRefreshCoroutine == null)
            {
                _resolvingRefreshCoroutine = StartCoroutine(RefreshAfterWorkerDelay());
            }
        }

        private IEnumerator RefreshAfterWorkerDelay()
        {
            yield return new WaitForSecondsRealtime(ResolvingRefreshDelaySeconds);
            _resolvingRefreshCoroutine = null;
            if (isActiveAndEnabled) LoadDeployments(_openSequence);
        }

        private void StopTimers()
        {
            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
            if (_resolvingRefreshCoroutine != null) StopCoroutine(_resolvingRefreshCoroutine);
            _countdownCoroutine = null;
            _resolvingRefreshCoroutine = null;
        }

        private static DateTime AsUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static void SetText(Label label, string value)
        {
            if (label != null) label.text = value;
        }

        private enum AdministrationTab
        {
            Movements,
            Deployments
        }

        private sealed class RenderedMovement
        {
            public RenderedMovement(UnitDeploymentDTO deployment, Label timingLabel)
            {
                Deployment = deployment;
                TimingLabel = timingLabel;
            }

            public UnitDeploymentDTO Deployment { get; }
            public Label TimingLabel { get; }
        }
    }
}
