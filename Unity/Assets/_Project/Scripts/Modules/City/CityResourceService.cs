using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Project.Network.Models;
using Assets.Scripts.Domain.State;
using Newtonsoft.Json;
using Project.Network.Manager;
using Assets._Project.Scripts.Domain.DTOs; // VIGTIGT: Husk dette namespace for at kunne bruge BuildingDTO

namespace Project.Modules.City
{
    /// <summary>
    /// Service ansvarlig for at synkronisere byens ressource-data mellem Backend API og Unity UI.
    /// Denne service fungerer nu også som 'Data Provider' for CityManager.
    /// </summary>
    public class CityResourceService : MonoBehaviour
    {
        public static CityResourceService Instance { get; private set; }

        // Event til UI (Ressourcer)
        public event Action<CityResourceState> OnResourceStateChanged;

        // NYT EVENT: Event til CityManager (Bygninger)
        // CityManager lytter på dette for at vide, hvornår den skal opdatere 3D byen.
        public event Action<List<CityControllerGetDetailedCityInformationBuildingDTO>> OnBuildingStateReceived;

        [Header("Konfiguration")]
        [SerializeField] private float _networkSynchronizationIntervalInSeconds = 30f;

        private CityResourceState _currentResourceState = new CityResourceState(); // Initialiseret for at undgå null ref
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
        /// Kan også kaldes manuelt for at tvinge en opdatering (Force Refresh).
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
            // Vi kører loopet uendeligt
            while (true)
            {
                yield return StartCoroutine(PerformDetailedCityInformationNetworkRequestCoroutine(cityIdentifier));
                yield return new WaitForSeconds(_networkSynchronizationIntervalInSeconds);
            }
        }

        private IEnumerator PerformDetailedCityInformationNetworkRequestCoroutine(Guid cityIdentifier)
        {
            if (_isRequestInProgress) yield break;

            // Validering af NetworkManager
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[CityResourceService] NetworkManager mangler!");
                yield break;
            }

            _isRequestInProgress = true;
            string token = NetworkManager.Instance.JwtToken;

            // Kald serveren
            yield return StartCoroutine(NetworkManager.Instance.City.GetDetailedCityInfo(cityIdentifier, token, (cityInfo) =>
            {
                if (cityInfo != null)
                {
                    HandleDetailedCityInformationResponseAndMapToState(cityInfo);
                }
                else
                {
                    Debug.LogWarning("[CityResourceService] Kunne ikke hente by-data (NULL response).");
                }
            }));

            _isRequestInProgress = false;
        }

        private void HandleDetailedCityInformationResponseAndMapToState(CityControllerGetDetailedCityInformationDTO detailedInformationDto)
        {
            try
            {
                // 1. Mapping af data til den lokale Resource State
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

                // 2. Notify UI lyttere (TopBar)
                OnResourceStateChanged?.Invoke(_currentResourceState);

                // 3. Notify CityManager lyttere (Bygninger) [VIGTIGT NYT TRIN]
                // Vi sender listen af bygninger videre, så CityManager kan opdatere 3D verdenen
                // uden selv at skulle lave et netværkskald.
                if (detailedInformationDto.BuildingList != null)
                {
                    OnBuildingStateReceived?.Invoke(detailedInformationDto.BuildingList);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CityResourceService] Kritisk fejl ved mapping af data: {exception.Message}");
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