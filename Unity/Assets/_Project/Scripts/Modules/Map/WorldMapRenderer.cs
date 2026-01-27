using Project.Scripts.Domain.Enums;
using System.Collections;
using UnityEngine.Tilemaps;
using UnityEngine;
using System;
using Project.Network.Manager;
using System.Collections.Generic;

public class WorldMapRenderer : MonoBehaviour
{
    [Header("Chunk Configuration")]
    [SerializeField] private byte _chunkWidth = 50;
    [SerializeField] private byte _chunkHeight = 50;
    [SerializeField] private float _cameraUpdateCheckFrequencySeconds = 0.5f;
    [SerializeField] private int _chunkUnloadDistanceBuffer = 2;

    [Header("References")]
    public Camera MainCamera;
    public Tilemap TargetTilemap;
    public BiomeVisuals VisualConfig;

    [Header("Object Spawning")]
    [SerializeField] private GameObject _cityPrefab;
    [SerializeField] private Transform _objectContainer;

    private HashSet<Vector2Int> _currentlyLoadedChunkCoordinates = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, List<GameObject>> _spawnedObjectsPerChunkLookup = new Dictionary<Vector2Int, List<GameObject>>();

    private Guid? _cachedWorldId = null;
    private Vector2Int _lastCalculatedCenterChunkCoordinate = new Vector2Int(-999, -999);

    private void Start()
    {
        if (MainCamera == null) MainCamera = Camera.main;
        StartCoroutine(ContinuousCameraMonitoringRoutine());
    }

    private IEnumerator ContinuousCameraMonitoringRoutine()
    {
        while (true)
        {
            EvaluateRequiredChunksBasedOnCameraPosition();
            yield return new WaitForSeconds(_cameraUpdateCheckFrequencySeconds);
        }
    }

    private void EvaluateRequiredChunksBasedOnCameraPosition()
    {
        if (MainCamera == null) return;

        Vector3 cameraWorldPosition = MainCamera.transform.position;

        int centerChunkX = Mathf.FloorToInt(cameraWorldPosition.x / _chunkWidth) * _chunkWidth;
        int centerChunkY = Mathf.FloorToInt(cameraWorldPosition.y / _chunkHeight) * _chunkHeight;
        Vector2Int currentCenterCoordinate = new Vector2Int(centerChunkX, centerChunkY);

        if (currentCenterCoordinate != _lastCalculatedCenterChunkCoordinate)
        {
            _lastCalculatedCenterChunkCoordinate = currentCenterCoordinate;
            UpdateVisibleWorldGrid(centerChunkX, centerChunkY);
        }
    }

