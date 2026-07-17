using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public class CombatSimulatorWindowController : BaseWindow
    {
        protected override string WindowName => "CombatSimulator";
        protected override string VisualContainerName => "Combat-Simulator-Window-MainContainer";
        protected override string HeaderName => "Combat-Simulator-Window-Header";

        private VisualElement _stateContainer;
        private VisualElement _contentContainer;
        private Label _originValueLabel;
        private Label _targetValueLabel;
        private Label _routeValueLabel;
        private Label _transportValueLabel;
        private Label _statusValueLabel;
        private Label _luckValueLabel;
        private Label _modifiersValueLabel;
        private VisualElement _attackerRowsContainer;
        private VisualElement _defenderRowsContainer;
        private VisualElement _resultContainer;
        private Button _simulateButton;
        private Button _addAttackerButton;
        private Button _addDefenderButton;
        private Button _clearAttackerButton;
        private Button _clearDefenderButton;
        private Button _useOriginArmyButton;
        private Button _closeButton;

        private CombatSimulatorPayload _payload;
        private bool _isLayoutReady;
        private int _requestVersion;
        private int _simulationVersion;
        private bool _requestInFlight;
        private string _lastRequestError;
        private readonly List<EditableUnitRow> _attackerRows = new();
        private readonly List<EditableUnitRow> _defenderRows = new();

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;

            CacheVisualElements();
            BindEvents();

            if (dataPayload is not CombatSimulatorPayload payload
                || payload.OriginCityId == Guid.Empty
                || payload.TargetCityId == Guid.Empty)
            {
                ShowErrorState("Invalid combat simulator payload.");
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

            ShowLoadingState();
            RenderPayloadPreview();
            BuildArmyEditors();
            ShowContentState();
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            if (MainContainer != null)
            {
                MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
            }

            UnbindEvents();
        }

        private void CacheVisualElements()
        {
            _stateContainer = Root.Q<VisualElement>("Combat-Simulator-State-Container");
            _contentContainer = Root.Q<VisualElement>("Combat-Simulator-Content-Container");
            _originValueLabel = Root.Q<Label>("Origin-City-Label");
            _targetValueLabel = Root.Q<Label>("Target-City-Label");
            _routeValueLabel = Root.Q<Label>("Route-Label");
            _transportValueLabel = Root.Q<Label>("Transport-Label");
            _statusValueLabel = Root.Q<Label>("Status-Label");
            _luckValueLabel = Root.Q<Label>("Luck-Label");
            _modifiersValueLabel = Root.Q<Label>("Modifiers-Label");
            _attackerRowsContainer = Root.Q<VisualElement>("Attacker-Units-Container");
            _defenderRowsContainer = Root.Q<VisualElement>("Defender-Units-Container");
            _resultContainer = Root.Q<VisualElement>("Simulation-Result-Container");
            _simulateButton = Root.Q<Button>("Simulate-Button");
            _addAttackerButton = Root.Q<Button>("Add-Attacker-Button");
            _addDefenderButton = Root.Q<Button>("Add-Defender-Button");
            _clearAttackerButton = Root.Q<Button>("Clear-Attacker-Button");
            _clearDefenderButton = Root.Q<Button>("Clear-Defender-Button");
            _useOriginArmyButton = Root.Q<Button>("Use-Origin-Army-Button");
            _closeButton = Root.Q<Button>($"{WindowName}-Close-Button");
        }

        private void BindEvents()
        {
            if (_simulateButton != null)
            {
                _simulateButton.clicked -= ExecuteSimulation;
                _simulateButton.clicked += ExecuteSimulation;
            }

            if (_addAttackerButton != null)
            {
                _addAttackerButton.clicked -= HandleAddAttackerClicked;
                _addAttackerButton.clicked += HandleAddAttackerClicked;
            }

            if (_addDefenderButton != null)
            {
                _addDefenderButton.clicked -= HandleAddDefenderClicked;
                _addDefenderButton.clicked += HandleAddDefenderClicked;
            }

            if (_clearAttackerButton != null)
            {
                _clearAttackerButton.clicked -= ClearAttackerRows;
                _clearAttackerButton.clicked += ClearAttackerRows;
            }

            if (_clearDefenderButton != null)
            {
                _clearDefenderButton.clicked -= ClearDefenderRows;
                _clearDefenderButton.clicked += ClearDefenderRows;
            }

            if (_useOriginArmyButton != null)
            {
                _useOriginArmyButton.clicked -= SeedAttackerArmyFromOrigin;
                _useOriginArmyButton.clicked += SeedAttackerArmyFromOrigin;
            }
        }

        private void UnbindEvents()
        {
            if (_simulateButton != null) _simulateButton.clicked -= ExecuteSimulation;
            if (_addAttackerButton != null) _addAttackerButton.clicked -= HandleAddAttackerClicked;
            if (_addDefenderButton != null) _addDefenderButton.clicked -= HandleAddDefenderClicked;
            if (_clearAttackerButton != null) _clearAttackerButton.clicked -= ClearAttackerRows;
            if (_clearDefenderButton != null) _clearDefenderButton.clicked -= ClearDefenderRows;
            if (_useOriginArmyButton != null) _useOriginArmyButton.clicked -= SeedAttackerArmyFromOrigin;
        }

        private void HandleInitialCenteringGeometryCallback(GeometryChangedEvent evt)
        {
            if (MainContainer == null || MainContainer.parent == null)
            {
                return;
            }

            float parentWidth = MainContainer.parent.resolvedStyle.width;
            float parentHeight = MainContainer.parent.resolvedStyle.height;
            if (float.IsNaN(parentWidth) || float.IsNaN(parentHeight) || evt.newRect.width <= 0 || evt.newRect.height <= 0)
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

        private void ShowLoadingState()
        {
            if (_stateContainer == null)
            {
                return;
            }

            _stateContainer.style.display = DisplayStyle.Flex;
            WindowAsyncStateHelper.ShowLoading(_stateContainer, "Loading combat simulator...");
        }

        private void ShowErrorState(string message)
        {
            if (_stateContainer == null)
            {
                return;
            }

            _stateContainer.style.display = DisplayStyle.Flex;
            WindowAsyncStateHelper.ShowError(_stateContainer, message);
            if (_contentContainer != null)
            {
                _contentContainer.style.display = DisplayStyle.None;
            }
        }

        private void ShowContentState()
        {
            if (_stateContainer != null)
            {
                _stateContainer.style.display = DisplayStyle.None;
                WindowAsyncStateHelper.Clear(_stateContainer);
            }

            if (_contentContainer != null)
            {
                _contentContainer.style.display = DisplayStyle.Flex;
            }
        }

        private void RenderPayloadPreview()
        {
            if (_payload == null)
            {
                return;
            }

            _originValueLabel.text = ResolveCityLabel(_payload.OriginCityName, _payload.OriginCityId);
            _targetValueLabel.text = ResolveCityLabel(_payload.TargetCityName, _payload.TargetCityId);
            _routeValueLabel.text = ResolveRouteLabel();
            _transportValueLabel.text = "Waiting for simulation...";
            _statusValueLabel.text = "Build or adjust both armies, then simulate.";
            _luckValueLabel.text = "-";
            _modifiersValueLabel.text = "-";
            ClearResults();
        }

        private string ResolveCityLabel(string name, Guid id)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return id == Guid.Empty ? "-" : id.ToString();
        }

        private string ResolveRouteLabel()
        {
            if (_payload?.OriginCoordinates.HasValue != true && _payload?.TargetCoordinates.HasValue != true)
            {
                return "-";
            }

            string origin = _payload?.OriginCoordinates.HasValue == true
                ? $"{_payload.OriginCoordinates.Value.x}, {_payload.OriginCoordinates.Value.y}"
                : "-";
            string target = _payload?.TargetCoordinates.HasValue == true
                ? $"{_payload.TargetCoordinates.Value.x}, {_payload.TargetCoordinates.Value.y}"
                : "-";
            return $"{origin} -> {target}";
        }

        private void BuildArmyEditors()
        {
            ClearRows();
            SeedAttackerArmyFromOrigin();
            if (_defenderRows.Count == 0)
            {
                AddRow(_defenderRowsContainer, _defenderRows, UnitTypeEnum.Militia, 1);
            }
        }

        private void SeedAttackerArmyFromOrigin()
        {
            ClearAttackerRows();

            var originStacks = CityStateManager.Instance?.CurrentStationedUnits?
                .Where(stack => stack.Quantity > 0)
                .Select(stack => (stack.Type, stack.Quantity))
                .ToList() ?? new List<(UnitTypeEnum Type, int Quantity)>();

            if (originStacks.Count == 0)
            {
                AddRow(_attackerRowsContainer, _attackerRows, UnitTypeEnum.Militia, 1);
                return;
            }

            foreach (var stack in originStacks)
            {
                AddRow(_attackerRowsContainer, _attackerRows, stack.Type, stack.Quantity);
            }
        }

        private void ClearRows()
        {
            _attackerRows.Clear();
            _defenderRows.Clear();
            _attackerRowsContainer?.Clear();
            _defenderRowsContainer?.Clear();
        }

        private void ClearAttackerRows()
        {
            _attackerRows.Clear();
            _attackerRowsContainer?.Clear();
        }

        private void ClearDefenderRows()
        {
            _defenderRows.Clear();
            _defenderRowsContainer?.Clear();
            AddRow(_defenderRowsContainer, _defenderRows, UnitTypeEnum.Militia, 1);
        }

        private void HandleAddAttackerClicked()
        {
            AddRow(_attackerRowsContainer, _attackerRows, UnitTypeEnum.Militia, 1);
        }

        private void HandleAddDefenderClicked()
        {
            AddRow(_defenderRowsContainer, _defenderRows, UnitTypeEnum.Militia, 1);
        }

        private void AddRow(VisualElement container, List<EditableUnitRow> rows, UnitTypeEnum initialType, int quantity)
        {
            if (container == null)
            {
                return;
            }

            var row = new VisualElement();
            row.AddToClassList("sim-unit-row");

            var typeField = new DropdownField(BuildUnitChoices(), initialType.ToString());
            typeField.AddToClassList("sim-unit-type");

            var quantityField = new IntegerField { value = Mathf.Max(0, quantity) };
            quantityField.AddToClassList("game-input");
            quantityField.AddToClassList("sim-unit-quantity");

            var removeButton = new Button { text = "X" };
            removeButton.AddToClassList("btn-global-base");
            removeButton.AddToClassList("btn-imperial-primary");
            removeButton.AddToClassList("sim-unit-remove");

            var editableRow = new EditableUnitRow
            {
                Root = row,
                TypeField = typeField,
                QuantityField = quantityField,
                RemoveButton = removeButton,
                SelectedType = initialType
            };

            typeField.RegisterValueChangedCallback(evt => editableRow.SelectedType = ParseUnitType(evt.newValue));
            quantityField.RegisterValueChangedCallback(evt =>
            {
                quantityField.SetValueWithoutNotify(Mathf.Max(0, evt.newValue));
                RefreshSimulationState();
            });
            removeButton.clicked += () =>
            {
                rows.Remove(editableRow);
                row.RemoveFromHierarchy();
                RefreshSimulationState();
            };

            row.Add(typeField);
            row.Add(quantityField);
            row.Add(removeButton);
            rows.Add(editableRow);
            container.Add(row);
            RefreshSimulationState();
        }

        private List<string> BuildUnitChoices()
        {
            return Enum.GetValues(typeof(UnitTypeEnum))
                .Cast<UnitTypeEnum>()
                .Where(unit => unit != UnitTypeEnum.None)
                .Select(unit => unit.ToString())
                .ToList();
        }

        private int GetChoiceIndex(UnitTypeEnum unitType)
        {
            var choices = BuildUnitChoices();
            return Math.Max(0, choices.FindIndex(x => string.Equals(x, unitType.ToString(), StringComparison.Ordinal)));
        }

        private UnitTypeEnum ParseUnitType(string value)
        {
            return Enum.TryParse(value, out UnitTypeEnum parsed) ? parsed : UnitTypeEnum.Militia;
        }

        private void RefreshSimulationState()
        {
            int attackerTotal = _attackerRows.Sum(row => Mathf.Max(0, row.QuantityField?.value ?? 0));
            int defenderTotal = _defenderRows.Sum(row => Mathf.Max(0, row.QuantityField?.value ?? 0));
            _statusValueLabel.text = $"ATTACKERS: {attackerTotal} | DEFENDERS: {defenderTotal}";
            _simulateButton?.SetEnabled(attackerTotal > 0 && defenderTotal > 0 && !_requestInFlight);
            _useOriginArmyButton?.SetEnabled(!_requestInFlight);
            _clearAttackerButton?.SetEnabled(!_requestInFlight);
            _clearDefenderButton?.SetEnabled(!_requestInFlight);
            _addAttackerButton?.SetEnabled(!_requestInFlight);
            _addDefenderButton?.SetEnabled(!_requestInFlight);
        }

        private List<UnitSelectionDTO> CollectSelections(List<EditableUnitRow> rows)
        {
            return rows
                .Select(row => new UnitSelectionDTO
                {
                    Type = row.SelectedType,
                    Quantity = Mathf.Max(0, row.QuantityField?.value ?? 0)
                })
                .Where(selection => selection.Quantity > 0 && selection.Type != UnitTypeEnum.None)
                .ToList();
        }

        private void ExecuteSimulation()
        {
            if (_requestInFlight || _payload == null || NetworkManager.Instance == null)
            {
                return;
            }

            var attackerUnits = CollectSelections(_attackerRows);
            var defenderUnits = CollectSelections(_defenderRows);
            if (attackerUnits.Count == 0 || defenderUnits.Count == 0)
            {
                _statusValueLabel.text = "Add at least one unit to both sides.";
                return;
            }

            _requestInFlight = true;
            _simulationVersion++;
            _lastRequestError = null;
            SetInputsEnabled(false);
            _statusValueLabel.text = "Running combat simulation...";
            ClearResults();

            StartCoroutine(NetworkManager.Instance.CombatSimulator.Simulate(
                new CombatSimulationRequestDTO
                {
                    OriginCityId = _payload.OriginCityId,
                    TargetCityId = _payload.TargetCityId,
                    AttackerUnits = attackerUnits,
                    DefenderUnits = defenderUnits
                },
                NetworkManager.Instance.JwtToken,
                result =>
                {
                    if (!isActiveAndEnabled)
                    {
                        return;
                    }

                    _requestInFlight = false;
                    SetInputsEnabled(true);

                    if (result == null)
                    {
                        _statusValueLabel.text = string.IsNullOrWhiteSpace(_lastRequestError)
                            ? "Combat simulation failed."
                            : _lastRequestError;
                        RefreshSimulationState();
                        return;
                    }

                    RenderSimulationResult(result);
                    RefreshSimulationState();
                },
                errorMessage =>
                {
                    if (isActiveAndEnabled)
                    {
                        _lastRequestError = errorMessage;
                    }
                }));
        }

        private void SetInputsEnabled(bool enabled)
        {
            foreach (var row in _attackerRows.Concat(_defenderRows))
            {
                row.TypeField?.SetEnabled(enabled);
                row.QuantityField?.SetEnabled(enabled);
                row.RemoveButton?.SetEnabled(enabled);
            }

            _simulateButton?.SetEnabled(enabled);
            _addAttackerButton?.SetEnabled(enabled);
            _addDefenderButton?.SetEnabled(enabled);
            _clearAttackerButton?.SetEnabled(enabled);
            _clearDefenderButton?.SetEnabled(enabled);
            _useOriginArmyButton?.SetEnabled(enabled);
            _closeButton?.SetEnabled(enabled);
        }

        private void RenderSimulationResult(CombatSimulationResultDTO result)
        {
            _statusValueLabel.text = result.HasSufficientTransportCapacity
                ? "Simulation complete."
                : "Simulation returned without sufficient transport capacity.";

            _transportValueLabel.text = result.RequiresTransport
                ? (result.HasSufficientTransportCapacity
                    ? $"Transport capacity {result.RequiredTransportCapacity}/{result.AvailableTransportCapacity} (margin {result.TransportCapacityMargin})"
                    : $"Insufficient transport capacity: need {result.RequiredTransportCapacity}, have {result.AvailableTransportCapacity}.")
                : "No sea transport required.";
            _luckValueLabel.text = result.LuckModifier.ToString("0.00");
            _modifiersValueLabel.text = result.AppliedModifiers != null && result.AppliedModifiers.Count > 0
                ? string.Join("\n", result.AppliedModifiers)
                : "-";

            ClearResults();
            AddResultSection("REMAINING ATTACKERS", result.RemainingAttackers?.Select(stack => (stack.Type, stack.Quantity)).ToList());
            AddResultSection("REMAINING DEFENDERS", result.RemainingDefenders?.Select(stack => (stack.Type, stack.Quantity)).ToList());
            AddResultSection("ATTACKER LOSSES", result.AttackerLosses?.Select(stack => (stack.Type, stack.Quantity)).ToList());
            AddResultSection("DEFENDER LOSSES", result.DefenderLosses?.Select(stack => (stack.Type, stack.Quantity)).ToList());
            AddResultSection("REVIVED DEFENDERS", result.RevivedDefenders?.Select(stack => (stack.Type, stack.Quantity)).ToList());
        }

        private void ClearResults()
        {
            _resultContainer?.Clear();
        }

        private void AddResultSection(string title, List<(UnitTypeEnum Type, int Quantity)> stacks)
        {
            if (_resultContainer == null)
            {
                return;
            }

            var section = new VisualElement();
            section.AddToClassList("sim-result-section");

            section.Add(new Label(title) { name = "sim-result-title" });

            if (stacks == null || stacks.Count == 0)
            {
                section.Add(new Label("NONE"));
                _resultContainer.Add(section);
                return;
            }

            foreach (var stack in stacks)
            {
                var row = new VisualElement();
                row.AddToClassList("sim-result-card");
                row.Add(new Label(stack.Type.ToString().ToUpperInvariant()));
                row.Add(new Label($"QTY: {stack.Quantity}"));
                section.Add(row);
            }

            _resultContainer.Add(section);
        }

        private void RenderPayloadPreviewAndState()
        {
            RenderPayloadPreview();
            RefreshSimulationState();
        }

        private void TryCompleteDeferredOpen()
        {
            if (_isLayoutReady)
            {
                CompleteDeferredOpen(_requestVersion);
            }
        }
    }

    public class CombatSimulatorPayload
    {
        public Guid OriginCityId;
        public Guid TargetCityId;
        public string OriginCityName;
        public string TargetCityName;
        public Vector2Int? OriginCoordinates;
        public Vector2Int? TargetCoordinates;
        public string TerrainName;
    }

    internal sealed class EditableUnitRow
    {
        public VisualElement Root;
        public DropdownField TypeField;
        public IntegerField QuantityField;
        public Button RemoveButton;
        public UnitTypeEnum SelectedType { get; set; } = UnitTypeEnum.Militia;
    }
}
