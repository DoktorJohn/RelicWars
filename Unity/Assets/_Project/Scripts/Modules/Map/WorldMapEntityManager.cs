using Assets.Scripts.Domain.Enums;
using Domain.StaticData.Generators;
using Project.Network.Models;
using Project.Modules.City;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapEntityManager : MonoBehaviour
    {
        private const float ResourceButtonRadiusInTiles = 4f;

        public static WorldMapEntityManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject _cityPrefab;
        [SerializeField] private GameObject _unitDeploymentPrefab;
        [SerializeField] private GameObject _islandButtonPrefab;
        [SerializeField] private Transform _objectContainer;

        [Header("Indstillinger")]
        [SerializeField] private int _cityLabelSortingOrder = 10;
        [SerializeField] private int _unitDeploymentLabelSortingOrder = 20;
        [SerializeField] private int _islandButtonSortingOrder = 100;
        [SerializeField] private int _islandResourceButtonSortingOrder = 90;
        [SerializeField] private string _unitLayerName = "Units";

        public Tilemap TerrainTilemap;
        private readonly Dictionary<Guid, GameObject> _activeUnitVisuals = new();
        private readonly Dictionary<Vector2Int, List<GameObject>> _activeMapObjectsPerChunk = new();
        private readonly Dictionary<Vector2Int, WorldMapChunkResponseDTO> _renderedChunkData = new();
        private readonly Dictionary<Vector2Int, WorldMapChunkResponseDTO> _pendingChunkData = new();
        private readonly Queue<Vector2Int> _pendingChunkKeys = new();
        private Coroutine _entitySyncCoroutine;

        private void Awake() => Instance = this;

        private void Start()
        {
            if (WorldMapStateManager.Instance != null)
                WorldMapStateManager.Instance.OnChunkDataReady += HandleEntitySynchronizationRequest;
        }

        private void OnDestroy()
        {
            if (WorldMapStateManager.Instance != null)
                WorldMapStateManager.Instance.OnChunkDataReady -= HandleEntitySynchronizationRequest;
        }

        private void HandleEntitySynchronizationRequest(WorldMapChunkResponseDTO data)
        {
            if (data == null)
            {
                return;
            }

            Vector2Int key = new Vector2Int(data.ChunkX, data.ChunkY);
            if (_renderedChunkData.TryGetValue(key, out var renderedData) && ReferenceEquals(renderedData, data))
            {
                return;
            }

            _pendingChunkData[key] = data;
            if (!_pendingChunkKeys.Contains(key))
            {
                _pendingChunkKeys.Enqueue(key);
            }

            if (_entitySyncCoroutine == null)
            {
                _entitySyncCoroutine = StartCoroutine(ProcessChunkEntityQueue());
            }
        }

        private IEnumerator ProcessChunkEntityQueue()
        {
            while (_pendingChunkKeys.Count > 0)
            {
                if (this == null)
                {
                    break;
                }

                Vector2Int key = _pendingChunkKeys.Dequeue();
                if (!_pendingChunkData.TryGetValue(key, out var data))
                {
                    continue;
                }

                _pendingChunkData.Remove(key);
                yield return StartCoroutine(SynchronizeChunkEntities(data));
                _renderedChunkData[key] = data;
            }

            _entitySyncCoroutine = null;
        }

        private IEnumerator SynchronizeChunkEntities(WorldMapChunkResponseDTO data)
        {
            Vector2Int key = new Vector2Int(data.ChunkX, data.ChunkY);

            if (TerrainTilemap == null)
            {
                yield break;
            }

            if (_activeMapObjectsPerChunk.TryGetValue(key, out List<GameObject> existingObjects))
            {
                int destroyedCount = 0;
                foreach (var obj in existingObjects)
                {
                    if (obj == null)
                    {
                        continue;
                    }

                    Destroy(obj);
                    destroyedCount++;
                    if (destroyedCount % 8 == 0)
                    {
                        yield return null;
                    }
                }

                existingObjects.Clear();
            }
            else
            {
                _activeMapObjectsPerChunk[key] = new List<GameObject>();
            }

            foreach (var city in data.Cities ?? Enumerable.Empty<CityDTO>())
            {
                yield return SpawnCityVisual(data, key, city);
            }

            if (data.FutureCitySites is { Count: > 0 })
            {
                yield return SpawnFutureCitySiteButtons(key, data.FutureCitySites);
            }

            if (data.Islands != null && _islandButtonPrefab != null)
            {
                var occupiedTiles = CreateOccupiedTileSet(data);

                foreach (var island in data.Islands)
                {
                    yield return SpawnIslandVisuals(data, key, island, occupiedTiles);
                }
            }
        }

        private IEnumerator SpawnCityVisual(WorldMapChunkResponseDTO data, Vector2Int key, CityDTO city)
        {
            Vector3 worldPos = TerrainTilemap.GetCellCenterWorld(new Vector3Int(city.X, city.Y, 0));
            GameObject inst = Instantiate(_cityPrefab, worldPos, Quaternion.identity, _objectContainer);
            inst.name = $"City_{city.CityName}";

            var uiDoc = inst.GetComponent<UIDocument>();
            if (uiDoc != null) uiDoc.sortingOrder = _cityLabelSortingOrder;

            inst.GetComponent<WorldMapCityInteractionLabelController>()?.InitializeCityInteractionLabel(city.CityName);
            _activeMapObjectsPerChunk[key].Add(inst);
            yield return null;
        }

        private IEnumerator SpawnFutureCitySiteButtons(
            Vector2Int key,
            IReadOnlyCollection<WorldMapCoordinateDTO> sites)
        {
            if (_islandButtonPrefab == null)
            {
                yield break;
            }

            GameObject buttonGroup = Instantiate(_islandButtonPrefab, Vector3.zero, Quaternion.identity, _objectContainer);
            buttonGroup.name = $"FutureCitySites_{key.x}_{key.y}";

            var uiDocument = buttonGroup.GetComponent<UIDocument>();
            if (uiDocument != null) uiDocument.sortingOrder = _islandResourceButtonSortingOrder;

            var worldPositions = sites
                .Select(site => TerrainTilemap.GetCellCenterWorld(new Vector3Int(site.X, site.Y, 0)))
                .ToList();
            buttonGroup.GetComponent<WorldMapIslandButtonController>()?.InitializeFutureCitySites(worldPositions);
            _activeMapObjectsPerChunk[key].Add(buttonGroup);
            yield return null;
        }

        private IEnumerator SpawnIslandVisuals(
            WorldMapChunkResponseDTO data,
            Vector2Int key,
            WorldIslandMapDTO island,
            HashSet<Vector2Int> occupiedTiles)
        {
            Vector3 worldPos = TerrainTilemap.GetCellCenterWorld(new Vector3Int(island.CenterX, island.CenterY, 0));
            GameObject islandButton = Instantiate(_islandButtonPrefab, worldPos, Quaternion.identity, _objectContainer);
            islandButton.name = $"Island_{island.Id}";

            var uiDocument = islandButton.GetComponent<UIDocument>();
            if (uiDocument != null) uiDocument.sortingOrder = _islandButtonSortingOrder;

            islandButton.GetComponent<WorldMapIslandButtonController>()?.Initialize(island);
            _activeMapObjectsPerChunk[key].Add(islandButton);

            SpawnIslandResourceButtons(data, island, occupiedTiles, key);
            yield return null;
        }

        private HashSet<Vector2Int> CreateOccupiedTileSet(WorldMapChunkResponseDTO data)
        {
            var occupiedTiles = new HashSet<Vector2Int>();

            if (data.MapObjects != null)
            {
                foreach (var mapObject in data.MapObjects)
                {
                    occupiedTiles.Add(new Vector2Int(mapObject.X, mapObject.Y));
                }
            }

            if (data.Cities != null)
            {
                foreach (var city in data.Cities)
                {
                    occupiedTiles.Add(new Vector2Int(city.X, city.Y));
                }
            }

            if (data.FutureCitySites != null)
            {
                foreach (var site in data.FutureCitySites)
                {
                    occupiedTiles.Add(new Vector2Int(site.X, site.Y));
                }
            }

            return occupiedTiles;
        }

        private void SpawnIslandResourceButtons(
            WorldMapChunkResponseDTO data,
            WorldIslandMapDTO island,
            HashSet<Vector2Int> occupiedTiles,
            Vector2Int chunkKey)
        {
            if (TerrainTilemap == null || data.Islands == null || _islandButtonPrefab == null)
            {
                return;
            }

            if (!WorldGenerationService.TryGetIslandCoordinates(island.CenterX, island.CenterY, data.WorldSeed, out int islandCellX, out int islandCellY))
            {
                return;
            }

            occupiedTiles.Add(new Vector2Int(island.CenterX, island.CenterY));

            var resourceTypes = GetAssignedExoticResources(data.WorldSeed, islandCellX, islandCellY);
            for (int slotIndex = 0; slotIndex < resourceTypes.Count; slotIndex++)
            {
                if (!TryGetResourceButtonTile(island, data.WorldSeed, slotIndex, occupiedTiles, out Vector2Int tilePosition))
                {
                    continue;
                }

                occupiedTiles.Add(tilePosition);

                Vector3 worldPos = TerrainTilemap.GetCellCenterWorld(new Vector3Int(tilePosition.x, tilePosition.y, 0));
                GameObject resourceButton = Instantiate(_islandButtonPrefab, worldPos, Quaternion.identity, _objectContainer);
                resourceButton.name = $"Island_{island.Id}_Resource_{slotIndex}";

                var uiDocument = resourceButton.GetComponent<UIDocument>();
                if (uiDocument != null)
                {
                    uiDocument.sortingOrder = _islandResourceButtonSortingOrder;
                }

                resourceButton.GetComponent<WorldMapIslandButtonController>()?.Initialize(island, resourceTypes[slotIndex], slotIndex);
                _activeMapObjectsPerChunk[chunkKey].Add(resourceButton);
            }
        }

        private static bool TryGetResourceButtonTile(
            WorldIslandMapDTO island,
            int worldSeed,
            int slotIndex,
            HashSet<Vector2Int> occupiedTiles,
            out Vector2Int tilePosition)
        {
            tilePosition = default;

            if (!WorldGenerationService.TryGetIslandCoordinates(island.CenterX, island.CenterY, worldSeed, out int islandCellX, out int islandCellY))
            {
                return false;
            }

            var candidateTiles = new List<(Vector2Int Tile, int Score)>();
            for (int radius = 1; radius <= WorldGenerationService.MaximumIslandRadius; radius++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    for (int offsetY = -radius; offsetY <= radius; offsetY++)
                    {
                        if (Math.Max(Math.Abs(offsetX), Math.Abs(offsetY)) != radius)
                        {
                            continue;
                        }

                        var candidate = new Vector2Int(island.CenterX + offsetX, island.CenterY + offsetY);
                        if (occupiedTiles.Contains(candidate))
                        {
                            continue;
                        }

                        if (!WorldGenerationService.TryGetIslandCoordinates(candidate.x, candidate.y, worldSeed, out int candidateCellX, out int candidateCellY))
                        {
                            continue;
                        }

                        if (candidateCellX != islandCellX || candidateCellY != islandCellY)
                        {
                            continue;
                        }

                        candidateTiles.Add((candidate, ComputeResourcePlacementScore(
                            worldSeed,
                            island.CenterX,
                            island.CenterY,
                            islandCellX,
                            islandCellY,
                            slotIndex,
                            candidate)));
                    }
                }
            }

            if (candidateTiles.Count == 0)
            {
                return false;
            }

            tilePosition = candidateTiles
                .OrderBy(candidate => candidate.Score)
                .First()
                .Tile;

            return true;
        }

        private static int ComputeResourcePlacementScore(
            int worldSeed,
            int islandCenterX,
            int islandCenterY,
            int islandCellX,
            int islandCellY,
            int slotIndex,
            Vector2Int candidate)
        {
            unchecked
            {
                int hash = worldSeed;
                hash = hash * 397 ^ islandCellX;
                hash = hash * 397 ^ islandCellY;
                hash = hash * 397 ^ slotIndex;
                hash = hash * 397 ^ candidate.x;
                hash = hash * 397 ^ candidate.y;
                hash ^= hash >> 16;

                int rotationHash = worldSeed;
                rotationHash = rotationHash * 397 ^ islandCellX;
                rotationHash = rotationHash * 397 ^ islandCellY;
                float rotationDegrees = (rotationHash & 0x7FFFFFFF) % 360;
                float angleRadians = (rotationDegrees + slotIndex * 120f) * Mathf.Deg2Rad;
                float targetRadius = ResourceButtonRadiusInTiles;
                float targetX = islandCenterX + Mathf.Cos(angleRadians) * targetRadius;
                float targetY = islandCenterY + Mathf.Sin(angleRadians) * targetRadius;
                float deltaX = candidate.x - targetX;
                float deltaY = candidate.y - targetY;
                int targetDistancePenalty = Mathf.RoundToInt((deltaX * deltaX + deltaY * deltaY) * 100_000f);
                return targetDistancePenalty + (hash & 0x0000_FFFF);
            }
        }

        private static List<ExoticResourceTypeEnum> GetAssignedExoticResources(int mapSeed, int cellX, int cellY)
        {
            return Enum.GetValues(typeof(ExoticResourceTypeEnum))
                .Cast<ExoticResourceTypeEnum>()
                .OrderBy(resource => GetResourceSortKey(mapSeed, cellX, cellY, resource))
                .Take(3)
                .ToList();
        }

        private static int GetResourceSortKey(int mapSeed, int cellX, int cellY, ExoticResourceTypeEnum resourceType)
        {
            unchecked
            {
                int hash = mapSeed;
                hash = hash * 397 ^ cellX;
                hash = hash * 397 ^ cellY;
                hash = hash * 397 ^ (int)resourceType;
                hash ^= hash >> 16;
                return hash & int.MaxValue;
            }
        }

        private GameObject SynchronizeUnitDeploymentVisual(UnitDeploymentDTO data)
        {
            GameObject unitObj;
            if (_activeUnitVisuals.TryGetValue(data.Id, out GameObject existing) && existing != null)
            {
                unitObj = existing;
                unitObj.GetComponent<WorldMapUnitVisualMovementController>().InitializeMovement(data, TerrainTilemap);
            }
            else
            {
                unitObj = Instantiate(_unitDeploymentPrefab, Vector3.zero, Quaternion.identity, _objectContainer);
                unitObj.name = $"UnitDeployment_{data.Id}";

                int layer = LayerMask.NameToLayer(_unitLayerName);
                if (layer != -1) unitObj.layer = layer;

                var uiDoc = unitObj.GetComponent<UIDocument>();
                if (uiDoc != null) uiDoc.sortingOrder = _unitDeploymentLabelSortingOrder;

                var moveCtrl = unitObj.GetComponent<WorldMapUnitVisualMovementController>() ?? unitObj.AddComponent<WorldMapUnitVisualMovementController>();
                moveCtrl.InitializeMovement(data, TerrainTilemap);
                _activeUnitVisuals[data.Id] = unitObj;
            }

            var trigger = unitObj.GetComponent<WorldMapUnitClickTrigger>();
            if (trigger != null && data.OriginCity != null) trigger.InitializeTrigger(data.Id, data.OriginCity.X, data.OriginCity.Y);

            int qty = data.UnitStacks?.Sum(s => s.Quantity) ?? 0;
            unitObj.GetComponent<WorldMapUnitDeploymentLabelController>()?.InitializeUnitDeploymentLabel(data.Name, qty);
            UpdateUnitVisualScale(data.Id, unitObj);

            return unitObj;
        }

        private void SyncUnitSelectionVisuals(Guid? selectedId)
        {
            foreach (var kvp in _activeUnitVisuals) UpdateUnitVisualScale(kvp.Key, kvp.Value, selectedId);
        }

        private void UpdateUnitVisualScale(Guid id, GameObject obj, Guid? selectedId = null)
        {
            if (obj == null) return;
            bool isSelected = selectedId.HasValue && selectedId.Value == id;
            obj.transform.localScale = isSelected ? new Vector3(1.3f, 1.3f, 1f) : Vector3.one;
        }

        public void RemoveUnitVisualExplicitly(Guid id)
        {
            if (_activeUnitVisuals.TryGetValue(id, out GameObject obj))
            {
                _activeUnitVisuals.Remove(id);
                if (obj != null) Destroy(obj);
            }
            else
            {
                Debug.LogWarning($"<color=orange>[WorldMapEntityManager]</color> Forsøgte eksplicit sletning af {id}, men visual findes ikke.");
            }
        }
    }
}
