using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Models;
using Project.Modules.City;
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

        private void Start()
        {
            StartCoroutine(InitializationSequence());
        }

        private IEnumerator InitializationSequence()
        {
            if (MainCamera == null) MainCamera = Camera.main;
            if (MainCamera != null) _cameraEdgePan = MainCamera.GetComponent<CameraEdgePan>();

            // 1. VIGTIGT: Vent på at den NYE instans af InteractionHandler er vågen
            yield return new WaitUntil(() => WorldMapInteractionHandler.Instance != null);

            // 2. FORNY REFERENCERNE: Hver gang vi kommer tilbage fra City, skal Handleren have de nye Tilemaps
            WorldMapInteractionHandler.Instance.AssignInteractionReferences(TargetTilemap, HighlightTilemap, SelectionFrameTile, MainCamera);

            // 3. Vent på by-data og centrer kamera
            yield return new WaitUntil(() => CityStateManager.Instance != null && CityStateManager.Instance.HomeCityX != 0);
            CenterCameraOnPlayerCity();

            // 4. Start netværks-lytter
            if (WorldMapStateManager.Instance != null)
            {
                WorldMapStateManager.Instance.OnChunkDataReady += HandleTerrainRenderRequest;
            }

            StartCoroutine(ExecuteCameraMonitoringRoutine());
            Debug.Log("<color=green>[Renderer]</color> Scene initialiseret og referencer synkroniseret.");
        }

        public void CenterCameraOnPlayerCity()
        {
            if (TargetTilemap == null || MainCamera == null || CityStateManager.Instance == null) return;
            Vector3 worldPos = TargetTilemap.GetCellCenterWorld(new Vector3Int(CityStateManager.Instance.HomeCityX, CityStateManager.Instance.HomeCityY, 0));
            MainCamera.transform.position = new Vector3(worldPos.x, worldPos.y, -10f);
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
            int centerX = Mathf.FloorToInt((float)cameraCell.x / _chunkWidth) * _chunkWidth;
            int centerY = Mathf.FloorToInt((float)cameraCell.y / _chunkHeight) * _chunkHeight;
            Vector2Int currentCoord = new Vector2Int(centerX, centerY);

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

            int totalTiles = data.Width * data.Height;
            Vector3Int[] positions = new Vector3Int[totalTiles];
            TileBase[] tiles = new TileBase[totalTiles];
            HashSet<Vector2Int> cityPositions = new HashSet<Vector2Int>(data.Cities.Select(c => new Vector2Int(c.X, c.Y)));

            int index = 0;
            for (short x = (short)data.ChunkX; x < data.ChunkX + data.Width; x++)
            {
                for (short y = (short)data.ChunkY; y < data.ChunkY + data.Height; y++)
                {
                    positions[index] = new Vector3Int(x, y, 0);
                    if (cityPositions.Contains(new Vector2Int(x, y))) tiles[index] = VisualConfig.CityTile;
                    else tiles[index] = VisualConfig.GetTile(WorldGenerationService.CalculateWorldMapBiomeVariant(x, y, data.WorldSeed));
                    index++;
                }
            }
            TargetTilemap.SetTiles(positions, tiles);
        }
    }
}
