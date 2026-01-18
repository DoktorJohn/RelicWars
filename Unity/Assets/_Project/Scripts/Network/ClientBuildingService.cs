using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Assets.Scripts.Domain.Enums;
using Newtonsoft.Json;
using Project.Scripts.Domain.DTOs;

namespace Project.Network
{
    public class ClientBuildingService
    {
        private readonly string _baseUrl;

        public ClientBuildingService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator UpgradeBuilding(Guid cityId, BuildingTypeEnum type, string token, Action<bool, string> callback)
        {
            string url = $"{_baseUrl}/building/{cityId}/upgrade/{type}";
            byte[] bodyRaw = new byte[0];

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.certificateHandler = new BypassCertificateHandler();

                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, request.downloadHandler.text);
                }
                else
                {
                    callback?.Invoke(false, request.error + ": " + request.downloadHandler.text);
                }
            }
        }

        public IEnumerator GetUniversityInfo(Guid cityId, string token, Action<List<UniversityInfoDTO>> callback)
        {
            // Antager endpoint: /api/building/{cityId}/university
            string url = $"{_baseUrl}/miscbuilding/{cityId}/university";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    try
                    {
                        var data = JsonConvert.DeserializeObject<List<UniversityInfoDTO>>(json);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ClientBuildingService] JSON Parse Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientBuildingService] Network Error: {request.error} | {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator GetWallInfo(Guid cityId, string token, Action<List<WallInfoDTO>> callback)
        {
            string url = $"{_baseUrl}/miscbuilding/{cityId}/wall";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    try
                    {
                        // Newtonsoft håndterer automatisk den nestede ModifierDTO
                        var data = JsonConvert.DeserializeObject<List<WallInfoDTO>>(json);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ClientBuildingService] JSON Parse Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientBuildingService] Network Error: {request.error} | {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }
        public IEnumerator GetHousingProjection(Guid cityId, string token, Action<List<HousingProjectionDTO>> callback)
        {
            string url = $"{_baseUrl}/economybuilding/{cityId}/housing";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    try
                    {
                        var data = JsonConvert.DeserializeObject<List<HousingProjectionDTO>>(json);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ClientBuildingService] JSON Parse Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientBuildingService] Network Error: {request.error} | {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }
        public IEnumerator GetResourceProductionInfo(Guid cityId, BuildingTypeEnum type, string token, Action<List<ResourceBuildingInfoDTO>> callback)
        {
            // URL matcher din backend controller: [HttpGet("{cityId}/resource/{buildingType}")]
            string url = $"{_baseUrl}/economybuilding/{cityId}/resource/{type}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Dev Environment Fix
                request.certificateHandler = new BypassCertificateHandler();

                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    try
                    {
                        // Newtonsoft klarer lister direkte
                        var data = JsonConvert.DeserializeObject<List<ResourceBuildingInfoDTO>>(json);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ClientBuildingService] JSON Parse Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientBuildingService] Network Error: {request.error} | {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator GetWarehouseProjection(Guid cityId, string token, Action<List<WarehouseProjectionDTO>> callback)
        {
            string url = $"{_baseUrl}/economybuilding/{cityId}/warehouse";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    try
                    {
                        // Newtonsoft håndterer lister og case-sensitivity automatisk!
                        var data = JsonConvert.DeserializeObject<List<WarehouseProjectionDTO>>(json);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ClientBuildingService] JSON Parse Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientBuildingService] Network Error: {request.error} | {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

    }
}