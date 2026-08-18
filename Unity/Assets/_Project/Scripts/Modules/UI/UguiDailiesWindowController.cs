using System;
using System.Collections;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiDailiesWindowController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private TMP_Text resetCountdownLabel;
        [SerializeField] private Transform rowsContainer;
        [SerializeField] private Transform tableHeader;
        [SerializeField] private UguiDailiesDataRowView rowPrefab;

        private Coroutine _resetTimerCoroutine;
        private DailyObjectivesDTO _dailyObjectives;
        private DateTime _resetAtUtc;
        private int _loadVersion;
        private bool _resetReloadRequested;

        private void OnEnable()
        {
            _resetAtUtc = DateTime.UtcNow.Date.AddDays(1);
            UpdateResetCountdown();
            _resetTimerCoroutine = StartCoroutine(UpdateResetCountdownEverySecond());
            ClearRows();
            Load();
        }

        private void OnDisable()
        {
            _loadVersion++;
            if (_resetTimerCoroutine != null)
            {
                StopCoroutine(_resetTimerCoroutine);
                _resetTimerCoroutine = null;
            }
        }

        private void Load()
        {
            int loadVersion = ++_loadVersion;
            _resetReloadRequested = true;

            if (NetworkManager.Instance == null ||
                NetworkManager.Instance.DailyObjectives == null ||
                !Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid worldPlayerId))
            {
                ShowError(loadVersion, "No active world player is available.");
                return;
            }

            string loadError = null;
            StartCoroutine(NetworkManager.Instance.DailyObjectives.Get(
                worldPlayerId,
                NetworkManager.Instance.JwtToken,
                response =>
                {
                    if (!CanApply(loadVersion))
                    {
                        return;
                    }

                    if (response == null)
                    {
                        ShowError(loadVersion, string.IsNullOrWhiteSpace(loadError)
                            ? "Daily objectives could not be loaded."
                            : loadError);
                        return;
                    }

                    _dailyObjectives = response;
                    _resetAtUtc = AsUtc(response.ResetAtUtc);
                    _resetReloadRequested = false;
                    RenderRows();
                    UpdateResetCountdown();
                },
                error =>
                {
                    if (CanApply(loadVersion))
                    {
                        loadError = error;
                    }
                }));
        }

        private void RenderRows()
        {
            ClearRows();
            if (rowsContainer == null || rowPrefab == null || _dailyObjectives?.Rows == null)
            {
                return;
            }

            foreach (DailyObjectiveRowDTO objective in _dailyObjectives.Rows)
            {
                UguiDailiesDataRowView row = Instantiate(rowPrefab, rowsContainer, false);
                if (row.transform is RectTransform rowRect)
                {
                    rowRect.anchorMin = new Vector2(0f, rowRect.anchorMin.y);
                    rowRect.anchorMax = new Vector2(1f, rowRect.anchorMax.y);
                    rowRect.anchoredPosition = new Vector2(0f, rowRect.anchoredPosition.y);
                    rowRect.sizeDelta = new Vector2(0f, rowRect.sizeDelta.y);
                }
                row.Bind(objective, Collect);
            }

            Canvas.ForceUpdateCanvases();
            if (rowsContainer.parent is RectTransform parentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            if (rowsContainer is RectTransform rowsRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowsRect);
        }

        private void Collect(int definitionId)
        {
            if (NetworkManager.Instance == null ||
                NetworkManager.Instance.DailyObjectives == null ||
                !Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid worldPlayerId) ||
                !NetworkManager.Instance.ActiveCityId.HasValue)
            {
                Debug.LogError("[UguiDailiesWindowController] No active world player is available.");
                return;
            }

            int loadVersion = ++_loadVersion;
            Guid activeCityId = NetworkManager.Instance.ActiveCityId.Value;
            StartCoroutine(NetworkManager.Instance.DailyObjectives.Collect(
                worldPlayerId,
                definitionId,
                activeCityId,
                NetworkManager.Instance.JwtToken,
                response =>
                {
                    if (!CanApply(loadVersion) || response == null) return;
                    _dailyObjectives = response;
                    RenderRows();
                    CityStateManager.Instance?.RequestImmediateRefresh(activeCityId);
                    WorldPlayerStateManager.Instance?.InitiateEconomyRefresh(worldPlayerId);
                },
                error =>
                {
                    Debug.LogError($"[UguiDailiesWindowController] {error}");
                    if (CanApply(loadVersion)) RenderRows();
                }));
        }

        private void ClearRows()
        {
            if (rowsContainer == null)
            {
                return;
            }

            for (int index = rowsContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = rowsContainer.GetChild(index);
                if (child != tableHeader)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private IEnumerator UpdateResetCountdownEverySecond()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f);
                UpdateResetCountdown();

                if (!_resetReloadRequested && DateTime.UtcNow >= _resetAtUtc)
                {
                    Load();
                }
            }
        }

        private void UpdateResetCountdown()
        {
            if (resetCountdownLabel == null)
            {
                return;
            }

            TimeSpan remaining = _resetAtUtc - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            int hours = (int)remaining.TotalHours;
            resetCountdownLabel.text = $"{hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private void ShowError(int loadVersion, string message)
        {
            if (!CanApply(loadVersion))
            {
                return;
            }

            Debug.LogError($"[UguiDailiesWindowController] {message}");
        }

        private bool CanApply(int loadVersion) => isActiveAndEnabled && loadVersion == _loadVersion;

        private static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
