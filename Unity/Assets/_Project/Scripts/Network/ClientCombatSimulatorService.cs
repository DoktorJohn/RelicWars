using System;
using System.Collections;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using UnityEngine.Networking;

namespace Project.Network.Manager
{
    public class ClientCombatSimulatorService
    {
        private readonly string _baseUrl;

        public ClientCombatSimulatorService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/CombatSimulator";
        }

        public IEnumerator Simulate(
            CombatSimulationRequestDTO requestDto,
            string jwtToken,
            Action<CombatSimulationResultDTO> callback,
            Action<string> errorCallback = null)
        {
            string url = $"{_baseUrl}/simulate";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, requestDto, jwtToken))
            {
                yield return BackendRequestHelper.SendJson<CombatSimulationResultDTO>(
                    webRequest,
                    callback,
                    "CombatSimulator",
                    request =>
                    {
                        errorCallback?.Invoke(BackendRequestHelper.GetErrorMessage(request));
                        return null;
                    });
            }
        }
    }
}
