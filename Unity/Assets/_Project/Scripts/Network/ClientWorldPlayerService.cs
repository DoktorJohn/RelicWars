using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Network
{
    public class ClientWorldPlayerService
    {
        private readonly string _baseUrl;

        public ClientWorldPlayerService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/WorldPlayer";
        }

        public IEnumerator JoinWorld(string playerId, Guid worldId, string jwtToken, Action<WorldPlayerJoinResponse> callback)
        {
            var payload = new { PlayerProfileId = playerId, WorldId = worldId.ToString() };
            string url = $"{_baseUrl}/join";

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<WorldPlayerJoinResponse>(request.downloadHandler.text);
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[World] Join Failed: {request.downloadHandler.text}");
                    callback?.Invoke(new WorldPlayerJoinResponse
                    {
                        ConnectionSuccessful = false,
                        Message = "Failed to join",
                        ActiveCityId = null,
                        WorldPlayerId = null,
                        SelectedIdeology = IdeologyTypeEnum.None
                    });
                }
            }
        }

        public IEnumerator GetWorldPlayerEconomy(Guid worldPlayerId, string jwtToken, Action<WorldPlayerEconomyDTO> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/economy";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<WorldPlayerEconomyDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[WorldPlayer] Deserialization Error (Economy): {ex.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[WorldPlayer] GetWorldPlayerEconomy Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator SelectIdeology(Guid worldPlayerId, IdeologyTypeEnum ideology, string jwtToken, Action<WorldPlayerSelectIdeologyResponse> callback)
        {
            var payload = new { WorldPlayerId = worldPlayerId.ToString(), Ideology = ideology };
            string url = $"{_baseUrl}/selectIdeology";

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<WorldPlayerSelectIdeologyResponse>(request.downloadHandler.text);
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[WorldPlayer] Select Ideology Failed: {request.downloadHandler.text}");

                    // Her bruger vi Object Initializer mønstret i stedet for constructor
                    callback?.Invoke(new WorldPlayerSelectIdeologyResponse
                    {
                        ConnectionSuccessful = false,
                        Message = "Internal client error or server rejection."
                    });
                }
            }
        }

        public IEnumerator GetPlayerProfile(Guid worldPlayerId, string jwtToken, Action<WorldPlayerProfileDTO> callback)
        {
            string requestUrl = $"{_baseUrl}/{worldPlayerId}/getWorldPlayerProfile";

            using (var webRequest = BackendRequestHelper.CreateGetRequest(requestUrl, jwtToken))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var deserializedProfileData = JsonConvert.DeserializeObject<WorldPlayerProfileDTO>(webRequest.downloadHandler.text);
                        callback?.Invoke(deserializedProfileData);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[WorldPlayer] Deserialiseringsfejl: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[WorldPlayer] Kunne ikke hente profil: {webRequest.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}
