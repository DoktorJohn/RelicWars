using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Network.Models;
using Assets._Project.Scripts.Domain.DTOs;
using Project.Network.Helper;

namespace Project.Network
{
    public class ClientCityService
    {
        private readonly string _baseUrl;

        public ClientCityService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/City";
        }

        public IEnumerator GetDetailedCityInfo(Guid cityId, string jwtToken, Action<CityControllerGetDetailedCityInformationDTO> callback)
        {
            string url = $"{_baseUrl}/GetDetailedCityInformation/{cityId}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<CityControllerGetDetailedCityInformationDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[City] Deserialization Error: {ex.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[City] GetDetailedInfo Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator GetSenateAvailableBuildings(Guid cityId, string jwtToken, Action<List<AvailableBuildingDTO>> callback)
        {
            string url = $"{_baseUrl}/{cityId}/senate/available-buildings";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = JsonConvert.DeserializeObject<List<AvailableBuildingDTO>>(request.downloadHandler.text);
                    callback?.Invoke(data);
                }
                else
                {
                    Debug.LogError($"[City] GetSenateData Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}