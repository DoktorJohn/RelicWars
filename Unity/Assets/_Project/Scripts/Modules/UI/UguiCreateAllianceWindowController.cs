using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using TMPro;
using Sunvale.AncientRomeUI.Buttons;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiCreateAllianceWindowController : MonoBehaviour
    {
        [SerializeField] private GameObject allianceWindowPrefab;
        [SerializeField] private TMP_InputField allianceNameInput;
        [SerializeField] private TMP_InputField allianceTagInput;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private CarvedPressButton createButton;
        [SerializeField] private RectTransform invitationRows;
        [SerializeField] private GameObject invitationRowTemplate;
        [SerializeField] private GameObject loadingState;
        [SerializeField] private GameObject errorState;

        private Guid _worldPlayerId;
        private bool _requestInFlight;
        private int _loadVersion;
        private readonly List<GameObject> _runtimeRows = new();

        private void Awake()
        {
            allianceNameInput ??= FindNamed<TMP_InputField>("Input-AllianceName");
            allianceTagInput ??= FindNamed<TMP_InputField>("Input-AllianceTag");
            statusText ??= FindNamed<TMP_Text>("Lbl-CreateStatus");
            createButton ??= FindNamed<CarvedPressButton>("Btn-CreateAlliance");
            loadingState ??= FindObject("Invitation-LoadingState");
            errorState ??= FindObject("Invitation-ErrorState");
            invitationRowTemplate ??= FindObject("Invitation-EmptyState");

            if (invitationRows == null)
            {
                GameObject list = FindObject("Invitation-List");
                ScrollRect scroll = list != null ? list.GetComponentInChildren<ScrollRect>(true) : null;
                invitationRows = scroll != null && scroll.content != null
                    ? scroll.content
                    : list != null ? list.transform as RectTransform : null;
            }
        }

        private void OnEnable()
        {
            if (allianceTagInput != null) allianceTagInput.onSubmit.AddListener(OnCreateSubmitted);
            if (createButton != null) createButton.OnButtonActivatedClicked += OnCreateClicked;
            if (!Guid.TryParse(NetworkManager.Instance?.WorldPlayerId, out _worldPlayerId))
            {
                SetStatus("Could not resolve the active player.", true);
                return;
            }
            LoadInvitations();
        }

        private void OnDisable()
        {
            if (allianceTagInput != null) allianceTagInput.onSubmit.RemoveListener(OnCreateSubmitted);
            if (createButton != null) createButton.OnButtonActivatedClicked -= OnCreateClicked;
            _loadVersion++;
            _requestInFlight = false;
        }

        private void OnCreateSubmitted(string _)
        {
            CreateAlliance();
        }

        private void OnCreateClicked(CarvedPressButton _)
        {
            CreateAlliance();
        }

        private void CreateAlliance()
        {
            if (_requestInFlight) return;
            string allianceName = allianceNameInput != null ? allianceNameInput.text.Trim() : string.Empty;
            string allianceTag = allianceTagInput != null ? allianceTagInput.text.Trim() : string.Empty;
            if (allianceName.Length < 3 || allianceTag.Length < 3)
            {
                SetStatus("Name and tag must contain at least 3 characters.", true);
                return;
            }

            SetBusy(true, "Creating alliance...");
            var dto = new CreateAllianceDTO { WorldPlayerIdFounder = _worldPlayerId, Name = allianceName, Tag = allianceTag };
            StartCoroutine(NetworkManager.Instance.Alliance.CreateAlliance(dto, NetworkManager.Instance.JwtToken, alliance =>
            {
                if (!this || !isActiveAndEnabled) return;
                SetBusy(false, alliance != null ? string.Empty : "Could not create alliance.");
                if (alliance != null) OpenAllianceWindow();
            }));
        }

        private void LoadInvitations()
        {
            int version = ++_loadVersion;
            SetState(loading: true, error: false, empty: false);
            StartCoroutine(NetworkManager.Instance.Alliance.GetInvitations(_worldPlayerId, NetworkManager.Instance.JwtToken, invitations =>
            {
                if (!this || !isActiveAndEnabled || version != _loadVersion) return;
                if (invitations == null)
                {
                    SetState(false, true, false);
                    return;
                }
                RenderInvitations(invitations);
            }));
        }

        private void RenderInvitations(List<AllianceInvitationDTO> invitations)
        {
            foreach (GameObject row in _runtimeRows) if (row != null) Destroy(row);
            _runtimeRows.Clear();
            SetState(false, false, invitations.Count == 0);
            if (invitations.Count == 0 || invitationRows == null || invitationRowTemplate == null) return;

            invitationRowTemplate.SetActive(false);
            foreach (AllianceInvitationDTO invitation in invitations)
            {
                GameObject row = Instantiate(invitationRowTemplate, invitationRows, false);
                row.name = $"AllianceInvitation_{invitation.Id}";
                row.SetActive(true);
                TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = $"[{invitation.AllianceTag}] {invitation.AllianceName} - invited by {invitation.InvitedByUserName} - ACCEPT";
                Button button = row.GetComponent<Button>() ?? row.AddComponent<Button>();
                Guid invitationId = invitation.Id;
                button.onClick.AddListener(() => AcceptInvitation(invitationId, button));
                _runtimeRows.Add(row);
            }
        }

        private void AcceptInvitation(Guid invitationId, Button button)
        {
            if (_requestInFlight) return;
            _requestInFlight = true;
            button.interactable = false;
            SetStatus("Accepting invitation...", false);
            var dto = new RespondToAllianceInvitationDTO { WorldPlayerId = _worldPlayerId, InvitationId = invitationId };
            StartCoroutine(NetworkManager.Instance.Alliance.AcceptInvitation(dto, NetworkManager.Instance.JwtToken, alliance =>
            {
                if (!this || !isActiveAndEnabled) return;
                _requestInFlight = false;
                if (alliance != null) OpenAllianceWindow();
                else
                {
                    button.interactable = true;
                    SetStatus("Could not accept invitation.", true);
                }
            }));
        }

        private void OpenAllianceWindow()
        {
            BottomNavigationFooterController footer = transform.root.GetComponentInChildren<BottomNavigationFooterController>(true);
            if (footer != null && allianceWindowPrefab != null) footer.ReplaceActiveAllianceWindow(allianceWindowPrefab);
            else Destroy(gameObject);
        }

        private void SetBusy(bool busy, string message)
        {
            _requestInFlight = busy;
            if (allianceNameInput != null) allianceNameInput.interactable = !busy;
            if (allianceTagInput != null) allianceTagInput.interactable = !busy;
            if (createButton != null) createButton.enabled = !busy;
            SetStatus(message, !busy && !string.IsNullOrEmpty(message));
        }

        private void SetStatus(string message, bool _)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void SetState(bool loading, bool error, bool empty)
        {
            if (loadingState != null) loadingState.SetActive(loading);
            if (errorState != null) errorState.SetActive(error);
            if (invitationRowTemplate != null) invitationRowTemplate.SetActive(empty);
        }

        private T FindNamed<T>(string objectName) where T : Component
        {
            foreach (T item in GetComponentsInChildren<T>(true)) if (item.name == objectName) return item;
            return null;
        }

        private GameObject FindObject(string objectName)
        {
            foreach (Transform item in GetComponentsInChildren<Transform>(true)) if (item.name == objectName) return item.gameObject;
            return null;
        }
    }
}
