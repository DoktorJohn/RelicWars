using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Network.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Network
{
    public class ClientBattleReportService
    {
        private readonly string _baseUrl;

        public ClientBattleReportService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/BattleReport";
        }

        public IEnumerator GetBattleReports(Guid worldPlayerId, string jwtToken, Action<List<BattleReportDTO>> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/reports";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "BattleReports", _ => new List<BattleReportDTO>());
            }
        }

        public IEnumerator GetUnreadBattleReportCount(Guid worldPlayerId, string jwtToken, Action<int> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/unread-status";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<BattleReportUnreadStatusDTO>(request.downloadHandler.text);
                        callback?.Invoke(Math.Max(0, data?.UnreadCount ?? 0));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[BattleReports] Failed to deserialize unread status: {e.Message}");
                        callback?.Invoke(0);
                    }
                }
                else
                {
                    callback?.Invoke(0);
                }
            }
        }

        public IEnumerator MarkBattleReportAsRead(Guid worldPlayerId, Guid battleReportId, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/reports/{battleReportId}/read";

            using (var request = BackendRequestHelper.CreatePutRequest(url, null, jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[BattleReports] MarkBattleReportAsRead Failed: {BackendRequestHelper.GetErrorMessage(request)}");
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator DeleteBattleReport(Guid worldPlayerId, Guid battleReportId, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/reports/{battleReportId}";

            using (var request = UnityWebRequest.Delete(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[BattleReports] DeleteBattleReport Failed: {BackendRequestHelper.GetErrorMessage(request)}");
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator SetBattleReportPublicStatus(Guid worldPlayerId, Guid battleReportId, bool isPublic, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/reports/{battleReportId}/public-status";
            var payload = new { IsPublic = isPublic };

            using (var request = BackendRequestHelper.CreatePutRequest(url, payload, jwtToken))
            {
                yield return request.SendWebRequest();
                bool success = request.result == UnityWebRequest.Result.Success;
                if (!success)
                {
                    Debug.LogError($"[BattleReports] Set public status failed: {BackendRequestHelper.GetErrorMessage(request)}");
                }
                callback?.Invoke(success);
            }
        }

        [Serializable]
        private class BattleReportUnreadStatusDTO
        {
            public int UnreadCount { get; set; }
        }
    }
}
