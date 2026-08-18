using System;
using System.Collections;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using UnityEngine.Networking;

namespace Project.Network.Manager
{
    public sealed class ClientDailyObjectivesService
    {
        private readonly string _baseUrl;

        public ClientDailyObjectivesService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/DailyObjectives";
        }

        public IEnumerator Get(
            Guid worldPlayerId,
            string jwtToken,
            Action<DailyObjectivesDTO> callback,
            Action<string> errorCallback = null)
        {
            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest($"{_baseUrl}/{worldPlayerId}", jwtToken))
            {
                yield return BackendRequestHelper.SendJson<DailyObjectivesDTO>(
                    request,
                    callback,
                    "DailyObjectives",
                    failedRequest =>
                    {
                        errorCallback?.Invoke(BackendRequestHelper.GetErrorMessage(failedRequest));
                        return null;
                    });
            }
        }

        public IEnumerator Collect(
            Guid worldPlayerId,
            int definitionId,
            Guid cityId,
            string jwtToken,
            Action<DailyObjectivesDTO> callback,
            Action<string> errorCallback = null)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/{definitionId}/collect";
            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, new { CityId = cityId }, jwtToken))
            {
                yield return BackendRequestHelper.SendJson<DailyObjectivesDTO>(
                    request,
                    callback,
                    "DailyObjectives",
                    failedRequest =>
                    {
                        errorCallback?.Invoke(BackendRequestHelper.GetErrorMessage(failedRequest));
                        return null;
                    });
            }
        }
    }
}
