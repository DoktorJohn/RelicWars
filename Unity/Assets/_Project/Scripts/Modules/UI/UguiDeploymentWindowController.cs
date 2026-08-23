using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.UI.Windows.Implementations;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiDeploymentWindowController : MonoBehaviour, IUguiWindowPayloadReceiver
    {
        private sealed class UnitCardBinding
        {
            public UnitTypeEnum Type;
            public TMP_Text Available;
            public TMP_InputField Input;
            public int AvailableQuantity;
        }

        private static readonly UnitTypeEnum[] AuthoredUnitTypes =
        {
            UnitTypeEnum.Militia,
            UnitTypeEnum.MenAtArms,
            UnitTypeEnum.Spearmen,
            UnitTypeEnum.Axemen,
            UnitTypeEnum.Swordsmen,
            UnitTypeEnum.LightCavalry,
            UnitTypeEnum.Knights,
            UnitTypeEnum.Cataphracts,
            UnitTypeEnum.Ballista,
            UnitTypeEnum.Catapult,
            UnitTypeEnum.Trebuchet,
            UnitTypeEnum.Cannon,
            UnitTypeEnum.Engineers,
            UnitTypeEnum.Longship,
            UnitTypeEnum.WarGalley,
            UnitTypeEnum.GrandTransport
        };

        private readonly List<UnitCardBinding> _cards = new();
        private TMP_Text _originCityLabel;
        private TMP_Text _destinationCityLabel;
        private TMP_Text _travelTimeText;
        private TMP_Text _arrivalTimeText;
        private TMP_Text _transportText;
        private CarvedPressButton _attackButton;
        private CarvedPressButton _supportButton;
        private CityDeploymentPayload _payload;
        private bool _requestInFlight;
        private bool _hasValidEstimate;
        private bool _synchronizingInputs;
        private int _estimateVersion;
        private int _lifecycleVersion;
        private string _lastRequestError;

        private void Awake()
        {
            _originCityLabel = FindComponent<TMP_Text>(transform, "OriginCityLabel");
            _destinationCityLabel = FindComponent<TMP_Text>(transform, "DestinationCityLabel");
            _travelTimeText = FindComponent<TMP_Text>(transform, "TravelTime text");
            _arrivalTimeText = FindComponent<TMP_Text>(transform, "ArrivalTime text");
            _transportText = FindComponent<TMP_Text>(transform, "TransportShips Text");
            _attackButton = FindComponent<CarvedPressButton>(transform, "AttackBtn");
            _supportButton = FindComponent<CarvedPressButton>(transform, "SupportBtn");
            BindUnitCards();
        }

        private void OnEnable()
        {
            if (_attackButton != null) _attackButton.OnButtonActivatedClicked += ExecuteAttack;
            if (_supportButton != null) _supportButton.OnButtonActivatedClicked += ExecuteSupport;
            foreach (UnitCardBinding card in _cards)
                if (card.Input != null) card.Input.onValueChanged.AddListener(OnQuantityChanged);
        }

        private void OnDisable()
        {
            _lifecycleVersion++;
            _estimateVersion++;
            StopAllCoroutines();
            if (_attackButton != null) _attackButton.OnButtonActivatedClicked -= ExecuteAttack;
            if (_supportButton != null) _supportButton.OnButtonActivatedClicked -= ExecuteSupport;
            foreach (UnitCardBinding card in _cards)
                if (card.Input != null) card.Input.onValueChanged.RemoveListener(OnQuantityChanged);
        }

        public void OnOpen(object payload)
        {
            _lifecycleVersion++;
            _estimateVersion++;
            _requestInFlight = false;
            _hasValidEstimate = false;
            _lastRequestError = null;
            _payload = payload as CityDeploymentPayload;

            if (_payload == null || _payload.TargetCityId == Guid.Empty)
            {
                Debug.LogError("[UguiDeploymentWindowController] Invalid deployment payload.", this);
                RenderInvalidState();
                return;
            }

            SetText(_originCityLabel, HasValidOrigin(out _) ? CityStateManager.Instance.CurrentCityName : "-");
            SetText(_destinationCityLabel, string.IsNullOrWhiteSpace(_payload.TargetCityName)
                ? _payload.TargetCityId.ToString()
                : _payload.TargetCityName);
            SetText(_travelTimeText, "--:--:--");
            SetText(_arrivalTimeText, "--");
            SetText(_transportText, "--");
            RenderUnitAvailability();
            RefreshActionState();
        }

        private void BindUnitCards()
        {
            foreach (UnitTypeEnum unitType in AuthoredUnitTypes)
            {
                Transform cardRoot = FindDescendant(transform, unitType.ToString());
                if (cardRoot == null)
                {
                    Debug.LogError($"[UguiDeploymentWindowController] Missing authored unit card '{unitType}'.", this);
                    continue;
                }

                TMP_InputField input = FindComponent<TMP_InputField>(cardRoot, "UnitAmountInputField");
                if (input != null)
                {
                    input.contentType = TMP_InputField.ContentType.IntegerNumber;
                    input.characterValidation = TMP_InputField.CharacterValidation.Integer;
                }

                _cards.Add(new UnitCardBinding
                {
                    Type = unitType,
                    Available = FindComponent<TMP_Text>(cardRoot, "Amount"),
                    Input = input
                });
            }
        }

        private void RenderUnitAvailability()
        {
            Dictionary<UnitTypeEnum, int> stationed = (CityStateManager.Instance?.CurrentStationedUnits
                    ?? new List<UnitStackDTO>())
                .GroupBy(stack => stack.Type)
                .ToDictionary(group => group.Key, group => group.Sum(stack => Math.Max(0, stack.Quantity)));

            _synchronizingInputs = true;
            foreach (UnitCardBinding card in _cards)
            {
                card.AvailableQuantity = stationed.TryGetValue(card.Type, out int available) ? available : 0;
                SetText(card.Available, card.AvailableQuantity.ToString("N0"));
                card.Input?.SetTextWithoutNotify("0");
                SetInputEnabled(card.Input, card.AvailableQuantity > 0 && HasValidOrigin(out _));
            }
            _synchronizingInputs = false;
        }

        private void OnQuantityChanged(string _)
        {
            if (_synchronizingInputs || _requestInFlight) return;
            ClampInputs();
            RefreshTravelEstimate();
        }

        private void ClampInputs()
        {
            _synchronizingInputs = true;
            foreach (UnitCardBinding card in _cards)
            {
                int quantity = ParseQuantity(card.Input);
                quantity = Mathf.Clamp(quantity, 0, card.AvailableQuantity);
                card.Input?.SetTextWithoutNotify(quantity.ToString(CultureInfo.InvariantCulture));
            }
            _synchronizingInputs = false;
        }

        private void RefreshTravelEstimate()
        {
            int estimateVersion = ++_estimateVersion;
            _hasValidEstimate = false;
            List<UnitSelectionDTO> selections = GetSelections();
            if (selections.Count == 0 || !HasValidOrigin(out Guid originCityId) || _payload == null)
            {
                SetText(_travelTimeText, "--:--:--");
                SetText(_arrivalTimeText, "--");
                SetText(_transportText, "--");
                RefreshActionState();
                return;
            }

            NetworkManager network = NetworkManager.Instance;
            if (network?.UnitDeployment == null)
            {
                RefreshActionState();
                return;
            }

            int lifecycleVersion = _lifecycleVersion;
            StartCoroutine(network.UnitDeployment.EstimateTravel(new DeploymentTravelEstimateRequestDTO
            {
                OriginCityId = originCityId,
                TargetCityId = _payload.TargetCityId,
                UnitsToDeploy = selections
            }, network.JwtToken, estimate =>
            {
                if (!CanApply(lifecycleVersion) || estimateVersion != _estimateVersion || estimate == null) return;
                long hours = estimate.DurationSeconds / 3600;
                long minutes = estimate.DurationSeconds % 3600 / 60;
                long seconds = estimate.DurationSeconds % 60;
                SetText(_travelTimeText, $"{hours:00}:{minutes:00}:{seconds:00}");
                SetText(_arrivalTimeText, estimate.ArrivalTime.ToLocalTime().ToString("dd:MM:yyyy HH:mm:ss"));
                SetText(_transportText, estimate.RequiresTransport
                    ? $"{estimate.RequiredTransportCapacity:N0} / {estimate.AvailableTransportCapacity:N0}"
                    : "NOT REQUIRED");
                _hasValidEstimate = estimate.HasSufficientTransportCapacity;
                RefreshActionState();
            }));
            RefreshActionState();
        }

        private void ExecuteAttack(CarvedPressButton _) => ExecuteDeployment(UnitDeploymentTypeEnum.Attack);
        private void ExecuteSupport(CarvedPressButton _) => ExecuteDeployment(UnitDeploymentTypeEnum.Support);

        private void ExecuteDeployment(UnitDeploymentTypeEnum type)
        {
            if (_requestInFlight || !_hasValidEstimate || _payload == null) return;
            if (!HasValidOrigin(out Guid originCityId)) return;
            if (type == UnitDeploymentTypeEnum.Attack && !_payload.CanAttack) return;
            if (type == UnitDeploymentTypeEnum.Support && !_payload.CanSupport) return;

            List<UnitSelectionDTO> selections = GetSelections();
            NetworkManager network = NetworkManager.Instance;
            if (selections.Count == 0 || network?.UnitDeployment == null) return;

            _requestInFlight = true;
            _lastRequestError = null;
            SetControlsEnabled(false);
            int lifecycleVersion = _lifecycleVersion;
            var request = type == UnitDeploymentTypeEnum.Attack
                ? network.UnitDeployment.AttackCityDeployment(new AttackCityDeploymentRequestDTO
                {
                    OriginCityId = originCityId,
                    TargetCityId = _payload.TargetCityId,
                    UnitsToDeploy = selections
                }, network.JwtToken, HandleResponse, HandleError)
                : network.UnitDeployment.SupportCityDeployment(new SupportCityDeploymentRequestDTO
                {
                    OriginCityId = originCityId,
                    TargetCityId = _payload.TargetCityId,
                    UnitsToDeploy = selections
                }, network.JwtToken, HandleResponse, HandleError);
            StartCoroutine(request);

            void HandleResponse(UnitDeploymentDTO response)
            {
                if (!CanApply(lifecycleVersion)) return;
                _requestInFlight = false;
                if (response == null)
                {
                    Debug.LogError(string.IsNullOrWhiteSpace(_lastRequestError)
                        ? "[UguiDeploymentWindowController] The server rejected the deployment order."
                        : $"[UguiDeploymentWindowController] {_lastRequestError}", this);
                    SetControlsEnabled(true);
                    RefreshActionState();
                    return;
                }

                CityStateManager.Instance?.RequestImmediateRefresh(originCityId);
                UguiWindowHostController.Instance?.CloseActiveWindow();
            }

            void HandleError(string message)
            {
                if (CanApply(lifecycleVersion)) _lastRequestError = message;
            }
        }

        private List<UnitSelectionDTO> GetSelections() => _cards
            .Select(card => new UnitSelectionDTO { Type = card.Type, Quantity = ParseQuantity(card.Input) })
            .Where(selection => selection.Quantity > 0)
            .ToList();

        private void RefreshActionState()
        {
            bool canSubmit = !_requestInFlight && _hasValidEstimate && GetSelections().Count > 0 && HasValidOrigin(out _);
            SetButtonVisible(_attackButton, _payload?.CanAttack == true);
            SetButtonVisible(_supportButton, _payload?.CanSupport == true);
            SetButtonEnabled(_attackButton, canSubmit && _payload?.CanAttack == true);
            SetButtonEnabled(_supportButton, canSubmit && _payload?.CanSupport == true);
        }

        private void SetControlsEnabled(bool enabled)
        {
            bool hasValidOrigin = HasValidOrigin(out _);
            foreach (UnitCardBinding card in _cards)
                SetInputEnabled(card.Input, enabled && hasValidOrigin && card.AvailableQuantity > 0);
            SetButtonEnabled(_attackButton, enabled && hasValidOrigin && _hasValidEstimate && _payload?.CanAttack == true);
            SetButtonEnabled(_supportButton, enabled && hasValidOrigin && _hasValidEstimate && _payload?.CanSupport == true);
        }

        private void RenderInvalidState()
        {
            SetText(_originCityLabel, "-");
            SetText(_destinationCityLabel, "-");
            SetText(_travelTimeText, "--:--:--");
            SetText(_arrivalTimeText, "--");
            SetText(_transportText, "--");
            SetButtonVisible(_attackButton, false);
            SetButtonVisible(_supportButton, false);
        }

        private static int ParseQuantity(TMP_InputField input) =>
            input != null && int.TryParse(input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? Math.Max(0, value)
                : 0;

        private static bool HasValidOrigin(out Guid originCityId)
        {
            originCityId = NetworkManager.Instance?.ActiveCityId ?? Guid.Empty;
            return originCityId != Guid.Empty
                && CityStateManager.Instance != null
                && CityStateManager.Instance.CityId == originCityId;
        }

        private bool CanApply(int lifecycleVersion) => isActiveAndEnabled && lifecycleVersion == _lifecycleVersion;

        private static void SetButtonVisible(CarvedPressButton button, bool visible)
        {
            if (button != null) button.gameObject.SetActive(visible);
        }

        private static void SetButtonEnabled(CarvedPressButton button, bool enabled)
        {
            if (button == null) return;
            button.enabled = enabled;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = enabled ? 1f : 0.5f;
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }

        private static void SetInputEnabled(TMP_InputField input, bool enabled)
        {
            if (input != null) input.interactable = enabled;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }

        private static T FindComponent<T>(Transform root, string objectName) where T : Component =>
            FindDescendant(root, objectName)?.GetComponent<T>();

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
