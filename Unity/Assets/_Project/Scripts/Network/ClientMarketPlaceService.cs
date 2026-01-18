using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Scripts.Domain.DTOs;

namespace Project.Network
{
    public class ClientMarketPlaceService
    {
        private readonly string _baseUrl;

        public ClientMarketPlaceService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetMarketPlaceInfo(Guid cityId, string token, Action<MarketPlaceInfoDTO> callback)
        {
            string url = $"{_baseUrl}/MarketPlaceBuilding/{cityId}/marketPlace";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // FIX: Deserialize Single Object directly
                        var data = JsonConvert.DeserializeObject<MarketPlaceInfoDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ClientMarketPlace] JSON Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientMarketPlace] Network Error: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}