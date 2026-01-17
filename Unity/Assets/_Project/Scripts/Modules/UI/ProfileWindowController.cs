using Project.Modules.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Unity.VisualScripting;

namespace Project.Modules.UI.Windows.Implementations
{
    public class ProfileWindowController : BaseWindow
    {
        // --- BASEWINDOW CONTRACT ---
        protected override string WindowName => "ProfileWindow";
        protected override string VisualContainerName => "ProfileWindow-Frame";
        protected override string HeaderName => "ProfileWindow-TitleBar";

        // --- UI REFERENCES ---
        private Label _playerNameLabel;
        private Label _allianceNameLabel;
        private Label _rankValueLabel;
        private Label _pointsValueLabel;
        private Label _citiesValueLabel;
        private Label _descriptionLabel;

        public override void OnOpen(object dataPayload)
        {
            InitializeVisualElementReferences();

            Guid targetWorldPlayerId = DetermineTargetPlayerId(dataPayload);

            if (targetWorldPlayerId != Guid.Empty)
            {
                RefreshPlayerProfileData(targetWorldPlayerId);
            }
            else
            {
                Debug.LogError("[ProfileWindow] Kunne ikke identificere spiller ID.");
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
                    Debug.LogError($"[ProfileWindow] Fandt ingen data for spiller {worldPlayerId}");
                    if (_playerNameLabel != null) _playerNameLabel.text = "Error loading data";
                }
            }));
        }

        private void UpdateUserProfileInterface(WorldPlayerProfileDTO data)
        {
            if (_playerNameLabel != null) _playerNameLabel.text = data.UserName;
            if (_allianceNameLabel != null) _allianceNameLabel.text = string.IsNullOrEmpty(data.AllianceName) ? "Ingen Alliance" : data.AllianceName;

            if (_rankValueLabel != null) _rankValueLabel.text = data.Ranking.ToString();
            if (_pointsValueLabel != null) _pointsValueLabel.text = data.TotalPoints.ToString("N0");
            if (_citiesValueLabel != null) _citiesValueLabel.text = data.CityCount.ToString();

        }
    }
}