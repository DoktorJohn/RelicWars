using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Project.Modules.UI;
using Assets.Scripts.Domain.Enums;
using Project.Modules.UI.Windows.Implementations;
using Domain.StaticData.Generators;
using Project.Modules.City;
using System;
using Project.Network.Manager;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapInteractionHandler : MonoBehaviour
    {
        public static WorldMapInteractionHandler Instance { get; private set; }

        [Header("Indstillinger for Interaktion")]
        [SerializeField] private LayerMask _unitLayerMask;

        private Tilemap _terrainTilemap;
        private Tilemap _highlightTilemap;
        private TileBase _selectionFrameTile;
        private Camera _mainCamera;

        public Guid? SelectedDeploymentId { get; private set; }
        public bool HasActiveSelection => SelectedDeploymentId.HasValue;

        private Vector3Int _lastHoveredCellCoordinate = new Vector3Int(-9999, -9999, 0);

        private void Awake() => Instance = this;

        public void AssignInteractionReferences(Tilemap terrain, Tilemap highlight, TileBase selectionTile, Camera mapCamera)
        {
            _terrainTilemap = terrain;
            _highlightTilemap = highlight;
            _selectionFrameTile = selectionTile;
            _mainCamera = mapCamera;
        }

        public void SetSelectedDeployment(Guid id)
        {
            SelectedDeploymentId = id;
            Debug.Log($"<color=cyan>[InteractionHandler]</color> Enhed markeret: {id}");
        }

        public void ClearSelection()
        {
            SelectedDeploymentId = null;
            Debug.Log("<color=yellow>[InteractionHandler]</color> Markering ryddet.");
        }

        private void Update()
        {
            if (_terrainTilemap == null || _mainCamera == null) return;

            HandleMapHoverInteraction();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverBlockingUI(out string blockedBy))
                {
                    if (!blockedBy.Contains("UnitDeploymentPrefab"))
                    {
                        Debug.Log($"<color=red>[InteractionHandler]</color> Klik blokeret af UI: {blockedBy}");
                        return;
                    }
                }

                HandleGlobalMapClick();
            }
        }

        private bool IsPointerOverBlockingUI(out string elementName)
        {
            elementName = "None";
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 uiToolkitPos = new Vector2(mousePos.x, Screen.height - mousePos.y);

            UIDocument[] allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in allDocs)
            {
                if (doc.rootVisualElement == null) continue;
                VisualElement hit = doc.rootVisualElement.panel.Pick(uiToolkitPos);
                if (hit != null && hit.pickingMode == PickingMode.Position)
                {
                    if (hit.name == "unit-label-container" || hit.name == "unit-owner-text" || hit.name == "unit-strength-badge") continue;

                    elementName = $"UI Toolkit: {hit.name}";
                    return true;
                }
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current) { position = mousePos };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                foreach (var res in results)
                {
                    if (res.gameObject.name.Contains("UnitDeploymentPrefab")) continue;

                    elementName = $"uGUI: {res.gameObject.name}";
                    return true;
                }
            }
            return false;
        }

        private void HandleMapHoverInteraction()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 1f));
            Vector3Int currentCell = _terrainTilemap.WorldToCell(new Vector3(worldPos.x, worldPos.y + 0.125f, 0));

            if (currentCell != _lastHoveredCellCoordinate)
            {
                _highlightTilemap.SetTile(_lastHoveredCellCoordinate, null);
                _highlightTilemap.SetTile(currentCell, _selectionFrameTile);
                _lastHoveredCellCoordinate = currentCell;
            }
        }

        private void HandleGlobalMapClick()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 1f));
            worldPos.z = 0;

            Vector2 uiPos = new Vector2(mousePos.x, Screen.height - mousePos.y);
            Vector2Int hexCoords = new Vector2Int(_lastHoveredCellCoordinate.x, _lastHoveredCellCoordinate.y);

            Collider2D hit = Physics2D.OverlapPoint(worldPos, _unitLayerMask);
            if (hit != null)
            {
                WorldMapUnitClickTrigger trigger = hit.GetComponent<WorldMapUnitClickTrigger>();
                if (trigger != null)
                {
                    SetSelectedDeployment(trigger.DeploymentId);
                    return;
                }
            }

            HandleHexagonClick(hexCoords, uiPos);
        }

        private void HandleHexagonClick(Vector2Int coords, Vector2 uiPos)
        {
            if (HasActiveSelection)
            {
                MapInteractionPayload payload = new MapInteractionPayload { Coordinates = coords, ScreenClickPosition = uiPos };
                GlobalWindowManager.Instance.OpenWindow(WindowTypeEnum.UnitDeployment, payload);
            }
            else
            {
                var unitOnTile = WorldMapStateManager.Instance.GetUnitDeploymentByCoordinate(coords.x, coords.y);
                var biome = WorldGenerationService.CalculateWorldMapBiomeVariant((short)coords.x, (short)coords.y, WorldMapStateManager.Instance.CurrentWorldSeed ?? 0);

                MapInteractionPayload payload = new MapInteractionPayload
                {
                    Coordinates = coords,
                    BiomeName = biome.ToString(),
                    ScreenClickPosition = uiPos,
                    DeploymentIdOnTile = unitOnTile?.Id,
                    IsPlayerOwned = unitOnTile != null && unitOnTile.WorldPlayerId == Guid.Parse(NetworkManager.Instance.WorldPlayerId)
                };

                GlobalWindowManager.Instance.OpenWindow(WindowTypeEnum.Hexagon, payload);
            }
        }
    }

}