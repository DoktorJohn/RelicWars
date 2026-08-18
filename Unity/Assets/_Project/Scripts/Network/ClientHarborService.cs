using System;
using System.Collections;
using System.Collections.Generic;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using UnityEngine.Networking;

namespace Project.Network.Manager
{
    public class ClientHarborService
    {
        private readonly string _baseUrl;

        public ClientHarborService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetRecruitmentQueue(Guid cityId, string token, Action<List<RecruitmentQueueItemDTO>> callback)
        {
            string url = $"{_baseUrl}/MilitaryBuilding/recruitmentQueue";

            var requestDataContainer = new GetRecruitmentQueueItemsDTO
            {
                CityId = cityId,
                UnitCategories = new List<UnitCategoryEnum> { UnitCategoryEnum.Naval }
            };

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, requestDataContainer, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "HarborService",
                    _ => new List<RecruitmentQueueItemDTO>());
            }
        }

        public IEnumerator GetHarborOverviewInformation(Guid cityId, string token, Action<HarborFullViewDTO> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/harborOverview";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                request.timeout = 10;
                yield return BackendRequestHelper.SendJson(request, callback, "HarborService");
            }
        }

        public IEnumerator RecruitUnits(Guid cityId, UnitTypeEnum unitType, int amount, string token, Action<RecruitmentResult> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/harborRecruit";

            var requestBody = new RecruitUnitRequestDTO
            {
                UnitType = unitType,
                Amount = amount
            };

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, requestBody, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    response => callback?.Invoke(response ?? new RecruitmentResult
                    {
                        Success = false,
                        Message = "Kunne ikke tolke succes-svar fra serveren."
                    }),
                    "HarborService",
                    errorRequest => new RecruitmentResult
                    {
                        Success = false,
                        Message = BackendRequestHelper.GetErrorMessage(errorRequest)
                    });
            }
        }

        public IEnumerator CancelRecruitment(Guid cityId, Guid queueId, string token, Action<RecruitmentResult> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/recruitment/{queueId}";
            using (UnityWebRequest request = BackendRequestHelper.CreateDeleteRequest(url, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    response => callback?.Invoke(response ?? new RecruitmentResult { Success = false, Message = "Empty server response." }),
                    "HarborService",
                    errorRequest => new RecruitmentResult { Success = false, Message = BackendRequestHelper.GetErrorMessage(errorRequest) });
            }
        }
    }
}
