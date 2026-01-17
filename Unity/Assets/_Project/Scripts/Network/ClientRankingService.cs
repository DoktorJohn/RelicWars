using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;

namespace Project.Network
{
    public class ClientRankingService
    {
        private readonly string _baseUrl;

        public ClientRankingService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/Ranking";
        }

        /// <summary>
        /// Henter den fulde globale rangliste.
        /// </summary>
        public IEnumerator GetGlobalRankings(string jwtToken, Action<List<RankingEntryDataDTO>> callback)
        {
            string url = $"{_baseUrl}/ranking";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<List<RankingEntryDataDTO>>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[Ranking] Deserialization Error: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Ranking] GetRankings Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Henter rangliste-data for en specifik spiller.
        /// </summary>
        public IEnumerator GetRankingByPlayerId(Guid worldPlayerId, string jwtToken, Action<RankingEntryDataDTO> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/getRankingById";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<RankingEntryDataDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[Ranking] Deserialization Error: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Ranking] GetRankingById Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}