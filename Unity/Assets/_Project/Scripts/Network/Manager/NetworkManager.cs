using Assets._Project.Scripts.Network;
using Project.Scripts.Domain.Enums;
using Project.Scripts.Network;
using System;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Project.Network.Manager
{
    public class NetworkManager : MonoBehaviour
    {
        private const string RememberedSessionEnabledKey = "RelicWars.Auth.Remembered";
        private const string RememberedJwtTokenKey = "RelicWars.Auth.JwtToken";
        private const string RememberedPlayerProfileIdKey = "RelicWars.Auth.PlayerProfileId";
        private const string RememberedPlayerNameKey = "RelicWars.Auth.PlayerName";

        public static NetworkManager Instance;

        [Header("Configuration")]
        [SerializeField] private string _backendBaseUrl = "https://reignofrelicswebapp-cdbgc5eyhvd2g0ah.belgiumcentral-01.azurewebsites.net/api";
        [SerializeField] private string _localBackendUrl = "https://localhost:55286/api";
        private string _activeBackendUrl;



        // --- State Management ---
        public string JwtToken { get; private set; }
        public string PlayerProfileId { get; private set; }
        public string WorldPlayerId { get; private set; }
        public Guid ActiveWorldId { get; private set; }
        public string PlayerName { get; private set; }
        public Guid? ActiveCityId { get; private set; }
        public bool HasRememberedSession { get; private set; }
        public event Action<Guid> ActiveCityChanged;

        // --- Services ---
        public ClientAuthService Auth { get; private set; }
        public ClientWorldService World { get; private set; }
        public ClientWorldPlayerService WorldPlayer { get; private set; }
        public ClientCityService City { get; private set; }
        public ClientBuildingService Building { get; private set; }
        public ClientBarracksService Barracks { get; private set; }
        public ClientStableService Stable { get; private set; }
        public ClientWorkshopService Workshop { get; private set; }
        public ClientHarborService Harbor { get; private set; }
        public ClientRankingService Ranking { get; private set; }
        public ClientAllianceService Alliance { get; private set; }
        public ClientMarketPlaceService MarketPlace { get; private set; }
        public ClientResearchService Research { get; private set; }
        public ClientBattleReportService BattleReports { get; private set; }
        public ClientUnitDeploymentService UnitDeployment { get; private set; }
        public ClientCombatSimulatorService CombatSimulator { get; private set; }
        public ClientIdeologyFocusService IdeologyFocus { get; private set; }
        public ClientMessagingService Messaging { get; private set; }
        public ClientBugReportService BugReports { get; private set; }
        public ClientDailyObjectivesService DailyObjectives { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ConfigureBackendUrl();
                InitializeServices();
                RestoreRememberedSession();

            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ConfigureBackendUrl()
        {
            _activeBackendUrl = Application.isEditor ? _localBackendUrl : _backendBaseUrl;
        }

        private void InitializeServices()
        {
            Auth = new ClientAuthService(_activeBackendUrl);
            World = new ClientWorldService(_activeBackendUrl);
            City = new ClientCityService(_activeBackendUrl);
            Building = new ClientBuildingService(_activeBackendUrl);
            Barracks = new ClientBarracksService(_activeBackendUrl);
            Stable = new ClientStableService(_activeBackendUrl);
            Workshop = new ClientWorkshopService(_activeBackendUrl);
            Harbor = new ClientHarborService(_activeBackendUrl);
            Ranking = new ClientRankingService(_activeBackendUrl);
            WorldPlayer = new ClientWorldPlayerService(_activeBackendUrl);
            Alliance = new ClientAllianceService(_activeBackendUrl);
            MarketPlace = new ClientMarketPlaceService(_activeBackendUrl);
            Research = new ClientResearchService(_activeBackendUrl);
            BattleReports = new ClientBattleReportService(_activeBackendUrl);
            UnitDeployment = new ClientUnitDeploymentService(_activeBackendUrl);
            CombatSimulator = new ClientCombatSimulatorService(_activeBackendUrl);
            IdeologyFocus = new ClientIdeologyFocusService(_activeBackendUrl);
            Messaging = new ClientMessagingService(_activeBackendUrl);
            BugReports = new ClientBugReportService(_activeBackendUrl);
            DailyObjectives = new ClientDailyObjectivesService(_activeBackendUrl);
        }

        // --- Public Methods til UI ---

        public void AuthenticateUser(string email, string password, bool rememberLogin, Action<AuthenticationResponse> onComplete)
        {
            StartCoroutine(Auth.Login(email, password, (response) =>
            {
                if (response != null && response.IsAuthenticated)
                {
                    SetSessionData(response);

                    if (rememberLogin)
                    {
                        SaveRememberedSession();
                    }
                    else
                    {
                        ClearRememberedSession();
                    }
                }

                onComplete?.Invoke(response);
            }));
        }

        public void RegisterUser(string email, string user, string pass, Action<AuthenticationResponse> onComplete)
        {
            StartCoroutine(Auth.Register(email, user, pass, (response) =>
            {
                if (response != null && response.IsAuthenticated)
                {
                    SetSessionData(response);
                }

                onComplete?.Invoke(response);
            }));
        }

        public void JoinWorld(Guid worldId, Action<bool, IdeologyTypeEnum> onComplete)
        {
            StartCoroutine(WorldPlayer.JoinWorld(PlayerProfileId, worldId, JwtToken, (response) =>
            {
                if (response.ConnectionSuccessful)
                {
                    ActiveWorldId = worldId;
                    if (response.ActiveCityId.HasValue)
                        SelectActiveCity(response.ActiveCityId.Value);

                    if (response.WorldPlayerId.HasValue)
                        WorldPlayerId = response.WorldPlayerId.Value.ToString();

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

            Guid worldPlayerGuid = Guid.Parse(WorldPlayerId);

            StartCoroutine(WorldPlayer.SelectIdeology(worldPlayerGuid, ideology, JwtToken, (response) =>
            {
                if (response != null && response.ConnectionSuccessful)
                {
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[NetworkManager] Ideology selection failed: {response?.Message}");
                    onComplete?.Invoke(false);
                }
            }));
        }

        public void ClearSession()
        {
            StopAllCoroutines();
            JwtToken = null;
            PlayerProfileId = null;
            WorldPlayerId = null;
            ActiveWorldId = Guid.Empty;
            PlayerName = null;
            ActiveCityId = null;
            ClearRememberedSession();
        }

        private void SetSessionData(AuthenticationResponse response)
        {
            if (response.Profile != null)
            {
                JwtToken = response.JwtToken;
                PlayerProfileId = response.Profile.PlayerId;
                PlayerName = response.Profile.UserName;
            }
        }

        public void SelectActiveCity(Guid cityId)
        {
            if (cityId == Guid.Empty || ActiveCityId == cityId)
            {
                return;
            }

            ActiveCityId = cityId;
            ActiveCityChanged?.Invoke(cityId);
        }

        private void RestoreRememberedSession()
        {
            if (PlayerPrefs.GetInt(RememberedSessionEnabledKey, 0) != 1)
            {
                return;
            }

            string jwtToken = PlayerPrefs.GetString(RememberedJwtTokenKey, string.Empty);
            string playerProfileId = PlayerPrefs.GetString(RememberedPlayerProfileIdKey, string.Empty);
            string playerName = PlayerPrefs.GetString(RememberedPlayerNameKey, string.Empty);

            if (string.IsNullOrWhiteSpace(playerProfileId)
                || string.IsNullOrWhiteSpace(playerName)
                || !HasValidLifetime(jwtToken))
            {
                ClearRememberedSession();
                return;
            }

            JwtToken = jwtToken;
            PlayerProfileId = playerProfileId;
            PlayerName = playerName;
            HasRememberedSession = true;
        }

        private void SaveRememberedSession()
        {
            if (string.IsNullOrWhiteSpace(JwtToken)
                || string.IsNullOrWhiteSpace(PlayerProfileId)
                || string.IsNullOrWhiteSpace(PlayerName))
            {
                ClearRememberedSession();
                return;
            }

            PlayerPrefs.SetInt(RememberedSessionEnabledKey, 1);
            PlayerPrefs.SetString(RememberedJwtTokenKey, JwtToken);
            PlayerPrefs.SetString(RememberedPlayerProfileIdKey, PlayerProfileId);
            PlayerPrefs.SetString(RememberedPlayerNameKey, PlayerName);
            PlayerPrefs.Save();
            HasRememberedSession = true;
        }

        private void ClearRememberedSession()
        {
            PlayerPrefs.DeleteKey(RememberedSessionEnabledKey);
            PlayerPrefs.DeleteKey(RememberedJwtTokenKey);
            PlayerPrefs.DeleteKey(RememberedPlayerProfileIdKey);
            PlayerPrefs.DeleteKey(RememberedPlayerNameKey);
            PlayerPrefs.Save();
            HasRememberedSession = false;
        }

        private static bool HasValidLifetime(string jwtToken)
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                return false;
            }

            try
            {
                string[] tokenParts = jwtToken.Split('.');
                if (tokenParts.Length != 3)
                {
                    return false;
                }

                string payload = tokenParts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
                string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                long expiresAt = JObject.Parse(payloadJson).Value<long?>("exp") ?? 0;

                return expiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
