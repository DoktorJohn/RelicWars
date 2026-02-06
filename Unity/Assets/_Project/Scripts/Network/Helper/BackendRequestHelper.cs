using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Project.Network.Helper
{
    public static class BackendRequestHelper
    {
        public static UnityWebRequest CreatePostRequest(string url, object bodyPayload, string jwtToken = null)
        {
            string json = JsonConvert.SerializeObject(bodyPayload);
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            SetStandardHeaders(request, jwtToken);

            return request;
        }

        public static UnityWebRequest CreateGetRequest(string url, string jwtToken = null)
        {
            var request = UnityWebRequest.Get(url);
            SetStandardHeaders(request, jwtToken);
            return request;
        }

        private static void SetStandardHeaders(UnityWebRequest request, string jwtToken)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrEmpty(jwtToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }

        }
    }
}