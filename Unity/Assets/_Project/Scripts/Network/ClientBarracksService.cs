using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Scripts.Domain.DTOs;
using Assets.Scripts.Domain.Enums;

namespace Project.Network
{
    public class ClientBarracksService
    {
        private readonly string _baseUrl;

        public ClientBarracksService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetBarracksOverviewInformation(Guid cityId, string token, Action<BarracksFullViewDTO> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/barracksOverview";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<BarracksFullViewDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[BarracksService] JSON Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[BarracksService] Network Error: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        // ÆNDRING: Callback tager nu også en string 'message' med
        public IEnumerator RecruitUnits(Guid cityId, UnitTypeEnum unitType, int amount, string token, Action<bool, string> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/barracksRecruit";

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

                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                string responseText = request.downloadHandler.text;
                string message = "Unknown error";

                // Forsøg at trække beskeden ud af JSON svaret
                try
                {
                    // Backend sender: { "Message": "..." } både ved succes og nogle fejl
                    var responseObj = JsonConvert.DeserializeObject<BackendMessageDTO>(responseText);
                    if (responseObj != null && !string.IsNullOrEmpty(responseObj.Message))
                    {
                        message = responseObj.Message;
                    }
                    else
                    {
                        // Hvis backenden bare sender en rå string ved fejl (BadRequest("Tekst"))
                        message = responseText;
                    }
                }
                catch
                {
                    message = request.error;
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, message);
                }
                else
                {
                    Debug.LogError($"[BarracksService] Recruit Failed: {message}");
                    callback?.Invoke(false, message);
                }
            }
        }
    }

    // Hvis denne klasse allerede findes i NetworkManager.cs (i samme namespace), 
    // kan du slette den herfra for at undgå "Duplicate definition" fejl.
    // Ellers behold den her.
    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}