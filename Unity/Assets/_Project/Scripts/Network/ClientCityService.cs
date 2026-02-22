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

        public IEnumerator GetCityResources(Guid cityId, string jwtToken, Action<CityResourcesDTO> callback)
        {
            string url = $"{_baseUrl}/{cityId}/resources";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<CityResourcesDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[City] Deserialization Error (Resources): {ex.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[City] GetCityResources Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator GetCityOverviewHUD(Guid cityId, string jwtToken, Action<CityOverviewHUDDTO> callback)
        {
            string url = $"{_baseUrl}/CityOverviewHUD/{cityId}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<CityOverviewHUDDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[City] Deserialization Error (OverviewHUD): {ex.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[City] GetCityOverviewHUD Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
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

        public IEnumerator GetPlayerCities(Guid cityId, string jwtToken, Action<List<CityDTO>> callback)
        {
            string url = $"{_baseUrl}/{cityId}/my-cities";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<List<CityDTO>>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[City] Deserialization Error (GetPlayerCities): {ex.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[City] GetPlayerCities Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator ChangeCityName(Guid cityId, string newName, string authenticationToken, Action<ChangeCityNameResponseDTO> callback)
        {
            string url = $"{_baseUrl}/ChangeCityName/{cityId}/{newName}";
            byte[] bodyRaw = new byte[0];

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + authenticationToken);

                yield return request.SendWebRequest();

                ChangeCityNameResponseDTO resultData;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        resultData = JsonConvert.DeserializeObject<ChangeCityNameResponseDTO>(request.downloadHandler.text);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[IdeologyFocusService] JSON Parse Error on Enact: {exception.Message}");
                        resultData = new ChangeCityNameResponseDTO();
                        resultData.Success = false;
                        resultData.Message = "Exception thrown";
                    }
                }
                else
                {
                    try
                    {
                        string errorFromBackend = request.downloadHandler.text;
                        resultData = new ChangeCityNameResponseDTO
                        {
                            CityId = cityId,
                            CityName = newName,
                            Success = false,
                            Message = "Some error happened in the backend"
                        };
                    }
                    catch
                    {
                        resultData = new ChangeCityNameResponseDTO
                        {
                            CityId = cityId,
                            CityName = newName,
                            Success = false,
                            Message = "Unknown error"
                        };
                    }

                    Debug.LogWarning($"[IdeologyFocusService] Enact Failed: {resultData.Message}");
                }

                callback?.Invoke(resultData);
            }
        }
    }
}