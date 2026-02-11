using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Project.Network.Models;
using Assets.Scripts.Domain.State;
using Newtonsoft.Json;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

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
        public event Action<List<BuildingDTO>> OnBuildingQueueChanged;
        public event Action<List<RecruitmentQueueItemDTO>> OnBarracksQueueChanged;
        public event Action<List<RecruitmentQueueItemDTO>> OnStableQueueChanged;
        public event Action<List<RecruitmentQueueItemDTO>> OnWorkshopQueueChanged;

        public event Action<List<CityControllerGetDetailedCityInformationBuildingDTO>> OnBuildingStateReceived;
        public event Action<List<UnitStackDTO>> OnTroopsStateReceived;

        [Header("Konfiguration")]
        [SerializeField] private float _networkSynchronizationIntervalInSeconds = 30f;

        // --- Intern Tilstand ---
        private CityResourceState _currentResourceState = new CityResourceState();
        private List<BuildingDTO> _currentBuildingQueue = new List<BuildingDTO>();
        private List<UnitStackDTO> _currentStationedUnits = new List<UnitStackDTO>();
        private List<UnitDeploymentDTO> _currentActiveDeployments = new List<UnitDeploymentDTO>();

        private List<RecruitmentQueueItemDTO> _currentBarracksQueue = new List<RecruitmentQueueItemDTO>();
        private List<RecruitmentQueueItemDTO> _currentStableQueue = new List<RecruitmentQueueItemDTO>();
        private List<RecruitmentQueueItemDTO> _currentWorkshopQueue = new List<RecruitmentQueueItemDTO>();


        // --- Public Properties ---
        public CityResourceState CurrentResources => _currentResourceState;
        public List<BuildingDTO> CurrentBuildingQueue => _currentBuildingQueue;
        public List<UnitStackDTO> CurrentStationedUnits => _currentStationedUnits;
        public List<UnitDeploymentDTO> CurrentActiveDeployments => _currentActiveDeployments;

        public List<RecruitmentQueueItemDTO> CurrentBarracksQueue => _currentBarracksQueue;
        public List<RecruitmentQueueItemDTO> CurrentStableQueue => _currentStableQueue;
        public List<RecruitmentQueueItemDTO> CurrentWorkshopQueue => _currentWorkshopQueue;

        public Guid CityId { get; set; }
        public int HomeCityX { get; private set; }
        public int HomeCityY { get; private set; }

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

                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }

                DontDestroyOnLoad(gameObject);
                Debug.Log("[CityStateManager] Global instans initialiseret.");
            }
            else if (Instance != this)
            {
                Debug.Log("[CityStateManager] Duplikat fundet og slettet.");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!_isDataInitialized) return;

            ExecuteLocalResourceExtrapolationPerFrame();
        }

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
            Debug.Log($"[CityStateManager] InitiateResourceRefresh kaldet for by: {cityIdentifier}");
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
                Debug.Log("[CityStateManager] Starter planlagt netværks-polling...");
                yield return StartCoroutine(PerformFullCityStateSyncCoroutine(cityIdentifier));
                yield return new WaitForSeconds(_networkSynchronizationIntervalInSeconds);
            }
        }

        private IEnumerator PerformFullCityStateSyncCoroutine(Guid cityIdentifier)
        {
            if (_isRequestInProgress) yield break;
            _isRequestInProgress = true;

            string token = NetworkManager.Instance.JwtToken;

            // 1. Hent Detailed Info
            yield return StartCoroutine(NetworkManager.Instance.City.GetDetailedCityInfo(cityIdentifier, token, (cityInfo) =>
            {
                if (cityInfo != null) HandleDetailedCityInformationResponseAndMapToState(cityInfo);
            }));

            // 2. Hent Bygningskø
            yield return StartCoroutine(NetworkManager.Instance.Building.GetBuildingQueue(cityIdentifier, token, (queue) =>
            {
                if (queue != null)
                {
                    _currentBuildingQueue = queue;
                    OnBuildingQueueChanged?.Invoke(_currentBuildingQueue);
                }
            }));

            // 3. Hent rekrutteringskø
            yield return StartCoroutine(NetworkManager.Instance.Barracks.GetRecruitmentQueue(cityIdentifier, token, (queue) => {
                _currentBarracksQueue = queue;
                OnBarracksQueueChanged?.Invoke(queue);
            }));

            yield return StartCoroutine(NetworkManager.Instance.Stable.GetRecruitmentQueue(cityIdentifier, token, (queue) => {
                _currentStableQueue = queue;
                OnStableQueueChanged?.Invoke(queue);
            }));

            yield return StartCoroutine(NetworkManager.Instance.Workshop.GetRecruitmentQueue(cityIdentifier, token, (queue) => {
                _currentWorkshopQueue = queue;
                OnWorkshopQueueChanged?.Invoke(queue);
            }));

            _isRequestInProgress = false;
        }

        private void HandleDetailedCityInformationResponseAndMapToState(CityControllerGetDetailedCityInformationDTO detailedInformationDto)
        {
            try
            {
                Debug.Log($"[CityStateManager] MODTAGET DATA fra server for {detailedInformationDto.CityName}. Analyserer tilstand...");

                // Log befolkning før overskrivning
                Debug.Log($"[STATE-SYNC] Population før: USED={_currentResourceState.CurrentPopulationUsage}, MAX={_currentResourceState.MaxPopulationCapacity}");
                Debug.Log($"[STATE-SYNC] Population fra server: USED={detailedInformationDto.CurrentPopulationUsage}, MAX={detailedInformationDto.MaxPopulationCapacity}");

                if (_currentResourceState.CurrentPopulationUsage != detailedInformationDto.CurrentPopulationUsage)
                {
                    Debug.LogWarning($"[DEBUG-POPULATION] AFVIGELSE FUNDET! Lokal: {_currentResourceState.CurrentPopulationUsage} vs Server: {detailedInformationDto.CurrentPopulationUsage}");
                }

                this.CityId = detailedInformationDto.CityId;
                this.HomeCityX = detailedInformationDto.X;
                this.HomeCityY = detailedInformationDto.Y;

                // Opdatering af ressourcer
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

                // Befolknings-mapping
                _currentResourceState.CurrentPopulationUsage = detailedInformationDto.CurrentPopulationUsage;
                _currentResourceState.MaxPopulationCapacity = detailedInformationDto.MaxPopulationCapacity;

                _currentResourceState.ResearchPointsAmount = detailedInformationDto.CurrentResearchPoints;
                _currentResourceState.ResearchPointsProductionPerHour = detailedInformationDto.ResearchPointsPerHour;

                _currentResourceState.IdeologyFocusPointsAmount = detailedInformationDto.CurrentIdeologyFocusPoints;
                _currentResourceState.IdeologyFocusPointsProductionPerHour = detailedInformationDto.IdeologyFocusPointsPerHour;

                if (detailedInformationDto.BuildingList != null) OnBuildingStateReceived?.Invoke(detailedInformationDto.BuildingList);

                _currentStationedUnits = detailedInformationDto.StationedUnits ?? new List<UnitStackDTO>();
                OnTroopsStateReceived?.Invoke(_currentStationedUnits);

                _isDataInitialized = true;
                OnResourceStateChanged?.Invoke(_currentResourceState);

                Debug.Log($"[CityStateManager] Synkronisering fuldført. Ny tilstand anvendt.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CityStateManager] Kritisk fejl ved mapping af data: {exception.Message}\n{exception.StackTrace}");
            }
        }

        public void DeductResourcesLocally(double wood, double stone, double metal, double silver = 0, double research = 0, double ideology = 0)
        {
            Debug.Log($"[CityStateManager] DeductResourcesLocally kaldet. Trækker Wood:{wood}, Stone:{stone}, Metal:{metal}");
            _currentResourceState.WoodAmount -= wood;
            _currentResourceState.StoneAmount -= stone;
            _currentResourceState.MetalAmount -= metal;
            _currentResourceState.SilverAmount -= silver;
            _currentResourceState.ResearchPointsAmount -= research;
            _currentResourceState.IdeologyFocusPointsAmount -= ideology;

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }

        public void UpdatePopulationState(int updatedUsage, int updatedMax)
        {
            Debug.Log($"[CityStateManager] UpdatePopulationState kaldet MANUELT. Ny Usage: {updatedUsage}, Ny Max: {updatedMax}");

            _currentResourceState.CurrentPopulationUsage = updatedUsage;
            _currentResourceState.MaxPopulationCapacity = updatedMax;

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }
    }
}