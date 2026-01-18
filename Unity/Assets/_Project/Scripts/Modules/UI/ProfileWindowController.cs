using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;

namespace Project.Modules.UI.Windows.Implementations
{
    public class ProfileWindowController : BaseWindow
    {
        // --- BASEWINDOW CONTRACT ---
        protected override string WindowName => "Profile";
        protected override string VisualContainerName => "Profile-Window-MainContainer";
        protected override string HeaderName => "Profile-Window-Header";

        // --- UI REFERENCES ---
        private Label _playerNameLabel;
        private Label _allianceNameLabel;
        private Label _rankValueLabel;
        private Label _pointsValueLabel;
        private Label _citiesValueLabel;
        private Label _descriptionLabel;
        private VisualElement _avatarImage;

        public override void OnOpen(object dataPayload)
        {
            // 1. Setup Close Button (Using standard Header button)
            var closeBtn = Root.Q<Button>("Header-Close-Button");
            if (closeBtn != null) { closeBtn.clicked -= Close; closeBtn.clicked += Close; }

            // 2. Initialize References
            InitializeVisualElementReferences();

            // 3. Determine ID
            Guid targetWorldPlayerId = DetermineTargetPlayerId(dataPayload);

            if (targetWorldPlayerId != Guid.Empty)
            {
                RefreshPlayerProfileData(targetWorldPlayerId);
            }
            else
            {
                Debug.LogError("[ProfileWindow] Could not identify Player ID.");
                if (_playerNameLabel != null) _playerNameLabel.text = "Error";
            }
        }

        private void InitializeVisualElementReferences()
        {
            _playerNameLabel = Root.Q<Label>("Lbl-PlayerName");
            _allianceNameLabel = Root.Q<Label>("Lbl-AllianceName");

            _rankValueLabel = Root.Q<Label>("Lbl-RankValue");
            _pointsValueLabel = Root.Q<Label>("Lbl-PointsValue");
            _citiesValueLabel = Root.Q<Label>("Lbl-CitiesValue");

            _descriptionLabel = Root.Q<Label>("Lbl-Description");
            _avatarImage = Root.Q<VisualElement>("Img-PlayerAvatar");
        }

        private Guid DetermineTargetPlayerId(object payload)
        {
            if (payload is Guid specificId)
            {
                return specificId;
            }

            if (!string.IsNullOrEmpty(NetworkManager.Instance.WorldPlayerId))
            {
                if (Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid parsedId))
                {
                    return parsedId;
                }
            }

            return Guid.Empty;
        }

        private void RefreshPlayerProfileData(Guid worldPlayerId)
        {
            string jwtToken = NetworkManager.Instance.JwtToken;

            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(worldPlayerId, jwtToken, (profileDto) =>
            {
                if (profileDto != null)
                {
                    UpdateUserProfileInterface(profileDto);
                }
                else
                {
                    Debug.LogError($"[ProfileWindow] No data found for player {worldPlayerId}");
                    if (_playerNameLabel != null) _playerNameLabel.text = "Error loading data";
                }
            }));
        }

        private void UpdateUserProfileInterface(WorldPlayerProfileDTO data)
        {
            if (_playerNameLabel != null) _playerNameLabel.text = data.UserName;

            if (_allianceNameLabel != null)
                _allianceNameLabel.text = string.IsNullOrEmpty(data.AllianceName) ? "-" : $"<{data.AllianceName}>";

            if (_rankValueLabel != null) _rankValueLabel.text = data.Ranking.ToString();
            if (_pointsValueLabel != null) _pointsValueLabel.text = data.TotalPoints.ToString("N0");
            if (_citiesValueLabel != null) _citiesValueLabel.text = data.CityCount.ToString();

            // Optional: Description support
            // if (_descriptionLabel != null) _descriptionLabel.text = ...
        }
    }
}