using System;
using System.Collections;
using System.Collections.Generic;
using Assets._Project.Scripts.Domain.Enums;
using Assets.Scripts.Domain.Enums;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using UnityEngine.Networking;

namespace Project.Network
{
    public class ClientStableService
    {
        private readonly string _baseUrl;

        public ClientStableService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetRecruitmentQueue(Guid cityId, string token, Action<List<RecruitmentQueueItemDTO>> callback)
        {
            string url = $"{_baseUrl}/MilitaryBuilding/recruitmentQueue";

            var requestDataContainer = new GetRecruitmentQueueItemsDTO
            {
                CityId = cityId,
                UnitCategories = new List<UnitCategoryEnum> { UnitCategoryEnum.Cavalry }
            };

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, requestDataContainer, token))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "StableService",
                    _ => new List<RecruitmentQueueItemDTO>());
            }
        }

        public IEnumerator GetStableOverviewInformation(Guid cityId, string token, Action<StableFullViewDTO> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/stableOverview";

            using (UnityWebRequest request = BackendRequestHelper.CreateGetRequest(url, token))
            {
                request.timeout = 10;
                yield return BackendRequestHelper.SendJson(request, callback, "StableService");
            }
        }

        public IEnumerator RecruitUnits(Guid cityId, UnitTypeEnum unitType, int amount, string token, Action<RecruitmentResult> callback)
        {
            string url = $"{_baseUrl}/militarybuilding/{cityId}/stableRecruit";

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
                    "StableService",
                    errorRequest => new RecruitmentResult
                    {
                        Success = false,
                        Message = BackendRequestHelper.GetErrorMessage(errorRequest)
                    });
            }
        }
    }
}
