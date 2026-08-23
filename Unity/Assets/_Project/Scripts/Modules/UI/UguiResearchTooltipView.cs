using System;
using System.Collections.Generic;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Demos.TooltipScene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiResearchTooltipView : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private TooltipWindow tooltipWindow;

        [Header("Text")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text researchTimeText;
        [SerializeField] private TMP_Text timeLeftText;

        [Header("Placement")]
        [SerializeField] private RectTransform placementBounds;
        [SerializeField] private float spacing = 14f;
        [SerializeField] private float screenPadding = 12f;
        [SerializeField] private float closeGraceSeconds = 0.12f;

        private DateTime? _expectedCompletionUtc;
        private DateTime _serverTimeAtRefreshUtc;
        private double _realtimeAtRefresh;
        private float _hideAtRealtime;
        private bool _hidePending;
        private float _nextCountdownRefresh;

        public string CurrentResearchId { get; private set; }

        public void Show(RectTransform source, ResearchNodeDTO node, ResearchTreeDTO tree)
        {
            if (tooltipWindow == null || source == null || node == null || tree == null) return;

            CurrentResearchId = node.Id;
            _hidePending = false;
            Refresh(node, tree);
            tooltipWindow.Show(null);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipWindow.RectTransform);
            Canvas.ForceUpdateCanvases();
            PositionBeside(source);
        }

        public void Refresh(ResearchNodeDTO node, ResearchTreeDTO tree)
        {
            if (node == null || tree == null || node.Id != CurrentResearchId) return;

            if (nameText != null) nameText.text = node.Name;
            if (descriptionText != null) descriptionText.text = node.Description;
            if (statusText != null) statusText.text = GetStatus(node);

            double power = tree.ResearchRate?.EffectiveResearchPower ?? 0d;
            double effectiveSeconds = power > 0d
                ? node.ResearchTimeInSeconds / power
                : node.ResearchTimeInSeconds;
            if (researchTimeText != null)
                researchTimeText.text = $"Research time: {FormatDuration(effectiveSeconds, false)}";

            bool isActiveNode = tree.ActiveJob != null &&
                                string.Equals(tree.ActiveJob.ResearchId, node.Id, StringComparison.OrdinalIgnoreCase);
            _expectedCompletionUtc = isActiveNode ? tree.ActiveJob.ExpectedCompletionTime : null;
            _serverTimeAtRefreshUtc = tree.ServerTimeUtc;
            _realtimeAtRefresh = Time.realtimeSinceStartupAsDouble;
            _nextCountdownRefresh = 0f;
            UpdateTimeLeft();
        }

        public void RequestHide(string researchId)
        {
            if (!string.Equals(CurrentResearchId, researchId, StringComparison.OrdinalIgnoreCase)) return;
            _hidePending = true;
            _hideAtRealtime = Time.unscaledTime + closeGraceSeconds;
        }

        public void Hide()
        {
            _hidePending = false;
            _expectedCompletionUtc = null;
            CurrentResearchId = null;
            tooltipWindow?.Hide();
        }

        private void Update()
        {
            if (_hidePending)
            {
                if (tooltipWindow != null && tooltipWindow.IsPointerInside)
                    _hideAtRealtime = Time.unscaledTime + closeGraceSeconds;
                else if (Time.unscaledTime >= _hideAtRealtime)
                    Hide();
            }

            if (_expectedCompletionUtc.HasValue && Time.unscaledTime >= _nextCountdownRefresh)
            {
                _nextCountdownRefresh = Time.unscaledTime + 0.25f;
                UpdateTimeLeft();
            }
        }

        private void UpdateTimeLeft()
        {
            if (timeLeftText == null) return;
            if (!_expectedCompletionUtc.HasValue)
            {
                timeLeftText.text = string.Empty;
                return;
            }

            DateTime estimatedServerNow = _serverTimeAtRefreshUtc.AddSeconds(
                Time.realtimeSinceStartupAsDouble - _realtimeAtRefresh);
            double secondsLeft = Math.Max(0d, (_expectedCompletionUtc.Value - estimatedServerNow).TotalSeconds);
            timeLeftText.text = FormatDuration(secondsLeft, true);
        }

        private void PositionBeside(RectTransform source)
        {
            RectTransform tooltipRect = tooltipWindow.RectTransform;
            if (placementBounds == null) return;

            Bounds sourceBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(placementBounds, source);
            Vector2 size = tooltipRect.rect.size;
            if (size.x <= 0.01f) size.x = LayoutUtility.GetPreferredWidth(tooltipRect);
            if (size.y <= 0.01f) size.y = LayoutUtility.GetPreferredHeight(tooltipRect);

            Rect bounds = placementBounds.rect;
            bounds.xMin += screenPadding;
            bounds.xMax -= screenPadding;
            bounds.yMin += screenPadding;
            bounds.yMax -= screenPadding;

            Vector2 pivot = tooltipRect.pivot;
            Vector2 position = new(
                sourceBounds.max.x + spacing + size.x * pivot.x,
                sourceBounds.center.y + size.y * (pivot.y - 0.5f));

            if (position.x + size.x * (1f - pivot.x) > bounds.xMax)
                position.x = sourceBounds.min.x - spacing - size.x * (1f - pivot.x);

            position.x = ClampPivot(position.x, size.x, pivot.x, bounds.xMin, bounds.xMax);
            position.y = ClampPivot(position.y, size.y, pivot.y, bounds.yMin, bounds.yMax);
            tooltipRect.position = placementBounds.TransformPoint(position);
        }

        private static float ClampPivot(float value, float size, float pivot, float min, float max)
        {
            float lower = min + size * pivot;
            float upper = max - size * (1f - pivot);
            return lower > upper ? (min + max) * 0.5f : Mathf.Clamp(value, lower, upper);
        }

        private static string GetStatus(ResearchNodeDTO node)
        {
            if (node.IsCompleted) return "Completed";
            if (node.IsResearching) return "Researching";
            if (node.CanStart) return "Available";
            if (node.IsLocked) return "Locked";
            return "Unavailable";
        }

        private static string FormatDuration(double seconds, bool includeSeconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0d, Math.Ceiling(seconds)));
            var parts = new List<string>(4);
            if (duration.Days > 0) parts.Add($"{duration.Days}d");
            if (duration.Hours > 0) parts.Add($"{duration.Hours}h");
            if (duration.Minutes > 0) parts.Add($"{duration.Minutes}m");
            if ((includeSeconds && duration.Seconds > 0) || parts.Count == 0) parts.Add($"{duration.Seconds}s");
            return string.Join(" ", parts);
        }
    }
}
