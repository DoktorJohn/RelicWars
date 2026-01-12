using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Project.Network.Models;
using Assets.Scripts.Domain.State;

namespace Project.Modules.City
{
    /// <summary>
    /// Service ansvarlig for at synkronisere byens ressource-data mellem Backend API og Unity UI.
    /// Denne service håndterer både periodisk polling fra serveren og lokal fremskrivning (ticking) af værdier.
    /// </summary>
    public class CityResourceService : MonoBehaviour
    {
        public static CityResourceService Instance { get; private set; }

        public event Action<CityResourceState> OnResourceStateChanged;

        [Header("API Netværks Konfiguration")]
        [SerializeField] private string _apiBaseUrl = "https://127.0.0.1:55286/api/City";
        [SerializeField] private float _networkSynchronizationIntervalInSeconds = 30f;

        private CityResourceState _currentResourceState;
        private bool _isRequestInProgress = false;
        private bool _isDataInitialized = false;
        private Coroutine _activePollingCoroutine;

        private void Awake()
        {
            InitializeServiceSingleton();
        }

        private void InitializeServiceSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[CityResourceService] Global instans initialiseret.");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Vi ekstrapolerer kun værdierne lokalt, hvis vi har modtaget et start-snapshot fra serveren.
            if (!_isDataInitialized) return;

            ExecuteLocalResourceExtrapolationPerFrame();
        }

        /// <summary>
        /// Beregner den visuelle vækst i ressourcer lokalt på klienten for at give en flydende oplevelse.
        /// Formlen for vækst pr. frame er: $$ \Delta R = \left( \frac{\text{Produktion pr. time}}{3600} \right) \times \text{Time.deltaTime} $$
        /// </summary>
        private void ExecuteLocalResourceExtrapolationPerFrame()
        {
            double secondsPassedSinceLastFrame = Time.deltaTime;

            // Beregning for Træ
            double woodGrowthThisFrame = (_currentResourceState.WoodProductionPerHour / 3600.0) * secondsPassedSinceLastFrame;
            _currentResourceState.WoodAmount = Math.Min(_currentResourceState.WoodMaxCapacity, _currentResourceState.WoodAmount + woodGrowthThisFrame);

            // Beregning for Sten
            double stoneGrowthThisFrame = (_currentResourceState.StoneProductionPerHour / 3600.0) * secondsPassedSinceLastFrame;
            _currentResourceState.StoneAmount = Math.Min(_currentResourceState.StoneMaxCapacity, _currentResourceState.StoneAmount + stoneGrowthThisFrame);

            // Beregning for Metal
            double metalGrowthThisFrame = (_currentResourceState.MetalProductionPerHour / 3600.0) * secondsPassedSinceLastFrame;
            _currentResourceState.MetalAmount = Math.Min(_currentResourceState.MetalMaxCapacity, _currentResourceState.MetalAmount + metalGrowthThisFrame);

            // Vi affyrer eventet hver frame, så UI'et (TopBar) kan opdatere sine labels og bue-painters flydende.
            OnResourceStateChanged?.Invoke(_currentResourceState);
        }

        /// <summary>
        /// Starter den automatiske synkroniserings-cyklus for en specifik by.
        /// </summary>
        public void InitiateResourceRefresh(Guid cityIdentifier)
        {
            if (_activePollingCoroutine != null)
            {
                StopCoroutine(_activePollingCoroutine);
            }

            _activePollingCoroutine = StartCoroutine(ExecuteResourcePollingCycleCoroutine(cityIdentifier));
        }

        /// <summary>
        /// En uendelig løkke der sørger for at hente friske "anker-værdier" fra serveren med et fast interval.
        /// </summary>
        private IEnumerator ExecuteResourcePollingCycleCoroutine(Guid cityIdentifier)
        {
            while (true)
            {
                yield return StartCoroutine(PerformDetailedCityInformationNetworkRequestCoroutine(cityIdentifier));
                yield return new WaitForSeconds(_networkSynchronizationIntervalInSeconds);
            }
        }

        private IEnumerator PerformDetailedCityInformationNetworkRequestCoroutine(Guid cityIdentifier)
        {
            if (_isRequestInProgress) yield break;

            _isRequestInProgress = true;
            string requestUrl = $"{_apiBaseUrl}/GetDetailedCityInformation/{cityIdentifier}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
            {
                // Inkludering af JWT token fra ApiService
                if (ApiService.Instance != null && !string.IsNullOrEmpty(ApiService.Instance.AuthenticationJwtToken))
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {ApiService.Instance.AuthenticationJwtToken}");
                }

                webRequest.certificateHandler = new BypassCertificate();

                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[CityResourceService] Netværksfejl: {webRequest.error} (Status: {webRequest.responseCode})");
                }
                else
                {
                    HandleDetailedCityInformationResponseAndMapToState(webRequest.downloadHandler.text);
                }
            }

            _isRequestInProgress = false;
        }

        private void HandleDetailedCityInformationResponseAndMapToState(string jsonResponse)
        {
            try
            {
                var detailedInformationDto = Newtonsoft.Json.JsonConvert.DeserializeObject<CityControllerGetDetailedCityInformationDTO>(jsonResponse);

                if (detailedInformationDto == null)
                {
                    Debug.LogError("[CityResourceService] Deserialisering fejlede. Svaret fra serveren var tomt eller ugyldigt.");
                    return;
                }

                // Mapping af data til den lokale state. 
                // Dette nulstiller eventuel 'drift' opstået ved lokal ekstrapolering.
                _currentResourceState.WoodAmount = detailedInformationDto.CurrentWoodAmount;
                _currentResourceState.WoodMaxCapacity = detailedInformationDto.MaxWoodCapacity;
                _currentResourceState.WoodProductionPerHour = detailedInformationDto.WoodProductionPerHour;

                _currentResourceState.StoneAmount = detailedInformationDto.CurrentStoneAmount;
                _currentResourceState.StoneMaxCapacity = detailedInformationDto.MaxStoneCapacity;
                _currentResourceState.StoneProductionPerHour = detailedInformationDto.StoneProductionPerHour;

                _currentResourceState.MetalAmount = detailedInformationDto.CurrentMetalAmount;
                _currentResourceState.MetalMaxCapacity = detailedInformationDto.MaxMetalCapacity;
                _currentResourceState.MetalProductionPerHour = detailedInformationDto.MetalProductionPerHour;

                _currentResourceState.SilverAmount = detailedInformationDto.CurrentSilverAmount;

                _currentResourceState.CurrentPopulationUsage = detailedInformationDto.CurrentPopulationUsage;
                _currentResourceState.MaxPopulationCapacity = detailedInformationDto.MaxPopulationCapacity;


                _isDataInitialized = true;

                // Notify lyttere om at vi har modtaget friske data fra ankeret (Serveren)
                OnResourceStateChanged?.Invoke(_currentResourceState);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CityResourceService] Kritisk fejl ved mapping af JSON: {exception.Message}");
            }
        }

        /// <summary>
        /// Bruges til manuelt at trække ressourcer fra lokalt (fx når brugeren starter en bygning),
        /// så UI'et reagerer med det samme uden at vente på næste server-poll.
        /// </summary>
        public void DeductResourcesLocally(double wood, double stone, double metal)
        {
            _currentResourceState.WoodAmount -= wood;
            _currentResourceState.StoneAmount -= stone;
            _currentResourceState.MetalAmount -= metal;

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }
    }
}