using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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

        public IEnumerator GetTownHallAvailableBuildings(Guid cityId, string jwtToken, Action<List<AvailableBuildingDTO>> callback)
        {
            string url = $"{_baseUrl}/{cityId}/townHall/available-buildings";

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