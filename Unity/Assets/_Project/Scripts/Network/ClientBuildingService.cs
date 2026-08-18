using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Domain.Enums;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Project.Network
{
    public class ClientBuildingService
    {
        private readonly string _baseUrl;

        public ClientBuildingService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator UpgradeBuilding(Guid cityId, BuildingTypeEnum type, string token, Action<bool, string> callback)
        {
            string url = $"{_baseUrl}/building/{cityId}/upgrade/{type}";

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, new { }, token))
            {
                yield return request.SendWebRequest();
                bool success = request.result == UnityWebRequest.Result.Success;
                callback?.Invoke(success, success ? request.downloadHandler.text : BackendRequestHelper.GetErrorMessage(request));
            }
        }

        public IEnumerator RepairBuilding(Guid cityId, BuildingTypeEnum type, string token, Action<bool, string> callback)
        {
            string url = $"{_baseUrl}/building/{cityId}/repair/{type}";

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, new { }, token))
            {
                yield return request.SendWebRequest();
                bool success = request.result == UnityWebRequest.Result.Success;
                callback?.Invoke(success, success ? request.downloadHandler.text : BackendRequestHelper.GetErrorMessage(request));
            }
        }

        public IEnumerator GetBuildingQueue(Guid cityId, string token, Action<List<BuildingDTO>> callback)
        {
            string url = $"{_baseUrl}/building/{cityId}/buildingQueue";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "ClientBuildingService",
                    _ => null);
            }
        }

        public IEnumerator CancelBuildingUpgrade(
            Guid cityId,
            Guid jobId,
            string token,
            Action<bool, List<BuildingDTO>, string> callback)
        {
            string url = $"{_baseUrl}/building/{cityId}/buildingQueue/{jobId}";
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {token}");
                yield return request.SendWebRequest();

                bool success = request.result == UnityWebRequest.Result.Success;
                List<BuildingDTO> queue = null;
                string error = null;
                if (success)
                {
                    try
                    {
                        queue = JsonConvert.DeserializeObject<List<BuildingDTO>>(request.downloadHandler.text)
                            ?? new List<BuildingDTO>();
                    }
                    catch (JsonException exception)
                    {
                        success = false;
                        error = $"Invalid building queue response: {exception.Message}";
                    }
                }
                else
                {
                    error = BackendRequestHelper.GetErrorMessage(request);
                }

                callback?.Invoke(success, queue, error);
            }
        }

        public IEnumerator GetUniversityInfo(Guid cityId, string token, Action<List<UniversityInfoDTO>> callback)
        {
            string url = $"{_baseUrl}/miscbuilding/{cityId}/university";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "ClientBuildingService",
                    _ => new List<UniversityInfoDTO>());
            }
        }

        public IEnumerator GetWallInfo(Guid cityId, string token, Action<List<WallInfoDTO>> callback)
        {
            string url = $"{_baseUrl}/miscbuilding/{cityId}/wall";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "ClientBuildingService",
                    _ => new List<WallInfoDTO>());
            }
        }

        public IEnumerator GetHousingProjection(Guid cityId, string token, Action<List<HousingInfoDTO>> callback)
        {
            string url = $"{_baseUrl}/economybuilding/{cityId}/housing";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "ClientBuildingService",
                    _ => new List<HousingInfoDTO>());
            }
        }

        public IEnumerator GetResourceProductionInfo(Guid cityId, BuildingTypeEnum type, string token, Action<List<ResourceBuildingInfoDTO>> callback)
        {
            string url = $"{_baseUrl}/economybuilding/{cityId}/resource/{type}";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "ClientBuildingService",
                    _ => new List<ResourceBuildingInfoDTO>());
            }
        }

        public IEnumerator GetWarehouseProjection(Guid cityId, string token, Action<List<WarehouseProjectionDTO>> callback)
        {
            string url = $"{_baseUrl}/economybuilding/{cityId}/warehouse";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "ClientBuildingService",
                    _ => new List<WarehouseProjectionDTO>());
            }
        }
    }
}
