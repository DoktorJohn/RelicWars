using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Network.Helper
{
    public static class BackendRequestHelper
    {
        [Serializable]
        public class ApiError
        {
            public string Code;
            public string Message;
            public object Details;
        }

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

        public static UnityWebRequest CreateGetWithBodyRequest(string url, object bodyPayload, string jwtToken = null)
        {
            string json = JsonConvert.SerializeObject(bodyPayload);
            var request = new UnityWebRequest(url, "GET");
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

        public static UnityWebRequest CreatePutRequest(string url, object bodyPayload = null, string jwtToken = null)
        {
            var request = new UnityWebRequest(url, "PUT");

            if (bodyPayload != null)
            {
                string json = JsonConvert.SerializeObject(bodyPayload);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            SetStandardHeaders(request, jwtToken);
            return request;
        }

        public static IEnumerator SendJson<TResponse>(
            UnityWebRequest request,
            Action<TResponse> callback,
            string logPrefix = null,
            Func<UnityWebRequest, TResponse> errorFactory = null)
        {
            return SendCommand(
                request,
                callback,
                text => JsonConvert.DeserializeObject<TResponse>(text),
                errorFactory,
                logPrefix);
        }

        public static IEnumerator SendCommand<TResponse>(
            UnityWebRequest request,
            Action<TResponse> callback,
            Func<string, TResponse> successParser,
            Func<UnityWebRequest, TResponse> errorFactory = null,
            string logPrefix = null)
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TResponse response = default;

                try
                {
                    response = successParser != null ? successParser(request.downloadHandler?.text) : default;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{BuildLogPrefix(logPrefix)} JSON parse fejl: {exception.Message}");
                    response = errorFactory != null ? errorFactory(request) : default;
                }

                callback?.Invoke(response);
                yield break;
            }

            Debug.LogError($"{BuildLogPrefix(logPrefix)} Request fejlede: {GetErrorMessage(request)}");
            callback?.Invoke(errorFactory != null ? errorFactory(request) : default);
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

        private static string BuildLogPrefix(string logPrefix)
        {
            return string.IsNullOrWhiteSpace(logPrefix) ? "[BackendRequestHelper]" : $"[{logPrefix}]";
        }

        public static ApiError ParseApiError(UnityWebRequest request)
        {
            string responseBody = request?.downloadHandler?.text;
            if (string.IsNullOrWhiteSpace(responseBody)) return null;

            try
            {
                return JsonConvert.DeserializeObject<ApiError>(responseBody);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string GetErrorMessage(UnityWebRequest request)
        {
            var apiError = ParseApiError(request);
            if (!string.IsNullOrWhiteSpace(apiError?.Message)) return apiError.Message;
            if (!string.IsNullOrWhiteSpace(request?.error)) return request.error;
            return "Serveren kunne ikke behandle anmodningen.";
        }
    }
}
