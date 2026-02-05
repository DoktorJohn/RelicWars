using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project.Network.Models;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.City
{
    public class WorldMapStateManager : MonoBehaviour
    {
        public static WorldMapStateManager Instance { get; private set; }
        public event Action<WorldMapChunkResponseDTO> OnChunkDataReady;
        public event Action<Guid> OnWorldIdResolved;
        private Dictionary<Vector2Int, WorldMapChunkResponseDTO> _cachedWorldChunks = new Dictionary<Vector2Int, WorldMapChunkResponseDTO>();
        private HashSet<Vector2Int> _activeNetworkRequests = new HashSet<Vector2Int>();
        public Guid? CurrentWorldId { get; private set; }
        public int? CurrentWorldSeed { get; private set; }

        private void Awake()
        {
            InitializeManagerSingleton();
        }

        private void InitializeManagerSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
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
                StartCoroutine(ExecuteDelayedCacheInvoke(existingData));
                return;
            }

            if (_activeNetworkRequests.Contains(chunkKey)) return;

            StartCoroutine(ExecuteGetChunkNetworkRequest(startX, startY, width, height));
        }

        private IEnumerator ExecuteDelayedCacheInvoke(WorldMapChunkResponseDTO data)

        {
            yield return null;
            OnChunkDataReady?.Invoke(data);
        }

        private IEnumerator ExecuteGetChunkNetworkRequest(short startX, short startY, byte width, byte height)

        {

            Vector2Int chunkKey = new Vector2Int(startX, startY);

            _activeNetworkRequests.Add(chunkKey);

            if (!CurrentWorldId.HasValue)

            {

                yield return StartCoroutine(ExecutePlayerWorldProfileResolutionSequence());

                if (!CurrentWorldId.HasValue)

                {

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
                        _cachedWorldChunks[chunkKey] = response;

                        if (!CurrentWorldSeed.HasValue) CurrentWorldSeed = response.WorldSeed;

                        OnChunkDataReady?.Invoke(response);

                    }

                    isFinished = true;

                });

            float timer = 0;

            while (!isFinished && timer < 10f) { timer += Time.deltaTime; yield return null; }

            _activeNetworkRequests.Remove(chunkKey);

        }
        private IEnumerator ExecutePlayerWorldProfileResolutionSequence()
        {
            if (string.IsNullOrEmpty(NetworkManager.Instance.WorldPlayerId)) yield break;

            Guid worldPlayerId = Guid.Parse(NetworkManager.Instance.WorldPlayerId);

            bool done = false;

            yield return NetworkManager.Instance.WorldPlayer.GetPlayerProfile(worldPlayerId, NetworkManager.Instance.JwtToken, (profile) =>
            {

                if (profile != null) CurrentWorldId = profile.WorldId;

                done = true;

            });

            while (!done) yield return null;

        }
        public void UpdateDeploymentInCache(UnitDeploymentDTO updatedDeployment)
        {
            int chunkX = Mathf.FloorToInt(updatedDeployment.CurrentX / 50f) * 50;

            int chunkY = Mathf.FloorToInt(updatedDeployment.CurrentY / 50f) * 50;

            Vector2Int key = new Vector2Int(chunkX, chunkY);

            if (_cachedWorldChunks.TryGetValue(key, out var chunk))

            {

                var existing = chunk.UnitDeployments.FirstOrDefault(u => u.Id == updatedDeployment.Id);

                if (existing != null)

                {

                    chunk.UnitDeployments.Remove(existing);

                }

                chunk.UnitDeployments.Add(updatedDeployment);

                OnChunkDataReady?.Invoke(chunk);

            }

        }
        public UnitDeploymentDTO GetUnitDeploymentByCoordinate(int targetX, int targetY)
        {
            foreach (var chunk in _cachedWorldChunks.Values)
            {
                if (chunk.UnitDeployments == null) continue;

                var deployment = chunk.UnitDeployments.FirstOrDefault(u => u.CurrentX == targetX && u.CurrentY == targetY);

                if (deployment != null) return deployment;

            }

            return null;

        }
        public CityDTO GetCityByCoordinate(int targetX, int targetY)
        {
            foreach (var chunk in _cachedWorldChunks.Values)
            {
                if (chunk.Cities == null) continue;

                var city = chunk.Cities.FirstOrDefault(c => c.X == targetX && c.Y == targetY);

                if (city != null) return city;
            }
            return null;
        }

        public void InvalidateAllCachedChunks()
        {
            _cachedWorldChunks.Clear();
            _activeNetworkRequests.Clear();

        }
    }
}