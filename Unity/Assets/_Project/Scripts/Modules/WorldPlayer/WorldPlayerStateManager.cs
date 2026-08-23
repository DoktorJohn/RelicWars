using UnityEngine;
using System;
using System.Collections;
using Project.Network.Manager;
using Assets.Scripts.Domain.State;
using Project.Scripts.Domain.DTOs;
using Project.Network.Models;

namespace Project.Modules.WorldPlayer
{
    public class WorldPlayerStateManager : MonoBehaviour
    {
        public static WorldPlayerStateManager Instance { get; private set; }

        public event Action<WorldPlayerState> OnEconomyStateChanged;

        [Header("Configuration")]
        [SerializeField] private float _networkSynchronizationIntervalInSeconds = 30f;

        private WorldPlayerState _currentEconomyState = new WorldPlayerState();
        public WorldPlayerState CurrentEconomy => _currentEconomyState;
        public bool HasEconomyState { get; private set; }

        private bool _isRequestInProgress = false;
        private Coroutine _activePollingCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                Debug.Log("[WorldPlayerStateManager] Global instance initialized.");
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            ExecuteLocalResourceExtrapolationPerFrame();
        }

        private void ExecuteLocalResourceExtrapolationPerFrame()
        {
            double hoursPassedThisFrame = Time.deltaTime / 3600.0;

            if (_currentEconomyState == null) return;

            _currentEconomyState.CoinsAmount += _currentEconomyState.CoinsProductionPerHour * hoursPassedThisFrame;
            _currentEconomyState.IdeologyFocusPointsAmount += _currentEconomyState.IdeologyFocusPointsProductionPerHour * hoursPassedThisFrame;

            // Debug.Log($"[WorldPlayerStateManager] Extrapolating: Coins={_currentEconomyState.CoinsAmount:F2} (+{_currentEconomyState.CoinsProductionPerHour:F2}/h)");
            OnEconomyStateChanged?.Invoke(_currentEconomyState);
        }

        public void InitiateEconomyRefresh(Guid worldPlayerId)
        {
            Debug.Log($"[WorldPlayerStateManager] Initiating economy refresh for WorldPlayerId: {worldPlayerId}");
            if (_activePollingCoroutine != null) StopCoroutine(_activePollingCoroutine);
            _activePollingCoroutine = StartCoroutine(ExecuteEconomyPollingCycleCoroutine(worldPlayerId));
        }

        public void ResetForLogout()
        {
            StopAllCoroutines();
            _activePollingCoroutine = null;
            _isRequestInProgress = false;
            _currentEconomyState = new WorldPlayerState();
            HasEconomyState = false;
        }

        private IEnumerator ExecuteEconomyPollingCycleCoroutine(Guid worldPlayerId)
        {
            while (true)
            {
                Debug.Log("[WorldPlayerStateManager] Starting planned economy sync...");
                yield return StartCoroutine(PerformFullEconomySyncCoroutine(worldPlayerId));
                yield return new WaitForSeconds(_networkSynchronizationIntervalInSeconds);
            }
        }

        private IEnumerator PerformFullEconomySyncCoroutine(Guid worldPlayerId)
        {
            if (_isRequestInProgress)
            {
                Debug.LogWarning("[WorldPlayerStateManager] Request already in progress, skipping sync.");
                yield break;
            }
            _isRequestInProgress = true;

            string token = NetworkManager.Instance.JwtToken;

            Debug.Log($"[WorldPlayerStateManager] Fetching economy data for {worldPlayerId}...");
            yield return StartCoroutine(NetworkManager.Instance.WorldPlayer.GetWorldPlayerEconomy(worldPlayerId, token, (economyDto) =>
            {
                if (economyDto != null)
                {
                    HandleEconomyResponseAndMapToState(economyDto);
                }
                else
                {
                    Debug.LogError("[WorldPlayerStateManager] Failed to fetch economy data.");
                }
            }));

            _isRequestInProgress = false;
        }

        private void HandleEconomyResponseAndMapToState(WorldPlayerEconomyDTO dto)
        {
            if (_currentEconomyState == null) _currentEconomyState = new WorldPlayerState();

            _currentEconomyState.CoinsAmount = dto.CurrentCoinsAmount;
            _currentEconomyState.CoinsProductionPerHour = dto.CoinsProductionPerHour;

            _currentEconomyState.BaseResearchPower = dto.ResearchRate?.BaseResearchPower ?? 0d;
            _currentEconomyState.EffectiveResearchPower = dto.ResearchRate?.EffectiveResearchPower ?? 0d;
            _currentEconomyState.ResearchSpeedMultiplier = dto.ResearchRate?.SpeedMultiplier ?? 0d;

            _currentEconomyState.IdeologyFocusPointsAmount = dto.CurrentIdeologyFocusPoints;
            _currentEconomyState.IdeologyFocusPointsProductionPerHour = dto.IdeologyFocusPointsPerHour;
            _currentEconomyState.TotalWoodAmount = dto.TotalWoodAmount;
            _currentEconomyState.TotalStoneAmount = dto.TotalStoneAmount;
            _currentEconomyState.TotalMetalAmount = dto.TotalMetalAmount;
            _currentEconomyState.TotalPopulationAmount = dto.TotalPopulationAmount;
            _currentEconomyState.PlayerCities = dto.PlayerCities ?? new System.Collections.Generic.List<CityDTO>();

            HasEconomyState = true;
            OnEconomyStateChanged?.Invoke(_currentEconomyState);
            Debug.Log($"[WorldPlayerStateManager] Economy state synchronized. Coins: {dto.CurrentCoinsAmount}, Research power: {_currentEconomyState.EffectiveResearchPower:F2}");
        }

        public void DeductResourcesLocally(double coins, double ideology)
        {
            _currentEconomyState.CoinsAmount -= coins;
            _currentEconomyState.IdeologyFocusPointsAmount -= ideology;

            OnEconomyStateChanged?.Invoke(_currentEconomyState);
        }
    }
}
