using System;
using Project.Network.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiReportsDataRowView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text reportText;
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private GameObject unread;
        [SerializeField] private GameObject read;
        [SerializeField] private Image selectionGraphic;
        [SerializeField] private Color selectedColor = new(0.72f, 0.88f, 1f, 0.55f);

        private Action _onClick;
        private Color _normalColor;

        private void Awake()
        {
            reportText ??= FindComponent<TMP_Text>("ReportText");
            timestampText ??= FindComponent<TMP_Text>("TimestampText");
            unread ??= FindObject("Unread");
            read ??= FindObject("Read");
            selectionGraphic ??= FindComponent<Image>("Background Highlit");
            if (selectionGraphic != null) _normalColor = selectionGraphic.color;
        }

        public void Bind(BattleReportDTO report, bool selected, Action onClick)
        {
            _onClick = onClick;
            if (reportText != null) reportText.text = report?.Title ?? string.Empty;
            if (timestampText != null) timestampText.text = FormatTimestamp(report?.OccurredAt ?? default);
            if (unread != null) unread.SetActive(report != null && !report.IsRead);
            if (read != null) read.SetActive(report != null && report.IsRead);
            if (selectionGraphic != null) selectionGraphic.color = selected ? selectedColor : _normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _onClick?.Invoke();
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            foreach (T component in GetComponentsInChildren<T>(true))
                if (component.name == objectName) return component;
            return null;
        }

        private GameObject FindObject(string objectName) => FindTransform(transform, objectName)?.gameObject;

        private static Transform FindTransform(Transform root, string objectName)
        {
            foreach (Transform child in root)
            {
                if (child.name == objectName) return child;
                Transform nested = FindTransform(child, objectName);
                if (nested != null) return nested;
            }
            return null;
        }

        private static string FormatTimestamp(DateTime timestamp)
        {
            DateTime utc = timestamp.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                : timestamp.ToUniversalTime();
            return utc.ToString("dd/MM HH:mm 'UTC'");
        }
    }
}
