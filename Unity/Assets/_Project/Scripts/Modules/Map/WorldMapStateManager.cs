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
        public event Action<List<WorldMapObjectDTO>> OnMapObjectsStateChanged;
        public event Action<WorldMapChunkResponseDTO> OnChunkDataReady;
        public event Action<Guid> OnWorldIdResolved;

        private readonly Dictionary<Vector2Int, WorldMapChunkResponseDTO> _cachedWorldChunks = new();
        private readonly HashSet<Vector2Int> _activeNetworkRequests = new();

        private List<CityDTO> _allVisibleCities = new();
        private List<WorldMapObjectDTO> _allVisibleMapObjects = new();

        public Guid? CurrentWorldId { get; private set; }
        public int? CurrentWorldSeed { get; private set; }

        public List<CityDTO> AllVisibleCities => _allVisibleCities;
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
            Debug.Log($"<color=cyan>[WorldMapStateManager]</color> Behandler indkommende chunk-data. Byer: {response.Cities.Count}");

            OnChunkDataReady?.Invoke(response);
            RefreshGlobalEntityLists();

            if (response.Cities != null) OnCitiesStateChanged?.Invoke(response.Cities);
            if (response.MapObjects != null) OnMapObjectsStateChanged?.Invoke(response.MapObjects);
        }

        private void RefreshGlobalEntityLists()
        {
            _allVisibleCities = _cachedWorldChunks.Values.SelectMany(c => c.Cities).ToList();
            _allVisibleMapObjects = _cachedWorldChunks.Values.SelectMany(c => c.MapObjects).ToList();
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

        public void InvalidateAllCachedChunks()
        {
            Debug.Log("<color=red>[WorldMapStateManager]</color> INVALIDERER ALT CACHE.");
            CurrentWorldId = null;
            CurrentWorldSeed = null;
            _cachedWorldChunks.Clear();
            _activeNetworkRequests.Clear();
            _allVisibleCities.Clear();
            _allVisibleMapObjects.Clear();
        }

        public void ResetForLogout()
        {
            StopAllCoroutines();
            InvalidateAllCachedChunks();
        }
    }
}