    private void UpdateVisibleWorldGrid(int centerX, int centerY)
    {
        List<Vector2Int> requiredChunks = new List<Vector2Int>();

        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                int targetX = centerX + (xOffset * _chunkWidth);
                int targetY = centerY + (yOffset * _chunkHeight);

                if (targetX < 0 || targetY < 0) continue;

                requiredChunks.Add(new Vector2Int(targetX, targetY));
            }
        }

        foreach (Vector2Int chunkCoord in requiredChunks)
        {
            if (!_currentlyLoadedChunkCoordinates.Contains(chunkCoord))
            {
                StartCoroutine(ExecuteChunkDataRequestSequence((short)chunkCoord.x, (short)chunkCoord.y));
            }
        }

        CleanupDistantMapData(centerX, centerY);
    }

    private IEnumerator ExecuteChunkDataRequestSequence(short startX, short startY)
    {
        Vector2Int chunkKey = new Vector2Int(startX, startY);

        if (_currentlyLoadedChunkCoordinates.Contains(chunkKey)) yield break;
        _currentlyLoadedChunkCoordinates.Add(chunkKey);

        if (!_cachedWorldId.HasValue)
        {
            yield return FetchPlayerWorldProfileId();
        }

        var chunkRequest = new GetWorldMapChunkDTO
        {
            worldId = _cachedWorldId.Value,
            startX = startX,
            startY = startY,
            width = _chunkWidth,
            height = _chunkHeight
        };

        yield return NetworkManager.Instance.World.GetWorldMapChunk(
            chunkRequest,
            NetworkManager.Instance.JwtToken,
            OnWorldMapChunkDataReceived);
    }

    private void OnWorldMapChunkDataReceived(WorldMapChunkResponseDTO data)
    {
        if (data == null) return;

        RenderChunkTerrainToTilemap(data);

        PopulateChunkWithMapObjects(new Vector2Int(data.ChunkX, data.ChunkY), data.MapObjects);
    }

    private void RenderChunkTerrainToTilemap(WorldMapChunkResponseDTO data)
    {
        int totalTileCount = data.Width * data.Height;
        Vector3Int[] tilePositions = new Vector3Int[totalTileCount];
        TileBase[] tileAssets = new TileBase[totalTileCount];

        int index = 0;
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                byte biomeValue = data.TerrainData[index];
                WorldBiomeVariantType variantType = (WorldBiomeVariantType)biomeValue;

                tilePositions[index] = new Vector3Int(data.ChunkX + x, data.ChunkY + y, 0);
                tileAssets[index] = VisualConfig.GetTile(variantType);

                index++;
            }
        }

        TargetTilemap.SetTiles(tilePositions, tileAssets);
    }

    private void PopulateChunkWithMapObjects(Vector2Int chunkKey, List<WorldMapObjectDTO> mapObjects)
    {
        if (mapObjects == null) return;

        List<GameObject> chunkInstances = new List<GameObject>();

        foreach (var mapObject in mapObjects)
        {
            Vector3Int tilePosition = new Vector3Int(mapObject.X, mapObject.Y, 0);
            Vector3 worldPosition = TargetTilemap.GetCellCenterWorld(tilePosition);

            if (mapObject.Type == 0 && _cityPrefab != null)
            {
                GameObject cityInstance = Instantiate(_cityPrefab, worldPosition, Quaternion.identity, _objectContainer);
                chunkInstances.Add(cityInstance);
            }
        }

        _spawnedObjectsPerChunkLookup[chunkKey] = chunkInstances;
    }

    private void CleanupDistantMapData(int centerX, int centerY)
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        float maxAllowedDistance = _chunkWidth * _chunkUnloadDistanceBuffer;

        foreach (Vector2Int loadedCoord in _currentlyLoadedChunkCoordinates)
        {
            float distance = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(loadedCoord.x, loadedCoord.y));
            if (distance > maxAllowedDistance)
            {
                chunksToRemove.Add(loadedCoord);
            }
        }

        foreach (Vector2Int coord in chunksToRemove)
        {
            RemoveTerrainTilesFromArea(coord.x, coord.y, _chunkWidth, _chunkHeight);

            if (_spawnedObjectsPerChunkLookup.TryGetValue(coord, out List<GameObject> objects))
            {
                foreach (GameObject obj in objects) if (obj != null) Destroy(obj);
                _spawnedObjectsPerChunkLookup.Remove(coord);
            }

            _currentlyLoadedChunkCoordinates.Remove(coord);
        }
    }

    private void RemoveTerrainTilesFromArea(int startX, int startY, int width, int height)
    {
        Vector3Int[] positions = new Vector3Int[width * height];
        TileBase[] nullTiles = new TileBase[width * height];

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                positions[index] = new Vector3Int(startX + x, startY + y, 0);
                nullTiles[index] = null;
                index++;
            }
        }
        TargetTilemap.SetTiles(positions, nullTiles);
    }

    private IEnumerator FetchPlayerWorldProfileId()
    {
        if (string.IsNullOrEmpty(NetworkManager.Instance.WorldPlayerId)) yield break;

        Guid worldPlayerId = Guid.Parse(NetworkManager.Instance.WorldPlayerId);
        bool success = false;

        yield return NetworkManager.Instance.WorldPlayer.GetPlayerProfile(
            worldPlayerId,
            NetworkManager.Instance.JwtToken,
            (profile) =>
            {
                if (profile != null)
                {
                    _cachedWorldId = profile.WorldId;
                    success = true;
                }
            });

        if (!success) yield break;
    }
}