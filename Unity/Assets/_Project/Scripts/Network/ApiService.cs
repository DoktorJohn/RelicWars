using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;
using System.Text;
using Project.Network.Models;

public class ApiService : MonoBehaviour
{
    public static ApiService Instance;

    [Header("Backend Netværks Konfiguration")]
    [SerializeField] private string backendBaseUrl = "https://127.0.0.1:55286/api";

    [Header("Aktuel Session Status")]
    public string AuthenticationJwtToken { get; private set; }
    public string AuthenticatedPlayerProfileId { get; private set; }
    public string AuthenticatedPlayerUserName { get; private set; }
    public Guid? CurrentlySelectedActiveCityId { get; private set; }

    private void Awake()
    {
        InitializeApiServiceSingleton();
    }

    private void InitializeApiServiceSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[ApiService] Global instans initialiseret.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- IDENTITY / AUTHENTICATION SEKTION ---

    public IEnumerator ExecutePlayerAuthenticationRequest(string email, string password, Action<AuthenticationResponse> authenticationCallback)
    {
        string requestUrl = $"{backendBaseUrl}/Auth/login";
        var loginPayload = new { Email = email, Password = password };
        string serializedJson = JsonConvert.SerializeObject(loginPayload);

        using (UnityWebRequest webRequest = new UnityWebRequest(requestUrl, "POST"))
        {
            ConfigureJsonWebRequestParameters(webRequest, serializedJson);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                AuthenticationResponse response = JsonConvert.DeserializeObject<AuthenticationResponse>(webRequest.downloadHandler.text);
                if (response != null && response.IsAuthenticated)
                {
                    StoreAuthenticatedSessionDataInGlobalState(response);
                }
                authenticationCallback?.Invoke(response);
            }
            else
            {
                Debug.LogError($"[ApiService] Auth Fejl: {webRequest.error}");
                authenticationCallback?.Invoke(new AuthenticationResponse { IsAuthenticated = false, FeedbackMessage = "Netværksfejl." });
            }
        }
    }

    public IEnumerator ExecutePlayerRegistrationRequest(string email, string userName, string password, Action<bool, string> registrationCallback)
    {
        string requestUrl = $"{backendBaseUrl}/Auth/register";
        var registrationPayload = new { Email = email, UserName = userName, Password = password };
        string serializedJson = JsonConvert.SerializeObject(registrationPayload);

        using (UnityWebRequest webRequest = new UnityWebRequest(requestUrl, "POST"))
        {
            ConfigureJsonWebRequestParameters(webRequest, serializedJson);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                AuthenticationResponse response = JsonConvert.DeserializeObject<AuthenticationResponse>(webRequest.downloadHandler.text);
                if (response != null && response.IsAuthenticated)
                {
                    StoreAuthenticatedSessionDataInGlobalState(response);
                    registrationCallback?.Invoke(true, "Profil oprettet.");
                }
            }
            else
            {
                registrationCallback?.Invoke(false, webRequest.error);
            }
        }
    }

    private void StoreAuthenticatedSessionDataInGlobalState(AuthenticationResponse response)
    {
        if (response.Profile != null)
        {
            AuthenticationJwtToken = response.JwtToken;
            AuthenticatedPlayerProfileId = response.Profile.PlayerId;
            AuthenticatedPlayerUserName = response.Profile.UserName;
            Debug.Log($"[ApiService] Session gemt. ID: {AuthenticatedPlayerProfileId}");
        }
    }

    // --- WORLD & CITY SELECTION ---

    public IEnumerator FetchAvailableActiveGameWorlds(Action<List<GameWorldAvailableResponseDTO>> successCallback)
    {
        string requestUrl = $"{backendBaseUrl}/GameWorld/available-worlds";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
        {
            webRequest.certificateHandler = new BypassCertificate();
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var worlds = JsonConvert.DeserializeObject<List<GameWorldAvailableResponseDTO>>(webRequest.downloadHandler.text);
                successCallback?.Invoke(worlds);
            }
        }
    }

    public IEnumerator SubmitRequestToJoinSelectedGameWorld(Guid targetWorldId, Action<PlayerWorldJoinResponse> joinCallback)
    {
        string requestUrl = $"{backendBaseUrl}/GameWorld/join";
        var joinPayload = new { PlayerProfileId = AuthenticatedPlayerProfileId, WorldId = targetWorldId.ToString() };
        string serializedJson = JsonConvert.SerializeObject(joinPayload);

        using (UnityWebRequest webRequest = new UnityWebRequest(requestUrl, "POST"))
        {
            ConfigureJsonWebRequestParameters(webRequest, serializedJson);
            AppendAuthenticationBearerTokenHeader(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                PlayerWorldJoinResponse response = JsonConvert.DeserializeObject<PlayerWorldJoinResponse>(webRequest.downloadHandler.text);
                if (response != null && !string.IsNullOrEmpty(response.ActiveCityId))
                {
                    CurrentlySelectedActiveCityId = Guid.Parse(response.ActiveCityId);
                }
                joinCallback?.Invoke(response);
            }
        }
    }

    // --- CITY DATA (Med Udvidet Debugging) ---

    public IEnumerator RetrieveDetailedCityInformationByCityIdentifier(Guid cityIdentifier, Action<CityControllerGetDetailedCityInformationDTO> dataCallback)
    {
        string requestUrl = $"{backendBaseUrl}/City/GetDetailedCityInformation/{cityIdentifier}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
        {
            AppendAuthenticationBearerTokenHeader(webRequest);
            webRequest.certificateHandler = new BypassCertificate();

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string rawJsonBody = webRequest.downloadHandler.text;
                Debug.Log($"[ApiService] Modtog succesfuldt svar fra server. Rå JSON: {rawJsonBody}");

                try
                {
                    var cityData = JsonConvert.DeserializeObject<CityControllerGetDetailedCityInformationDTO>(rawJsonBody);

                    if (cityData != null && cityData.BuildingList != null)
                    {
                        Debug.Log($"[ApiService] Deserialisering færdig. Antal bygninger i listen: {cityData.BuildingList.Count}");
                    }
                    else
                    {
                        Debug.LogWarning("[ApiService] Deserialisering lykkedes, men BuildingList er null eller tom.");
                    }

                    dataCallback?.Invoke(cityData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ApiService] JSON Deserialiserings fejl: {e.Message}");
                    dataCallback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"[ApiService] Kunne ikke hente by-data: {webRequest.error} | URL: {requestUrl}");
                dataCallback?.Invoke(null);
            }
        }
    }

    private void ConfigureJsonWebRequestParameters(UnityWebRequest webRequest, string jsonData)
    {
        byte[] encodedBody = Encoding.UTF8.GetBytes(jsonData);
        webRequest.uploadHandler = new UploadHandlerRaw(encodedBody);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("Accept", "application/json");
        webRequest.certificateHandler = new BypassCertificate();
    }

    private void AppendAuthenticationBearerTokenHeader(UnityWebRequest webRequest)
    {
        if (!string.IsNullOrEmpty(AuthenticationJwtToken))
        {
            webRequest.SetRequestHeader("Authorization", $"Bearer {AuthenticationJwtToken}");
        }
    }
}
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}