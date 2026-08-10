using Assets.Scripts.Domain.Enums;
using Project.Modules.Reports;
using Project.Network.Manager;
using Project.Network.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public class BattleReportWindowController : BaseWindow
    {
        protected override string WindowName => "Reports";
        protected override string VisualContainerName => "Reports-Window-MainContainer";
        protected override string HeaderName => "Reports-Window-Header";

        private ScrollView _reportList;
        private ScrollView _reportDetailsPanel;
        private Label _reportCountLabel;
        private Label _unreadCountLabel;
        private Label _selectedTitleLabel;
        private Label _selectedTimestampLabel;
        private Label _selectedTypeLabel;
        private Label _selectedSummaryLabel;
        private Label _attackerLossesLabel;
        private Label _defenderLossesLabel;
        private Label _revivedUnitsLabel;
        private Label _appliedModifiersLabel;
        private Button _deleteReportButton;
        private Button _shareReportButton;

        private readonly List<BattleReportDTO> _reports = new();
        private Guid _worldPlayerId = Guid.Empty;
        private Guid _selectedReportId = Guid.Empty;
        private bool _isInitialized;
        private bool _markReadRequestInFlight;
        private bool _deleteRequestInFlight;
        private bool _deleteReportConfirmationPending;
        private bool _unshareConfirmationPending;
        private bool _shareRequestInFlight;
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            InitializeReferences();

            if (!TryResolveWorldPlayerId())
            {
                CompleteDeferredOpen(version);
                return;
            }

            LoadReports(version);
        }

        private void InitializeReferences()
        {
            if (_isInitialized || Root == null)
            {
                return;
            }

            _reportList = Root.Q<ScrollView>("ReportList");
            _reportDetailsPanel = Root.Q<ScrollView>("ReportDetailsPanel");
            _reportCountLabel = Root.Q<Label>("ReportCountLabel");
            _unreadCountLabel = Root.Q<Label>("UnreadReportCountLabel");
            _selectedTitleLabel = Root.Q<Label>("SelectedReportTitle");
            _selectedTimestampLabel = Root.Q<Label>("SelectedReportTimestamp");
            _selectedSummaryLabel = Root.Q<Label>("SelectedReportSummary");
            _attackerLossesLabel = Root.Q<Label>("SelectedAttackerLosses");
            _defenderLossesLabel = Root.Q<Label>("SelectedDefenderLosses");
            _revivedUnitsLabel = Root.Q<Label>("SelectedRevivedUnits");
            _appliedModifiersLabel = Root.Q<Label>("SelectedAppliedModifiers");

            _isInitialized = true;
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            _markReadRequestInFlight = false;
            _deleteRequestInFlight = false;
            _shareRequestInFlight = false;
            ResetDeleteReportButton();
        }

        private bool TryResolveWorldPlayerId()
        {
            if (NetworkManager.Instance == null || string.IsNullOrWhiteSpace(NetworkManager.Instance.WorldPlayerId))
            {
                Debug.LogError("[BattleReports] No active world player.");
                if (_reportList != null)
                {
                    WindowAsyncStateHelper.ShowError(_reportList, "No active world player.");
                }
                return false;
            }

            if (!Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out _worldPlayerId))
            {
                Debug.LogError("[BattleReports] Invalid world player id.");
                if (_reportList != null)
                {
                    WindowAsyncStateHelper.ShowError(_reportList, "Invalid world player id.");
                }
                return false;
            }

            return true;
        }

        private void LoadReports(int version)
        {
            if (NetworkManager.Instance == null || _reportList == null)
            {
                WindowAsyncStateHelper.ShowError(_reportList, "Reports unavailable.");
                CompleteDeferredOpen(version);
                return;
            }

            ResetDeleteReportButton();
            _deleteRequestInFlight = false;

            WindowAsyncStateHelper.ShowLoading(_reportList, "Loading reports...");
            ClearDetails();

            StartCoroutine(NetworkManager.Instance.BattleReports.GetBattleReports(
                _worldPlayerId,
                NetworkManager.Instance.JwtToken,
                reports =>
                {
                    if (!isActiveAndEnabled || version != _requestVersion)
                    {
                        return;
                    }

                    if (reports == null)
                    {
                        WindowAsyncStateHelper.ShowError(_reportList, "Could not load reports.", () => LoadReports(version));
                        ClearDetails();
                        CompleteDeferredOpen(version);
                        return;
                    }

                    _reports.Clear();
                    _reports.AddRange(reports.OrderByDescending(report => report.OccurredAt));
                    UpdateCounters();

                    if (_reports.Count == 0)
                    {
                        WindowAsyncStateHelper.ShowEmpty(_reportList, "No reports available.");
                        ClearDetails();
                        CompleteDeferredOpen(version);
                        return;
                    }

                    RenderReportList();
                    var initialSelection = _reports.FirstOrDefault(report => !report.IsRead) ?? _reports[0];
                    SelectReport(initialSelection.Id);
                    CompleteDeferredOpen(version);
                }));
        }

        private void UpdateCounters()
        {
            var unreadCount = _reports.Count(report => !report.IsRead);
            if (_reportCountLabel != null)
            {
                _reportCountLabel.text = _reports.Count.ToString();
            }

            if (_unreadCountLabel != null)
            {
                _unreadCountLabel.text = unreadCount.ToString();
            }
        }

        private void RenderReportList()
        {
            if (_reportList == null)
            {
                return;
            }

            _reportList.Clear();

            foreach (var report in _reports)
            {
                _reportList.Add(CreateReportItem(report));
            }
        }

        private VisualElement CreateReportItem(BattleReportDTO report)
        {
            var item = new VisualElement();
            item.AddToClassList("battle-report-item");
            if (_selectedReportId == report.Id)
            {
                item.AddToClassList("battle-report-item--selected");
            }

            if (!report.IsRead)
            {
                item.AddToClassList("battle-report-item--unread");
            }

            var titleLabel = new Label($"{report.Title} - {FormatReportListTimestamp(report.OccurredAt)}");
            titleLabel.AddToClassList("battle-report-item__title");
            item.Add(titleLabel);

            item.RegisterCallback<ClickEvent>(_ => SelectReport(report.Id));
            return item;
        }

        private void SelectReport(Guid reportId)
        {
            var report = _reports.FirstOrDefault(entry => entry.Id == reportId);
            if (report == null)
            {
                return;
            }

            _selectedReportId = reportId;
            _deleteReportConfirmationPending = false;
            RenderReportList();
            RenderReportDetails(report);

            if (!report.IsRead && !_markReadRequestInFlight)
            {
                _markReadRequestInFlight = true;
                StartCoroutine(NetworkManager.Instance.BattleReports.MarkBattleReportAsRead(
                    _worldPlayerId,
                    reportId,
                    NetworkManager.Instance.JwtToken,
                    success =>
                    {
                        _markReadRequestInFlight = false;

                        if (!isActiveAndEnabled || !success)
                        {
                            return;
                        }

                        var storedReport = _reports.FirstOrDefault(entry => entry.Id == reportId);
                        if (storedReport != null)
                        {
                            storedReport.IsRead = true;
                        }

                        UpdateCounters();
                        RenderReportList();
                        BattleReportStateEvents.RaiseUnreadStateChanged();
                    }));
            }
        }

        private void RenderReportDetails(BattleReportDTO report)
        {
            if (_reportDetailsPanel == null)
            {
                return;
            }

            _reportDetailsPanel.Clear();
            ResetDeleteReportButton();

            _selectedTitleLabel = AddDetailLabel("SelectedReportTitle", report.Title, "report-detail-title");
            _selectedTimestampLabel = AddDetailLabel("SelectedReportTimestamp", FormatServerTimestamp(report.OccurredAt, includeDayName: true), "report-detail-timestamp");
            _selectedTypeLabel = AddDetailLabel("SelectedReportType", GetReportTypeDisplayName(report.ReportType), "report-detail-type");
            _selectedSummaryLabel = AddDetailLabel("SelectedReportSummary", report.Body, "report-detail-summary");

            if (report.ReportType == ReportTypeEnum.Battle
                || report.ReportType == ReportTypeEnum.Attack
                || report.ReportType == ReportTypeEnum.CityAttacked
                || report.ReportType == ReportTypeEnum.SupportingUnitsAttacked)
            {
                _attackerLossesLabel = AddDetailSection("SelectedAttackerLosses", "Attacker losses", FormatStacks(report.AttackerLosses));
                _defenderLossesLabel = AddDetailSection("SelectedDefenderLosses", "Defender losses", FormatStacks(report.DefenderLosses));
                _revivedUnitsLabel = AddDetailSection("SelectedRevivedUnits", "Revived units", FormatStacks(report.RevivedUnits));
                _appliedModifiersLabel = AddDetailSection("SelectedAppliedModifiers", "Applied modifiers", FormatStrings(report.AppliedModifiers));
            }

            AddDetailActions();
        }

        private void AddDetailActions()
        {
            var actions = new VisualElement();
            actions.AddToClassList("report-detail-actions");

            var selectedReport = _reports.FirstOrDefault(report => report.Id == _selectedReportId);
            _shareReportButton = new Button(OnShareReportClicked)
            {
                text = selectedReport?.IsPublic == true ? "UNSHARE" : "SHARE"
            };
            _shareReportButton.AddToClassList("btn-global-base");
            _shareReportButton.AddToClassList("btn-imperial-primary");
            _shareReportButton.AddToClassList("report-detail-share-button");

            _deleteReportButton = new Button(OnDeleteReportClicked)
            {
                text = "DELETE"
            };
            _deleteReportButton.AddToClassList("btn-global-base");
            _deleteReportButton.AddToClassList("btn-imperial-danger");
            _deleteReportButton.AddToClassList("report-detail-delete-button");

            actions.Add(_shareReportButton);
            actions.Add(_deleteReportButton);
            _reportDetailsPanel.Add(actions);
        }

        private void OnShareReportClicked()
        {
            if (_shareRequestInFlight || _selectedReportId == Guid.Empty || NetworkManager.Instance == null)
            {
                return;
            }

            var report = _reports.FirstOrDefault(entry => entry.Id == _selectedReportId);
            if (report == null)
            {
                return;
            }

            if (report.IsPublic && !_unshareConfirmationPending)
            {
                _unshareConfirmationPending = true;
                _shareReportButton.text = "CONFIRM UNSHARE";
                return;
            }

            _shareRequestInFlight = true;
            _shareReportButton.SetEnabled(false);
            _shareReportButton.text = report.IsPublic ? "UNSHARING" : "SHARING";
            var makePublic = !report.IsPublic;
            var reportId = report.Id;

            StartCoroutine(NetworkManager.Instance.BattleReports.SetBattleReportPublicStatus(
                _worldPlayerId, reportId, makePublic, NetworkManager.Instance.JwtToken, success =>
                {
                    _shareRequestInFlight = false;
                    if (!isActiveAndEnabled)
                    {
                        return;
                    }

                    if (success)
                    {
                        var storedReport = _reports.FirstOrDefault(entry => entry.Id == reportId);
                        if (storedReport != null)
                        {
                            storedReport.IsPublic = makePublic;
                            RenderReportDetails(storedReport);
                        }
                    }
                    else
                    {
                        ResetShareReportButton(report);
                    }
                }));
        }

        private void ResetShareReportButton(BattleReportDTO report = null)
        {
            _unshareConfirmationPending = false;
            if (_shareReportButton != null)
            {
                _shareReportButton.SetEnabled(true);
                _shareReportButton.text = report?.IsPublic == true ? "UNSHARE" : "SHARE";
            }
        }

        private void OnDeleteReportClicked()
        {
            if (_deleteRequestInFlight || _selectedReportId == Guid.Empty || NetworkManager.Instance == null)
            {
                return;
            }

            if (!_deleteReportConfirmationPending)
            {
                _deleteReportConfirmationPending = true;
                if (_deleteReportButton != null)
                {
                    _deleteReportButton.text = "CONFIRM";
                }

                return;
            }

            _deleteRequestInFlight = true;
            if (_deleteReportButton != null)
            {
                _deleteReportButton.SetEnabled(false);
                _deleteReportButton.text = "DELETING";
            }

            var reportId = _selectedReportId;
            StartCoroutine(NetworkManager.Instance.BattleReports.DeleteBattleReport(
                _worldPlayerId,
                reportId,
                NetworkManager.Instance.JwtToken,
                success =>
                {
                    _deleteRequestInFlight = false;

                    if (!isActiveAndEnabled)
                    {
                        return;
                    }

                    if (!success)
                    {
                        ResetDeleteReportButton();
                        return;
                    }

                    var removedReport = _reports.FirstOrDefault(report => report.Id == reportId);
                    if (removedReport != null)
                    {
                        _reports.Remove(removedReport);
                    }

                    _selectedReportId = Guid.Empty;
                    ResetDeleteReportButton();
                    UpdateCounters();
                    BattleReportStateEvents.RaiseUnreadStateChanged();

                    if (_reports.Count == 0)
                    {
                        if (_reportList != null)
                        {
                            WindowAsyncStateHelper.ShowEmpty(_reportList, "No reports available.");
                        }

                        ClearDetails();
                        return;
                    }

                    RenderReportList();
                    var nextSelection = _reports.FirstOrDefault(report => !report.IsRead) ?? _reports[0];
                    SelectReport(nextSelection.Id);
                }));
        }

        private void ResetDeleteReportButton()
        {
            _deleteReportConfirmationPending = false;
            _unshareConfirmationPending = false;

            if (_deleteReportButton != null)
            {
                _deleteReportButton.SetEnabled(true);
                _deleteReportButton.text = "DELETE";
            }
        }

        private Label AddDetailLabel(string name, string value, string className)
        {
            var label = new Label(value ?? string.Empty);
            label.name = name;
            label.AddToClassList(className);
            _reportDetailsPanel.Add(label);
            return label;
        }

        private Label AddDetailSection(string name, string title, string value)
        {
            var section = new VisualElement();
            section.AddToClassList("report-detail-section-block");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("report-detail-section-title");
            section.Add(titleLabel);

            var valueLabel = new Label(value ?? string.Empty);
            valueLabel.name = name;
            valueLabel.AddToClassList("report-detail-section-value");
            section.Add(valueLabel);

            _reportDetailsPanel.Add(section);
            return valueLabel;
        }

        private void ClearDetails()
        {
            if (_reportDetailsPanel == null)
            {
                return;
            }

            _reportDetailsPanel.Clear();
            ResetDeleteReportButton();
            WindowAsyncStateHelper.ShowEmpty(_reportDetailsPanel, "Select a report to inspect it.");
        }

        private static string GetReportTypeDisplayName(ReportTypeEnum reportType)
        {
            return reportType switch
            {
                ReportTypeEnum.BuildingCompleted => "BUILDING",
                ReportTypeEnum.RecruitmentCompleted => "RECRUITMENT",
                ReportTypeEnum.Attack => "ATTACK",
                ReportTypeEnum.CityAttacked => "DEFENSE",
                ReportTypeEnum.SupportingUnitsAttacked => "SUPPORT DEFENSE",
                ReportTypeEnum.SupportingUnitsRecalled => "SUPPORT RECALL",
                ReportTypeEnum.SupportingUnitsReturned => "SUPPORT RETURN",
                ReportTypeEnum.FocusEnacted => "FOCUS",
                _ => "BATTLE"
            };
        }

        private static string FormatStacks(List<UnitStackDTO> stacks)
        {
            if (stacks == null || stacks.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", stacks.Select(stack => $"{stack.Type} x{stack.Quantity}"));
        }

        private static string FormatStrings(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "None";
            }

            var joined = string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
            return string.IsNullOrWhiteSpace(joined) ? "None" : joined;
        }

        private static string FormatServerTimestamp(DateTime timestamp, bool includeDayName = false)
        {
            var utcTimestamp = timestamp.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                : timestamp.ToUniversalTime();

            var format = includeDayName ? "dddd, MMMM d yyyy HH:mm 'UTC'" : "MMM d, yyyy HH:mm 'UTC'";
            return utcTimestamp.ToString(format);
        }

        private static string FormatReportListTimestamp(DateTime timestamp)
        {
            var utcTimestamp = timestamp.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                : timestamp.ToUniversalTime();

            return utcTimestamp.ToString("MMM d, HH:mm 'UTC'");
        }
    }
}
