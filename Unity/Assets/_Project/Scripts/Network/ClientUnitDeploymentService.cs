using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Scripts.Network
{
    public class ClientUnitDeploymentService
    {
        private readonly string _baseUrl;

        public ClientUnitDeploymentService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/UnitDeployment";
        }

        public IEnumerator AttackCityDeployment(AttackCityDeploymentRequestDTO attackCityDeploymentRequestDto, string jwtToken, Action<UnitDeploymentDTO> callback, Action<string> errorCallback = null)
        {
            string url = $"{_baseUrl}/attacks";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, attackCityDeploymentRequestDto, jwtToken))
            {
                yield return BackendRequestHelper.SendJson<UnitDeploymentDTO>(
                    webRequest,
                    responseData =>
                    {
                        if (responseData != null)
                        {
                            Debug.Log($"[Deployment] Angreb oprettet. ID: {responseData.Id}");
                        }

                        callback?.Invoke(responseData);
                    },
                    "Deployment",
                    request => { errorCallback?.Invoke(BackendRequestHelper.GetErrorMessage(request)); return null; });
            }
        }

        public IEnumerator GetActiveDeployments(Guid worldPlayerId, string jwtToken, Action<List<UnitDeploymentDTO>> callback, Action<string> errorCallback = null)
        {
            string url = $"{_baseUrl}/worldPlayers/{worldPlayerId}/deployments";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(
                    webRequest,
                    callback,
                    "Deployment",
                    request =>
                    {
                        errorCallback?.Invoke(BackendRequestHelper.GetErrorMessage(request));
                        return errorCallback == null ? new List<UnitDeploymentDTO>() : null;
                    });
            }
        }

        public IEnumerator SupportCityDeployment(SupportCityDeploymentRequestDTO requestDto, string jwtToken, Action<UnitDeploymentDTO> callback, Action<string> errorCallback = null)
        {
            string url = $"{_baseUrl}/supports";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, requestDto, jwtToken))
            {
                yield return BackendRequestHelper.SendJson<UnitDeploymentDTO>(
                    webRequest,
                    responseData =>
                    {
                        if (responseData != null)
                        {
                            Debug.Log($"[Deployment] Support created. ID: {responseData.Id}");
                        }

                        callback?.Invoke(responseData);
                    },
                    "Deployment",
                    request => { errorCallback?.Invoke(BackendRequestHelper.GetErrorMessage(request)); return null; });
            }
        }

        public IEnumerator EstimateTravel(DeploymentTravelEstimateRequestDTO requestDto, string jwtToken, Action<DeploymentTravelEstimateDTO> callback)
        {
            string url = $"{_baseUrl}/travel-estimate";
            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, requestDto, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(webRequest, callback, "Deployment estimate");
            }
        }

        public IEnumerator GetIncomingAttacks(Guid worldPlayerId, string jwtToken, Action<List<IncomingAttackDTO>> callback)
        {
            string url = $"{_baseUrl}/worldPlayers/{worldPlayerId}/incoming-attacks";
            using (UnityWebRequest webRequest = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(webRequest, callback, "Incoming attacks", _ => new List<IncomingAttackDTO>());
            }
        }

        public IEnumerator Recall(Guid deploymentId, string jwtToken, Action<UnitDeploymentDTO> callback, Action<string> errorCallback = null)
        {
            string url = $"{_baseUrl}/{deploymentId}/recall";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, new { }, jwtToken))
            {
                yield return BackendRequestHelper.SendJson<UnitDeploymentDTO>(
                    webRequest,
                    responseData =>
                    {
                        if (responseData != null)
                        {
                            Debug.Log($"[Deployment] Recall accepted: {responseData.Id}");
                        }

                        callback?.Invoke(responseData);
                    },
                    "Deployment",
                    request => { errorCallback?.Invoke(BackendRequestHelper.GetErrorMessage(request)); return null; });
            }
        }
    }
}
