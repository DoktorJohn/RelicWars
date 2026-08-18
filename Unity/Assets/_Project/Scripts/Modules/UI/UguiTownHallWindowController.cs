using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using TMPro;
using UnityEngine;

namespace Project.Modules.UI
{
    public sealed class UguiTownHallWindowController : MonoBehaviour
    {
        private const int QueueCapacity = 7;

        [Header("Authored content")]
        [SerializeField] private TMP_Text queueCountText;
        [SerializeField] private RectTransform queueContainer;
        [SerializeField] private UguiBuildingQueueCardView queueTemplate;

        private readonly List<UguiTownHallBuildingCardView> _buildingCards = new();
        private readonly List<UguiBuildingQueueCardView> _queueRows = new();
        private readonly HashSet<Guid> _resolutionRequested = new();
        private List<AvailableBuildingDTO> _availableBuildings = new();
        private List<BuildingDTO> _queue = new();
        private Guid _cityId;
        private int _requestVersion;
        private bool _upgradeInFlight;
        private bool _cancelInFlight;
        private Coroutine _timerCoroutine;

        private void Awake()
        {
            ResolveAuthoredReferences();
        }

        private void OnEnable()
        {
            int version = ++_requestVersion;
            _cityId = NetworkManager.Instance?.ActiveCityId ?? Guid.Empty;
            if (_cityId == Guid.Empty) return;

            CityStateManager state = CityStateManager.Instance;
            if (state != null)
            {
                state.OnBuildingQueueChanged += HandleQueueChanged;
                state.OnTownHallAvailableBuildingsChanged += HandleAvailableBuildingsChanged;
                state.OnResourceStateChanged += HandleResourceStateChanged;
                if (state.HasBuildingQueueData) HandleQueueChanged(state.CurrentBuildingQueue);
                if (state.HasTownHallAvailableBuildingsData) HandleAvailableBuildingsChanged(state.CurrentTownHallAvailableBuildings);
            }

            LoadQueue(version);
            LoadAvailableBuildings(version);
            if (_timerCoroutine == null) _timerCoroutine = StartCoroutine(UpdateQueueTimers());
        }

        private void OnDisable()
        {
            _requestVersion++;
            _upgradeInFlight = false;
            _cancelInFlight = false;
            if (CityStateManager.Instance != null)
            {
                CityStateManager.Instance.OnBuildingQueueChanged -= HandleQueueChanged;
                CityStateManager.Instance.OnTownHallAvailableBuildingsChanged -= HandleAvailableBuildingsChanged;
                CityStateManager.Instance.OnResourceStateChanged -= HandleResourceStateChanged;
            }
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
            foreach (var card in _buildingCards) card?.Dispose();
            ClearGeneratedQueueRows();
        }

        private void ResolveAuthoredReferences()
        {
            _buildingCards.Clear();
            foreach (var button in GetComponentsInChildren<Sunvale.AncientRomeUI.Buttons.LargeBuildingButton>(true))
            {
                var view = button.GetComponent<UguiTownHallBuildingCardView>();
                if (view == null)
                {
                    Debug.LogError($"[UguiTownHallWindowController] Authored card '{button.name}' is missing UguiTownHallBuildingCardView.");
                    continue;
                }
                view.Initialize(HandleBuildingClicked);
                _buildingCards.Add(view);
            }

            if (queueCountText == null)
            {
                queueCountText = GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(text => text.name == "Queue Count");
            }

            if (queueTemplate == null)
            {
                Transform authoredTemplate = GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(candidate => candidate.name == "BuildingQueueCard");
                if (authoredTemplate != null)
                    queueTemplate = authoredTemplate.GetComponent<UguiBuildingQueueCardView>();
            }

            if (queueTemplate == null)
                Debug.LogError("[UguiTownHallWindowController] Authored BuildingQueueCard template is missing UguiBuildingQueueCardView.");

            queueContainer ??= queueTemplate != null ? queueTemplate.transform.parent as RectTransform : null;
        }

        private void LoadQueue(int version)
        {
            NetworkManager network = NetworkManager.Instance;
            if (network == null) return;
            StartCoroutine(network.Building.GetBuildingQueue(_cityId, network.JwtToken, queue =>
            {
                if (!IsCurrent(version)) return;
                if (queue != null) CityStateManager.Instance?.UpdateBuildingQueue(queue);
            }));
        }

        private void LoadAvailableBuildings(int version)
        {
            NetworkManager network = NetworkManager.Instance;
            if (network == null) return;
            StartCoroutine(network.City.GetTownHallAvailableBuildings(_cityId, network.JwtToken, buildings =>
            {
                if (!IsCurrent(version) || buildings == null) return;
                CityStateManager.Instance?.UpdateTownHallAvailableBuildings(buildings);
            }));
        }

