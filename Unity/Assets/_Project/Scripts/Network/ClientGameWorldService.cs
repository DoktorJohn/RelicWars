using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Network.Helper;

namespace Project.Network
{
    public class ClientGameWorldService
    {
        private readonly string _baseUrl;

        public ClientGameWorldService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/GameWorld";
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
                    Debug.LogError($"[GameWorld] Fetch Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator JoinWorld(string playerId, Guid worldId, string jwtToken, Action<PlayerWorldJoinResponse> callback)
        {
            var payload = new { PlayerProfileId = playerId, WorldId = worldId.ToString() };
            string url = $"{_baseUrl}/join";

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<PlayerWorldJoinResponse>(request.downloadHandler.text);
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[GameWorld] Join Failed: {request.downloadHandler.text}");
                    callback?.Invoke(new PlayerWorldJoinResponse
                    {
                        ConnectionSuccessful = false,
                        Message = "Failed to join",
                        ActiveCityId = null
                    });
                }
            }
        }
    }
}