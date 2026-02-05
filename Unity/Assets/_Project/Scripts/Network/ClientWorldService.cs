using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Network.Models;

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
                    Debug.LogError($"[World] Fetch Available Worlds Failed: {request.error} - URL: {url}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator GetWorldMapChunk(GetWorldMapChunkDTO request, string token, Action<WorldMapChunkResponseDTO> callback)
        {
            string url = $"{_baseUrl}/chunk?worldId={request.worldId}&startX={request.startX}&startY={request.startY}&width={request.width}&height={request.height}";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreateGetRequest(url, token))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    WorldMapChunkResponseDTO responseData = null;
                    try
                    {
                        string json = webRequest.downloadHandler.text;
                        if (!string.IsNullOrEmpty(json))
                        {
                            responseData = JsonConvert.DeserializeObject<WorldMapChunkResponseDTO>(json);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[ClientWorldService] JSON Deserialization Error: {exception.Message}");
                    }

                    callback?.Invoke(responseData);
                }
                else
                {
                    // Vi inkluderer URL'en i fejlen for nemmere debugging fremover
                    Debug.LogError($"[ClientWorldService] Network Request Failed: {webRequest.error} - URL: {url}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}