        private void HandleAvailableBuildingsChanged(List<AvailableBuildingDTO> buildings)
        {
            _availableBuildings = buildings ?? new List<AvailableBuildingDTO>();
            foreach (UguiTownHallBuildingCardView card in _buildingCards)
            {
                AvailableBuildingDTO data = _availableBuildings.FirstOrDefault(item => item.BuildingType == card.BuildingType);
                if (data != null) card.Bind(data);
            }
        }

        private void HandleResourceStateChanged(Assets.Scripts.Domain.State.CityResourceState resources)
        {
            foreach (UguiTownHallBuildingCardView card in _buildingCards)
                card?.RefreshAffordability(resources);
        }

        private void HandleQueueChanged(List<BuildingDTO> queue)
        {
            _queue = (queue ?? new List<BuildingDTO>())
                .OrderBy(job => job.UpgradeFinished ?? DateTime.MaxValue)
                .ThenBy(job => job.Id)
                .ToList();
            _resolutionRequested.RemoveWhere(id => _queue.All(job => job.Id != id));
            RenderQueue();
        }

        private void RenderQueue()
        {
            if (queueCountText != null) queueCountText.text = $"{_queue.Count}/{QueueCapacity}";
            ClearGeneratedQueueRows();
            if (queueTemplate == null || queueContainer == null) return;

            queueTemplate.gameObject.SetActive(false);
            for (int index = 0; index < _queue.Count; index++)
            {
                UguiBuildingQueueCardView row = Instantiate(queueTemplate, queueContainer, false);
                row.name = "BuildingQueueCard";
                row.gameObject.SetActive(true);
                row.Bind(
                    _queue[index],
                    index == 0,
                    index == _queue.Count - 1 && !_cancelInFlight,
                    HandleCancelClicked);
                _queueRows.Add(row);
            }
        }

        private void HandleBuildingClicked(BuildingTypeEnum type)
        {
            if (_upgradeInFlight || _cancelInFlight || _queue.Count >= QueueCapacity) return;
            AvailableBuildingDTO building = _availableBuildings.FirstOrDefault(item => item.BuildingType == type);
            if (building == null || !building.CanAfford || (building.CurrentLevel ?? 0) >= building.MaximumLevel) return;

            NetworkManager network = NetworkManager.Instance;
            if (network == null) return;
            _upgradeInFlight = true;
            int version = _requestVersion;
            StartCoroutine(network.Building.UpgradeBuilding(_cityId, type, network.JwtToken, (success, error) =>
            {
                if (!IsCurrent(version)) return;
                _upgradeInFlight = false;
                if (!success)
                {
                    Debug.LogError($"[UguiTownHallWindowController] Upgrade failed: {error}");
                    return;
                }
                CityStateManager.Instance?.RequestImmediateRefresh(_cityId);
                LoadQueue(version);
                LoadAvailableBuildings(version);
            }));
        }

        private void HandleCancelClicked(Guid jobId)
        {
            if (_cancelInFlight || _upgradeInFlight || _queue.Count == 0 || _queue[^1].Id != jobId) return;
            NetworkManager network = NetworkManager.Instance;
            if (network == null) return;
            _cancelInFlight = true;
            foreach (var row in _queueRows) row.SetCancelVisible(false);
            int version = _requestVersion;
            StartCoroutine(network.Building.CancelBuildingUpgrade(_cityId, jobId, network.JwtToken, (success, queue, error) =>
            {
                if (!IsCurrent(version)) return;
                _cancelInFlight = false;
                if (!success)
                {
                    Debug.LogError($"[UguiTownHallWindowController] Cancel failed: {error}");
                    RenderQueue();
                    return;
                }
                CityStateManager.Instance?.UpdateBuildingQueue(queue);
                CityStateManager.Instance?.RequestImmediateRefresh(_cityId);
                LoadAvailableBuildings(version);
            }));
        }

        private IEnumerator UpdateQueueTimers()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                DateTime now = DateTime.UtcNow;
                for (int index = 0; index < _queueRows.Count; index++)
                {
                    UguiBuildingQueueCardView row = _queueRows[index];
                    if (row == null || !row.RefreshTime(now, index == 0) || row.Job == null || !_resolutionRequested.Add(row.Job.Id)) continue;
                    CityStateManager.Instance?.RequestBuildingQueueResolution(_cityId, row.Job.Id);
                }
                yield return wait;
            }
        }

        private void ClearGeneratedQueueRows()
        {
            foreach (UguiBuildingQueueCardView row in _queueRows)
            {
                if (row == null) continue;
                row.Dispose();
                Destroy(row.gameObject);
            }
            _queueRows.Clear();
        }

        private bool IsCurrent(int version) => isActiveAndEnabled && version == _requestVersion && (NetworkManager.Instance?.ActiveCityId ?? Guid.Empty) == _cityId;
    }
}
