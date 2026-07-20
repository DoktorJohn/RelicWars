using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Models;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Scripts.Modules.Map;
using Domain.StaticData.Generators;

namespace Project.Scripts.Modules.Map
{
    public class WorldMapRenderer : MonoBehaviour
    {
        [Header("Konfiguration")]
        [SerializeField] private byte _chunkWidth = 50;
        [SerializeField] private byte _chunkHeight = 50;
        [SerializeField] private float _cameraUpdateCheckInterval = 0.5f;

        [Header("Referencer")]
        public Camera MainCamera;
        public Tilemap TargetTilemap;
        public Tilemap HighlightTilemap;
        public TileBase SelectionFrameTile;
        public BiomeVisuals VisualConfig;

        private Vector2Int _lastCenterChunkCoordinate = new Vector2Int(-999, -999);
        private CameraEdgePan _cameraEdgePan;
        private int _configuredWorldWidth;
        private int _configuredWorldHeight;
        private bool _hasLoggedFirstCenterChunk;
        private bool _hasConfiguredMapBounds;
        private bool _isWaitingForInitialBounds;
        private Vector2Int _initialFocusCoordinates;
        private Coroutine _cameraMonitoringCoroutine;
        private Coroutine _activeCityFocusCoroutine;

        private void Start()
        {
            StartCoroutine(InitializationSequence());
        }

        private IEnumerator InitializationSequence()
        {
            if (MainCamera == null) MainCamera = Camera.main;
            if (MainCamera != null) _cameraEdgePan = MainCamera.GetComponent<CameraEdgePan>();
            _cameraEdgePan?.SetInputEnabled(false);

            float managerWaitTime = 0f;
            while ((WorldMapInteractionHandler.Instance == null ||
                    WorldMapStateManager.Instance == null ||
                    CityStateManager.Instance == null ||
                    NetworkManager.Instance == null) &&
                   managerWaitTime < 10f)
            {
                managerWaitTime += Time.unscaledDeltaTime;
                yield return null;
            }

            if (WorldMapInteractionHandler.Instance == null ||
                WorldMapStateManager.Instance == null ||
                CityStateManager.Instance == null ||
                NetworkManager.Instance == null)
            {
                Debug.LogError("[Renderer] World map initialization timed out while waiting for required managers.");
                _cameraEdgePan?.SetInputEnabled(true);
                yield break;
            }

            WorldMapInteractionHandler.Instance.AssignInteractionReferences(TargetTilemap, HighlightTilemap, SelectionFrameTile, MainCamera);
            WorldMapStateManager.Instance.OnChunkDataReady += HandleTerrainRenderRequest;
            NetworkManager.Instance.ActiveCityChanged += HandleActiveCityChanged;

            Guid? activeCityId = NetworkManager.Instance.ActiveCityId;
            if (!activeCityId.HasValue || activeCityId.Value == Guid.Empty)
            {
                Debug.LogError("[Renderer] Cannot initialize world map without an active city.");
                _cameraEdgePan?.SetInputEnabled(true);
                yield break;
            }

            if (!CityStateManager.Instance.HasDetailedCityStateFor(activeCityId.Value) &&
                !CityStateManager.Instance.IsPollingCity(activeCityId.Value))
            {
                CityStateManager.Instance.StartPollingForCity(activeCityId.Value);
            }

            yield return WaitForActiveCityState(activeCityId.Value);
            if (NetworkManager.Instance.ActiveCityId != activeCityId.Value)
            {
                yield break;
            }

            if (!CityStateManager.Instance.HasDetailedCityStateFor(activeCityId.Value))
            {
                Debug.LogError($"[Renderer] Timed out while loading active city {activeCityId.Value} for initial map focus.");
                _cameraEdgePan?.SetInputEnabled(true);
                yield break;
            }

            _initialFocusCoordinates = new Vector2Int(
                CityStateManager.Instance.CurrentCityX,
                CityStateManager.Instance.CurrentCityY);
            _isWaitingForInitialBounds = true;
            CenterCameraOnCoordinates(_initialFocusCoordinates.x, _initialFocusCoordinates.y);
            RequestChunksAroundCoordinates(_initialFocusCoordinates.x, _initialFocusCoordinates.y);
            Debug.Log("<color=green>[Renderer]</color> Scene initialiseret og referencer synkroniseret.");
        }

