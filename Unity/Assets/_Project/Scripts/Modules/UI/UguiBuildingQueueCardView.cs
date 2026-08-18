using System;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using Sunvale.AncientRomeUI.Graphics;
using Assets.Scripts.Domain.Enums;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiBuildingQueueCardView : MonoBehaviour
    {
        [Serializable]
        private sealed class BuildingIconEntry
        {
            public BuildingTypeEnum buildingType;
            public Sprite icon;
        }

        [Header("Authored content")]
        [SerializeField] private TMP_Text buildingNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text timeLeftText;
        [SerializeField] private SimpleFillBar progressFillBar;
        [SerializeField] private CarvedPressButton cancelButton;

        [Header("Building icons")]
        [SerializeField] private Image buildingIconImage;
        [SerializeField] private BuildingIconEntry[] buildingIcons;

        private BuildingDTO _job;
        private Action<Guid> _cancelled;
        public BuildingDTO Job => _job;

        private void Awake() => ResolveReferences();

        public void Bind(BuildingDTO job, bool isActiveJob, bool canCancel, Action<Guid> cancelled)
        {
            ResolveReferences();
            _job = job;
            _cancelled = cancelled;
            if (buildingNameText != null) buildingNameText.text = Humanize(job.Type);
            if (levelText != null) levelText.text = $"Lvl {job.Level}";
            ApplyBuildingIcon(job.Type);
            SetCancelVisible(canCancel);
            RefreshTime(DateTime.UtcNow, isActiveJob);
            if (cancelButton != null)
            {
                cancelButton.OnButtonActivatedClicked -= HandleCancelClicked;
                cancelButton.OnButtonActivatedClicked += HandleCancelClicked;
            }
        }

        public bool RefreshTime(DateTime utcNow, bool isActiveJob)
        {
            if (_job?.UpgradeFinished == null) return false;

            if (!isActiveJob)
            {
                if (timeLeftText != null) timeLeftText.text = "Queued";
                progressFillBar?.SetNormalizedValue(0f);
                return false;
            }

            DateTime finish = NormalizeUtc(_job.UpgradeFinished.Value);
            TimeSpan remaining = finish - utcNow;
            bool due = remaining <= TimeSpan.Zero;
            if (timeLeftText != null) timeLeftText.text = due ? "Completing..." : FormatDuration(remaining);

            if (progressFillBar != null)
            {
                float progress = due ? 1f : 0f;
                if (_job.UpgradeStarted.HasValue)
                {
                    DateTime start = NormalizeUtc(_job.UpgradeStarted.Value);
                    double durationSeconds = (finish - start).TotalSeconds;
                    if (durationSeconds > 0d)
                        progress = Mathf.Clamp01((float)((utcNow - start).TotalSeconds / durationSeconds));
                }

                progressFillBar.SetNormalizedValue(progress);
            }

            return due;
        }

        public void SetCancelVisible(bool visible)
        {
            if (cancelButton != null) cancelButton.gameObject.SetActive(visible);
        }

        public void Dispose()
        {
            if (cancelButton != null) cancelButton.OnButtonActivatedClicked -= HandleCancelClicked;
            _cancelled = null;
        }

        private void HandleCancelClicked(CarvedPressButton _) => _cancelled?.Invoke(_job.Id);

        private void ResolveReferences()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "BuildingName label") buildingNameText ??= text;
                else if (text.name == "Level label") levelText ??= text;
                else if (text.name == "Time left label") timeLeftText ??= text;
            }
            progressFillBar ??= GetComponentInChildren<SimpleFillBar>(true);
            if (buildingIconImage == null)
            {
                Transform iconTransform = transform.Find("MiddleRow/IconColumn/Icon");
                if (iconTransform != null) buildingIconImage = iconTransform.GetComponent<Image>();
            }
            foreach (CarvedPressButton button in GetComponentsInChildren<CarvedPressButton>(true))
                if (button.name.Equals("CancelBtn", StringComparison.OrdinalIgnoreCase) ||
                    button.name.Equals("X button", StringComparison.OrdinalIgnoreCase))
                {
                    cancelButton ??= button;
                    break;
                }
        }

        private void ApplyBuildingIcon(string buildingTypeName)
        {
            if (buildingIconImage == null || buildingIcons == null ||
                !Enum.TryParse(buildingTypeName, true, out BuildingTypeEnum buildingType)) return;

            foreach (BuildingIconEntry entry in buildingIcons)
            {
                if (entry != null && entry.buildingType == buildingType && entry.icon != null)
                {
                    buildingIconImage.sprite = entry.icon;
                    return;
                }
            }
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        private static string Humanize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(value, "(?<!^)([A-Z])", " $1");
        private static string FormatDuration(TimeSpan value) => value.Days > 0
            ? $"{value.Days}d {value.Hours:00}h {value.Minutes:00}m {value.Seconds:00}s"
            : $"{value.Hours:00}h {value.Minutes:00}m {value.Seconds:00}s";
    }
}
