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
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "Auth",
                    _ => new AuthenticationResponse
                    {
                        IsAuthenticated = false,
                        FeedbackMessage = GetUserFacingErrorMessage(request)
                    });
            }
        }

        public IEnumerator Register(string email, string username, string password, Action<AuthenticationResponse> callback)
        {
            var payload = new { Email = email, UserName = username, Password = password };
            string url = $"{_baseUrl}/register";

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload))
            {
                yield return BackendRequestHelper.SendJson(
                    request,
                    callback,
                    "Auth",
                    _ => new AuthenticationResponse
                    {
                        IsAuthenticated = false,
                        FeedbackMessage = GetUserFacingErrorMessage(request)
                    });
            }
        }

        private static string GetUserFacingErrorMessage(UnityWebRequest request)
        {
            var apiError = BackendRequestHelper.ParseApiError(request);
            return string.IsNullOrWhiteSpace(apiError?.Message)
                ? "Unable to reach the realm. Please try again."
                : apiError.Message;
        }
    }
}