        private IEnumerator WaitForActiveCityState(Guid activeCityId)
        {
            float waitTime = 0f;
            while (CityStateManager.Instance != null &&
                   !CityStateManager.Instance.HasDetailedCityStateFor(activeCityId) &&
                   waitTime < 10f)
            {
                waitTime += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void CenterCameraOnCoordinates(int x, int y)
        {
            if (TargetTilemap == null || MainCamera == null) return;
            Vector3 worldPos = TargetTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
            MainCamera.transform.position = new Vector3(worldPos.x, worldPos.y, MainCamera.transform.position.z);
        }

        private void OnDestroy()
        {
            if (WorldMapStateManager.Instance != null)
                WorldMapStateManager.Instance.OnChunkDataReady -= HandleTerrainRenderRequest;
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.ActiveCityChanged -= HandleActiveCityChanged;
        }

        private void HandleActiveCityChanged(Guid cityId)
        {
            _cameraEdgePan?.SetInputEnabled(false);
            if (_activeCityFocusCoroutine != null)
            {
                StopCoroutine(_activeCityFocusCoroutine);
            }

            _activeCityFocusCoroutine = StartCoroutine(FocusOnActiveCity(cityId));
        }

        private IEnumerator FocusOnActiveCity(Guid cityId)
        {
            if (CityStateManager.Instance != null &&
                !CityStateManager.Instance.HasDetailedCityStateFor(cityId) &&
                !CityStateManager.Instance.IsPollingCity(cityId))
            {
                CityStateManager.Instance.StartPollingForCity(cityId);
            }

            yield return WaitForActiveCityState(cityId);
            if (CityStateManager.Instance == null ||
                !CityStateManager.Instance.HasDetailedCityStateFor(cityId) ||
                NetworkManager.Instance?.ActiveCityId != cityId)
            {
                Debug.LogError($"[Renderer] Could not focus world map on active city {cityId}.");
                _cameraEdgePan?.SetInputEnabled(true);
                _activeCityFocusCoroutine = null;
                yield break;
            }

            int x = CityStateManager.Instance.CurrentCityX;
            int y = CityStateManager.Instance.CurrentCityY;
            CenterCameraOnCoordinates(x, y);
            if (!_hasConfiguredMapBounds)
            {
                _initialFocusCoordinates = new Vector2Int(x, y);
                _isWaitingForInitialBounds = true;
            }
            RequestChunksAroundCoordinates(x, y);
            if (_hasConfiguredMapBounds)
            {
                _cameraEdgePan?.SetInputEnabled(true);
            }
            _activeCityFocusCoroutine = null;
        }

        private IEnumerator ExecuteCameraMonitoringRoutine()
        {
            while (true)
            {
                // Hvis vi er ved at skifte scene, kan TargetTilemap blive null midt i loopet
                if (this == null || TargetTilemap == null) yield break;

                ExecuteVisibleChunkEvaluation();
                yield return new WaitForSeconds(_cameraUpdateCheckInterval);
            }
        }

        private void ExecuteVisibleChunkEvaluation()
        {
            if (MainCamera == null || TargetTilemap == null) return;
            Vector3Int cameraCell = TargetTilemap.WorldToCell(MainCamera.transform.position);
            RequestChunksAroundCoordinates(cameraCell.x, cameraCell.y);
        }

        private void RequestChunksAroundCoordinates(int cellX, int cellY)
        {
            if (WorldMapStateManager.Instance == null) return;

            int centerX = Mathf.FloorToInt((float)cellX / _chunkWidth) * _chunkWidth;
            int centerY = Mathf.FloorToInt((float)cellY / _chunkHeight) * _chunkHeight;
            Vector2Int currentCoord = new Vector2Int(centerX, centerY);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_hasLoggedFirstCenterChunk)
            {
                _hasLoggedFirstCenterChunk = true;
                Debug.Log($"[Renderer] First center chunk is ({centerX},{centerY}) for focus cell ({cellX},{cellY}).");
            }
#endif

            if (currentCoord != _lastCenterChunkCoordinate)
            {
                _lastCenterChunkCoordinate = currentCoord;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        WorldMapStateManager.Instance.RequestWorldMapChunkData(
                            (short)(centerX + x * _chunkWidth),
                            (short)(centerY + y * _chunkHeight),
                            _chunkWidth, _chunkHeight);
                    }
                }
            }
        }

