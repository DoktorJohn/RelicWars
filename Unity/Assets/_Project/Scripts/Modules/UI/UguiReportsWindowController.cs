using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Domain.Enums;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Modules.Reports;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiReportsWindowController : MonoBehaviour
    {
        private const int PageSize = 9;

        [SerializeField] private UguiReportsDataRowView reportRowPrefab;

        private readonly List<BattleReportDTO> _reports = new();
        private readonly List<UguiReportsDataRowView> _rows = new();
        private Transform _rowsContainer;
        private TMP_Text _amountText;
        private TMP_Text _titleText;
        private TMP_Text _timestampText;
        private TMP_Text _categoryText;
        private TMP_Text _descriptionText;
        private Text _pageLabel;
        private Button _previousButton;
        private Button _nextButton;
        private CarvedPressButton _shareButton;
        private CarvedPressButton _deleteButton;
        private Guid _worldPlayerId;
        private Guid _selectedReportId;
        private int _page;
        private int _requestVersion;
        private bool _markReadInFlight;
        private bool _shareInFlight;
        private bool _deleteInFlight;
        private bool _confirmUnshare;
        private bool _confirmDelete;

        private void Awake()
        {
            Transform reportsList = FindTransform(transform, "ReportsList");
            ScrollRect reportsScroll = reportsList != null
                ? reportsList.GetComponentInChildren<ScrollRect>(true)
                : null;
            _rowsContainer = reportsScroll != null ? reportsScroll.content : null;
            if (_rowsContainer != null)
            {
                // The scene-authored row is an editor template nested below the layout container.
                // Never let that dummy instance participate in runtime rendering.
                foreach (UguiReportsDataRowView authoredRow in
                         _rowsContainer.GetComponentsInChildren<UguiReportsDataRowView>(true))
                    authoredRow.gameObject.SetActive(false);
            }
            _amountText = FindComponent<TMP_Text>("AmountText");
            _titleText = FindComponent<TMP_Text>("ReportTitleText");
            _timestampText = FindComponent<TMP_Text>("ReportTimeStampText");
            _categoryText = FindComponent<TMP_Text>("ReportCategoryText");
            _descriptionText = FindComponent<TMP_Text>("ReportDescriptionText");
            _pageLabel = FindComponent<Text>("PageLabel");
            _previousButton = FindComponent<Button>("PreviousPageButton");
            _nextButton = FindComponent<Button>("NextPageButton");
            _shareButton = FindComponent<CarvedPressButton>("ShareBtn");
            _deleteButton = FindComponent<CarvedPressButton>("DeleteBtn");
        }

        private void OnEnable()
        {
            _requestVersion++;
            RegisterActions();
            if (!TryResolvePlayer()) return;
            LoadReports(_requestVersion);
        }

        private void OnDisable()
        {
            _requestVersion++;
            UnregisterActions();
            StopAllCoroutines();
            _markReadInFlight = _shareInFlight = _deleteInFlight = false;
            ResetConfirmations();
        }

        private void RegisterActions()
        {
            if (_previousButton != null) _previousButton.onClick.AddListener(PreviousPage);
            if (_nextButton != null) _nextButton.onClick.AddListener(NextPage);
            if (_shareButton != null) _shareButton.OnButtonActivatedClicked += ShareSelected;
            if (_deleteButton != null) _deleteButton.OnButtonActivatedClicked += DeleteSelected;
        }

        private void UnregisterActions()
        {
            if (_previousButton != null) _previousButton.onClick.RemoveListener(PreviousPage);
            if (_nextButton != null) _nextButton.onClick.RemoveListener(NextPage);
            if (_shareButton != null) _shareButton.OnButtonActivatedClicked -= ShareSelected;
            if (_deleteButton != null) _deleteButton.OnButtonActivatedClicked -= DeleteSelected;
        }

        private bool TryResolvePlayer()
        {
            return NetworkManager.Instance != null &&
                   Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out _worldPlayerId) &&
                   NetworkManager.Instance.BattleReports != null;
        }

        private void LoadReports(int version)
        {
            StartCoroutine(NetworkManager.Instance.BattleReports.GetBattleReports(
                _worldPlayerId, NetworkManager.Instance.JwtToken, reports =>
                {
                    if (!CanApply(version) || reports == null) return;
                    _reports.Clear();
                    _reports.AddRange(reports.OrderByDescending(report => report.OccurredAt));
                    _page = 0;
                    BattleReportDTO initial = _reports.FirstOrDefault(report => !report.IsRead) ?? _reports.FirstOrDefault();
                    _selectedReportId = initial?.Id ?? Guid.Empty;
                    Render();
                    if (initial != null) SelectReport(initial.Id);
                    else ClearDetails();
                }));
        }

        private void Render()
        {
            RenderRows();
            UpdateUnreadCount();
            UpdatePagination();
        }

        private void RenderRows()
        {
            foreach (UguiReportsDataRowView row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();
            if (_rowsContainer == null || reportRowPrefab == null) return;

            foreach (BattleReportDTO report in _reports.Skip(_page * PageSize).Take(PageSize))
            {
                UguiReportsDataRowView row = Instantiate(reportRowPrefab, _rowsContainer, false);
                row.gameObject.SetActive(true);
                row.Bind(report, report.Id == _selectedReportId, () => SelectReport(report.Id));
                _rows.Add(row);
            }
        }

        private void SelectReport(Guid reportId)
        {
            BattleReportDTO report = _reports.FirstOrDefault(item => item.Id == reportId);
            if (report == null) return;
            _selectedReportId = reportId;
            ResetConfirmations();
            RenderDetails(report);
            RenderRows();
            if (report.IsRead || _markReadInFlight) return;

            _markReadInFlight = true;
            int version = _requestVersion;
            StartCoroutine(NetworkManager.Instance.BattleReports.MarkBattleReportAsRead(
                _worldPlayerId, reportId, NetworkManager.Instance.JwtToken, success =>
                {
                    _markReadInFlight = false;
                    if (!CanApply(version) || !success) return;
                    BattleReportDTO stored = _reports.FirstOrDefault(item => item.Id == reportId);
                    if (stored != null) stored.IsRead = true;
                    RenderRows();
                    UpdateUnreadCount();
                    BattleReportStateEvents.RaiseUnreadStateChanged();
                }));
        }

        private void RenderDetails(BattleReportDTO report)
        {
            if (_titleText != null) _titleText.text = report.Title ?? string.Empty;
            if (_timestampText != null) _timestampText.text = FormatTimestamp(report.OccurredAt);
            if (_categoryText != null) _categoryText.text = $"Category: {GetCategory(report.ReportType)}";
            if (_descriptionText != null) _descriptionText.text = report.Body ?? string.Empty;
            UpdateShareButton(report);
            if (_deleteButton != null) _deleteButton.SetTextOnLabel("DELETE");
        }

        private void ClearDetails()
        {
            if (_titleText != null) _titleText.text = string.Empty;
            if (_timestampText != null) _timestampText.text = string.Empty;
            if (_categoryText != null) _categoryText.text = string.Empty;
            if (_descriptionText != null) _descriptionText.text = string.Empty;
        }

        private void UpdateUnreadCount()
        {
            if (_amountText != null) _amountText.text = _reports.Count(report => !report.IsRead).ToString();
        }

        private void PreviousPage()
        {
            if (_page <= 0) return;
            _page--;
            RenderRows();
            UpdatePagination();
        }

        private void NextPage()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(_reports.Count / (float)PageSize));
            if (_page + 1 >= pages) return;
            _page++;
            RenderRows();
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(_reports.Count / (float)PageSize));
            _page = Mathf.Clamp(_page, 0, pages - 1);
            if (_pageLabel != null) _pageLabel.text = $"REPORTS {_page + 1}/{pages}";
            if (_previousButton != null) _previousButton.interactable = _page > 0;
            if (_nextButton != null) _nextButton.interactable = _page + 1 < pages;
        }

        private void ShareSelected(CarvedPressButton _)
        {
            if (_shareInFlight || _selectedReportId == Guid.Empty) return;
            BattleReportDTO report = _reports.FirstOrDefault(item => item.Id == _selectedReportId);
            if (report == null) return;
            if (report.IsPublic && !_confirmUnshare)
            {
                _confirmUnshare = true;
                _shareButton?.SetTextOnLabel("CONFIRM UNSHARE");
                return;
            }

            bool makePublic = !report.IsPublic;
            _shareInFlight = true;
            SetButtonEnabled(_shareButton, false);
            int version = _requestVersion;
            StartCoroutine(NetworkManager.Instance.BattleReports.SetBattleReportPublicStatus(
                _worldPlayerId, report.Id, makePublic, NetworkManager.Instance.JwtToken, success =>
                {
                    _shareInFlight = false;
                    if (!CanApply(version)) return;
                    if (success) report.IsPublic = makePublic;
                    SetButtonEnabled(_shareButton, true);
                    _confirmUnshare = false;
                    UpdateShareButton(report);
                }));
        }

        private void DeleteSelected(CarvedPressButton _)
        {
            if (_deleteInFlight || _selectedReportId == Guid.Empty) return;
            if (!_confirmDelete)
            {
                _confirmDelete = true;
                _deleteButton?.SetTextOnLabel("CONFIRM");
                return;
            }

            Guid reportId = _selectedReportId;
            _deleteInFlight = true;
            SetButtonEnabled(_deleteButton, false);
            int version = _requestVersion;
            StartCoroutine(NetworkManager.Instance.BattleReports.DeleteBattleReport(
                _worldPlayerId, reportId, NetworkManager.Instance.JwtToken, success =>
                {
                    _deleteInFlight = false;
                    if (!CanApply(version)) return;
                    SetButtonEnabled(_deleteButton, true);
                    if (!success) { ResetConfirmations(); return; }
                    _reports.RemoveAll(report => report.Id == reportId);
                    BattleReportDTO next = _reports.FirstOrDefault(report => !report.IsRead) ?? _reports.FirstOrDefault();
                    _selectedReportId = next?.Id ?? Guid.Empty;
                    Render();
                    if (next != null) SelectReport(next.Id); else ClearDetails();
                    BattleReportStateEvents.RaiseUnreadStateChanged();
                }));
        }

        private void ResetConfirmations()
        {
            _confirmDelete = false;
            _confirmUnshare = false;
            BattleReportDTO report = _reports.FirstOrDefault(item => item.Id == _selectedReportId);
            UpdateShareButton(report);
            if (_deleteButton != null) _deleteButton.SetTextOnLabel("DELETE");
        }

        private void UpdateShareButton(BattleReportDTO report)
        {
            if (_shareButton != null) _shareButton.SetTextOnLabel(report?.IsPublic == true ? "UNSHARE" : "SHARE");
        }

        private static void SetButtonEnabled(CarvedPressButton button, bool enabled)
        {
            if (button == null) return;
            button.enabled = enabled;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group != null) { group.interactable = enabled; group.blocksRaycasts = enabled; }
        }

        private bool CanApply(int version) => isActiveAndEnabled && version == _requestVersion;

        private T FindComponent<T>(string objectName) where T : Component
        {
            foreach (T component in GetComponentsInChildren<T>(true))
                if (component.name == objectName) return component;
            return null;
        }

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
            DateTime utc = timestamp.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc) : timestamp.ToUniversalTime();
            return utc.ToString("dddd, MMMM d yyyy HH:mm 'UTC'");
        }

        private static string GetCategory(ReportTypeEnum type) => type switch
        {
            ReportTypeEnum.BuildingCompleted => "Building",
            ReportTypeEnum.RecruitmentCompleted => "Recruitment",
            ReportTypeEnum.Attack => "Attack",
            ReportTypeEnum.CityAttacked => "Defense",
            ReportTypeEnum.SupportingUnitsAttacked => "Support Defense",
            ReportTypeEnum.SupportingUnitsRecalled => "Support Recall",
            ReportTypeEnum.SupportingUnitsReturned => "Support Return",
            ReportTypeEnum.FocusEnacted => "Focus",
            _ => "Battle"
        };
    }
}
