using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
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

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "ClientResearchService");
            }
        }

        public IEnumerator StartResearchProcess(Guid worldPlayerId, string researchId, string jwtToken, Action<bool, string> callback)
        {
            string url = $"{_controllerBaseUrl}/start/{worldPlayerId}/{researchId}";

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, new { }, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, "Research started");
                }
                else
                {
                    callback?.Invoke(false, BackendRequestHelper.GetErrorMessage(request));
                }
            }
        }

        public IEnumerator CancelActiveResearch(Guid worldPlayerId, Guid jobId, string jwtToken, Action<bool, string> callback)
        {
            string url = $"{_controllerBaseUrl}/cancel/{worldPlayerId}/{jobId}";

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, new { }, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, "Research cancelled");
                }
                else
                {
                    callback?.Invoke(false, BackendRequestHelper.GetErrorMessage(request));
                }
            }
        }
    }
}