        private void HandleTerrainRenderRequest(WorldMapChunkResponseDTO data)
        {
            // Sikkerhed mod async kald efter scene-skift
            if (this == null || TargetTilemap == null || data == null) return;

            if (_cameraEdgePan != null
                && (data.WorldWidth != _configuredWorldWidth || data.WorldHeight != _configuredWorldHeight))
            {
                _cameraEdgePan.ConfigureMapBounds(TargetTilemap, data.WorldWidth, data.WorldHeight);
                _configuredWorldWidth = data.WorldWidth;
                _configuredWorldHeight = data.WorldHeight;
            }

            _hasConfiguredMapBounds = data.WorldWidth > 0 && data.WorldHeight > 0;

            if (_isWaitingForInitialBounds && _hasConfiguredMapBounds)
            {
                _isWaitingForInitialBounds = false;
                CenterCameraOnCoordinates(_initialFocusCoordinates.x, _initialFocusCoordinates.y);
                _cameraEdgePan?.SetInputEnabled(true);
                if (_cameraMonitoringCoroutine == null)
                {
                    _cameraMonitoringCoroutine = StartCoroutine(ExecuteCameraMonitoringRoutine());
                }
            }

            int totalTiles = data.Width * data.Height;
            Vector3Int[] positions = new Vector3Int[totalTiles];
            TileBase[] tiles = new TileBase[totalTiles];
            Dictionary<Vector2Int, CityDTO> citiesByPosition = new Dictionary<Vector2Int, CityDTO>();
            foreach (var cityGroup in (data.Cities ?? new List<CityDTO>())
                .Where(city => city != null)
                .GroupBy(city => new Vector2Int(city.X, city.Y)))
            {
                var orderedCities = cityGroup.OrderBy(city => city.Id).ToList();
                var selectedCity = orderedCities[0];
                citiesByPosition[cityGroup.Key] = selectedCity;
                if (orderedCities.Count > 1)
                {
                    Debug.LogWarning(
                        $"[Renderer] Legacy duplicate cities at ({cityGroup.Key.x},{cityGroup.Key.y}); " +
                        $"selected {selectedCity.Id}, discarded {string.Join(",", orderedCities.Skip(1).Select(city => city.Id))}.");
                }
            }
            HashSet<Vector2Int> futureCitySites = new HashSet<Vector2Int>(
                (data.FutureCitySites ?? new List<WorldMapCoordinateDTO>())
                    .Where(site => site != null)
                    .Select(site => new Vector2Int(site.X, site.Y)));

            int index = 0;
            for (short x = (short)data.ChunkX; x < data.ChunkX + data.Width; x++)
            {
                for (short y = (short)data.ChunkY; y < data.ChunkY + data.Height; y++)
                {
                    positions[index] = new Vector3Int(x, y, 0);
                    var position = new Vector2Int(x, y);
                    if (citiesByPosition.TryGetValue(position, out CityDTO city))
                        tiles[index] = city.IsNPC
                            ? VisualConfig.NPCVillageTile
                            : VisualConfig.GetCityTile(city.Points, data.MaximumCityPoints);
                    else if (futureCitySites.Contains(position))
                        tiles[index] = VisualConfig.FutureCitySiteTile;
                    else
                        tiles[index] = VisualConfig.GetTile(WorldGenerationService.CalculateWorldMapBiomeVariant(x, y, data.WorldSeed));
                    index++;
                }
            }
            TargetTilemap.SetTiles(positions, tiles);
        }
    }
}
