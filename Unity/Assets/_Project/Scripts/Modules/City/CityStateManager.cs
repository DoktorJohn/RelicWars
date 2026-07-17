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
        public event Action<string> OnCityNameChanged;
        public event Action<List<BuildingDTO>> OnBuildingQueueChanged;
        public event Action<List<RecruitmentQueueItemDTO>> OnBarracksQueueChanged;
        public event Action<List<RecruitmentQueueItemDTO>> OnStableQueueChanged;
        public event Action<List<RecruitmentQueueItemDTO>> OnWorkshopQueueChanged;
        public event Action<List<RecruitmentQueueItemDTO>> OnHarborQueueChanged;

        public event Action<List<CityControllerGetDetailedCityInformationBuildingDTO>> OnBuildingStateReceived;
        public event Action<List<AvailableBuildingDTO>> OnTownHallAvailableBuildingsChanged;
        public event Action<List<UnitStackDTO>> OnTroopsStateReceived;

        [Header("Konfiguration")]
        [SerializeField] private float _networkSynchronizationIntervalInSeconds = 30f;

        // --- Intern Tilstand ---
        private CityResourceState _currentResourceState = new CityResourceState();
        private List<BuildingDTO> _currentBuildingQueue = new List<BuildingDTO>();
        private List<CityControllerGetDetailedCityInformationBuildingDTO> _currentBuildingState = new List<CityControllerGetDetailedCityInformationBuildingDTO>();
        private List<AvailableBuildingDTO> _currentTownHallAvailableBuildings = new List<AvailableBuildingDTO>();
        private List<UnitStackDTO> _currentStationedUnits = new List<UnitStackDTO>();
        private List<UnitDeploymentDTO> _currentActiveDeployments = new List<UnitDeploymentDTO>();

        private List<RecruitmentQueueItemDTO> _currentBarracksQueue = new List<RecruitmentQueueItemDTO>();
        private List<RecruitmentQueueItemDTO> _currentStableQueue = new List<RecruitmentQueueItemDTO>();
        private List<RecruitmentQueueItemDTO> _currentWorkshopQueue = new List<RecruitmentQueueItemDTO>();
        private List<RecruitmentQueueItemDTO> _currentHarborQueue = new List<RecruitmentQueueItemDTO>();
        private List<CityExoticResourceDTO> _currentExoticResources = new List<CityExoticResourceDTO>();
        private List<WorldIslandResourceDTO> _currentIslandExoticResources = new List<WorldIslandResourceDTO>();


        // --- Public Properties ---
        public CityResourceState CurrentResources => _currentResourceState;
        public List<BuildingDTO> CurrentBuildingQueue => _currentBuildingQueue;
        public List<CityControllerGetDetailedCityInformationBuildingDTO> CurrentBuildingState => _currentBuildingState;
        public List<AvailableBuildingDTO> CurrentTownHallAvailableBuildings => _currentTownHallAvailableBuildings;
        public List<UnitStackDTO> CurrentStationedUnits => _currentStationedUnits;
        public List<UnitDeploymentDTO> CurrentActiveDeployments => _currentActiveDeployments;

        public List<RecruitmentQueueItemDTO> CurrentBarracksQueue => _currentBarracksQueue;
        public List<RecruitmentQueueItemDTO> CurrentStableQueue => _currentStableQueue;
        public List<RecruitmentQueueItemDTO> CurrentWorkshopQueue => _currentWorkshopQueue;
        public List<RecruitmentQueueItemDTO> CurrentHarborQueue => _currentHarborQueue;
        public List<CityExoticResourceDTO> CurrentExoticResources => _currentExoticResources;
        public List<WorldIslandResourceDTO> CurrentIslandExoticResources => _currentIslandExoticResources;

        public Guid CityId { get; private set; }
        public string CurrentCityName { get; private set; }
        public int HomeCityX { get; private set; }
        public int HomeCityY { get; private set; }
        public double Resistance { get; private set; }
        public double ResistanceTarget { get; private set; }
        public double ResistanceRecoveryPerHour { get; private set; }
        public bool HasDetailedCityState => _isDataInitialized;
        public bool HasBuildingQueueData => _hasBuildingQueueData;
        public bool HasBuildingStateData => _hasBuildingStateData;
        public bool HasTownHallAvailableBuildingsData => _hasTownHallAvailableBuildingsData;

        public bool IsPollingCity(Guid cityIdentifier)
        {
            return cityIdentifier != Guid.Empty &&
                   _activePollingCoroutine != null &&
                   _pollingCityId == cityIdentifier;
        }

        private bool _isRequestInProgress = false;
        private bool _isDataInitialized = false;
        private bool _hasBuildingQueueData = false;
        private bool _hasBuildingStateData = false;
        private bool _hasTownHallAvailableBuildingsData = false;
        private Coroutine _activePollingCoroutine;
        private Guid _pollingCityId;
        private int _pollingGeneration;
        private bool _immediateRefreshRequested;
        private Coroutine _buildingQueueResolutionCoroutine;
        private readonly HashSet<Guid> _buildingJobsAwaitingRemoval = new HashSet<Guid>();

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
            }
            else if (Instance != this)
            {
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

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }

        public void StartPollingForCity(Guid cityIdentifier)
        {
            if (cityIdentifier == Guid.Empty)
            {
                return;
            }

            if (_activePollingCoroutine != null && _pollingCityId == cityIdentifier)
            {
                _immediateRefreshRequested = true;
                return;
            }

            StopActivePolling();
            ClearCitySpecificState();

            _pollingCityId = cityIdentifier;
            int generation = _pollingGeneration;
            _activePollingCoroutine = StartCoroutine(
                ExecuteResourcePollingCycleCoroutine(cityIdentifier, generation));
        }

        public void RequestImmediateRefresh(Guid cityIdentifier)
        {
            if (cityIdentifier == Guid.Empty)
            {
                return;
            }

            if (_activePollingCoroutine == null)
            {
                StartPollingForCity(cityIdentifier);
                return;
            }

            if (_pollingCityId != cityIdentifier)
            {
                Debug.LogWarning(
                    $"[CityStateManager] Ignorerer refresh for city {cityIdentifier}, " +
                    $"fordi polleren ejer city {_pollingCityId}.");
                return;
            }

            _immediateRefreshRequested = true;
        }

        public void RequestBuildingQueueResolution(Guid cityIdentifier, Guid completedJobId)
        {
            if (cityIdentifier == Guid.Empty || completedJobId == Guid.Empty ||
                _pollingCityId != cityIdentifier)
            {
                return;
            }

            _buildingJobsAwaitingRemoval.Add(completedJobId);
            if (_buildingQueueResolutionCoroutine == null)
            {
                _buildingQueueResolutionCoroutine = StartCoroutine(
                    ResolveCompletedBuildingJobsCoroutine(cityIdentifier, _pollingGeneration));
            }
        }

        public void ResetForLogout()
        {
            StopActivePolling();
            ClearCitySpecificState();
        }

        private void StopActivePolling()
        {
            _pollingGeneration++;
            _immediateRefreshRequested = false;

            if (_activePollingCoroutine != null)
            {
                StopCoroutine(_activePollingCoroutine);
                _activePollingCoroutine = null;
            }

            if (_buildingQueueResolutionCoroutine != null)
            {
                StopCoroutine(_buildingQueueResolutionCoroutine);
                _buildingQueueResolutionCoroutine = null;
            }

            _buildingJobsAwaitingRemoval.Clear();

            _pollingCityId = Guid.Empty;
            _isRequestInProgress = false;
        }

        private void ClearCitySpecificState()
        {
            _isDataInitialized = false;
            _hasBuildingQueueData = false;
            _hasBuildingStateData = false;
            _hasTownHallAvailableBuildingsData = false;
            CityId = Guid.Empty;
            CurrentCityName = string.Empty;
            _currentResourceState = new CityResourceState();
            _currentBuildingQueue.Clear();
            _currentBuildingState.Clear();
            _currentTownHallAvailableBuildings.Clear();
            _currentStationedUnits.Clear();
            _currentActiveDeployments.Clear();
            _currentBarracksQueue.Clear();
            _currentStableQueue.Clear();
            _currentWorkshopQueue.Clear();
            _currentHarborQueue.Clear();
            _currentExoticResources.Clear();
            _currentIslandExoticResources.Clear();
        }

        private IEnumerator ExecuteResourcePollingCycleCoroutine(Guid cityIdentifier, int generation)
        {
            try
            {
                while (IsCurrentPollingRequest(cityIdentifier, generation))
                {
                    _immediateRefreshRequested = false;
                    yield return StartCoroutine(PerformFullCityStateSyncCoroutine(cityIdentifier, generation));

                    if (!IsCurrentPollingRequest(cityIdentifier, generation))
                    {
                        yield break;
                    }

                    float waitTime = 0f;
                    while (waitTime < _networkSynchronizationIntervalInSeconds && !_immediateRefreshRequested)
                    {
                        waitTime += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }
            finally
            {
                if (IsCurrentPollingRequest(cityIdentifier, generation))
                {
                    _activePollingCoroutine = null;
                    _isRequestInProgress = false;
                }
            }
        }

        private IEnumerator PerformFullCityStateSyncCoroutine(Guid cityIdentifier, int generation)
        {
            if (_isRequestInProgress) yield break;
            _isRequestInProgress = true;

            try
            {
                if (NetworkManager.Instance == null)
                {
                    yield break;
                }

                string token = NetworkManager.Instance.JwtToken;

                // 1. Hent Detailed Info
                yield return StartCoroutine(NetworkManager.Instance.City.GetDetailedCityInfo(cityIdentifier, token, cityInfo =>
                {
                    if (IsCurrentPollingRequest(cityIdentifier, generation) && cityInfo != null)
                    {
                        HandleDetailedCityInformationResponseAndMapToState(cityInfo);
                    }
                }));

                // 2. Hent Bygningskø
                yield return StartCoroutine(NetworkManager.Instance.Building.GetBuildingQueue(cityIdentifier, token, queue =>
                {
                    if (IsCurrentPollingRequest(cityIdentifier, generation) && queue != null)
                    {
                        ApplyBuildingQueue(queue);
                    }
                }));

                // 3. Hent rekrutteringskø
                yield return StartCoroutine(NetworkManager.Instance.Barracks.GetRecruitmentQueue(cityIdentifier, token, queue =>
                {
                    if (!IsCurrentPollingRequest(cityIdentifier, generation)) return;
                    _currentBarracksQueue = queue ?? new List<RecruitmentQueueItemDTO>();
                    OnBarracksQueueChanged?.Invoke(_currentBarracksQueue);
                }));

                yield return StartCoroutine(NetworkManager.Instance.Stable.GetRecruitmentQueue(cityIdentifier, token, queue =>
                {
                    if (!IsCurrentPollingRequest(cityIdentifier, generation)) return;
                    _currentStableQueue = queue ?? new List<RecruitmentQueueItemDTO>();
                    OnStableQueueChanged?.Invoke(_currentStableQueue);
                }));

                yield return StartCoroutine(NetworkManager.Instance.Workshop.GetRecruitmentQueue(cityIdentifier, token, queue =>
                {
                    if (!IsCurrentPollingRequest(cityIdentifier, generation)) return;
                    _currentWorkshopQueue = queue ?? new List<RecruitmentQueueItemDTO>();
                    OnWorkshopQueueChanged?.Invoke(_currentWorkshopQueue);
                }));

                yield return StartCoroutine(NetworkManager.Instance.Harbor.GetRecruitmentQueue(cityIdentifier, token, queue =>
                {
                    if (!IsCurrentPollingRequest(cityIdentifier, generation)) return;
                    _currentHarborQueue = queue ?? new List<RecruitmentQueueItemDTO>();
                    OnHarborQueueChanged?.Invoke(_currentHarborQueue);
                }));
            }
            finally
            {
                if (IsCurrentPollingRequest(cityIdentifier, generation))
                {
                    _isRequestInProgress = false;
                }
            }
        }

        private bool IsCurrentPollingRequest(Guid cityIdentifier, int generation)
        {
            return _pollingGeneration == generation && _pollingCityId == cityIdentifier;
        }

        private IEnumerator ResolveCompletedBuildingJobsCoroutine(Guid cityIdentifier, int generation)
        {
            var retryDelay = new WaitForSecondsRealtime(1f);

            while (IsCurrentPollingRequest(cityIdentifier, generation) &&
                   _buildingJobsAwaitingRemoval.Count > 0)
            {
                List<BuildingDTO> queue = null;
                if (NetworkManager.Instance != null)
                {
                    yield return StartCoroutine(NetworkManager.Instance.Building.GetBuildingQueue(
                        cityIdentifier,
                        NetworkManager.Instance.JwtToken,
                        response => queue = response));
                }

                if (!IsCurrentPollingRequest(cityIdentifier, generation))
                {
                    break;
                }

                if (queue != null)
                {
                    ApplyBuildingQueue(queue);
                    _buildingJobsAwaitingRemoval.RemoveWhere(jobId =>
                        queue.TrueForAll(job => job.Id != jobId));
                }

                if (_buildingJobsAwaitingRemoval.Count > 0)
                {
                    yield return retryDelay;
                }
            }

            if (IsCurrentPollingRequest(cityIdentifier, generation))
            {
                _buildingQueueResolutionCoroutine = null;
            }
        }

        private void ApplyBuildingQueue(List<BuildingDTO> queue)
        {
            _currentBuildingQueue = queue;
            _hasBuildingQueueData = true;
            OnBuildingQueueChanged?.Invoke(_currentBuildingQueue);
        }

        private void HandleDetailedCityInformationResponseAndMapToState(CityControllerGetDetailedCityInformationDTO detailedInformationDto)
        {
            try
            {
                this.CityId = detailedInformationDto.CityId;
                
                if (this.CurrentCityName != detailedInformationDto.CityName)
                {
                    this.CurrentCityName = detailedInformationDto.CityName;
                    OnCityNameChanged?.Invoke(this.CurrentCityName);
                }
                
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

                _currentResourceState.CoinsProductionPerHour = detailedInformationDto.CoinsProductionPerHour;
                _currentResourceState.ResearchPointsPerHour = detailedInformationDto.ResearchPointsPerHour;
                _currentResourceState.IdeologyFocusPointsPerHour = detailedInformationDto.IdeologyFocusPointsPerHour;

                // Befolknings-mapping
                _currentResourceState.CurrentPopulationUsage = detailedInformationDto.CurrentPopulationUsage;
                _currentResourceState.MaxPopulationCapacity = detailedInformationDto.MaxPopulationCapacity;
                if (detailedInformationDto.Population != null)
                {
                    _currentResourceState.HousingPopulationCapacity = detailedInformationDto.Population.HousingCapacity;
                    _currentResourceState.PopulationModifierBonus = detailedInformationDto.Population.ModifierBonus;
                    _currentResourceState.CurrentPopulationUsage = detailedInformationDto.Population.InUse;
                    _currentResourceState.MaxPopulationCapacity = detailedInformationDto.Population.TotalCapacity;
                    _currentResourceState.RemainingPopulation = detailedInformationDto.Population.Remaining;
                }
                Resistance = detailedInformationDto.Resistance;
                ResistanceTarget = detailedInformationDto.ResistanceTarget;
                ResistanceRecoveryPerHour = detailedInformationDto.ResistanceRecoveryPerHour;
                _currentExoticResources = detailedInformationDto.ExoticResources ?? new List<CityExoticResourceDTO>();
                _currentIslandExoticResources = detailedInformationDto.IslandExoticResources ?? new List<WorldIslandResourceDTO>();

                if (detailedInformationDto.BuildingList != null)
                {
                    _currentBuildingState = detailedInformationDto.BuildingList;
                    _hasBuildingStateData = true;
                    OnBuildingStateReceived?.Invoke(_currentBuildingState);
                }

                _currentStationedUnits = detailedInformationDto.StationedUnits ?? new List<UnitStackDTO>();
                OnTroopsStateReceived?.Invoke(_currentStationedUnits);

                _isDataInitialized = true;
                OnResourceStateChanged?.Invoke(_currentResourceState);

            }
            catch (Exception exception)
            {
                Debug.LogError($"[CityStateManager] Kritisk fejl ved mapping af data: {exception.Message}\n{exception.StackTrace}");
            }
        }

        public void DeductResourcesLocally(double wood, double stone, double metal)
        {
            _currentResourceState.WoodAmount -= wood;
            _currentResourceState.StoneAmount -= stone;
            _currentResourceState.MetalAmount -= metal;

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }

        public void UpdatePopulationState(int updatedUsage, int updatedMax)
        {
            _currentResourceState.CurrentPopulationUsage = updatedUsage;
            _currentResourceState.MaxPopulationCapacity = updatedMax;

            OnResourceStateChanged?.Invoke(_currentResourceState);
        }

        public void UpdateTownHallAvailableBuildings(List<AvailableBuildingDTO> availableBuildings)
        {
            _currentTownHallAvailableBuildings = availableBuildings ?? new List<AvailableBuildingDTO>();
            _hasTownHallAvailableBuildingsData = true;
            OnTownHallAvailableBuildingsChanged?.Invoke(_currentTownHallAvailableBuildings);
        }
    }
}
