using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Project.Network.Helper
{

    public static class BackendRequestHelper
    {
        // Konfigurerer en POST request med JSON body og Headers
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

        // Konfigurerer en GET request
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

            // SSL Bypass (Kun til development!)
            request.certificateHandler = new BypassCertificate();
        }
    }

    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}