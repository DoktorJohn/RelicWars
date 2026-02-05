using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Project.Scripts.Modules.Map;
using Project.Network.Models;
using Domain.StaticData.Generators;
using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Project.Modules.City
{
    public class WorldMapRenderer : MonoBehaviour
    {
        [Header("Chunk Konfiguration")]
        [SerializeField] private byte _chunkWidth = 50;
        [SerializeField] private byte _chunkHeight = 50;
        [SerializeField] private float _cameraUpdateCheckFrequencySeconds = 0.5f;
        [SerializeField] private float _forcedDataRefreshIntervalSeconds = 5f;

        private Guid _currentlyVisualizedSelectionId = Guid.Empty;

        [Header("Referencer")]
        public Camera MainCamera;
        public Tilemap TargetTilemap;
        public Tilemap HighlightTilemap;
        public TileBase SelectionFrameTile;
        public BiomeVisuals VisualConfig;

        [Header("Objekt Spawning")]
        [SerializeField] private GameObject _cityPrefab;
        [SerializeField] private GameObject _unitDeploymentPrefab;
        [SerializeField] private Transform _objectContainer;

        [Header("UI Sorting")]
        [SerializeField] private int _cityLabelSortingOrder = 10;
        [SerializeField] private int _unitDeploymentLabelSortingOrder = 20;

        private HashSet<Vector2Int> _visuallyRenderedChunks = new HashSet<Vector2Int>();
        private Dictionary<Vector2Int, List<GameObject>> _activeMapObjectsPerChunkLookup = new Dictionary<Vector2Int, List<GameObject>>();
        private Dictionary<Guid, GameObject> _activeUnitVisuals = new Dictionary<Guid, GameObject>();

        private Vector2Int _lastCalculatedCenterChunkCoordinate = new Vector2Int(-999, -999);

        private void OnEnable()
        {
            _visuallyRenderedChunks.Clear();
            _activeMapObjectsPerChunkLookup.Clear();
            _activeUnitVisuals.Clear();
            _lastCalculatedCenterChunkCoordinate = new Vector2Int(-999, -999);
            _currentlyVisualizedSelectionId = Guid.Empty;
        }

        private void Start()
        {
            StartCoroutine(InitializationSequence());
        }

        private void Update()
        {
            HandleSelectionVisuals();
        }

        private void HandleSelectionVisuals()
        {
            Guid? activeId = WorldMapInteractionHandler.Instance.SelectedDeploymentId;

            if (!activeId.HasValue && _currentlyVisualizedSelectionId != Guid.Empty)
            {
                ResetPreviousSelectionVisual();
                return;
            }

            if (activeId.HasValue && _currentlyVisualizedSelectionId != activeId.Value)
            {
                ResetPreviousSelectionVisual();

                if (_activeUnitVisuals.TryGetValue(activeId.Value, out GameObject unitObj) && unitObj != null)
                {
                    unitObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                    _currentlyVisualizedSelectionId = activeId.Value;
                }
            }
        }

        private void ResetPreviousSelectionVisual()
        {
            if (_currentlyVisualizedSelectionId != Guid.Empty && _activeUnitVisuals.TryGetValue(_currentlyVisualizedSelectionId, out GameObject oldUnit))
            {
                if (oldUnit != null) oldUnit.transform.localScale = Vector3.one;
            }
            _currentlyVisualizedSelectionId = Guid.Empty;
        }

        private IEnumerator InitializationSequence()
        {
            yield return new WaitUntil(() => WorldMapStateManager.Instance != null);
            WorldMapStateManager.Instance.OnChunkDataReady += HandleChunkRenderRequest;

            if (MainCamera == null) MainCamera = Camera.main;
            yield return new WaitUntil(() => MainCamera != null);
            yield return new WaitUntil(() => CityStateManager.Instance != null && CityStateManager.Instance.HomeCityX != 0);

            WorldMapInteractionHandler interactionHandler = FindObjectOfType<WorldMapInteractionHandler>();
            if (interactionHandler != null)
                interactionHandler.AssignInteractionReferences(TargetTilemap, HighlightTilemap, SelectionFrameTile, MainCamera);

            CenterCameraOnPlayerCity();
            StartCoroutine(ExecuteContinuousCameraMonitoringRoutine());
            StartCoroutine(ExecutePeriodicDataRefreshRoutine());
        }

        private void OnDestroy()
        {
            if (WorldMapStateManager.Instance != null)
                WorldMapStateManager.Instance.OnChunkDataReady -= HandleChunkRenderRequest;
        }

        private IEnumerator ExecuteContinuousCameraMonitoringRoutine()
        {
            while (true)
            {
                ExecuteVisibleChunkEvaluation(false);
                yield return new WaitForSeconds(_cameraUpdateCheckFrequencySeconds);
            }
        }

        private IEnumerator ExecutePeriodicDataRefreshRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_forcedDataRefreshIntervalSeconds);
                ExecuteVisibleChunkEvaluation(true);
            }
        }

        private void ExecuteVisibleChunkEvaluation(bool forceRefresh)
        {
            if (MainCamera == null || TargetTilemap == null) return;
            Vector3Int cameraCell = TargetTilemap.WorldToCell(MainCamera.transform.position);
            int centerChunkX = Mathf.FloorToInt((float)cameraCell.x / _chunkWidth) * _chunkWidth;
            int centerChunkY = Mathf.FloorToInt((float)cameraCell.y / _chunkHeight) * _chunkHeight;
            Vector2Int currentCoord = new Vector2Int(centerChunkX, centerChunkY);

            if (currentCoord != _lastCalculatedCenterChunkCoordinate || forceRefresh)
            {
                _lastCalculatedCenterChunkCoordinate = currentCoord;
                RequestNearbyChunks(centerChunkX, centerChunkY, forceRefresh);
            }
        }

        private void RequestNearbyChunks(int centerX, int centerY, bool forceRefresh)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    short tX = (short)(centerX + (x * _chunkWidth));
                    short tY = (short)(centerY + (y * _chunkHeight));
                    if (tX < 0 || tY < 0) continue;
                    WorldMapStateManager.Instance.RequestWorldMapChunkData(tX, tY, _chunkWidth, _chunkHeight, forceRefresh);
                }
            }
        }

        private void HandleChunkRenderRequest(WorldMapChunkResponseDTO data)
        {
            if (data == null || this == null) return;
            Vector2Int key = new Vector2Int(data.ChunkX, data.ChunkY);

            if (_activeMapObjectsPerChunkLookup.TryGetValue(key, out List<GameObject> existingObjects))
            {
                foreach (var obj in existingObjects) if (obj != null) Destroy(obj);
                existingObjects.Clear();
            }
            else
            {
                _activeMapObjectsPerChunkLookup[key] = new List<GameObject>();
            }

            RenderTerrain(data);
            SpawnOrUpdateObjects(key, data);
            _visuallyRenderedChunks.Add(key);
        }

        private void RenderTerrain(WorldMapChunkResponseDTO data)
        {
            int total = data.Width * data.Height;
            Vector3Int[] pos = new Vector3Int[total];
            TileBase[] assets = new TileBase[total];

            HashSet<Vector2Int> cityCoords = new HashSet<Vector2Int>();
            if (data.Cities != null)
            {
                foreach (var city in data.Cities) cityCoords.Add(new Vector2Int(city.X, city.Y));
            }

            int i = 0;
            for (short x = (short)data.ChunkX; x < data.ChunkX + data.Width; x++)
            {
                for (short y = (short)data.ChunkY; y < data.ChunkY + data.Height; y++)
                {
                    pos[i] = new Vector3Int(x, y, 0);
                    if (cityCoords.Contains(new Vector2Int(x, y))) assets[i] = VisualConfig.CityTile;
                    else assets[i] = VisualConfig.GetTile(WorldGenerationService.CalculateWorldMapBiomeVariant(x, y, data.WorldSeed));
                    i++;
                }
            }
            TargetTilemap.SetTiles(pos, assets);
        }

        private void SpawnOrUpdateObjects(Vector2Int key, WorldMapChunkResponseDTO data)
        {
            if (!_activeMapObjectsPerChunkLookup.ContainsKey(key)) return;
            List<GameObject> chunkList = _activeMapObjectsPerChunkLookup[key];

            // 1. Byer
            if (data.Cities != null)
            {
                foreach (var city in data.Cities)
                {
                    if (_cityPrefab == null) continue;
                    Vector3 worldPos = TargetTilemap.GetCellCenterWorld(new Vector3Int(city.X, city.Y, 0));
                    GameObject inst = Instantiate(_cityPrefab, worldPos, Quaternion.identity, _objectContainer);

                    var doc = inst.GetComponent<UIDocument>();
                    if (doc != null) doc.sortingOrder = _cityLabelSortingOrder;

                    var labelCtrl = inst.GetComponent<WorldMapCityInteractionLabelController>();
                    if (labelCtrl != null) labelCtrl.InitializeCityInteractionLabel(city.CityName, city.Points);

                    chunkList.Add(inst);
                }
            }

            // 2. Enheder - FØRST opdater eksisterende, SÅ spawn nye
            if (data.UnitDeployments != null)
            {
                // Track hvilke units der faktisk blev modtaget i dette chunk
                HashSet<Guid> processedUnitIds = new HashSet<Guid>();

                // FØRST: Opdater alle eksisterende units med nyt data fra serveren
                foreach (var unit in data.UnitDeployments)
                {
                    processedUnitIds.Add(unit.Id);

                    if (_activeUnitVisuals.TryGetValue(unit.Id, out GameObject existingUnitObj) && existingUnitObj != null)
                    {
                        // Eksisterende unit - opdatér bevægelsesdata (med Timestamp Guard beskyttelse)
                        var moveCtrl = existingUnitObj.GetComponent<WorldMapUnitVisualMovementController>();
                        if (moveCtrl != null)
                        {
                            moveCtrl.InitializeMovement(unit, TargetTilemap);
                        }

                        // Opdatér label data (altid - dette ændrer sig ofte)
                        var labelCtrl = existingUnitObj.GetComponent<WorldMapUnitDeploymentLabelController>();
                        if (labelCtrl != null)
                        {
                            int qty = (unit.UnitStacks != null) ? unit.UnitStacks.Sum(s => s.Quantity) : 0;
                            labelCtrl.InitializeUnitDeploymentLabel(unit.WorldPlayerUserName, qty);
                        }

                        // Opdatér trigger position (hvis den er rykket)
                        var trigger = existingUnitObj.GetComponent<WorldMapUnitClickTrigger>();
                        if (trigger != null)
                        {
                            trigger.InitializeTrigger(unit.Id, unit.CurrentX, unit.CurrentY);
                        }

                        // Håndér selection visuel
                        if (WorldMapInteractionHandler.Instance.SelectedDeploymentId == unit.Id)
                        {
                            existingUnitObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                            _currentlyVisualizedSelectionId = unit.Id;
                        }
                        else
                        {
                            existingUnitObj.transform.localScale = Vector3.one;
                        }
                    }
                    else
                    {
                        // NY enhed - spawn prefab
                        if (_unitDeploymentPrefab == null) continue;

                        GameObject newUnitObj = Instantiate(_unitDeploymentPrefab, Vector3.zero, Quaternion.identity, _objectContainer);

                        var doc = newUnitObj.GetComponent<UIDocument>();
                        if (doc != null) doc.sortingOrder = _unitDeploymentLabelSortingOrder;

                        // Tilføj movement controller hvis mangler
                        var moveCtrl = newUnitObj.GetComponent<WorldMapUnitVisualMovementController>();
                        if (moveCtrl == null) moveCtrl = newUnitObj.AddComponent<WorldMapUnitVisualMovementController>();
                        moveCtrl.InitializeMovement(unit, TargetTilemap);

                        // Tilføj til tracking
                        _activeUnitVisuals[unit.Id] = newUnitObj;

                        // Setup trigger
                        var trigger = newUnitObj.GetComponent<WorldMapUnitClickTrigger>();
                        if (trigger != null) trigger.InitializeTrigger(unit.Id, unit.CurrentX, unit.CurrentY);

                        // Setup label
                        var labelCtrl = newUnitObj.GetComponent<WorldMapUnitDeploymentLabelController>();
                        if (labelCtrl != null)
                        {
                            int qty = (unit.UnitStacks != null) ? unit.UnitStacks.Sum(s => s.Quantity) : 0;
                            labelCtrl.InitializeUnitDeploymentLabel(unit.WorldPlayerUserName, qty);
                        }

                        // Håndér hvis denne nye unit skal være selected
                        if (WorldMapInteractionHandler.Instance.SelectedDeploymentId == unit.Id)
                        {
                            newUnitObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                            _currentlyVisualizedSelectionId = unit.Id;
                        }
                    }
                }

                // BONUS: Fjern units der ikke længere findes i dette chunk (ryddet op)
                // Dette forhindrer "spøgelsesunits" når de flytter ud af chunk
                List<Guid> toRemove = new List<Guid>();
                foreach (var kvp in _activeUnitVisuals)
                {
                    // Tjek om denne unit burde være i dette chunk
                    var unit = data.UnitDeployments.FirstOrDefault(u => u.Id == kvp.Key);
                    if (unit != null)
                    {
                        int unitChunkX = Mathf.FloorToInt(unit.CurrentX / 50f) * 50;
                        int unitChunkY = Mathf.FloorToInt(unit.CurrentY / 50f) * 50;

                        // Hvis unit er i dette chunk men ikke i processed liste, er den fjernet fra server
                        if (unitChunkX == key.x && unitChunkY == key.y && !processedUnitIds.Contains(kvp.Key))
                        {
                            toRemove.Add(kvp.Key);
                        }
                    }
                }

                foreach (var id in toRemove)
                {
                    if (_activeUnitVisuals.TryGetValue(id, out GameObject obj) && obj != null)
                    {
                        Destroy(obj);
                    }
                    _activeUnitVisuals.Remove(id);

                    // Ryd selection hvis den var selected
                    if (_currentlyVisualizedSelectionId == id)
                    {
                        _currentlyVisualizedSelectionId = Guid.Empty;
                    }
                }
            }
        }

        public void CenterCameraOnPlayerCity()
        {
            Vector3 worldPos = TargetTilemap.CellToWorld(new Vector3Int(CityStateManager.Instance.HomeCityX, CityStateManager.Instance.HomeCityY, 0));
            MainCamera.transform.position = new Vector3(worldPos.x, worldPos.y, -1f);
        }
    }
}