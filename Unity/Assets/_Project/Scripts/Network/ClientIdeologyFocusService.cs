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

namespace Assets._Project.Scripts.Network
{
    public class ClientIdeologyFocusService
    {
        private readonly string _baseUrl;

        public ClientIdeologyFocusService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public IEnumerator GetIdeologyOverview(Guid worldPlayerId, string authenticationToken, Action<IdeologyOverviewDTO> callback)
        {
            string url = $"{_baseUrl}/IdeologyFocus/getIdeologyOverview/{worldPlayerId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", "Bearer " + authenticationToken);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var overviewData = JsonConvert.DeserializeObject<IdeologyOverviewDTO>(request.downloadHandler.text);
                        callback?.Invoke(overviewData);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[IdeologyFocusService] JSON Deserialization Error in Overview: {exception.Message}");
                        callback?.Invoke(new IdeologyOverviewDTO { Message = "Kunne ikke læse data fra serveren." });
                    }
                }
                else
                {
                    Debug.LogError($"[IdeologyFocusService] Network Error ({request.responseCode}): {request.error}");
                    callback?.Invoke(new IdeologyOverviewDTO { Message = "Netværksfejl ved hentning af ideologi." });
                }
            }
        }

        public IEnumerator EnactIdeologyFocus(IdeologyFocusRequestDTO ideologyFocusRequest, string authenticationToken, Action<IdeologyFocusAnswerDTO> callback)
        {
            string focusName = ideologyFocusRequest.IdeologyFocusName.ToString();
            string url = $"{_baseUrl}/IdeologyFocus/enactIdeologyFocus/{focusName}";

            string jsonPayload = JsonConvert.SerializeObject(ideologyFocusRequest);
            byte[] rawBody = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(rawBody);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + authenticationToken);

                yield return request.SendWebRequest();

                IdeologyFocusAnswerDTO resultData;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        resultData = JsonConvert.DeserializeObject<IdeologyFocusAnswerDTO>(request.downloadHandler.text);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[IdeologyFocusService] JSON Parse Error on Enact: {exception.Message}");
                        resultData = new IdeologyFocusAnswerDTO(null, null, "Fejl ved tolkning af server-svar.", false);
                    }
                }
                else
                {
                    try
                    {
                        string errorFromBackend = request.downloadHandler.text;
                        resultData = new IdeologyFocusAnswerDTO(
                            ideologyFocusRequest.IdeologyFocusName,
                            ideologyFocusRequest.CityId,
                            string.IsNullOrEmpty(errorFromBackend) ? request.error : errorFromBackend,
                            false);
                    }
                    catch
                    {
                        resultData = new IdeologyFocusAnswerDTO(null, null, "Ukendt serverfejl.", false);
                    }

                    Debug.LogWarning($"[IdeologyFocusService] Enact Failed: {resultData.Message}");
                }

                callback?.Invoke(resultData);
            }
        }
    }
}
