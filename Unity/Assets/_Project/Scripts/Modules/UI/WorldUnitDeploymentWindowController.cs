using Project.Modules.City;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Modules.UI;
using Assets.Scripts.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Network.Models;
using Assets._Project.Scripts.Domain.Enums;

namespace Project.Modules.UI.Windows.Implementations
{
    public class WorldUnitDeploymentWindowController : BaseWindow
    {
        protected override string WindowName => "WorldUnitDeploymentWindow";
        protected override string VisualContainerName => "Deployment-Window-MainContainer";
        protected override string HeaderName => "Deployment-Window-Header";

        private Label _statusLabel;
        private Label _travelDurationLabel;
        private Label _arrivalTimeLabel;
        private VisualElement _unitSummaryContainer;
        private Button _attackButton;
        private Button _supportButton;
        private Button _closeButton;
        private readonly List<Button> _maxButtons = new();
        private CityDeploymentPayload _payload;
        private readonly Dictionary<UnitTypeEnum, IntegerField> _quantityInputs = new();
        private bool _requestInFlight;
        private int _requestVersion;
        private int _estimateVersion;
        private string _lastRequestError;
        private bool _isLayoutReady;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;

            CacheElements();
            SetupEvents();

            if (dataPayload is not CityDeploymentPayload payload || payload.TargetCityId == Guid.Empty)
            {
                SetStatus("Invalid target city.");
                CompleteDeferredOpen(version);
                return;
            }

            _payload = payload;
            _isLayoutReady = false;
            if (MainContainer != null)
            {
                MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
                MainContainer.RegisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
            }
            RenderPayloadPreview();
            SetStatus(string.Empty);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            MainContainer?.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);

            if (_attackButton != null)
            {
                _attackButton.clicked -= ExecuteAttack;
            }

