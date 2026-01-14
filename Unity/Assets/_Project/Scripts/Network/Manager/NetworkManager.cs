using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Network.Manager
{

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;

        [Header("Configuration")]
        [SerializeField] private string _backendBaseUrl = "https://127.0.0.1:55286/api";

        // --- State Management ---
        public string JwtToken { get; private set; }
        public string PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public Guid? ActiveCityId { get; private set; }

        // --- Services ---
        public ClientAuthService Auth { get; private set; }
        public ClientGameWorldService GameWorld { get; private set; }
        public ClientCityService City { get; private set; }
        public ClientBuildingService Building { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeServices();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeServices()
        {
            // Vi instansierer services med base URL
            Auth = new ClientAuthService(_backendBaseUrl);
            GameWorld = new ClientGameWorldService(_backendBaseUrl);
            City = new ClientCityService(_backendBaseUrl);
            Building = new ClientBuildingService(_backendBaseUrl);

            Debug.Log("[NetworkManager] Services Initialized.");
        }

        // --- Public Methods til UI ---

        public void AuthenticateUser(string email, string password, Action<bool> onComplete)
        {
            StartCoroutine(Auth.Login(email, password, (response) =>
            {
                if (response != null && response.IsAuthenticated)
                {
                    SetSessionData(response);
                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            }));
        }

        public void RegisterUser(string email, string user, string pass, Action<bool> onComplete)
        {
            StartCoroutine(Auth.Register(email, user, pass, (response) =>
            {
                if (response != null && response.IsAuthenticated)
                {
                    SetSessionData(response);
                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            }));
        }

        public void JoinWorld(Guid worldId, Action<bool> onComplete)
        {
            StartCoroutine(GameWorld.JoinWorld(PlayerId, worldId, JwtToken, (response) =>
            {
                if (response.ConnectionSuccessful && !string.IsNullOrEmpty(response.ActiveCityId))
                {
                    ActiveCityId = Guid.Parse(response.ActiveCityId);
                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            }));
        }

        private void SetSessionData(AuthenticationResponse response)
        {
            if (response.Profile != null)
            {
                JwtToken = response.JwtToken;
                PlayerId = response.Profile.PlayerId;
                PlayerName = response.Profile.UserName;
                Debug.Log($"[NetworkManager] Session Startet: {PlayerName} ({PlayerId})");
            }
        }
    }

}