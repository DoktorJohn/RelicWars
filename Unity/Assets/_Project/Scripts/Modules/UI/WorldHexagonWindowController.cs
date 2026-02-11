using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Modules.UI.Windows;
using Project.Modules.City;
using Project.Scripts.Modules.Map;
using Project.Scripts.Domain.DTOs;
using Project.Network.Manager;
using Assets.Scripts.Domain.Enums;

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

    public class WorldHexagonWindowController : BaseWindow
    {
        protected override string WindowName => "WorldHexagonWindow";
        protected override string VisualContainerName => "Hexagon-Window-MainContainer";
        protected override string HeaderName => "Hexagon-Window-Header";

        [SerializeField] private VisualTreeAsset _unitRowTemplate;

        private VisualElement _unitDeploymentViewContainer;
        private VisualElement _primaryActionContainer;
        private ScrollView _availableUnitsScrollView;

        private MapInteractionPayload _currentInteractionPayload;
        private List<UnitSelectionDTO> _currentlySelectedUnitsToDeploy = new List<UnitSelectionDTO>();

        public override void OnOpen(object dataPayload)
        {
            if (dataPayload is MapInteractionPayload interactionPayload)
            {
                _currentInteractionPayload = interactionPayload;

                CacheRequiredVisualElements();
                InitializeInteractionBlockingEvents();

                // 1. Gør vinduet usynligt i et millisekund mens vi beregner position
                MainContainer.style.visibility = Visibility.Hidden;

                // 2. Vi venter på at layout-motoren har beregnet vinduets størrelse
                MainContainer.RegisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);

                UpdateHexagonDisplayInformation();
                RefreshContextualActionButtons();
                ResetDeploymentViewToDefaultState();
            }
        }

        private void OnDisable()
        {
            if (WorldMapInteractionHandler.Instance != null)
            {
                WorldMapInteractionHandler.Instance.SetMouseOverUI(false);
            }

            // Sikkerhed: Fjern altid callback hvis vinduet lukkes før det når at centrere
            MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
        }

        /// <summary>
        /// OBJEKTIV FIX: Beregner centrerings-pixels manuelt for at undgå 'translate' konflikten med dragger-komponenten.
        /// </summary>
        private void HandleInitialCenteringGeometryCallback(GeometryChangedEvent evt)
        {
            if (MainContainer == null || MainContainer.parent == null) return;

            // Vi finder forældre-containerens (skærmens) dimensioner
            float parentWidth = MainContainer.parent.resolvedStyle.width;
            float parentHeight = MainContainer.parent.resolvedStyle.height;

            // Vi finder vinduets egne ny-beregnede dimensioner
            float windowWidth = evt.newRect.width;
            float windowHeight = evt.newRect.height;

            if (float.IsNaN(parentWidth) || windowWidth <= 0) return;

            // Beregn præcis pixel-position for center
            float targetLeft = (parentWidth - windowWidth) / 2f;
            float targetTop = (parentHeight - windowHeight) / 2f;

            // Sæt positionen i hårde pixels. Dette format forstår din dragger 100%.
            MainContainer.style.position = Position.Absolute;
            MainContainer.style.left = targetLeft;
            MainContainer.style.top = targetTop;

            // VIGTIGT: Vi rører IKKE 'translate' her, da det er det der skaber hoppet.
            // Ved at lade translate være default (0), vil draggeren læse de korrekte pixels fra start.

            // Nu er vi klar til at vise vinduet
            MainContainer.style.visibility = Visibility.Visible;

            // Vi har kun brug for at gøre dette én gang ved åbning
            MainContainer.UnregisterCallback<GeometryChangedEvent>(HandleInitialCenteringGeometryCallback);
        }

        private void InitializeInteractionBlockingEvents()
        {
            if (MainContainer != null)
            {
                MainContainer.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    WorldMapInteractionHandler.Instance?.SetMouseOverUI(true);
                });

                MainContainer.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    WorldMapInteractionHandler.Instance?.SetMouseOverUI(false);
                });
            }
        }

        private void CacheRequiredVisualElements()
        {
            _unitDeploymentViewContainer = Root.Q<VisualElement>("Deploy-Unit-View");
            _primaryActionContainer = Root.Q<VisualElement>("Action-Container");
            _availableUnitsScrollView = Root.Q<ScrollView>("Unit-List-Container");
        }

        private void UpdateHexagonDisplayInformation()
        {
            Root.Q<Label>("Target-Coordinates-Label").text = $"{_currentInteractionPayload.Coordinates.x} , {_currentInteractionPayload.Coordinates.y}";
            Root.Q<Label>("Biome-Type-Label").text = _currentInteractionPayload.BiomeName.Replace("_", " ").ToUpper();
        }

        private void RefreshContextualActionButtons()
        {
            var selectExpeditionButton = Root.Q<Button>("Btn-Select-Unit");
            var orderMarchButton = Root.Q<Button>("Btn-Move-Here");
            var openDeployViewButton = Root.Q<Button>("Btn-Open-Deploy-View");

            if (_currentInteractionPayload.DeploymentIdOnTile.HasValue && _currentInteractionPayload.IsPlayerOwned)
            {
                selectExpeditionButton.RemoveFromClassList("hidden");
                selectExpeditionButton.clicked -= HandleSelectExpeditionAction;
                selectExpeditionButton.clicked += HandleSelectExpeditionAction;
            }
            else selectExpeditionButton.AddToClassList("hidden");

            if (WorldMapInteractionHandler.Instance.HasActiveSelection && !_currentInteractionPayload.DeploymentIdOnTile.HasValue)
            {
                orderMarchButton.RemoveFromClassList("hidden");
                orderMarchButton.clicked -= HandleOrderMarchAction;
                orderMarchButton.clicked += HandleOrderMarchAction;
            }
            else orderMarchButton.AddToClassList("hidden");

            if (openDeployViewButton != null)
            {
                openDeployViewButton.clicked -= HandleSwitchToDeploymentModeAction;
                openDeployViewButton.clicked += HandleSwitchToDeploymentModeAction;
            }
        }

        private void ResetDeploymentViewToDefaultState()
        {
            _unitDeploymentViewContainer.AddToClassList("hidden");
            _primaryActionContainer.RemoveFromClassList("hidden");
        }

        private void HandleSwitchToDeploymentModeAction()
        {
            _primaryActionContainer.AddToClassList("hidden");
            _unitDeploymentViewContainer.RemoveFromClassList("hidden");
            PopulateAvailableUnitsList();
        }

        private void PopulateAvailableUnitsList()
        {
            _availableUnitsScrollView.Clear();
            _currentlySelectedUnitsToDeploy.Clear();

            var stationedUnitsInCity = CityStateManager.Instance.CurrentStationedUnits;

            if (stationedUnitsInCity == null || stationedUnitsInCity.Count == 0)
            {
                Label emptyReservesLabel = new Label("No reserves available in city.");
                emptyReservesLabel.AddToClassList("info-text");
                emptyReservesLabel.style.marginTop = 20;
                emptyReservesLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _availableUnitsScrollView.Add(emptyReservesLabel);
                return;
            }

            foreach (var unitStack in stationedUnitsInCity)
            {
                VisualElement unitSelectionRow = _unitRowTemplate.Instantiate();
                unitSelectionRow.Q<Label>("Unit-Name").text = unitStack.Type.ToString();
                unitSelectionRow.Q<Label>("Available-Amount").text = $"In City: {unitStack.Quantity}";

                var quantityInputField = unitSelectionRow.Q<TextField>("Input-Amount");
                quantityInputField.RegisterValueChangedCallback(evt => {
                    if (int.TryParse(evt.newValue, out int parsedAmount))
                    {
                        parsedAmount = Mathf.Clamp(parsedAmount, 0, unitStack.Quantity);
                        _currentlySelectedUnitsToDeploy.RemoveAll(selection => selection.Type == unitStack.Type);
                        if (parsedAmount > 0)
                        {
                            _currentlySelectedUnitsToDeploy.Add(new UnitSelectionDTO { Type = unitStack.Type, Quantity = parsedAmount });
                        }
                    }
                });

                _availableUnitsScrollView.Add(unitSelectionRow);
            }

            var confirmDeploymentButton = Root.Q<Button>("Btn-Confirm-Deployment");
            confirmDeploymentButton.clicked -= HandleConfirmDeploymentAction;
            confirmDeploymentButton.clicked += HandleConfirmDeploymentAction;
        }

        private void HandleSelectExpeditionAction()
        {
            WorldMapInteractionHandler.Instance.SetSelectedDeployment(_currentInteractionPayload.DeploymentIdOnTile.Value);
            Close();
        }

        private void HandleOrderMarchAction()
        {
            var selectedUnitIdentifier = WorldMapInteractionHandler.Instance.SelectedDeploymentId.Value;
            var marchOrderRequest = new MoveUnitRequestDTO
            {
                UnitDeploymentId = selectedUnitIdentifier,
                TargetX = _currentInteractionPayload.Coordinates.x,
                TargetY = _currentInteractionPayload.Coordinates.y
            };

            StartCoroutine(NetworkManager.Instance.UnitDeployment.MoveUnits(marchOrderRequest, NetworkManager.Instance.JwtToken, (response) => {
                if (response != null)
                {
                    WorldMapStateManager.Instance.UpdateDeploymentInCache(response);
                    WorldMapInteractionHandler.Instance.ClearSelection();
                    Close();
                }
            }));
        }

        private void HandleConfirmDeploymentAction()
        {
            if (!_currentlySelectedUnitsToDeploy.Any()) return;

            var deploymentRequest = new DeployUnitRequestDTO
            {
                OriginCityId = CityStateManager.Instance.CityId,
                TargetX = _currentInteractionPayload.Coordinates.x,
                TargetY = _currentInteractionPayload.Coordinates.y,
                UnitsToDeploy = _currentlySelectedUnitsToDeploy,
                WorldPlayerId = Guid.Parse(NetworkManager.Instance.WorldPlayerId)
            };

            StartCoroutine(NetworkManager.Instance.UnitDeployment.DeployUnits(deploymentRequest, NetworkManager.Instance.JwtToken, (response) => {
                if (response != null)
                {
                    WorldMapStateManager.Instance.RequestWorldMapChunkData((short)response.CurrentX, (short)response.CurrentY, 50, 50, true);
                    Close();
                }
            }));
        }
    }
}