using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Assets._Project.Scripts.Domain.DTOs;
using Assets.Scripts.Domain.Enums;
using Newtonsoft.Json;

namespace Project.Network.Manager
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

        public IEnumerator GetBarracksInfo(Guid cityId, string token, Action<List<BarracksInfoDTO>> callback)
        {
            // Antager endpoint: /api/building/{cityId}/barracks
            string url = $"{_baseUrl}/building/{cityId}/barracks";

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
                        var data = JsonConvert.DeserializeObject<List<BarracksInfoDTO>>(json);
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

        public IEnumerator GetStableInfo(Guid cityId, string token, Action<List<StableInfoDTO>> callback)
        {
            // Antager endpoint: /api/building/{cityId}/stable
            string url = $"{_baseUrl}/building/{cityId}/stable";

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
                        var data = JsonConvert.DeserializeObject<List<StableInfoDTO>>(json);
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
        public IEnumerator GetAcademyInfo(Guid cityId, string token, Action<List<AcademyInfoDTO>> callback)
        {
            // Antager endpoint: /api/building/{cityId}/academy
            string url = $"{_baseUrl}/building/{cityId}/academy";

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
                        var data = JsonConvert.DeserializeObject<List<AcademyInfoDTO>>(json);
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
            string url = $"{_baseUrl}/building/{cityId}/wall";

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
            string url = $"{_baseUrl}/building/{cityId}/housing";

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
            string url = $"{_baseUrl}/building/{cityId}/resource/{type}";

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

        public IEnumerator GetWorkshopInfo(Guid cityId, string token, Action<List<WorkshopInfoDTO>> callback)
        {
            // Antager at endpointet er /api/building/{cityId}/workshop
            // Husk at lave controller-metoden i backend hvis den mangler, 
            // men du bad specifikt om client-siden her.
            string url = $"{_baseUrl}/building/{cityId}/workshop";

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
                        var data = JsonConvert.DeserializeObject<List<WorkshopInfoDTO>>(json);
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
            string url = $"{_baseUrl}/building/{cityId}/warehouse";

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

    // Læg denne klasse her i bunden af filen
    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // VIGTIGT: Dette må kun bruges i development! 
            // Det gør forbindelsen usikker overfor hackere, men er nødvendigt for localhost.
            return true;
        }
    }
}