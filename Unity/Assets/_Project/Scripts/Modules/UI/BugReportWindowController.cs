using Project.Network.Manager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI
{
    public class BugReportWindowController : BaseWindow
    {
        private const int MaximumDescriptionLength = 4000;

        protected override string WindowName => "BugReport";
        protected override string VisualContainerName => "BugReport-Window-MainContainer";
        protected override string HeaderName => "BugReport-Window-Header";

        private TextField _descriptionField;
        private Button _sendButton;
        private Label _statusLabel;
        private bool _isInitialized;
        private bool _requestInFlight;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            InitializeReferences();
            SetStatus(string.Empty, false);
            CompleteDeferredOpen(version);
            _descriptionField?.Focus();
        }

        private void InitializeReferences()
        {
            if (_isInitialized || Root == null)
            {
                return;
            }

            _descriptionField = Root.Q<TextField>("BugReport-Description");
            _sendButton = Root.Q<Button>("BugReport-Send-Button");
            _statusLabel = Root.Q<Label>("BugReport-Status");

            if (_descriptionField != null)
            {
                _descriptionField.maxLength = MaximumDescriptionLength;
            }

            _sendButton?.RegisterCallback<ClickEvent>(OnSendClicked);
            _isInitialized = true;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _requestInFlight = false;
            _sendButton?.SetEnabled(true);
        }

        private void OnDestroy()
        {
            _sendButton?.UnregisterCallback<ClickEvent>(OnSendClicked);
        }

        private void OnSendClicked(ClickEvent clickEvent)
        {
            if (_requestInFlight)
            {
                return;
            }

            var description = _descriptionField?.value?.Trim() ?? string.Empty;
            if (description.Length == 0)
            {
                SetStatus("Please describe the bug.", true);
                return;
            }

            if (NetworkManager.Instance == null || string.IsNullOrWhiteSpace(NetworkManager.Instance.JwtToken))
            {
                SetStatus("You must be logged in to submit a bug report.", true);
                return;
            }

            _requestInFlight = true;
            _sendButton.SetEnabled(false);
            SetStatus("Sending...", false);

            StartCoroutine(NetworkManager.Instance.BugReports.Submit(
                description,
                NetworkManager.Instance.JwtToken,
                (success, error) =>
                {
                    if (!isActiveAndEnabled)
                    {
                        return;
                    }

                    _requestInFlight = false;
                    _sendButton.SetEnabled(true);

                    if (!success)
                    {
                        SetStatus(string.IsNullOrWhiteSpace(error) ? "The bug report could not be submitted." : error, true);
                        return;
                    }

                    _descriptionField.value = string.Empty;
                    SetStatus("Thank you. Your bug report has been submitted.", false);
                }));
        }

        private void SetStatus(string message, bool isError)
        {
            if (_statusLabel == null)
            {
                return;
            }

            _statusLabel.text = message;
            _statusLabel.EnableInClassList("bug-report-status--error", isError);
        }
    }
}
