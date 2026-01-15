using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Assets.Scripts.Domain.Enums;
using Project.Scripts.Domain.DTOs;

namespace Project.Network.Manager
{
    public class ClientWorkshopService
    {
        private readonly string _baseUrl;

        public ClientWorkshopService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetWorkshopOverviewInformation(Guid cityId, string token, Action<WorkshopFullViewDTO> callback)
        {
            string url = $"{_baseUrl}/workshop/{cityId}/overview";

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
                        var data = JsonConvert.DeserializeObject<WorkshopFullViewDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[ClientWorkshopService] JSON Error: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[ClientWorkshopService] Network Error: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator RecruitUnits(Guid cityId, UnitTypeEnum unitType, int amount, string token, Action<bool, string> callback)
        {
            string url = $"{_baseUrl}/workshop/{cityId}/recruit";

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