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

    }
}