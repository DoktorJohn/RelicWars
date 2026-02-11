using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project.Network.Models;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;
using Project.Scripts.Modules.Map;

namespace Project.Modules.City
{
    public class WorldMapStateManager : MonoBehaviour
    {
        public static WorldMapStateManager Instance { get; private set; }

        public event Action<List<CityDTO>> OnCitiesStateChanged;
        public event Action<List<UnitDeploymentDTO>> OnUnitDeploymentsStateChanged;
        public event Action<List<WorldMapObjectDTO>> OnMapObjectsStateChanged;
        public event Action<WorldMapChunkResponseDTO> OnChunkDataReady;
        public event Action<Guid> OnWorldIdResolved;

        private readonly Dictionary<Vector2Int, WorldMapChunkResponseDTO> _cachedWorldChunks = new();
        private readonly HashSet<Vector2Int> _activeNetworkRequests = new();

        private List<CityDTO> _allVisibleCities = new();
        private List<UnitDeploymentDTO> _allVisibleDeployments = new();
        private List<WorldMapObjectDTO> _allVisibleMapObjects = new();

        public Guid? CurrentWorldId { get; private set; }
        public int? CurrentWorldSeed { get; private set; }

        public List<CityDTO> AllVisibleCities => _allVisibleCities;
        public List<UnitDeploymentDTO> AllVisibleDeployments => _allVisibleDeployments;
        public List<WorldMapObjectDTO> AllVisibleMapObjects => _allVisibleMapObjects;

        private void Awake() => InitializeManagerSingleton();

        private void InitializeManagerSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                Debug.Log("<color=cyan>[WorldMapStateManager]</color> Singleton Initialiseret.");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RequestWorldMapChunkData(short startX, short startY, byte width, byte height, bool forceRefresh = false)
        {
            Vector2Int chunkKey = new Vector2Int(startX, startY);

            if (!forceRefresh && _cachedWorldChunks.TryGetValue(chunkKey, out var existingData))
            {
                Debug.Log($"<color=cyan>[WorldMapStateManager]</color> Leverer Chunk {chunkKey} fra CACHE.");
                StartCoroutine(ExecuteDelayedCacheInvoke(existingData));
                return;
            }

            if (_activeNetworkRequests.Contains(chunkKey))
            {
                Debug.Log($"<color=orange>[WorldMapStateManager]</color> Anmodning om {chunkKey} er allerede i gang. Skipper.");
                return;
            }

            Debug.Log($"<color=cyan>[WorldMapStateManager]</color> Anmoder om NYT Chunk-data for {chunkKey} via netværk.");
            StartCoroutine(ExecuteGetChunkNetworkRequest(startX, startY, width, height));
        }

        private IEnumerator ExecuteDelayedCacheInvoke(WorldMapChunkResponseDTO data)
        {
            yield return null;
            HandleIncomingChunkData(data);
        }

        private IEnumerator ExecuteGetChunkNetworkRequest(short startX, short startY, byte width, byte height)
        {
            Vector2Int chunkKey = new Vector2Int(startX, startY);
            _activeNetworkRequests.Add(chunkKey);

            if (!CurrentWorldId.HasValue)
            {
                Debug.Log("<color=yellow>[WorldMapStateManager]</color> WorldId mangler. Starter resolution sequence...");
                yield return StartCoroutine(ExecutePlayerWorldProfileResolutionSequence());

                if (!CurrentWorldId.HasValue)
                {
                    Debug.LogError("<color=red>[WorldMapStateManager]</color> Kunne ikke resolvere WorldId. Afbryder chunk-request.");
                    _activeNetworkRequests.Remove(chunkKey);
                    yield break;
                }
            }

            var chunkRequest = new GetWorldMapChunkDTO
            {
                worldId = CurrentWorldId.Value,
                startX = startX,
                startY = startY,
                width = width,
                height = height
            };

            bool isFinished = false;
            yield return NetworkManager.Instance.World.GetWorldMapChunk(
                chunkRequest,
                NetworkManager.Instance.JwtToken,
                (response) =>
                {
                    if (response != null)
                    {
                        Debug.Log($"<color=green>[WorldMapStateManager]</color> Modtog data for chunk {chunkKey}.");
                        _cachedWorldChunks[chunkKey] = response;
                        if (!CurrentWorldSeed.HasValue) CurrentWorldSeed = response.WorldSeed;
                        HandleIncomingChunkData(response);
                    }
                    else
                    {
                        Debug.LogError($"<color=red>[WorldMapStateManager]</color> Modtog NULL respons for chunk {chunkKey}.");
                    }
                    isFinished = true;
                });

            float timer = 0;
            while (!isFinished && timer < 10f) { timer += Time.deltaTime; yield return null; }
            _activeNetworkRequests.Remove(chunkKey);
        }

