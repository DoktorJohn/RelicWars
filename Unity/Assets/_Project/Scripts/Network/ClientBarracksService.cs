using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Scripts.Domain.DTOs;
using Assets.Scripts.Domain.Enums;
using Assets._Project.Scripts.Domain.Enums;
using Project.Network.Helper;

namespace Project.Network
{
    public class ClientBarracksService
    {
        private readonly string _baseUrl;

        public ClientBarracksService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetRecruitmentQueue(Guid cityId, string token, Action<List<RecruitmentQueueItemDTO>> callback)
        {
            string url = $"{_baseUrl}/MilitaryBuilding/recruitmentQueue";

            var requestDataContainer = new GetRecruitmentQueueItemsDTO
            {
                CityId = cityId,
                UnitCategories = new List<UnitCategoryEnum> { UnitCategoryEnum.Infantry, UnitCategoryEnum.Ranged }
            };

            using (UnityWebRequest request = BackendRequestHelper.CreateGetWithBodyRequest(url, requestDataContainer, token))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        List<RecruitmentQueueItemDTO> queueItems = JsonConvert.DeserializeObject<List<RecruitmentQueueItemDTO>>(request.downloadHandler.text);
                        callback?.Invoke(queueItems);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[ClientBarracksService] JSON Deserialization Error: {exception.Message}");
                        callback?.Invoke(new List<RecruitmentQueueItemDTO>());
                    }
                }
                else
                {
                    Debug.LogError($"[ClientBarracksService] Network Error ({request.responseCode}): {request.error}");
                    callback?.Invoke(new List<RecruitmentQueueItemDTO>());
                }
            }
        }

        public IEnumerator GetBarracksOverviewInformation(Guid cityId, string token, Action<BarracksFullViewDTO> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/barracksOverview";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<BarracksFullViewDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[BarracksService] JSON Error: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[BarracksService] Network Error: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator RecruitUnits(Guid cityId, UnitTypeEnum unitType, int amount, string token, Action<RecruitmentResult> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/barracksRecruit";

            var requestBody = new RecruitUnitRequestDTO
            {
                UnitType = unitType,
                Amount = amount
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                RecruitmentResult recruitmentResult = new RecruitmentResult();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // Vi parser det fulde svar inkl. remainingFreePopulation
                        recruitmentResult = JsonConvert.DeserializeObject<RecruitmentResult>(request.downloadHandler.text);
                    }
                    catch (Exception ex)
                    {
                        recruitmentResult.Success = false;
                        recruitmentResult.Message = "Kunne ikke tolke succes-svar fra serveren.";
                        Debug.LogError($"[BarracksService] JSON Parse Error: {ex.Message}");
                    }
                }
                else
                {
                    // Ved fejl (400, 500 osv.) forsøger vi stadig at læse backends fejlbesked
                    try
                    {
                        recruitmentResult = JsonConvert.DeserializeObject<RecruitmentResult>(request.downloadHandler.text);
                    }
                    catch
                    {
                        // Hvis det ikke er JSON, bruger vi den rå fejlbesked
                        recruitmentResult.Success = false;
                        recruitmentResult.Message = string.IsNullOrEmpty(request.downloadHandler.text) ? request.error : request.downloadHandler.text;
                    }

                    Debug.LogError($"[BarracksService] Recruit Failed: {recruitmentResult.Message}");
                }

                // Vi returnerer hele objektet til controlleren
                callback?.Invoke(recruitmentResult);
            }
        }

    }
}