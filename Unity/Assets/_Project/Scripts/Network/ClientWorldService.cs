using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Network.Helper;

namespace Project.Network
{
    public class ClientWorldService
    {
        private readonly string _baseUrl;

        public ClientWorldService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/World";
        }

        public IEnumerator GetAvailableWorlds(Action<List<WorldAvailableResponseDTO>> callback)
        {
            string url = $"{_baseUrl}/available-worlds";

            using (var request = BackendRequestHelper.CreateGetRequest(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var worlds = JsonConvert.DeserializeObject<List<WorldAvailableResponseDTO>>(request.downloadHandler.text);
                    callback?.Invoke(worlds);
                }
                else
                {
                    Debug.LogError($"[World] Fetch Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Fetches world map data for a specific area using a DTO to define the bounds.
        /// </summary>
        public IEnumerator GetWorldMapChunk(GetWorldMapChunkDTO chunkDto, string token, Action<WorldMapChunkResponseDTO> callback)
        {
            string queryString = $"?worldId={chunkDto.worldId}&startX={chunkDto.startX}&startY={chunkDto.startY}&width={chunkDto.width}&height={chunkDto.height}";
            string url = $"{_baseUrl}/chunk{queryString}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var chunkData = JsonConvert.DeserializeObject<WorldMapChunkResponseDTO>(request.downloadHandler.text);
                        callback?.Invoke(chunkData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ClientWorldService] JSON Parse Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientWorldService] Network Error fetching chunk: {request.error} | {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

    }
}