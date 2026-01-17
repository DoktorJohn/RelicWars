using System;
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
        public string PlayerProfileId { get; private set; }
        public string WorldPlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public Guid? ActiveCityId { get; private set; }

        // --- Services ---
        public ClientAuthService Auth { get; private set; }
        public ClientWorldService World { get; private set; }
        public ClientWorldPlayerService WorldPlayer { get; private set; }
        public ClientCityService City { get; private set; }
        public ClientBuildingService Building { get; private set; }
        public ClientBarracksService Barracks { get; private set; }
        public ClientStableService Stable { get; private set; }
        public ClientWorkshopService Workshop { get; private set; }
        public ClientRankingService Ranking { get; private set; }
        public ClientAllianceService Alliance { get; private set; }

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
            World = new ClientWorldService(_backendBaseUrl);
            City = new ClientCityService(_backendBaseUrl);
            Building = new ClientBuildingService(_backendBaseUrl);
            Barracks = new ClientBarracksService(_backendBaseUrl);
            Stable = new ClientStableService(_backendBaseUrl);
            Workshop = new ClientWorkshopService(_backendBaseUrl);
            Ranking = new ClientRankingService(_backendBaseUrl);
            WorldPlayer = new ClientWorldPlayerService(_backendBaseUrl);
            Alliance = new ClientAllianceService(_backendBaseUrl);

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
            StartCoroutine(WorldPlayer.JoinWorld(PlayerProfileId, worldId, JwtToken, (response) =>
            {
                if (response.ConnectionSuccessful)
                {
                    if (!string.IsNullOrEmpty(response.ActiveCityId))
                    {
                        ActiveCityId = Guid.Parse(response.ActiveCityId);
                    }

                    if (!string.IsNullOrEmpty(response.WorldPlayerId))
                    {
                        WorldPlayerId = response.WorldPlayerId;
                    }

                    Debug.Log($"[NetworkManager] Joined World. City: {ActiveCityId}, Player: {WorldPlayerId}");
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
                PlayerProfileId = response.Profile.PlayerId;
                PlayerName = response.Profile.UserName;
                Debug.Log($"[NetworkManager] Session Startet: {PlayerName} ({PlayerProfileId})");
            }
        }
    }

}