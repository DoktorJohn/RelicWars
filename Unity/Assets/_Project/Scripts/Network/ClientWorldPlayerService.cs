using Assets._Project.Scripts.Domain.Enums;
using Project.Network.Helper;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Project.Scripts.Domain.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
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
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "WorldPlayer",
                    errorRequest => new WorldPlayerJoinResponse
                    {
                        ConnectionSuccessful = false,
                        Message = BackendRequestHelper.GetErrorMessage(errorRequest),
                        ActiveCityId = null,
                        WorldPlayerId = null,
                        SelectedIdeology = IdeologyTypeEnum.None
                    });
            }
        }

        public IEnumerator GetWorldPlayerEconomy(Guid worldPlayerId, string jwtToken, Action<WorldPlayerEconomyDTO> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/economy";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "WorldPlayer");
            }
        }

        public IEnumerator SelectIdeology(Guid worldPlayerId, IdeologyTypeEnum ideology, string jwtToken, Action<WorldPlayerSelectIdeologyResponse> callback)
        {
            var payload = new { WorldPlayerId = worldPlayerId.ToString(), Ideology = ideology };
            string url = $"{_baseUrl}/selectIdeology";

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "WorldPlayer",
                    errorRequest => new WorldPlayerSelectIdeologyResponse
                    {
                        ConnectionSuccessful = false,
                        Message = BackendRequestHelper.GetErrorMessage(errorRequest)
                    });
            }
        }

        public IEnumerator SearchPlayers(Guid worldId, string query, string jwtToken, Action<List<PlayerSearchResultDTO>> callback)
        {
            string url = $"{_baseUrl}/search?worldId={worldId}&query={UnityWebRequest.EscapeURL(query)}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "WorldPlayer", _ => new List<PlayerSearchResultDTO>());
            }
        }

        public IEnumerator GetPlayerProfile(Guid worldPlayerId, string jwtToken, Action<WorldPlayerProfileDTO> callback)
        {
            string requestUrl = $"{_baseUrl}/{worldPlayerId}/getWorldPlayerProfile";

            using (var webRequest = BackendRequestHelper.CreateGetRequest(requestUrl, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(webRequest, callback, "WorldPlayer");
            }
        }

        public IEnumerator UpdatePlayerDescription(Guid worldPlayerId, string description, string jwtToken, Action<WorldPlayerProfileDTO> callback)
        {
            string requestUrl = $"{_baseUrl}/{worldPlayerId}/description";
            var payload = new UpdateWorldPlayerDescriptionRequestDTO
            {
                Description = description
            };

            using (var webRequest = BackendRequestHelper.CreatePutRequest(requestUrl, payload, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(webRequest, callback, "WorldPlayer");
            }
        }
    }
}
