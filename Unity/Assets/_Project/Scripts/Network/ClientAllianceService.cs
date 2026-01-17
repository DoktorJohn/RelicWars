using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Network
{
    public class ClientAllianceService
    {
        private readonly string _baseUrl;

        public ClientAllianceService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/Alliance";
        }

        public IEnumerator GetAllianceInfo(Guid allianceId, string jwtToken, Action<AllianceDTO> callback)
        {
            string url = $"{_baseUrl}/getAllianceInfo/{allianceId}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = JsonConvert.DeserializeObject<AllianceDTO>(request.downloadHandler.text);
                    callback?.Invoke(data);
                }
                else
                {
                    Debug.LogError($"[Alliance] GetInfo Error: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator CreateAlliance(CreateAllianceDTO dto, string jwtToken, Action<AllianceDTO> callback)
        {
            // OBS: Ret din backend controller fra [HttpGet("create")] til [HttpPost("create")]
            string url = $"{_baseUrl}/create";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = JsonConvert.DeserializeObject<AllianceDTO>(request.downloadHandler.text);
                    callback?.Invoke(data);
                }
                else
                {
                    Debug.LogError($"[Alliance] Create Error: {request.error} - {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator DisbandAlliance(DisbandAllianceDTO dto, string jwtToken, Action<bool> callback)
        {
            // OBS: Ret din backend controller til [HttpPost] eller [HttpDelete]
            string url = $"{_baseUrl}/disband";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Alliance] Disband Error: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator InviteToAlliance(InviteToAllianceDTO dto, string jwtToken, Action<bool> callback)
        {
            // OBS: Ret din backend controller til [HttpPost]
            string url = $"{_baseUrl}/inviteToAlliance";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Alliance] Invite Error: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator KickPlayer(KickPlayerFromAllianceDTO dto, string jwtToken, Action<bool> callback)
        {
            // OBS: Ret din backend controller til [HttpPost]
            string url = $"{_baseUrl}/kickPlayer";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Alliance] Kick Error: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }
    }
}