            if (_supportButton != null) _supportButton.clicked -= ExecuteSupport;
        }

        private void HandleInitialCenteringGeometryCallback(GeometryChangedEvent evt)
        {
            if (_isLayoutReady || MainContainer == null || MainContainer.parent == null) return;

            float parentWidth = MainContainer.parent.resolvedStyle.width;
            float parentHeight = MainContainer.parent.resolvedStyle.height;
            if (float.IsNaN(parentWidth) || float.IsNaN(parentHeight)
                || evt.newRect.width <= 0 || evt.newRect.height <= 0)
            {
                return;
            }

            MainContainer.style.position = Position.Absolute;
            MainContainer.style.left = Mathf.Max(0, (parentWidth - evt.newRect.width) / 2f);
            MainContainer.style.top = Mathf.Max(0, (parentHeight - evt.newRect.height) / 2f);
            _isLayoutReady = true;
            MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
            CompleteDeferredOpen(_requestVersion);
        }

        private void CacheElements()
        {
            _statusLabel = Root.Q<Label>("Deployment-Status-Label");
            _unitSummaryContainer = Root.Q<VisualElement>("Deployment-Units-Container");
            _travelDurationLabel = Root.Q<Label>("Deployment-Travel-Duration-Label");
            _arrivalTimeLabel = Root.Q<Label>("Deployment-Arrival-Time-Label");
            _closeButton = Root.Q<Button>($"{WindowName}-Close-Button");
        }

        private void SetupEvents()
        {
            _attackButton = Root.Q<Button>("Btn-Confirm-Attack");
            _supportButton = Root.Q<Button>("Btn-Confirm-Support");

            if (_attackButton != null)
            {
                _attackButton.clicked -= ExecuteAttack;
                _attackButton.clicked += ExecuteAttack;
            }
            if (_supportButton != null)
            {
                _supportButton.clicked -= ExecuteSupport;
                _supportButton.clicked += ExecuteSupport;
            }

        }

        private void RenderPayloadPreview()
        {
            if (_payload == null)
            {
                return;
            }

            SetText("Source-City-Label", ResolveOriginCityName());
            SetText("Destination-Label", string.IsNullOrWhiteSpace(_payload.TargetCityName)
                ? $"{_payload.TargetCityId}"
                : _payload.TargetCityName);
            SetText("Deployment-Coordinates-Label", _payload.TargetCoordinates.HasValue
                ? $"{_payload.TargetCoordinates.Value.x}, {_payload.TargetCoordinates.Value.y}"
                : "-");
            SetText("Deployment-Terrain-Label", string.IsNullOrWhiteSpace(_payload.TerrainName) ? "-" : _payload.TerrainName);

            if (_unitSummaryContainer == null)
            {
                return;
            }

            _unitSummaryContainer.Clear();
            _quantityInputs.Clear();
            _maxButtons.Clear();
            if (_attackButton != null) _attackButton.style.display = _payload.CanAttack ? DisplayStyle.Flex : DisplayStyle.None;
            if (_supportButton != null) _supportButton.style.display = _payload.CanSupport ? DisplayStyle.Flex : DisplayStyle.None;

            var stationedUnits = (CityStateManager.Instance?.CurrentStationedUnits ?? new List<UnitStackDTO>())
                .Where(stack => stack.Quantity > 0)
                .ToList();
            if (stationedUnits.Count == 0)
            {
                var emptyLabel = new Label("NO STATIONED UNITS AVAILABLE") { name = "Deployment-Units-Empty" };
                emptyLabel.AddToClassList("deployment-empty");
                _unitSummaryContainer.Add(emptyLabel);
                return;
            }

            foreach (var unitStack in stationedUnits)
            {
                var row = new VisualElement();
                row.AddToClassList("deployment-unit-row");
                var unitName = new Label(unitStack.Type.ToString());
                unitName.AddToClassList("deployment-unit-name");
                unitName.AddToClassList("deployment-unit-label");
                row.Add(unitName);
                var available = new Label(unitStack.Quantity.ToString());
                available.AddToClassList("deployment-unit-available");
                available.AddToClassList("deployment-unit-label");
                row.Add(available);
                var quantity = new IntegerField { value = 0, name = $"Quantity-{unitStack.Type}" };
                quantity.AddToClassList("game-input");
                quantity.AddToClassList("deployment-unit-quantity");
                quantity.RegisterValueChangedCallback(change =>
                {
                    quantity.SetValueWithoutNotify(Math.Clamp(change.newValue, 0, unitStack.Quantity));
                    UpdateTotal();
                    RefreshTravelEstimate();
                });
                var max = new Button(() => { quantity.value = unitStack.Quantity; }) { text = "MAX" };
                max.AddToClassList("btn-global-base");
                max.AddToClassList("btn-imperial-primary");
                max.AddToClassList("deployment-max-button");
                row.Add(quantity);
                row.Add(max);
                _maxButtons.Add(max);
                _quantityInputs[unitStack.Type] = quantity;
                _unitSummaryContainer.Add(row);
            }
        }

        private string ResolveOriginCityName()
        {
            if (CityStateManager.Instance == null || CityStateManager.Instance.CityId == Guid.Empty)
            {
                return "ACTIVE CITY";
            }

            return CityStateManager.Instance.CurrentCityName ?? "ACTIVE CITY";
        }

        private void ExecuteAttack()
        {
            ExecuteDeployment(UnitDeploymentTypeEnum.Attack);
        }

        private void ExecuteSupport()
        {
            ExecuteDeployment(UnitDeploymentTypeEnum.Support);
        }

        private void ExecuteDeployment(UnitDeploymentTypeEnum missionType)
        {
            if (_requestInFlight) return;
            if (_payload == null)
            {
                SetStatus("Missing attack payload.");
                return;
            }

            if (NetworkManager.Instance == null)
            {
                SetStatus("Network is unavailable.");
                return;
            }

            if (CityStateManager.Instance == null || CityStateManager.Instance.CityId == Guid.Empty)
            {
                SetStatus("No active origin city selected.");
                return;
            }

            var stationedUnits = _quantityInputs
                .Where(input => input.Value.value > 0)
                .Select(input => new UnitSelectionDTO { Type = input.Key, Quantity = input.Value.value })
                .ToList() ?? new List<UnitSelectionDTO>();

            if (stationedUnits.Count == 0)
            {
                SetStatus("No stationed units available.");
                return;
            }

            var attackRequest = new AttackCityDeploymentRequestDTO
            {
                OriginCityId = CityStateManager.Instance.CityId,
                TargetCityId = _payload.TargetCityId,
                UnitsToDeploy = stationedUnits
            };

            SetStatus(missionType == UnitDeploymentTypeEnum.Attack
                ? "Sending attack order..."
                : "Sending support order...");
            _requestInFlight = true;
            _lastRequestError = null;
            SetRequestControlsEnabled(false);

            var request = missionType == UnitDeploymentTypeEnum.Attack
                ? NetworkManager.Instance.UnitDeployment.AttackCityDeployment(attackRequest, NetworkManager.Instance.JwtToken, HandleResponse, HandleError)
                : NetworkManager.Instance.UnitDeployment.SupportCityDeployment(new SupportCityDeploymentRequestDTO
                {
                    OriginCityId = attackRequest.OriginCityId, TargetCityId = attackRequest.TargetCityId, UnitsToDeploy = attackRequest.UnitsToDeploy
                }, NetworkManager.Instance.JwtToken, HandleResponse, HandleError);
            StartCoroutine(request);

            void HandleResponse(UnitDeploymentDTO response)
            {
                if (!isActiveAndEnabled) return;
                _requestInFlight = false;
                if (response == null)
                {
                    SetStatus(string.IsNullOrWhiteSpace(_lastRequestError) ? "The server rejected the deployment order." : _lastRequestError);
                    SetRequestControlsEnabled(true);
                    return;
                }

                CityStateManager.Instance.InitiateResourceRefresh(CityStateManager.Instance.CityId);
                Close();
            }

            void HandleError(string message)
            {
                if (!isActiveAndEnabled) return;
                _lastRequestError = message;
            }
        }

        private void UpdateTotal()
        {
            SetText("Deployment-Total-Label", $"TOTAL: {_quantityInputs.Values.Sum(input => input.value)}");
        }

        private void RefreshTravelEstimate()
        {
            int version = ++_estimateVersion;
            var selections = _quantityInputs
                .Where(input => input.Value.value > 0)
                .Select(input => new UnitSelectionDTO { Type = input.Key, Quantity = input.Value.value })
                .ToList();
            if (selections.Count == 0 || CityStateManager.Instance == null || NetworkManager.Instance == null)
            {
                _travelDurationLabel.text = "--:--:--";
                _arrivalTimeLabel.text = "--";
                return;
            }

            StartCoroutine(NetworkManager.Instance.UnitDeployment.EstimateTravel(new DeploymentTravelEstimateRequestDTO
            {
                OriginCityId = CityStateManager.Instance.CityId,
                TargetCityId = _payload.TargetCityId,
                UnitsToDeploy = selections
            }, NetworkManager.Instance.JwtToken, estimate =>
            {
                if (!isActiveAndEnabled || version != _estimateVersion || estimate == null) return;
                long hours = estimate.DurationSeconds / 3600;
                long minutes = (estimate.DurationSeconds % 3600) / 60;
                long seconds = estimate.DurationSeconds % 60;
                _travelDurationLabel.text = $"{hours:00}:{minutes:00}:{seconds:00}";
                _arrivalTimeLabel.text = estimate.ArrivalTime.ToLocalTime().ToString("dd:MM:yyyy HH:mm:ss");
            }));
        }

        private void SetRequestControlsEnabled(bool enabled)
        {
            foreach (var input in _quantityInputs.Values) input.SetEnabled(enabled);
            WindowAsyncStateHelper.SetButtonsEnabled(_maxButtons, enabled);
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _attackButton, _supportButton, _closeButton }, enabled);
        }

        private void SetText(string elementName, string value)
        {
            var label = Root.Q<Label>(elementName);
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private void SetStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message ?? string.Empty;
            }
        }
    }

    public class CityDeploymentPayload
    {
        public Guid TargetCityId;
        public string TargetCityName;
        public Vector2Int? TargetCoordinates;
        public string TerrainName;
        public bool CanAttack;
        public bool CanSupport;
    }
}
