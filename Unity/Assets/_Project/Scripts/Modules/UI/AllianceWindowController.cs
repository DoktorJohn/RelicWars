using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class AllianceWindowController : BaseWindow
    {
        // --- BaseWindow Setup ---
        protected override string WindowName => "AllianceWindow";
        protected override string VisualContainerName => "AllianceWindow-Frame";
        protected override string HeaderName => "AllianceWindow-TitleBar";

        // --- View Containers ---
        private VisualElement _viewCreateAlliance;
        private VisualElement _viewAllianceInfo;

        // --- Create View Elements ---
        private TextField _inputName;
        private TextField _inputTag;
        private Button _btnCreate;
        private Label _lblError;

        // --- Info View Elements ---
        private Label _lblInfoName;
        private Label _lblInfoTag;
        private Label _lblInfoDescription;
        private Label _lblMemberCount;
        private Label _lblTotalPoints;
        private Button _btnLeave;

        public override void OnOpen(object dataPayload)
        {
            InitializeUI();

            // 1. Vi er nødt til at vide om spilleren allerede er i en alliance.
            // Vi henter spillerens profil først.
            Guid currentPlayerId = GetCurrentWorldPlayerId();

            if (currentPlayerId == Guid.Empty)
            {
                Debug.LogError("[AllianceWindow] Ingen WorldPlayerId fundet.");
                return;
            }

            CheckAllianceStatus(currentPlayerId);
        }

        private void InitializeUI()
        {
            // Containers
            _viewCreateAlliance = Root.Q<VisualElement>("View-CreateAlliance");
            _viewAllianceInfo = Root.Q<VisualElement>("View-AllianceInfo");

            // Create Elements
            _inputName = Root.Q<TextField>("Input-AllianceName");
            _inputTag = Root.Q<TextField>("Input-AllianceTag");
            _btnCreate = Root.Q<Button>("Btn-CreateAlliance");
            _lblError = Root.Q<Label>("Lbl-ErrorStatus");

            if (_btnCreate != null) _btnCreate.clicked += OnCreateClicked;

            // Info Elements
            _lblInfoName = Root.Q<Label>("Lbl-AllianceName");
            _lblInfoTag = Root.Q<Label>("Lbl-AllianceTag");
            _lblInfoDescription = Root.Q<Label>("Lbl-AllianceDescription");
            _lblMemberCount = Root.Q<Label>("Lbl-MemberCount");
            _lblTotalPoints = Root.Q<Label>("Lbl-TotalPoints");
            _btnLeave = Root.Q<Button>("Btn-LeaveAlliance");

            // TODO: Implement Leave Logic senere
        }

        private Guid GetCurrentWorldPlayerId()
        {
            if (!string.IsNullOrEmpty(NetworkManager.Instance.WorldPlayerId) &&
                Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid id))
            {
                return id;
            }
            return Guid.Empty;
        }

        private void CheckAllianceStatus(Guid playerId)
        {
            string token = NetworkManager.Instance.JwtToken;

            // Vi genbruger WorldPlayer servicen til at hente vores egen profil
            // for at se om "AllianceId" er sat.
            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(playerId, token, (profile) =>
            {
                if (profile == null) return;

                // LOGIKKEN: Har spilleren et AllianceId?
                if (profile.AllianceId != Guid.Empty)
                {
                    ShowInfoView(profile.AllianceId);
                }
                else
                {
                    ShowCreateView();
                }
            }));
        }

        // --- STATE 1: CREATE VIEW ---
        private void ShowCreateView()
        {
            if (_viewCreateAlliance != null) _viewCreateAlliance.style.display = DisplayStyle.Flex;
            if (_viewAllianceInfo != null) _viewAllianceInfo.style.display = DisplayStyle.None;
        }

        private void OnCreateClicked()
        {
            string name = _inputName.text;
            string tag = _inputTag.text;

            // Simpel validering
            if (string.IsNullOrEmpty(name) || name.Length < 3)
            {
                SetError("Name too short");
                return;
            }
            if (string.IsNullOrEmpty(tag) || tag.Length < 3)
            {
                SetError("Tag must be 3-4 chars");
                return;
            }

            SetError("Creating...");
            _btnCreate.SetEnabled(false);

            Guid founderId = GetCurrentWorldPlayerId();
            string token = NetworkManager.Instance.JwtToken;

            var dto = new CreateAllianceDTO
            {
                WorldPlayerIdFounder = founderId,
                Name = name,
                Tag = tag
            };

            StartCoroutine(NetworkManager.Instance.Alliance.CreateAlliance(dto, token, (resultDto) =>
            {
                _btnCreate.SetEnabled(true);

                if (resultDto != null)
                {
                    // Succes! Skift view til Info View med den nye alliance ID
                    ShowInfoView(resultDto.Id);
                }
                else
                {
                    SetError("Creation failed (Name taken?)");
                }
            }));
        }

        private void SetError(string msg)
        {
            if (_lblError != null) _lblError.text = msg;
        }

        // --- STATE 2: INFO VIEW ---
        private void ShowInfoView(Guid allianceId)
        {
            if (_viewCreateAlliance != null) _viewCreateAlliance.style.display = DisplayStyle.None;
            if (_viewAllianceInfo != null) _viewAllianceInfo.style.display = DisplayStyle.Flex;

            RefreshAllianceData(allianceId);
        }

        private void RefreshAllianceData(Guid allianceId)
        {
            string token = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.Alliance.GetAllianceInfo(allianceId, token, (data) =>
            {
                if (data == null) return;

                if (_lblInfoName != null) _lblInfoName.text = data.Name;
                if (_lblInfoTag != null) _lblInfoTag.text = $"[{data.Tag}]";
                if (_lblInfoDescription != null) _lblInfoDescription.text = data.Description;
                if (_lblMemberCount != null) _lblMemberCount.text = $"{data.MemberCount} / {data.MaxPlayers}";
                if (_lblTotalPoints != null) _lblTotalPoints.text = data.TotalPoints.ToString("N0");
            }));
        }
    }
}