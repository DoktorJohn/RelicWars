using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Scripts.Network
{
    public class ClientUnitDeploymentService
    {
        private readonly string _baseUrl;

        public ClientUnitDeploymentService(string baseUrl)
        {
            // Vi mapper til din UnitDeploymentController
            _baseUrl = $"{baseUrl}/UnitDeployment";
        }

        /// <summary>
        /// Sender en anmodning til backenden om at deploye enheder fra en by ud på verdenskortet.
        /// </summary>
        public IEnumerator DeployUnits(DeployUnitRequestDTO deployUnitRequestDto, string jwtToken, Action<UnitDeploymentDTO> callback)
        {
            string url = $"{_baseUrl}/deployUnitDeployment";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, deployUnitRequestDto, jwtToken))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        UnitDeploymentDTO responseData = JsonConvert.DeserializeObject<UnitDeploymentDTO>(webRequest.downloadHandler.text);
                        Debug.Log($"[Hexagon] Deployment succesfuld. ID: {responseData.Id}");
                        callback?.Invoke(responseData);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[Hexagon] Fejl ved deserialisering af data: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    // Vi logger både fejlen og svaret fra serveren (da din controller returnerer BadRequest med fejlbesked)
                    string errorDetail = webRequest.downloadHandler?.text;
                    Debug.LogError($"[Hexagon] DeployUnits Error: {webRequest.error} - Detaljer: {errorDetail}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator MoveUnits(MoveUnitRequestDTO moveUnitRequestDto, string jwtToken, Action<UnitDeploymentDTO> callback)
        {
            string url = $"{_baseUrl}/moveUnitDeployment";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, moveUnitRequestDto, jwtToken))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        UnitDeploymentDTO responseData = JsonConvert.DeserializeObject<UnitDeploymentDTO>(webRequest.downloadHandler.text);
                        Debug.Log($"[Hexagon] March-ordre modtaget for enhed: {responseData.Id}");
                        callback?.Invoke(responseData);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[Hexagon] Fejl ved deserialisering af Move-data: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    string errorDetail = webRequest.downloadHandler?.text;
                    Debug.LogError($"[Hexagon] MoveUnits Error: {webRequest.error} - Detaljer: {errorDetail}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator AbortMovementUnits(Guid deploymentId, string jwtToken, Action<UnitDeploymentDTO> callback)
        {
            string url = $"{_baseUrl}/haltUnitDeployment/{deploymentId}";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, "", jwtToken))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        UnitDeploymentDTO responseData = JsonConvert.DeserializeObject<UnitDeploymentDTO>(webRequest.downloadHandler.text);
                        Debug.Log($"[Expeditions] March afbrudt for enhed: {deploymentId}");
                        callback?.Invoke(responseData);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[Expeditions] Fejl ved deserialisering af Abort-data: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Expeditions] AbortUnits Error: {webRequest.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator ReturnToOriginCityUnits(Guid deploymentId, string jwtToken, Action<UnitDeploymentDTO> callback)
        {
            string url = $"{_baseUrl}/returnToOriginCity/{deploymentId}";

            using (UnityWebRequest webRequest = BackendRequestHelper.CreatePostRequest(url, "", jwtToken))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        UnitDeploymentDTO responseData = JsonConvert.DeserializeObject<UnitDeploymentDTO>(webRequest.downloadHandler.text);
                        Debug.Log($"[Expeditions] Retur-ordre bekræftet for: {deploymentId}");
                        callback?.Invoke(responseData);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[Expeditions] Fejl ved deserialisering af Return-data: {exception.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Expeditions] ReturnToOrigin Error: {webRequest.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}
