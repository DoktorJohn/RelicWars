using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;
using Project.Modules.City;
using Project.Modules.UI.Windows.Implementations;
using Project.Modules.UI;
using Domain.StaticData.Generators;
using Assets.Scripts.Domain.Enums;
using System.Linq;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapInteractionHandler : MonoBehaviour
    {
        public static WorldMapInteractionHandler Instance { get; private set; }

        private Tilemap _terrainTilemap;
        private Tilemap _highlightTilemap;
        private TileBase _selectionFrameTile;
        private Camera _mainCamera;
        public bool IsMouseOverUI { get; private set; }

        private Vector3Int _lastHoveredCellCoordinate = new Vector3Int(-9999, -9999, 0);
        private const float TouchTapMovementThreshold = 18f;
        private bool _touchPressStartedOverUi;
        private bool _hasTouchPress;
        private Vector2 _touchPressPosition;

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

        private void Update()
        {
            if (_terrainTilemap == null || _mainCamera == null) return;

            if (!TryGetPrimaryPointer(out Vector2 pointerPosition, out bool isTouchPointer))
            {
                IsMouseOverUI = false;
                return;
            }

            // 1. Tjek om den aktive pointer er over blokerende UI
            string blockingElementName;
            bool isBlockingUIActive = VerifyIfPointerIsOverBlockingUserInterface(pointerPosition, out blockingElementName);

            // Opdater internt flag (bruges bl.a. af kamerastyring til at stoppe zoom over UI)
            IsMouseOverUI = isBlockingUIActive;

            // 2. Hover er kun en desktop-affordance. Touch bruger selve tap-positionen.
            if (!IsMouseOverUI && !isTouchPointer)
            {
                ExecuteMapHoverInteractionLogic(pointerPosition);
            }

            // 3. Klik/tap håndtering. A tap is committed on release so map drags do not open a city.
            if (isTouchPointer && Touchscreen.current != null)
            {
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    _touchPressPosition = pointerPosition;
                    _touchPressStartedOverUi = IsMouseOverUI;
                    _hasTouchPress = true;
                    return;
                }

                if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                {
                    bool isTap = _hasTouchPress
                        && !_touchPressStartedOverUi
                        && !IsMouseOverUI
                        && Vector2.Distance(_touchPressPosition, pointerPosition) <= TouchTapMovementThreshold;
                    _hasTouchPress = false;

                    if (isTap)
                    {
                        ExecuteGlobalWorldMapClickHandling(pointerPosition);
                    }
                }

                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (IsMouseOverUI)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"<color=orange>[InteractionHandler]</color> Input blokeret af UI: {blockingElementName}");
#endif
                    return;
                }

                ExecuteGlobalWorldMapClickHandling(pointerPosition);
            }
        }

        /// <summary>
        /// OBJEKTIV FIX: Denne metode skelner nu mellem 'Verdens-Labels' og 'Menu-Vinduer'.
        /// </summary>
        public bool VerifyIfPointerIsOverBlockingUserInterface(out string nameOfBlockingElement)
        {
            if (!TryGetPrimaryPointer(out Vector2 pointerPosition, out _))
            {
                nameOfBlockingElement = "None";
                return false;
            }

            return VerifyIfPointerIsOverBlockingUserInterface(pointerPosition, out nameOfBlockingElement);
        }

        private bool VerifyIfPointerIsOverBlockingUserInterface(Vector2 pointerPosition, out string nameOfBlockingElement)
        {
            nameOfBlockingElement = "None";

            // UI Toolkit panel coordinates have their origin at the top-left.
            Vector2 adjustedPos = new Vector2(pointerPosition.x, Screen.height - pointerPosition.y);

            UIDocument[] allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in allDocs)
            {
                if (doc.rootVisualElement == null || doc.rootVisualElement.panel == null) continue;

                VisualElement picked = doc.rootVisualElement.panel.Pick(adjustedPos);
                if (picked != null && picked.pickingMode == PickingMode.Position)
                {
                    // Tjek navne på elementer vi tillader at klikke IGENNEM
                    // (Disse skal matche navnene i din hær-label UXML)
                    string n = picked.name.ToLower();
                    bool isUnitLabel = n.Contains("unit-label") || n.Contains("unit-strength") || n.Contains("unit-owner") || n.Contains("badge");

                    if (isUnitLabel)
                    {
                        // Vi har ramt en label, men vi tillader at klikke igennem den
                        continue;
                    }

                    // Hvis vi rammer noget andet (Menuer, Sidebars, HUD), så blokerer vi
                    nameOfBlockingElement = $"UI Toolkit: {picked.name} ({doc.gameObject.name})";
                    return true;
                }
            }

            // Tjek for Legacy uGUI (hvis du har Canvases)
            // Men kun hvis vi ikke allerede har godkendt at vi ramte en label
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = pointerPosition };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                // Ignorer objekter på "Units" lag (hvis dine labels er her)
                if (result.gameObject.layer == LayerMask.NameToLayer("Units")) continue;

                // Hvis vi finder noget andet UI, så bloker
                nameOfBlockingElement = $"EventSystem: {result.gameObject.name}";
                return true;
            }

            return false;
        }

        private void ExecuteMapHoverInteractionLogic(Vector2 pointerPosition)
        {
            Vector3Int cell = GetCellAtScreenPosition(pointerPosition);

            if (cell != _lastHoveredCellCoordinate)
            {
                if (_highlightTilemap != null)
                {
                    _highlightTilemap.SetTile(_lastHoveredCellCoordinate, null);
                    _highlightTilemap.SetTile(cell, _selectionFrameTile);
                }
                _lastHoveredCellCoordinate = cell;
            }
        }

        private void ExecuteGlobalWorldMapClickHandling(Vector2 pointerPosition)
        {
            ExecuteHexagonClickInteraction(
                new Vector2Int(GetCellAtScreenPosition(pointerPosition).x, GetCellAtScreenPosition(pointerPosition).y)
            );
        }

        private Vector3Int GetCellAtScreenPosition(Vector2 pointerPosition)
        {
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, 1f));
            return _terrainTilemap.WorldToCell(new Vector3(worldPos.x, worldPos.y + 0.125f, 0));
        }

        private static bool TryGetPrimaryPointer(out Vector2 pointerPosition, out bool isTouchPointer)
        {
            if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed
                || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                isTouchPointer = true;
                return true;
            }

            if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                isTouchPointer = false;
                return true;
            }

            pointerPosition = default;
            isTouchPointer = false;
            return false;
        }
        private void ExecuteHexagonClickInteraction(Vector2Int coords)
        {
            if (WorldMapStateManager.Instance == null)
            {
                return;
            }

            var city = WorldMapStateManager.Instance.AllVisibleCities
                .FirstOrDefault(candidate => candidate.X == coords.x && candidate.Y == coords.y);

            if (city == null)
            {
                return;
            }

            var seed = WorldMapStateManager.Instance.CurrentWorldSeed ?? 0;
            var biome = WorldGenerationService.CalculateWorldMapBiomeVariant((short)coords.x, (short)coords.y, seed);

            if (GlobalWindowManager.Instance == null)
            {
                return;
            }

            CityInspectionPayload payload = new CityInspectionPayload
            {
                CityId = city.Id,
                Coordinates = coords,
                TerrainName = biome.ToString(),
            };

            GlobalWindowManager.Instance.OpenWindow(WindowTypeEnum.Hexagon, payload);
        }
    }
}
