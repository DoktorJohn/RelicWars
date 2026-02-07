using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;

namespace Project.Network
{
    public class ClientStableService
    {
        private readonly string _baseUrl;

        public ClientStableService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetStableOverviewInformation(
    Guid cityId,
    string token,
    Action<StableFullViewDTO> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/stableOverview";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[StableService] NETWORK ERROR: {request.error}");
                    callback?.Invoke(null);
                    yield break;
                }

                string json = request.downloadHandler.text;

                Debug.Log("[StableService] RAW JSON:");
                Debug.Log(json);

                try
                {
                    var data = JsonConvert.DeserializeObject<StableFullViewDTO>(json);

                    Debug.Log("[StableService] DESERIALIZE OK");
                    Debug.Log($"BuildingLevel: {data?.BuildingLevel}");
                    Debug.Log($"AvailableUnits null? {data?.AvailableUnits == null}");
                    Debug.Log($"RecruitmentQueue null? {data?.RecruitmentQueue == null}");

                    callback?.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogError("[StableService] JSON DESERIALIZE FAILED");
                    Debug.LogError(e); // 👈 FULD STACKTRACE
                    Debug.LogError("[StableService] RAW JSON WAS:");
                    Debug.LogError(json);

                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator RecruitUnits(Guid cityId, UnitTypeEnum unitType, int amount, string token, Action<bool, string> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/stableRecruit";

            var requestBody = new RecruitUnitRequestDTO
            {
                UnitType = unitType,
                Amount = amount
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.certificateHandler = new BypassCertificateHandler();
                request.timeout = 10;

                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                string responseText = request.downloadHandler.text;
                string message = "Unknown error";

                try
                {
                    var responseObj = JsonConvert.DeserializeObject<BackendMessageDTO>(responseText);
                    message = responseObj?.Message ?? responseText;
                }
                catch { message = request.error; }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, message);
                }
                else
                {
                    callback?.Invoke(false, message);
                }
            }
        }
    }
}