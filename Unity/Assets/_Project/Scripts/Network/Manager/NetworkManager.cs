using Project.Scripts.Domain.Enums;
using Project.Scripts.Network;
using System;
using UnityEngine;

namespace Project.Network.Manager
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;

        [Header("Configuration")]
        [SerializeField] private string _backendBaseUrl = "https://rorgamebackend-dmfadtcvdabpepd7.francecentral-01.azurewebsites.net/api";
        [SerializeField] private string _localBackendUrl = "https://localhost:55286/api";
        private string _activeBackendUrl;



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
        public ClientMarketPlaceService MarketPlace { get; private set; }
        public ClientResearchService Research { get; private set; }
        public ClientUnitDeploymentService UnitDeployment { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ConfigureBackendUrl();
                InitializeServices();

            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ConfigureBackendUrl()
        {
#if UNITY_EDITOR
            _activeBackendUrl = _localBackendUrl;
            Debug.Log($"<color=green>NetworkManager:</color> Bruger LOKAL backend: {_activeBackendUrl}");
#else
            // Hvis det er en build (WebGL), brug Azure backend
            _activeBackendUrl = _backendBaseUrl;
            Debug.Log($"<color=blue>NetworkManager:</color> Bruger AZURE backend: {_activeBackendUrl}");
#endif
        }

        private void InitializeServices()
        {
            // Vi skal bruge _activeBackendUrl her, ellers ignorerer koden din ConfigureBackendUrl logik
            Auth = new ClientAuthService(_activeBackendUrl);
            World = new ClientWorldService(_activeBackendUrl);
            City = new ClientCityService(_activeBackendUrl);
            Building = new ClientBuildingService(_activeBackendUrl);
            Barracks = new ClientBarracksService(_activeBackendUrl);
            Stable = new ClientStableService(_activeBackendUrl);
            Workshop = new ClientWorkshopService(_activeBackendUrl);
            Ranking = new ClientRankingService(_activeBackendUrl);
            WorldPlayer = new ClientWorldPlayerService(_activeBackendUrl);
            Alliance = new ClientAllianceService(_activeBackendUrl);
            MarketPlace = new ClientMarketPlaceService(_activeBackendUrl);
            Research = new ClientResearchService(_activeBackendUrl);
            UnitDeployment = new ClientUnitDeploymentService(_activeBackendUrl);

            Debug.Log($"[NetworkManager] Services Initialized pointing to: {_activeBackendUrl}");
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

        public void JoinWorld(Guid worldId, Action<bool, IdeologyTypeEnum> onComplete)
        {
            StartCoroutine(WorldPlayer.JoinWorld(PlayerProfileId, worldId, JwtToken, (response) =>
            {
                if (response.ConnectionSuccessful)
                {
                    if (!string.IsNullOrEmpty(response.ActiveCityId))
                        ActiveCityId = Guid.Parse(response.ActiveCityId);

                    if (!string.IsNullOrEmpty(response.WorldPlayerId))
                        WorldPlayerId = response.WorldPlayerId;

                    Debug.Log($"[NetworkManager] Joined World. Ideology: {response.SelectedIdeology}");

                    onComplete?.Invoke(true, response.SelectedIdeology);
                }
                else
                {
                    onComplete?.Invoke(false, IdeologyTypeEnum.None);
                }
            }));
        }

        public void SelectIdeology(IdeologyTypeEnum ideology, Action<bool> onComplete)
        {
            if (string.IsNullOrEmpty(WorldPlayerId))
            {
                Debug.LogError("[NetworkManager] Cannot select ideology: WorldPlayerId is null.");
                onComplete?.Invoke(false);
                return;
            }

            // Vi parser string ID til Guid da servicen forventer Guid
            Guid worldPlayerGuid = Guid.Parse(WorldPlayerId);

            StartCoroutine(WorldPlayer.SelectIdeology(worldPlayerGuid, ideology, JwtToken, (response) =>
            {
                if (response != null && response.ConnectionSuccessful)
                {
                    Debug.Log($"[NetworkManager] Ideology {ideology} successfully selected.");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Ideology selection failed: {response?.Message}");
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