        private void HandleIncomingChunkData(WorldMapChunkResponseDTO response)
        {
            Debug.Log($"<color=cyan>[WorldMapStateManager]</color> Behandler indkommende chunk-data. Byer: {response.Cities.Count}, Hære: {response.UnitDeployments.Count}");

            OnChunkDataReady?.Invoke(response);
            RefreshGlobalEntityLists();

            if (response.Cities != null) OnCitiesStateChanged?.Invoke(response.Cities);
            if (response.UnitDeployments != null) OnUnitDeploymentsStateChanged?.Invoke(response.UnitDeployments);
            if (response.MapObjects != null) OnMapObjectsStateChanged?.Invoke(response.MapObjects);
        }

        private void RefreshGlobalEntityLists()
        {
            _allVisibleCities = _cachedWorldChunks.Values.SelectMany(c => c.Cities).ToList();
            _allVisibleDeployments = _cachedWorldChunks.Values.SelectMany(c => c.UnitDeployments).ToList();
            _allVisibleMapObjects = _cachedWorldChunks.Values.SelectMany(c => c.MapObjects).ToList();

            Debug.Log($"<color=cyan>[WorldMapStateManager]</color> Globale lister opdateret. Total synlige hære: {_allVisibleDeployments.Count}");
        }

        private IEnumerator ExecutePlayerWorldProfileResolutionSequence()
        {
            if (string.IsNullOrEmpty(NetworkManager.Instance.WorldPlayerId)) yield break;
            Guid worldPlayerId = Guid.Parse(NetworkManager.Instance.WorldPlayerId);
            bool done = false;
            yield return NetworkManager.Instance.WorldPlayer.GetPlayerProfile(worldPlayerId, NetworkManager.Instance.JwtToken, (profile) =>
            {
                if (profile != null)
                {
                    CurrentWorldId = profile.WorldId;
                    Debug.Log($"<color=green>[WorldMapStateManager]</color> WorldId resolved: {CurrentWorldId}");
                    OnWorldIdResolved?.Invoke(CurrentWorldId.Value);
                }
                done = true;
            });
            while (!done) yield return null;
        }

        public void UpdateDeploymentInCache(UnitDeploymentDTO updatedDeployment)
        {
            int chunkX = Mathf.FloorToInt(updatedDeployment.CurrentX / 50f) * 50;
            int chunkY = Mathf.FloorToInt(updatedDeployment.CurrentY / 50f) * 50;
            Vector2Int key = new Vector2Int(chunkX, chunkY);

            Debug.Log($"<color=cyan>[WorldMapStateManager]</color> Manuel cache-opdatering for hær {updatedDeployment.Id} i chunk {key}.");

            if (_cachedWorldChunks.TryGetValue(key, out var chunk))
            {
                var existing = chunk.UnitDeployments.FirstOrDefault(u => u.Id == updatedDeployment.Id);
                if (existing != null) chunk.UnitDeployments.Remove(existing);

                chunk.UnitDeployments.Add(updatedDeployment);
                RefreshGlobalEntityLists();
                OnUnitDeploymentsStateChanged?.Invoke(new List<UnitDeploymentDTO> { updatedDeployment });
            }
            else
            {
                Debug.LogWarning($"<color=orange>[WorldMapStateManager]</color> Kunne ikke opdatere cache for {updatedDeployment.Id}. Chunken er ikke indlæst.");
            }
        }

        public void RemoveDeploymentFromCacheExplicitly(Guid deploymentId)
        {
            Debug.Log($"<color=red>[WorldMapStateManager]</color> Forsøger eksplicit fjernelse af hær {deploymentId} fra cache.");
            foreach (var chunk in _cachedWorldChunks.Values)
            {
                var existing = chunk.UnitDeployments.FirstOrDefault(u => u.Id == deploymentId);
                if (existing != null)
                {
                    chunk.UnitDeployments.Remove(existing);
                    Debug.Log($"<color=red>[WorldMapStateManager]</color> Hær {deploymentId} fjernet fra chunk {chunk.ChunkX}, {chunk.ChunkY}.");
                    RefreshGlobalEntityLists();
                    OnUnitDeploymentsStateChanged?.Invoke(chunk.UnitDeployments);
                }
            }
        }

        public void InvalidateAllCachedChunks()
        {
            Debug.Log("<color=red>[WorldMapStateManager]</color> INVALIDERER ALT CACHE.");
            _cachedWorldChunks.Clear();
            _activeNetworkRequests.Clear();
            _allVisibleCities.Clear();
            _allVisibleDeployments.Clear();
            _allVisibleMapObjects.Clear();
        }
    }
}