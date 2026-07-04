using Project.Network.Helper;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Project.Network
{
    public class ClientCityService
    {
        private readonly string _baseUrl;

        public ClientCityService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/City";
        }

        public IEnumerator GetCityResources(Guid cityId, string jwtToken, Action<CityResourcesDTO> callback)
        {
            string url = $"{_baseUrl}/{cityId}/resources";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "City");
            }
        }

        public IEnumerator GetCityOverviewHUD(Guid cityId, string jwtToken, Action<CityOverviewHUDDTO> callback)
        {
            string url = $"{_baseUrl}/CityOverviewHUD/{cityId}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "City");
            }
        }

        public IEnumerator GetDetailedCityInfo(Guid cityId, string jwtToken, Action<CityControllerGetDetailedCityInformationDTO> callback)
        {
            string url = $"{_baseUrl}/GetDetailedCityInformation/{cityId}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "City");
            }
        }

        public IEnumerator GetTownHallAvailableBuildings(Guid cityId, string jwtToken, Action<List<AvailableBuildingDTO>> callback)
        {
            string url = $"{_baseUrl}/{cityId}/townHall/available-buildings";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "City", _ => new List<AvailableBuildingDTO>());
            }
        }

        public IEnumerator GetPlayerCities(Guid cityId, string jwtToken, Action<List<CityDTO>> callback)
        {
            string url = $"{_baseUrl}/{cityId}/my-cities";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "City", _ => new List<CityDTO>());
            }
        }

        public IEnumerator ChangeCityName(Guid cityId, string newName, string authenticationToken, Action<ChangeCityNameResponseDTO> callback)
        {
            string url = $"{_baseUrl}/ChangeCityName/{cityId}/{newName}";

            using (UnityWebRequest request = BackendRequestHelper.CreatePostRequest(url, new { }, authenticationToken))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    response =>
                    {
                        if (response == null)
                        {
                            callback?.Invoke(new ChangeCityNameResponseDTO
                            {
                                CityId = cityId,
                                CityName = newName,
                                Success = false,
                                Message = "Kunne ikke tolke serverens svar."
                            });
                            return;
                        }

                        callback?.Invoke(response);
                    },
                    "City",
                    _ => new ChangeCityNameResponseDTO
                    {
                        CityId = cityId,
                        CityName = newName,
                        Success = false,
                        Message = BackendRequestHelper.GetErrorMessage(request)
                    });
            }
        }

        public IEnumerator InvestInExoticResource(Guid cityId, ExoticResourceInvestmentRequestDTO request, string authenticationToken, Action<ExoticResourceInvestmentResponseDTO> callback)
        {
            string url = $"{_baseUrl}/{cityId}/exotic-resources/invest";

            using (UnityWebRequest requestObject = BackendRequestHelper.CreatePostRequest(url, request, authenticationToken))
            {
                yield return BackendRequestHelper.SendJson(
                    requestObject,
                    callback,
                    "City",
                    _ => new ExoticResourceInvestmentResponseDTO
                    {
                        CityId = cityId,
                        SlotIndex = request.SlotIndex,
                        NewTier = 0
                    });
            }
        }
    }
}
