using Project.Network.Helper;
using System;
using System.Collections;

namespace Project.Network
{
    public class ClientBugReportService
    {
        private readonly string _baseUrl;

        public ClientBugReportService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/BugReport";
        }

        public IEnumerator Submit(string description, string jwtToken, Action<bool, string> callback)
        {
            using (var request = BackendRequestHelper.CreatePostRequest(
                _baseUrl,
                new SubmitBugReportRequest { Description = description },
                jwtToken))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, null);
                    yield break;
                }

                callback?.Invoke(false, BackendRequestHelper.GetErrorMessage(request));
            }
        }

        [Serializable]
        private class SubmitBugReportRequest
        {
            public string Description;
        }
    }
}
