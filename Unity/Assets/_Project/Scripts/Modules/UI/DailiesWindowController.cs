using System;
using System.Collections;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public class DailiesWindowController : BaseWindow
    {
        protected override string WindowName => "DailiesWindow";
        protected override string VisualContainerName => "Dailies-Window-MainContainer";
        protected override string HeaderName => "Dailies-Window-Header";

        [Header("Dailies Row Configuration")]
        [SerializeField] private VisualTreeAsset _dailyRowTemplate;

        private Label _resetCountdownLabel;
        private VisualElement _rowsContainer;
        private VisualElement _loadState;
        private VisualElement _dataContainer;
        private Coroutine _resetTimerCoroutine;
        private DailyObjectivesDTO _dailyObjectives;
        private int _openSequence;
        private int _loadVersion;

        public override void OnOpen(object dataPayload)
        {
            _openSequence = BeginDeferredOpen();
            _resetCountdownLabel = Root.Q<Label>("Dailies-Reset-Countdown");
            _rowsContainer = Root.Q<VisualElement>("Dailies-Rows");
            _loadState = Root.Q<VisualElement>("Dailies-Load-State");
            _dataContainer = Root.Q<VisualElement>("Dailies-Data");
            Load(_openSequence);
        }

        private void OnDisable()
        {
            _loadVersion++;
            StopResetTimer();
            InvalidateDeferredOpen();
        }

        private void Load(int openSequence)
        {
            int loadVersion = ++_loadVersion;
            StopResetTimer();
            _dataContainer?.AddToClassList("hidden");
            _loadState?.RemoveFromClassList("hidden");
            WindowAsyncStateHelper.ShowLoading(_loadState, "Loading daily objectives...");

            if (NetworkManager.Instance == null || !Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid worldPlayerId))
            {
                ShowError(openSequence, loadVersion, "No active world player is available.");
                return;
            }

            string loadError = null;
            StartCoroutine(NetworkManager.Instance.DailyObjectives.Get(
                worldPlayerId,
                NetworkManager.Instance.JwtToken,
                response =>
                {
                    if (!CanApply(openSequence, loadVersion)) return;
                    if (response == null)
                    {
                        ShowError(openSequence, loadVersion, string.IsNullOrWhiteSpace(loadError)
                            ? "Daily objectives could not be loaded."
                            : loadError);
                        return;
                    }

                    _dailyObjectives = response;
                    RenderRows();
                    RestartResetTimer();
                    CompleteDeferredOpen(openSequence);
                },
                error =>
                {
                    if (CanApply(openSequence, loadVersion)) loadError = error;
                }));
        }

        private void ShowError(int openSequence, int loadVersion, string message)
        {
            if (!CanApply(openSequence, loadVersion)) return;
            WindowAsyncStateHelper.ShowError(_loadState, message, () => Load(_openSequence));
            CompleteDeferredOpen(openSequence);
        }

        private bool CanApply(int openSequence, int loadVersion) =>
            isActiveAndEnabled && IsDeferredOpenCurrent(openSequence) && loadVersion == _loadVersion;

        private void RenderRows()
        {
            _rowsContainer?.Clear();
            if (_rowsContainer == null || _dailyRowTemplate == null || _dailyObjectives?.Rows == null) return;

            foreach (DailyObjectiveRowDTO objective in _dailyObjectives.Rows)
            {
                TemplateContainer instance = _dailyRowTemplate.Instantiate();
                VisualElement row = instance.Q<VisualElement>("Dailies-Row");
                if (row == null) continue;

                SetLabel(row, "Dailies-Row-Level", objective.Slot.ToString());
                SetLabel(row, "Dailies-Row-Objective", objective.Name);
                SetLabel(row, "Dailies-Row-Reward", objective.RewardTier.ToString().ToUpperInvariant());
                if (objective.State == DailyObjectiveState.ComingSoon)
                {
                    SetLabel(row, "Dailies-Row-Completion", objective.CompletionInfo);
                    SetLabel(row, "Dailies-Row-Status", "COMING SOON");
                    row.AddToClassList("dailies-row-coming-soon");
                }
                else
                {
                    SetLabel(row, "Dailies-Row-Completion",
                        $"{objective.CompletionInfo} ({Math.Floor(objective.Progress):N0} / {Math.Floor(objective.Target):N0})");
                    SetLabel(row, "Dailies-Row-Status",
                        objective.State == DailyObjectiveState.Complete ? "COMPLETE" : "IN PROGRESS");
                }
                _rowsContainer.Add(row);
            }

            WindowAsyncStateHelper.Clear(_loadState);
            _loadState?.AddToClassList("hidden");
            _dataContainer?.RemoveFromClassList("hidden");
        }

        private static void SetLabel(VisualElement row, string elementName, string value)
        {
            Label label = row.Q<Label>(elementName);
            if (label != null) label.text = value;
        }

        private void RestartResetTimer()
        {
            StopResetTimer();
            UpdateResetCountdown();
            _resetTimerCoroutine = StartCoroutine(UpdateResetCountdownEverySecond());
        }

        private IEnumerator UpdateResetCountdownEverySecond()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f);
                if (_dailyObjectives != null && DateTime.UtcNow >= AsUtc(_dailyObjectives.ResetAtUtc))
                {
                    Load(_openSequence);
                    yield break;
                }
                UpdateResetCountdown();
            }
        }

        private void UpdateResetCountdown()
        {
            if (_resetCountdownLabel == null || _dailyObjectives == null) return;
            TimeSpan remaining = AsUtc(_dailyObjectives.ResetAtUtc) - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            int hours = (int)remaining.TotalHours;
            _resetCountdownLabel.text = $"{hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private void StopResetTimer()
        {
            if (_resetTimerCoroutine != null) StopCoroutine(_resetTimerCoroutine);
            _resetTimerCoroutine = null;
        }

        private static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
