using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Project.Network.Helper;

namespace Project.Network
{

    public class ClientAuthService
    {
        private readonly string _baseUrl;

        public ClientAuthService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/Auth";
        }

        public IEnumerator Login(string email, string password, Action<AuthenticationResponse> callback)
        {
            var payload = new { Email = email, Password = password };
            string url = $"{_baseUrl}/login";

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<AuthenticationResponse>(request.downloadHandler.text);
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[Auth] Login Failed: {request.error}");
                    callback?.Invoke(new AuthenticationResponse { IsAuthenticated = false, FeedbackMessage = "Netværksfejl." });
                }
            }
        }

        public IEnumerator Register(string email, string username, string password, Action<AuthenticationResponse> callback)
        {
            var payload = new { Email = email, UserName = username, Password = password };
            string url = $"{_baseUrl}/register";

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<AuthenticationResponse>(request.downloadHandler.text);
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"[Auth] Register Failed: {request.error}");
                    callback?.Invoke(new AuthenticationResponse { IsAuthenticated = false, FeedbackMessage = request.error });
                }
            }
        }
    }
}