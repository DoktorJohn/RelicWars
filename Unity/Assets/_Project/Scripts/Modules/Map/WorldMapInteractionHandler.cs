using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Modules.UI.Windows.Implementations;
using Project.Modules.UI;
using Domain.StaticData.Generators;
using Assets.Scripts.Domain.Enums;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapInteractionHandler : MonoBehaviour
    {
        public static WorldMapInteractionHandler Instance { get; private set; }
        public event Action<Guid?> OnSelectionChanged;

        [Header("Indstillinger for Interaktion")]
        [SerializeField] private LayerMask _unitLayerMask;

        private Tilemap _terrainTilemap;
        private Tilemap _highlightTilemap;
        private TileBase _selectionFrameTile;
        private Camera _mainCamera;

        public Guid? SelectedDeploymentId { get; private set; }
        public bool HasActiveSelection => SelectedDeploymentId.HasValue;

        // Denne property styres af UI-events og valideres i Update for at forhindre click-through
        public bool IsMouseOverUI { get; private set; }

        private Vector3Int _lastHoveredCellCoordinate = new Vector3Int(-9999, -9999, 0);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("<color=green>[InteractionHandler]</color> Awake: Global instans initialiseret.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetMouseOverUI(bool isMouseCurrentlyOverUserInterface)
        {
            IsMouseOverUI = isMouseCurrentlyOverUserInterface;
        }

        public void AssignInteractionReferences(Tilemap terrain, Tilemap highlight, TileBase selectionTile, Camera mapCamera)
        {
            _terrainTilemap = terrain;
            _highlightTilemap = highlight;
            _selectionFrameTile = selectionTile;
            _mainCamera = mapCamera;
            _lastHoveredCellCoordinate = new Vector3Int(-9999, -9999, 0);
        }

        public void SetSelectedDeployment(Guid deploymentIdentifier)
        {
            if (SelectedDeploymentId == deploymentIdentifier)
            {
                ClearSelection();
                return;
            }
            SelectedDeploymentId = deploymentIdentifier;
            Debug.Log($"<color=cyan>[InteractionHandler]</color> Enhed markeret: {deploymentIdentifier}");
            OnSelectionChanged?.Invoke(deploymentIdentifier);
        }

        public void ClearSelection()
        {
            SelectedDeploymentId = null;
            Debug.Log("<color=yellow>[InteractionHandler]</color> Markering ryddet.");
            OnSelectionChanged?.Invoke(null);
        }

        private void Update()
        {
            // OBJEKTIV FIX: Fail-safe check. 
            // Vi verificerer hver frame om musen reelt er over et blokerende UI element.
            string blockingElementName;
            bool isCurrentlyOverBlockingElement = VerifyIfPointerIsOverBlockingUserInterface(out blockingElementName);

            // Hvis det automatiske tjek siger nej, og ingen manuel override er aktiv, så nulstil
            if (!isCurrentlyOverBlockingElement && IsMouseOverUI)
            {
                // Vi tillader en lille buffer for at undgå flimmer ved hurtige bevægelser
                IsMouseOverUI = false;
            }
            else if (isCurrentlyOverBlockingElement)
            {
                IsMouseOverUI = true;
            }

            if (_terrainTilemap == null || _mainCamera == null) return;

            // Stop hover-highlight hvis vi er over UI
            if (!IsMouseOverUI)
            {
                ExecuteMapHoverInteractionLogic();
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Hvis flaget er sat, eller det automatiske tjek finder UI, så stop klikket øjeblikkeligt
                if (IsMouseOverUI || isCurrentlyOverBlockingElement)
                {
                    Debug.Log($"<color=orange>[InteractionHandler]</color> Klik blokeret af: {blockingElementName}");
                    return;
                }

                ExecuteGlobalWorldMapClickHandling();
            }
        }

        public bool VerifyIfPointerIsOverBlockingUserInterface(out string nameOfBlockingElement)
        {
            nameOfBlockingElement = "None";
            if (Mouse.current == null) return false;

            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector2 adjustedUiToolkitPosition = new Vector2(currentMousePosition.x, Screen.height - currentMousePosition.y);

            UIDocument[] allActiveUiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var currentDocument in allActiveUiDocuments)
            {
                if (currentDocument.rootVisualElement == null) continue;
                VisualElement pickedElement = currentDocument.rootVisualElement.panel.Pick(adjustedUiToolkitPosition);

                if (pickedElement != null && pickedElement.pickingMode == PickingMode.Position)
                {
                    // OBJEKTIV FIX: Vi gør exclusion-listen ekstremt specifik til hær-enhedens labels på mappet.
                    // Vi tjekker efter det unikke navn vi gav dem i WorldMapEntityManager (fx "unit-label-container")
                    // Dette sikrer at Labels i dine Vinduer (HexagonWindow) STADIG blokerer klikket.
                    string elementName = pickedElement.name.ToLower();
                    if (elementName == "unit-label-container" ||
                        elementName == "unit-owner-text" ||
                        elementName == "unit-strength-badge")
                    {
                        continue;
                    }

                    nameOfBlockingElement = $"UI Toolkit: {pickedElement.name}";
                    return true;
                }
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                nameOfBlockingElement = "uGUI System / Legacy EventSystem";
                return true;
            }

            return false;
        }

        private void ExecuteMapHoverInteractionLogic()
        {
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector3 worldSpacePosition = _mainCamera.ScreenToWorldPoint(new Vector3(currentMousePosition.x, currentMousePosition.y, 1f));
            Vector3Int calculatedCellCoordinate = _terrainTilemap.WorldToCell(new Vector3(worldSpacePosition.x, worldSpacePosition.y + 0.125f, 0));

            if (calculatedCellCoordinate != _lastHoveredCellCoordinate)
            {
                if (_highlightTilemap != null)
                {
                    try
                    {
                        _highlightTilemap.SetTile(_lastHoveredCellCoordinate, null);
                        _highlightTilemap.SetTile(calculatedCellCoordinate, _selectionFrameTile);
                    }
                    catch { }
                }
                _lastHoveredCellCoordinate = calculatedCellCoordinate;
            }
        }

        private void ExecuteGlobalWorldMapClickHandling()
        {
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector3 worldSpacePosition = _mainCamera.ScreenToWorldPoint(new Vector3(currentMousePosition.x, currentMousePosition.y, 1f));
            worldSpacePosition.z = 0;

            Collider2D detectedUnitCollider = Physics2D.OverlapPoint(worldSpacePosition, _unitLayerMask);
            if (detectedUnitCollider != null)
            {
                WorldMapUnitClickTrigger unitTriggerComponent = detectedUnitCollider.GetComponent<WorldMapUnitClickTrigger>();
                if (unitTriggerComponent != null)
                {
                    SetSelectedDeployment(unitTriggerComponent.DeploymentId);
                    return;
                }
            }

            ExecuteHexagonClickInteraction(
                new Vector2Int(_lastHoveredCellCoordinate.x, _lastHoveredCellCoordinate.y),
                new Vector2(currentMousePosition.x, Screen.height - currentMousePosition.y)
            );
        }

        private void ExecuteHexagonClickInteraction(Vector2Int hexagonCoordinates, Vector2 screenPosition)
        {
            if (HasActiveSelection)
            {
                MapInteractionPayload interactionPayload = new MapInteractionPayload
                {
                    Coordinates = hexagonCoordinates,
                    ScreenClickPosition = screenPosition
                };
                GlobalWindowManager.Instance.OpenWindow(WindowTypeEnum.UnitDeployment, interactionPayload);
            }
            else
            {
                var unitOnTargetTile = WorldMapStateManager.Instance.GetUnitDeploymentByCoordinate(hexagonCoordinates.x, hexagonCoordinates.y);
                var worldMapSeed = WorldMapStateManager.Instance.CurrentWorldSeed ?? 0;
                var calculatedBiome = WorldGenerationService.CalculateWorldMapBiomeVariant((short)hexagonCoordinates.x, (short)hexagonCoordinates.y, worldMapSeed);

                MapInteractionPayload interactionPayload = new MapInteractionPayload
                {
                    Coordinates = hexagonCoordinates,
                    BiomeName = calculatedBiome.ToString(),
                    ScreenClickPosition = screenPosition,
                    DeploymentIdOnTile = unitOnTargetTile?.Id,
                    IsPlayerOwned = unitOnTargetTile != null && unitOnTargetTile.WorldPlayerId == Guid.Parse(NetworkManager.Instance.WorldPlayerId)
                };

                GlobalWindowManager.Instance.OpenWindow(WindowTypeEnum.Hexagon, interactionPayload);
            }
        }
    }
}