using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Project.Network.Models;
using Assets.Scripts.Domain.State;
using Newtonsoft.Json;
using Project.Network.Manager;

namespace Project.Modules.City
{
    /// <summary>
    /// Service ansvarlig for at synkronisere byens ressource-data mellem Backend API og Unity UI.
    /// Denne service fungerer nu også som 'Data Provider' for CityManager.
    /// </summary>
    public class CityStateManager : MonoBehaviour
    {
        public static CityStateManager Instance { get; private set; }

        // --- Events ---
        public event Action<CityResourceState> OnResourceStateChanged;
        public event Action<List<CityControllerGetDetailedCityInformationBuildingDTO>> OnBuildingStateReceived;
        public event Action<List<UnitStackDTO>> OnTroopsStateReceived;
        public event Action<List<UnitDeploymentDTO>> OnDeploymentsStateReceived;

        [Header("Konfiguration")]
        [SerializeField] private float _networkSynchronizationIntervalInSeconds = 30f;

        // --- Intern Tilstand ---
        private CityResourceState _currentResourceState = new CityResourceState();
        private List<UnitStackDTO> _currentStationedUnits = new List<UnitStackDTO>();
        private List<UnitDeploymentDTO> _currentActiveDeployments = new List<UnitDeploymentDTO>();

        // --- Public Properties ---
        public CityResourceState CurrentResources => _currentResourceState;
        public List<UnitStackDTO> CurrentStationedUnits => _currentStationedUnits;
        public List<UnitDeploymentDTO> CurrentActiveDeployments => _currentActiveDeployments;

        private bool _isRequestInProgress = false;
        private bool _isDataInitialized = false;
        private Coroutine _activePollingCoroutine;

        private void Awake()
        {
            InitializeManagerSingleton();
        }

        private void InitializeManagerSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[CityStateManager] Global instans initialiseret.");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!_isDataInitialized) return;

            ExecuteLocalResourceExtrapolationPerFrame();
        }

        /// <summary>
        /// Beregner den visuelle vækst i ressourcer lokalt på klienten (Client-side prediction).
        /// </summary>
        private void ExecuteLocalResourceExtrapolationPerFrame()
        {
            double secondsPassedSinceLastFrame = Time.deltaTime;
            double hoursPassedThisFrame = secondsPassedSinceLastFrame / 3600.0;

            _currentResourceState.WoodAmount = Math.Min(
                _currentResourceState.WoodMaxCapacity,
                _currentResourceState.WoodAmount + (_currentResourceState.WoodProductionPerHour * hoursPassedThisFrame));

            _currentResourceState.StoneAmount = Math.Min(
                _currentResourceState.StoneMaxCapacity,
                _currentResourceState.StoneAmount + (_currentResourceState.StoneProductionPerHour * hoursPassedThisFrame));

            _currentResourceState.MetalAmount = Math.Min(
                _currentResourceState.MetalMaxCapacity,
                _currentResourceState.MetalAmount + (_currentResourceState.MetalProductionPerHour * hoursPassedThisFrame));

            _currentResourceState.SilverAmount += _currentResourceState.SilverProductionPerHour * hoursPassedThisFrame;
            _currentResourceState.ResearchPointsAmount += _currentResourceState.ResearchPointsProductionPerHour * hoursPassedThisFrame;
            _currentResourceState.IdeologyFocusPointsAmount += _currentResourceState.IdeologyFocusPointsProductionPerHour * hoursPassedThisFrame;

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }

        public void InitiateResourceRefresh(Guid cityIdentifier)
        {
            if (_activePollingCoroutine != null)
            {
                StopCoroutine(_activePollingCoroutine);
            }

            _activePollingCoroutine = StartCoroutine(ExecuteResourcePollingCycleCoroutine(cityIdentifier));
        }

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

            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[CityStateManager] NetworkManager mangler!");
                yield break;
            }

            _isRequestInProgress = true;
            string token = NetworkManager.Instance.JwtToken;

            yield return StartCoroutine(NetworkManager.Instance.City.GetDetailedCityInfo(cityIdentifier, token, (cityInfo) =>
            {
                if (cityInfo != null)
                {
                    HandleDetailedCityInformationResponseAndMapToState(cityInfo);
                }
                else
                {
                    Debug.LogWarning("[CityStateManager] Kunne ikke hente by-data (NULL response).");
                }
            }));

            _isRequestInProgress = false;
        }

        private void HandleDetailedCityInformationResponseAndMapToState(CityControllerGetDetailedCityInformationDTO detailedInformationDto)
        {
            try
            {
                // 1. Map Ressourcer
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
                _currentResourceState.SilverProductionPerHour = detailedInformationDto.SilverProductionPerHour;

                _currentResourceState.CurrentPopulationUsage = detailedInformationDto.CurrentPopulationUsage;
                _currentResourceState.MaxPopulationCapacity = detailedInformationDto.MaxPopulationCapacity;

                _currentResourceState.ResearchPointsAmount = detailedInformationDto.CurrentResearchPoints;
                _currentResourceState.ResearchPointsProductionPerHour = detailedInformationDto.ResearchPointsPerHour;

                _currentResourceState.IdeologyFocusPointsAmount = detailedInformationDto.CurrentIdeologyFocusPoints;
                _currentResourceState.IdeologyFocusPointsProductionPerHour = detailedInformationDto.IdeologyFocusPointsPerHour;

                // 2. Map Bygningsliste
                if (detailedInformationDto.BuildingList != null)
                {
                    OnBuildingStateReceived?.Invoke(detailedInformationDto.BuildingList);
                }

                // 3. Map Tropper (Garnison)
                _currentStationedUnits = detailedInformationDto.StationedUnits ?? new List<UnitStackDTO>();
                OnTroopsStateReceived?.Invoke(_currentStationedUnits);

                // 4. Map Deployments (Hær-bevægelser)
                _currentActiveDeployments = detailedInformationDto.DeployedUnits ?? new List<UnitDeploymentDTO>();
                OnDeploymentsStateReceived?.Invoke(_currentActiveDeployments);

                _isDataInitialized = true;
                OnResourceStateChanged?.Invoke(_currentResourceState);

                Debug.Log($"[CityStateManager] Synkronisering fuldført for {detailedInformationDto.CityName}. Tropper: {_currentStationedUnits.Count}, Deployments: {_currentActiveDeployments.Count}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CityStateManager] Kritisk fejl ved mapping af data: {exception.Message}");
            }
        }

        public void DeductResourcesLocally(double wood, double stone, double metal, double silver = 0, double research = 0, double ideology = 0)
        {
            _currentResourceState.WoodAmount -= wood;
            _currentResourceState.StoneAmount -= stone;
            _currentResourceState.MetalAmount -= metal;
            _currentResourceState.SilverAmount -= silver;
            _currentResourceState.ResearchPointsAmount -= research;
            _currentResourceState.IdeologyFocusPointsAmount -= ideology;

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }
    }
}