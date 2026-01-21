using Newtonsoft.Json;
using Project.Network;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Scripts.Network
{
    public class ClientResearchService
    {
        private readonly string _controllerBaseUrl;

        public ClientResearchService(string apiBaseUrl)
        {
            _controllerBaseUrl = $"{apiBaseUrl}/Research";
        }

        public IEnumerator GetResearchTreeState(Guid worldPlayerId, string jwtToken, Action<ResearchTreeDTO> callback)
        {
            string url = $"{_controllerBaseUrl}/tree/{worldPlayerId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + jwtToken);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = JsonConvert.DeserializeObject<ResearchTreeDTO>(request.downloadHandler.text);
                    callback?.Invoke(data);
                }
                else
                {
                    Debug.LogError($"[ClientResearchService] Kunne ikke hente research træ: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator StartResearchProcess(Guid worldPlayerId, string researchId, string jwtToken, Action<bool, string> callback)
        {
            string url = $"{_controllerBaseUrl}/start/{worldPlayerId}/{researchId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + jwtToken);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, "Research started");
                }
                else
                {
                    callback?.Invoke(false, request.downloadHandler.text);
                }
            }
        }

        public IEnumerator CancelActiveResearch(Guid worldPlayerId, Guid jobId, string jwtToken, Action<bool, string> callback)
        {
            string url = $"{_controllerBaseUrl}/cancel/{worldPlayerId}/{jobId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.certificateHandler = new BypassCertificateHandler();
                request.SetRequestHeader("Authorization", "Bearer " + jwtToken);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, "Research cancelled");
                }
                else
                {
                    callback?.Invoke(false, request.downloadHandler.text);
                }
            }
        }
    }
}