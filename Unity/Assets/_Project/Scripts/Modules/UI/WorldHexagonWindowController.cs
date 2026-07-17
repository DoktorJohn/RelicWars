using System;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Modules.UI;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Modules.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public class MapInteractionPayload
    {
        public Vector2Int Coordinates;
        public string BiomeName;
        public Guid? DeploymentIdOnTile;
        public bool IsPlayerOwned;
        public Vector2 ScreenClickPosition;
    }

    public class CityInspectionPayload
    {
        public Guid CityId;
        public Vector2Int Coordinates;
        public string TerrainName;
    }

    public class WorldHexagonWindowController : BaseWindow
    {
        protected override string WindowName => "WorldHexagonWindow";
        protected override string VisualContainerName => "Hexagon-Window-MainContainer";
        protected override string HeaderName => "Hexagon-Window-Header";

        private VisualElement _stateContainer;
        private VisualElement _detailsContainer;
        private Label _coordinatesValueLabel;
        private Label _terrainValueLabel;
        private Label _cityNameValueLabel;
        private Label _playerNameLabel;
        private Label _allianceNameLabel;
        private Label _pointsValueLabel;
        private Button _cityInfoTab;
        private Button _attackCityTab;
        private Button _supportCityTab;
        private VisualElement _cityInfoPanel;
        private VisualElement _marchOrdersPanel;
        private VisualElement _marchUnitsContainer;
        private Label _marchSourceLink;
        private Label _marchDestinationLink;
        private Label _marchMissionBadge;
        private Label _marchDetailTarget;
        private Label _marchTravelTimeLabel;
        private Label _marchArrivalLabel;
        private Label _marchTransportLabel;
        private Label _marchTotalLabel;
        private Label _marchStatusLabel;
        private Button _marchSubmitButton;
        private Button _closeButton;
        private readonly Dictionary<UnitTypeEnum, IntegerField> _marchQuantityInputs = new();
        private readonly List<Button> _marchMaxButtons = new();
        private VisualElement _incomingAttacksContainer;
        private VisualElement _incomingAttacksList;
        private Button _moveHereButton;
        private Button _simulateBattleButton;
        private VisualElement _simulateBattleRow;

        private CityInspectionPayload _currentPayload;
        private CityInspectionDTO _currentInspectionData;
        private int _requestVersion;
        private bool _isLayoutReady;
        private bool _hasLoadedInspectionData;
        private bool _deploymentRequestInFlight;
        private int _estimateVersion;
        private UnitDeploymentTypeEnum _selectedMissionType;
        private string _lastDeploymentError;
        private long? _estimatedDurationSeconds;
        private int _lastArrivalDisplaySecond = -1;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            CacheVisualElements();
            BindButtons();
            InitializeInteractionBlockingEvents();
            _currentInspectionData = null;
            HideInspectionState();
            ResetInspectionFields();

            if (dataPayload is not CityInspectionPayload payload || payload.CityId == Guid.Empty)
            {
                ShowErrorState("Invalid city.");
                CompleteDeferredOpen(version);
                return;
            }

            _currentPayload = payload;
            _isLayoutReady = false;
            _hasLoadedInspectionData = false;

            if (MainContainer != null)
            {
                MainContainer.style.visibility = Visibility.Hidden;
                MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
                MainContainer.RegisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
            }

            ShowLoadingState();
            RenderPayloadPreview();
            LoadCityInspection(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            UnbindLinkCallbacks();

            if (WorldMapInteractionHandler.Instance != null)
            {
                WorldMapInteractionHandler.Instance.SetMouseOverUI(false);
            }

            if (MainContainer != null)
            {
                MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
            }
        }

        private void CacheVisualElements()
        {
            _stateContainer = Root.Q<VisualElement>("Inspection-State-Container");
            _detailsContainer = Root.Q<VisualElement>("Inspection-Details-Container");
            _coordinatesValueLabel = Root.Q<Label>("Target-Coordinates-Label");
            _terrainValueLabel = Root.Q<Label>("Terrain-Type-Label");
            _cityNameValueLabel = Root.Q<Label>("City-Name-Label");
            _playerNameLabel = Root.Q<Label>("Player-Name-Label");
            _allianceNameLabel = Root.Q<Label>("Alliance-Name-Label");
            _pointsValueLabel = Root.Q<Label>("City-Points-Label");
            _cityInfoTab = Root.Q<Button>("City-Info-Tab");
            _attackCityTab = Root.Q<Button>("Attack-City-Tab");
            _supportCityTab = Root.Q<Button>("Support-City-Tab");
            _cityInfoPanel = Root.Q<VisualElement>("City-Info-Panel");
            _marchOrdersPanel = Root.Q<VisualElement>("March-Orders-Panel");
            _marchUnitsContainer = Root.Q<VisualElement>("March-Units-Container");
            _marchSourceLink = Root.Q<Label>("March-Source-Link");
            _marchDestinationLink = Root.Q<Label>("March-Destination-Link");
            _marchMissionBadge = Root.Q<Label>("March-Mission-Badge");
            _marchDetailTarget = Root.Q<Label>("March-Detail-Target");
            _marchTravelTimeLabel = Root.Q<Label>("March-Travel-Time-Label");
            _marchArrivalLabel = Root.Q<Label>("March-Arrival-Label");
            _marchTransportLabel = Root.Q<Label>("March-Transport-Label");
            _marchTotalLabel = Root.Q<Label>("March-Total-Label");
            _marchStatusLabel = Root.Q<Label>("March-Status-Label");
            _marchSubmitButton = Root.Q<Button>("March-Submit-Button");
            _closeButton = Root.Q<Button>($"{WindowName}-Close-Button");
            _incomingAttacksContainer = Root.Q<VisualElement>("Incoming-Attacks-Container");
            _incomingAttacksList = Root.Q<VisualElement>("Incoming-Attacks-List");
            _moveHereButton = Root.Q<Button>("Move-Here-Button");
        }

        private void BindButtons()
        {
            BindLink(_playerNameLabel, HandlePlayerNameClickEvent);
            BindLink(_allianceNameLabel, HandleAllianceNameClickEvent);
            if (_marchSubmitButton != null)
            {
                _marchSubmitButton.clicked -= ExecuteDeployment;
                _marchSubmitButton.clicked += ExecuteDeployment;
            }
            BindTab(_cityInfoTab, ShowCityInfoTab);
            BindTab(_attackCityTab, ShowAttackTab);
            BindTab(_supportCityTab, ShowSupportTab);
            if (_moveHereButton != null)
            {
                _moveHereButton.clicked -= HandleMoveHereClicked;
                _moveHereButton.clicked += HandleMoveHereClicked;
            }
        }

        private static void BindLink(Label label, EventCallback<ClickEvent> handler)
        {
            if (label == null || handler == null)
            {
                return;
            }

            label.UnregisterCallback<ClickEvent>(handler);
            label.RegisterCallback<ClickEvent>(handler);
        }

        private void UnbindLinkCallbacks()
        {
            if (_playerNameLabel != null)
            {
                _playerNameLabel.UnregisterCallback<ClickEvent>(HandlePlayerNameClickEvent);
            }

            if (_allianceNameLabel != null)
            {
                _allianceNameLabel.UnregisterCallback<ClickEvent>(HandleAllianceNameClickEvent);
            }

            if (_marchSubmitButton != null) _marchSubmitButton.clicked -= ExecuteDeployment;
            if (_cityInfoTab != null) _cityInfoTab.clicked -= ShowCityInfoTab;
            if (_attackCityTab != null) _attackCityTab.clicked -= ShowAttackTab;
            if (_supportCityTab != null) _supportCityTab.clicked -= ShowSupportTab;
            if (_moveHereButton != null) _moveHereButton.clicked -= HandleMoveHereClicked;
        }

        private static void BindTab(Button button, Action handler)
        {
            if (button == null) return;
            button.clicked -= handler;
            button.clicked += handler;
        }

        private void InitializeInteractionBlockingEvents()
        {
            if (MainContainer == null)
            {
                return;
            }

            MainContainer.UnregisterCallback<PointerEnterEvent>(HandleMainContainerPointerEnter);
            MainContainer.UnregisterCallback<PointerLeaveEvent>(HandleMainContainerPointerLeave);
            MainContainer.RegisterCallback<PointerEnterEvent>(HandleMainContainerPointerEnter);
            MainContainer.RegisterCallback<PointerLeaveEvent>(HandleMainContainerPointerLeave);
        }

        private void HandleMainContainerPointerEnter(PointerEnterEvent _)
        {
            WorldMapInteractionHandler.Instance?.SetMouseOverUI(true);
        }

        private void HandleMainContainerPointerLeave(PointerLeaveEvent _)
        {
            WorldMapInteractionHandler.Instance?.SetMouseOverUI(false);
        }

        private void HandleInitialCenteringGeometryCallback(GeometryChangedEvent evt)
        {
            if (MainContainer == null || MainContainer.parent == null)
            {
                return;
            }

            float parentWidth = MainContainer.parent.resolvedStyle.width;
            float parentHeight = MainContainer.parent.resolvedStyle.height;
            float windowWidth = evt.newRect.width;
            float windowHeight = evt.newRect.height;

            if (float.IsNaN(parentWidth) || windowWidth <= 0)
            {
                return;
            }

            float targetLeft = (parentWidth - windowWidth) / 2f;
            float targetTop = (parentHeight - windowHeight) / 2f;

            MainContainer.style.position = Position.Absolute;
            MainContainer.style.left = targetLeft;
            MainContainer.style.top = targetTop;
            MainContainer.style.visibility = Visibility.Visible;

            _isLayoutReady = true;
            MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
            TryCompleteDeferredOpen();
        }

        private void ShowLoadingState()
        {
            if (_stateContainer == null)
            {
                return;
            }

            _stateContainer.style.display = DisplayStyle.Flex;
            WindowAsyncStateHelper.ShowLoading(_stateContainer, "Loading city inspection...");
        }

        private void ShowErrorState(string message, Action retry = null)
        {
            if (_stateContainer == null)
            {
                return;
            }

            _stateContainer.style.display = DisplayStyle.Flex;
            WindowAsyncStateHelper.ShowError(_stateContainer, message, retry);
        }

        private void ShowInspectionState()
        {
            if (_stateContainer != null)
            {
                _stateContainer.style.display = DisplayStyle.None;
                WindowAsyncStateHelper.Clear(_stateContainer);
            }

            if (_detailsContainer != null)
            {
                _detailsContainer.style.display = DisplayStyle.Flex;
            }
        }

        private void HideInspectionState()
        {
            if (_detailsContainer != null)
            {
                _detailsContainer.style.display = DisplayStyle.None;
            }
        }

        private void RenderPayloadPreview()
        {
            if (_currentPayload == null)
            {
                return;
            }

            if (_coordinatesValueLabel != null)
            {
                _coordinatesValueLabel.text = $"{_currentPayload.Coordinates.x}, {_currentPayload.Coordinates.y}";
            }

            if (_terrainValueLabel != null)
            {
                _terrainValueLabel.text = string.IsNullOrWhiteSpace(_currentPayload.TerrainName)
                    ? "-"
                    : _currentPayload.TerrainName.Replace("_", " ").ToUpperInvariant();
            }
        }

        private void ResetInspectionFields()
        {
            if (_cityNameValueLabel != null)
            {
                _cityNameValueLabel.text = string.Empty;
            }

            if (_pointsValueLabel != null)
            {
                _pointsValueLabel.text = string.Empty;
            }

            SetLinkLabel(_playerNameLabel, null, string.Empty);
            SetLinkLabel(_allianceNameLabel, null, string.Empty);
        }

        private void LoadCityInspection(int version)
        {
            if (NetworkManager.Instance == null)
            {
                _hasLoadedInspectionData = true;
                HideInspectionState();
                ShowErrorState("Network is unavailable.");
                TryCompleteDeferredOpen();
                return;
            }

            _hasLoadedInspectionData = false;
            HideInspectionState();
            ShowLoadingState();

            StartCoroutine(NetworkManager.Instance.World.GetCityInspection(
                _currentPayload.CityId,
                NetworkManager.Instance.JwtToken,
                inspectionDto =>
                {
                    if (!isActiveAndEnabled || version != _requestVersion)
                    {
                        return;
                    }

                    if (inspectionDto == null)
                    {
                        _hasLoadedInspectionData = true;
                        HideInspectionState();
                        ShowErrorState("Could not load city inspection.", () => LoadCityInspection(version));
                        TryCompleteDeferredOpen();
                        return;
                    }

                    _currentInspectionData = inspectionDto;
                    _hasLoadedInspectionData = true;
                    RenderInspectionData(inspectionDto);
                    ShowInspectionState();
                    TryCompleteDeferredOpen();
                }));
        }

        private void RenderInspectionData(CityInspectionDTO data)
        {
            if (data == null)
            {
                return;
            }

            if (_coordinatesValueLabel != null)
            {
                _coordinatesValueLabel.text = $"{data.X}, {data.Y}";
            }

            if (_terrainValueLabel != null && _currentPayload != null)
            {
                _terrainValueLabel.text = string.IsNullOrWhiteSpace(_currentPayload.TerrainName)
                    ? "-"
                    : _currentPayload.TerrainName.Replace("_", " ").ToUpperInvariant();
            }

            if (_cityNameValueLabel != null)
            {
                _cityNameValueLabel.text = string.IsNullOrWhiteSpace(data.CityName) ? "-" : data.CityName;
            }

            if (_pointsValueLabel != null)
            {
                _pointsValueLabel.text = data.Points.ToString("N0");
            }

            if (data.IsNPC)
            {
                SetNPCOwnerLabel(_playerNameLabel);
            }
            else
            {
                SetLinkLabel(_playerNameLabel, data.WorldPlayerId, data.WorldPlayerName);
            }
            SetLinkLabel(_allianceNameLabel, data.AllianceId, data.AllianceName);
            UpdateAttackButtonState(data);
            EnsureCombatSimulatorButton();
            ShowCityInfoTab();
            LoadIncomingAttacks(data);
        }

        private void EnsureCombatSimulatorButton()
        {
            if (_cityInfoPanel == null)
            {
                return;
            }

            if (_simulateBattleButton != null)
            {
                _simulateBattleButton.SetEnabled(TryGetActiveOriginCityId(out _));
                return;
            }

            _simulateBattleRow = new VisualElement();
            _simulateBattleRow.AddToClassList("info-row");

            var label = new Label("SIMULATOR");
            label.AddToClassList("info-label");

            _simulateBattleButton = new Button { text = "SIMULATE BATTLE" };
            _simulateBattleButton.AddToClassList("btn-global-base");
            _simulateBattleButton.AddToClassList("btn-imperial-primary");
            _simulateBattleButton.AddToClassList("inspection-action-button");
            _simulateBattleButton.clicked += HandleCombatSimulatorClicked;
            _simulateBattleButton.SetEnabled(TryGetActiveOriginCityId(out _));

            _simulateBattleRow.Add(label);
            _simulateBattleRow.Add(_simulateBattleButton);
            _cityInfoPanel.Add(_simulateBattleRow);
        }

        private static void SetLinkLabel(Label label, Guid? targetId, string text)
        {
            if (label == null)
            {
                return;
            }

            var hasTarget = targetId.HasValue && targetId.Value != Guid.Empty;
            label.text = hasTarget && !string.IsNullOrWhiteSpace(text) ? text : "-";
            label.SetEnabled(hasTarget);
            label.pickingMode = hasTarget ? PickingMode.Position : PickingMode.Ignore;
        }

        private static void SetNPCOwnerLabel(Label label)
        {
            if (label == null)
            {
                return;
            }

            label.text = "NPC Village";
            label.SetEnabled(false);
            label.pickingMode = PickingMode.Ignore;
        }

        private void HandlePlayerNameClickEvent(ClickEvent _)
        {
            HandlePlayerNameClicked();
        }

        private void HandleAllianceNameClickEvent(ClickEvent _)
        {
            HandleAllianceNameClicked();
        }

        private void HandlePlayerNameClicked()
        {
            if (_currentInspectionData?.WorldPlayerId.HasValue != true)
            {
                return;
            }

            WindowNavigationHelper.OpenProfile(_currentInspectionData.WorldPlayerId.Value);
        }

        private void HandleAllianceNameClicked()
        {
            if (_currentInspectionData?.AllianceId.HasValue != true)
            {
                return;
            }

            WindowNavigationHelper.OpenAlliance(_currentInspectionData.AllianceId.Value);
        }

        private void UpdateAttackButtonState(CityInspectionDTO data)
        {
            _attackCityTab?.SetEnabled(data.CanAttack);
            _supportCityTab?.SetEnabled(data.CanSupport);
        }

        private void ShowCityInfoTab() => ShowTab(_cityInfoPanel, null);
        private void ShowAttackTab()
        {
            if (_currentInspectionData?.CanAttack == true) ShowTab(_marchOrdersPanel, UnitDeploymentTypeEnum.Attack);
        }

        private void ShowSupportTab()
        {
            if (_currentInspectionData?.CanSupport == true) ShowTab(_marchOrdersPanel, UnitDeploymentTypeEnum.Support);
        }

        private void ShowTab(VisualElement selected, UnitDeploymentTypeEnum? missionType)
        {
            _cityInfoPanel?.EnableInClassList("hidden", selected != _cityInfoPanel);
            _marchOrdersPanel?.EnableInClassList("hidden", selected != _marchOrdersPanel);
            _cityInfoTab?.EnableInClassList("window-tab-active", selected == _cityInfoPanel);
            _attackCityTab?.EnableInClassList("window-tab-active", missionType == UnitDeploymentTypeEnum.Attack);
            _supportCityTab?.EnableInClassList("window-tab-active", missionType == UnitDeploymentTypeEnum.Support);

            if (missionType.HasValue)
            {
                _selectedMissionType = missionType.Value;
                RenderMarchOrders();
            }
        }

        private void RenderMarchOrders()
        {
            if (_currentInspectionData == null || _marchUnitsContainer == null) return;

            _estimateVersion++;
            _estimatedDurationSeconds = null;
            _lastArrivalDisplaySecond = -1;
            _marchSourceLink.text = CityStateManager.Instance?.CurrentCityName ?? "ACTIVE CITY";
            _marchDestinationLink.text = string.IsNullOrWhiteSpace(_currentInspectionData.CityName)
                ? _currentInspectionData.CityId.ToString()
                : _currentInspectionData.CityName;
            if (_marchDetailTarget != null) _marchDetailTarget.text = _marchDestinationLink.text;
            _marchTravelTimeLabel.text = "--:--:--";
            _marchArrivalLabel.text = "--";
            _marchTransportLabel.text = "--";
            _marchTransportLabel.RemoveFromClassList("march-metric-positive");
            _marchTransportLabel.RemoveFromClassList("march-metric-negative");
            _marchTotalLabel.text = "TOTAL TROOPS  0";
            _marchStatusLabel.text = string.Empty;
            _marchMissionBadge.text = _selectedMissionType == UnitDeploymentTypeEnum.Attack ? "ATTACK" : "SUPPORT";
            _marchMissionBadge.EnableInClassList("march-mission-attack", _selectedMissionType == UnitDeploymentTypeEnum.Attack);
            _marchMissionBadge.EnableInClassList("march-mission-support", _selectedMissionType == UnitDeploymentTypeEnum.Support);
            _marchSubmitButton.text = _selectedMissionType == UnitDeploymentTypeEnum.Attack ? "ATTACK" : "SUPPORT";
            _marchSubmitButton.EnableInClassList("btn-imperial-danger", _selectedMissionType == UnitDeploymentTypeEnum.Attack);
            _marchSubmitButton.EnableInClassList("btn-imperial-success", _selectedMissionType == UnitDeploymentTypeEnum.Support);
            _marchSubmitButton.SetEnabled(false);

            _marchUnitsContainer.Clear();
            _marchQuantityInputs.Clear();
            _marchMaxButtons.Clear();
            if (!TryGetActiveOriginCityId(out _))
            {
                _marchStatusLabel.text = "The active origin city could not be verified.";
                _marchSubmitButton.SetEnabled(false);
                return;
            }

            var stationedUnits = (CityStateManager.Instance?.CurrentStationedUnits ?? new List<UnitStackDTO>())
                .Where(stack => stack.Quantity > 0)
                .ToList();
            if (stationedUnits.Count == 0)
            {
                var empty = new Label("NO STATIONED UNITS AVAILABLE");
                empty.AddToClassList("march-empty");
                _marchUnitsContainer.Add(empty);
                return;
            }

            foreach (var unitStack in stationedUnits)
            {
                var row = new VisualElement();
                row.AddToClassList("march-unit-row");
                var name = new Label(unitStack.Type.ToString());
                name.AddToClassList("march-unit-name");
                name.AddToClassList("march-unit-label");
                var available = new Label(unitStack.Quantity.ToString());
                available.AddToClassList("march-unit-available");
                available.AddToClassList("march-unit-label");
                var quantity = new IntegerField { value = 0 };
                quantity.AddToClassList("game-input");
                quantity.AddToClassList("march-unit-quantity");
                quantity.RegisterValueChangedCallback(change =>
                {
                    quantity.SetValueWithoutNotify(Math.Clamp(change.newValue, 0, unitStack.Quantity));
                    UpdateMarchTotal();
                    RefreshTravelEstimate();
                });
                var max = new Button(() => quantity.value = unitStack.Quantity) { text = "MAX" };
                max.AddToClassList("btn-global-base");
                max.AddToClassList("btn-imperial-primary");
                max.AddToClassList("march-max-button");
                row.Add(name);
                row.Add(available);
                row.Add(quantity);
                row.Add(max);
                _marchQuantityInputs[unitStack.Type] = quantity;
                _marchMaxButtons.Add(max);
                _marchUnitsContainer.Add(row);
            }
        }

        private void UpdateMarchTotal()
        {
            _marchTotalLabel.text = $"TOTAL TROOPS  {_marchQuantityInputs.Values.Sum(input => input.value)}";
        }

        private void RefreshTravelEstimate()
        {
            int version = ++_estimateVersion;
            var selections = GetSelectedUnits();
            if (selections.Count == 0 || !TryGetActiveOriginCityId(out Guid originCityId))
            {
                _marchTravelTimeLabel.text = "--:--:--";
                _marchArrivalLabel.text = "--";
                _marchTransportLabel.text = "--";
                _marchTransportLabel.RemoveFromClassList("march-metric-positive");
                _marchTransportLabel.RemoveFromClassList("march-metric-negative");
                _marchStatusLabel.text = "Select units to calculate transport capacity.";
                _marchSubmitButton?.SetEnabled(false);
                return;
            }

            StartCoroutine(NetworkManager.Instance.UnitDeployment.EstimateTravel(new DeploymentTravelEstimateRequestDTO
            {
                OriginCityId = originCityId,
                TargetCityId = _currentInspectionData.CityId,
                UnitsToDeploy = selections
            }, NetworkManager.Instance.JwtToken, estimate =>
            {
                if (!isActiveAndEnabled || version != _estimateVersion || estimate == null) return;
                long hours = estimate.DurationSeconds / 3600;
                long minutes = (estimate.DurationSeconds % 3600) / 60;
                long seconds = estimate.DurationSeconds % 60;
                _marchTravelTimeLabel.text = $"{hours:00}:{minutes:00}:{seconds:00}";
                _estimatedDurationSeconds = estimate.DurationSeconds;
                UpdateArrivalDisplay();
                _marchTransportLabel.text = estimate.RequiresTransport
                    ? $"{estimate.RequiredTransportCapacity} / {estimate.AvailableTransportCapacity}"
                    : "NOT REQUIRED";
                _marchTransportLabel.EnableInClassList("march-metric-positive", estimate.HasSufficientTransportCapacity);
                _marchTransportLabel.EnableInClassList("march-metric-negative", !estimate.HasSufficientTransportCapacity);
                _marchStatusLabel.text = estimate.RequiresTransport
                    ? (estimate.HasSufficientTransportCapacity
                        ? $"Transport capacity {estimate.RequiredTransportCapacity}/{estimate.AvailableTransportCapacity} (margin {estimate.TransportCapacityMargin})"
                        : $"Insufficient transport capacity: need {estimate.RequiredTransportCapacity}, have {estimate.AvailableTransportCapacity}.")
                    : "No sea transport required.";
                _marchSubmitButton?.SetEnabled(estimate.HasSufficientTransportCapacity);
            }));
        }

        private void Update()
        {
            if (!_estimatedDurationSeconds.HasValue || _marchOrdersPanel == null
                || _marchOrdersPanel.ClassListContains("hidden"))
            {
                return;
            }

            int currentSecond = DateTime.Now.Second;
            if (currentSecond == _lastArrivalDisplaySecond) return;
            UpdateArrivalDisplay();
        }

        private void UpdateArrivalDisplay()
        {
            if (!_estimatedDurationSeconds.HasValue || _marchArrivalLabel == null) return;
            DateTime now = DateTime.Now;
            _lastArrivalDisplaySecond = now.Second;
            _marchArrivalLabel.text = now.AddSeconds(_estimatedDurationSeconds.Value).ToString("dd.MM.yyyy HH:mm:ss");
        }

        private List<UnitSelectionDTO> GetSelectedUnits()
        {
            return _marchQuantityInputs
                .Where(input => input.Value.value > 0)
                .Select(input => new UnitSelectionDTO { Type = input.Key, Quantity = input.Value.value })
                .ToList();
        }

        private void ExecuteDeployment()
        {
            if (_deploymentRequestInFlight || _currentInspectionData == null) return;
            if (!TryGetActiveOriginCityId(out Guid originCityId))
            {
                _marchStatusLabel.text = "The active origin city could not be verified.";
                return;
            }

            var selections = GetSelectedUnits();
            if (selections.Count == 0)
            {
                _marchStatusLabel.text = "Select at least one unit.";
                return;
            }

            _deploymentRequestInFlight = true;
            _lastDeploymentError = null;
            _marchStatusLabel.text = _selectedMissionType == UnitDeploymentTypeEnum.Attack
                ? "Sending attack order..."
                : "Sending support order...";
            SetMarchControlsEnabled(false);

            var attackRequest = new AttackCityDeploymentRequestDTO
            {
                OriginCityId = originCityId,
                TargetCityId = _currentInspectionData.CityId,
                UnitsToDeploy = selections
            };
            var request = _selectedMissionType == UnitDeploymentTypeEnum.Attack
                ? NetworkManager.Instance.UnitDeployment.AttackCityDeployment(attackRequest, NetworkManager.Instance.JwtToken, HandleResponse, HandleError)
                : NetworkManager.Instance.UnitDeployment.SupportCityDeployment(new SupportCityDeploymentRequestDTO
                {
                    OriginCityId = attackRequest.OriginCityId,
                    TargetCityId = attackRequest.TargetCityId,
                    UnitsToDeploy = attackRequest.UnitsToDeploy
                }, NetworkManager.Instance.JwtToken, HandleResponse, HandleError);
            StartCoroutine(request);

            void HandleResponse(UnitDeploymentDTO response)
            {
                if (!isActiveAndEnabled) return;
                _deploymentRequestInFlight = false;
                if (response == null)
                {
                    _marchStatusLabel.text = string.IsNullOrWhiteSpace(_lastDeploymentError)
                        ? "The server rejected the deployment order."
                        : _lastDeploymentError;
                    SetMarchControlsEnabled(true);
                    return;
                }

                CityStateManager.Instance.RequestImmediateRefresh(originCityId);
                Close();
            }

            void HandleError(string message)
            {
                if (isActiveAndEnabled) _lastDeploymentError = message;
            }
        }

        private void SetMarchControlsEnabled(bool enabled)
        {
            foreach (var input in _marchQuantityInputs.Values) input.SetEnabled(enabled);
            WindowAsyncStateHelper.SetButtonsEnabled(_marchMaxButtons, enabled);
            WindowAsyncStateHelper.SetButtonsEnabled(new[]
            {
                _marchSubmitButton,
                _cityInfoTab,
                _attackCityTab,
                _supportCityTab,
                _closeButton
            }, enabled);
        }

        private void HandleMoveHereClicked()
        {
            if (_currentInspectionData == null) return;
            var renderer = FindFirstObjectByType<WorldMapRenderer>();
            renderer?.CenterCameraOnCoordinates(_currentInspectionData.X, _currentInspectionData.Y);
        }

        private void HandleCombatSimulatorClicked()
        {
            if (_currentInspectionData == null)
            {
                return;
            }

            if (!TryGetActiveOriginCityId(out Guid originCityId))
            {
                return;
            }

            WindowNavigationHelper.OpenCombatSimulator(new CombatSimulatorPayload
            {
                OriginCityId = originCityId,
                OriginCityName = CityStateManager.Instance.CurrentCityName,
                OriginCoordinates = new Vector2Int(CityStateManager.Instance.HomeCityX, CityStateManager.Instance.HomeCityY),
                TargetCityId = _currentInspectionData.CityId,
                TargetCityName = _currentInspectionData.CityName,
                TargetCoordinates = new Vector2Int(_currentInspectionData.X, _currentInspectionData.Y),
                TerrainName = _currentPayload?.TerrainName
            });
        }

        private static bool TryGetActiveOriginCityId(out Guid originCityId)
        {
            originCityId = NetworkManager.Instance?.ActiveCityId ?? Guid.Empty;
            return originCityId != Guid.Empty
                && CityStateManager.Instance != null
                && CityStateManager.Instance.CityId == originCityId;
        }

        private void LoadIncomingAttacks(CityInspectionDTO city)
        {
            _incomingAttacksContainer?.AddToClassList("hidden");
            _incomingAttacksList?.Clear();
            if (NetworkManager.Instance == null
                || !Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid currentWorldPlayerId)
                || city.WorldPlayerId != currentWorldPlayerId)
            {
                return;
            }

            int version = _requestVersion;
            StartCoroutine(NetworkManager.Instance.UnitDeployment.GetIncomingAttacks(
                currentWorldPlayerId,
                NetworkManager.Instance.JwtToken,
                attacks =>
                {
                    if (!isActiveAndEnabled || version != _requestVersion || attacks == null) return;
                    var cityAttacks = attacks.FindAll(attack => attack.TargetCityId == city.CityId);
                    if (cityAttacks.Count == 0) return;
                    foreach (var attack in cityAttacks)
                    {
                        _incomingAttacksList.Add(new Label(
                            $"{attack.SenderWorldPlayerName} -> {attack.TargetCityName} ({attack.TargetX}, {attack.TargetY}) | {attack.ArrivalTime.ToLocalTime():dd:MM:yyyy:HH:mm:ss}"));
                    }
                    _incomingAttacksContainer.RemoveFromClassList("hidden");
                }));
        }

        private void TryCompleteDeferredOpen()
        {
            if (!_isLayoutReady || !_hasLoadedInspectionData)
            {
                return;
            }

            CompleteDeferredOpen(_requestVersion);
        }
    }
}
