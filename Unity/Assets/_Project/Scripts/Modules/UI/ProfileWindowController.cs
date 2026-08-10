using Project.Modules.UI;
using Project.Network.Manager;
using Project.Network.Models;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public class ProfileWindowController : BaseWindow
    {
        protected override string WindowName => "Profile";
        protected override string VisualContainerName => "Profile-Window-MainContainer";
        protected override string HeaderName => "Profile-Window-Header";

        private Label _playerNameLabel;
        private Label _allianceNameLabel;
        private Button _messagePlayerButton;
        private Button _inviteToAllianceButton;
        private VisualElement _actionRow;
        private Label _actionStatusLabel;
        private Label _rankValueLabel;
        private Label _pointsValueLabel;
        private Label _citiesValueLabel;
        private Label _descriptionViewLabel;
        private Button _editDescriptionButton;
        private VisualElement _descriptionEditorContainer;
        private TextField _descriptionInput;
        private Button _saveDescriptionButton;
        private Button _cancelDescriptionButton;
        private ScrollView _citiesList;
        private VisualElement _avatarImage;

        private readonly List<CityDTO> _cities = new();
        private Guid _currentWorldPlayerId;
        private Guid _displayedWorldPlayerId;
        private Guid _displayedAllianceId;
        private int _requestVersion;
        private bool _saveDescriptionInFlight;
        private bool _inviteInFlight;
        private bool _isEditingDescription;
        private string _currentDescriptionText = string.Empty;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            BindVisualElementReferences();
            BindButtons();
            ResetInvitationAction();

            _currentWorldPlayerId = ResolveCurrentWorldPlayerId();
            _displayedWorldPlayerId = ResolveTargetPlayerId(dataPayload);

            if (_displayedWorldPlayerId == Guid.Empty)
            {
                Debug.LogError("[ProfileWindow] Could not identify Player ID.");
                if (_playerNameLabel != null) _playerNameLabel.text = "Error";
                CompleteDeferredOpen(version);
                return;
            }

            _requestVersion = version;
            RefreshPlayerProfileData(_displayedWorldPlayerId, version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
            _saveDescriptionInFlight = false;
            _inviteInFlight = false;
            _isEditingDescription = false;
            ResetDescriptionSaveButton();
        }

        private void BindVisualElementReferences()
        {
            _playerNameLabel = Root.Q<Label>("Lbl-PlayerName");
            _allianceNameLabel = Root.Q<Label>("Lbl-AllianceName");
            _messagePlayerButton = Root.Q<Button>("Btn-MessagePlayer");
            _inviteToAllianceButton = Root.Q<Button>("Btn-InviteToAlliance");
            _actionRow = Root.Q<VisualElement>("Profile-ActionRow");
            _actionStatusLabel = Root.Q<Label>("Lbl-ProfileActionStatus");

            _rankValueLabel = Root.Q<Label>("Lbl-RankValue");
            _pointsValueLabel = Root.Q<Label>("Lbl-PointsValue");
            _citiesValueLabel = Root.Q<Label>("Lbl-CitiesValue");

            _descriptionViewLabel = Root.Q<Label>("Lbl-Description");
            _editDescriptionButton = Root.Q<Button>("Btn-EditDescription");
            _descriptionEditorContainer = Root.Q<VisualElement>("Profile-Description-Editor");
            _descriptionInput = Root.Q<TextField>("Txt-DescriptionInput");
            _saveDescriptionButton = Root.Q<Button>("Btn-SaveDescription");
            _cancelDescriptionButton = Root.Q<Button>("Btn-CancelDescription");
            _citiesList = Root.Q<ScrollView>("Profile-Cities-List");
            _avatarImage = Root.Q<VisualElement>("Img-PlayerAvatar");
        }

        private void BindButtons()
        {
            if (_playerNameLabel != null)
            {
                _playerNameLabel.UnregisterCallback<ClickEvent>(HandlePlayerNameClickEvent);
                _playerNameLabel.RegisterCallback<ClickEvent>(HandlePlayerNameClickEvent);
            }

            if (_allianceNameLabel != null)
            {
                _allianceNameLabel.UnregisterCallback<ClickEvent>(HandleAllianceNameClickEvent);
                _allianceNameLabel.RegisterCallback<ClickEvent>(HandleAllianceNameClickEvent);
            }

            Bind(_messagePlayerButton, HandleMessagePlayerClicked);
            Bind(_inviteToAllianceButton, HandleInviteToAllianceClicked);
            Bind(_editDescriptionButton, HandleEditDescriptionClicked);
            Bind(_saveDescriptionButton, HandleSaveDescriptionClicked);
            Bind(_cancelDescriptionButton, HandleCancelDescriptionClicked);

            var closeBtn = Root.Q<Button>("Header-Close-Button");
            Bind(closeBtn, Close);
        }

        private static void Bind(Button button, Action action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.clicked -= action;
            button.clicked += action;
        }

        private void HandlePlayerNameClickEvent(ClickEvent _) => HandlePlayerNameClicked();
        private void HandleAllianceNameClickEvent(ClickEvent _) => HandleAllianceNameClicked();

        private Guid ResolveCurrentWorldPlayerId()
        {
            if (!string.IsNullOrWhiteSpace(NetworkManager.Instance?.WorldPlayerId) &&
                Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out var parsedId))
            {
                return parsedId;
            }

            return Guid.Empty;
        }

        private Guid ResolveTargetPlayerId(object payload)
        {
            if (payload is Guid specificId)
            {
                return specificId;
            }

            if (payload is string targetIdText && Guid.TryParse(targetIdText, out var parsedId))
            {
                return parsedId;
            }

            return _currentWorldPlayerId;
        }

        private void RefreshPlayerProfileData(Guid worldPlayerId, int version)
        {
            if (NetworkManager.Instance == null)
            {
                if (_playerNameLabel != null) _playerNameLabel.text = "Error loading data";
                CompleteDeferredOpen(version);
                return;
            }

            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(worldPlayerId, NetworkManager.Instance.JwtToken, profileDto =>
            {
                if (version != _requestVersion || !isActiveAndEnabled)
                {
                    return;
                }

                if (profileDto != null)
                {
                    UpdateUserProfileInterface(profileDto);
                    if (profileDto.WorldPlayerId == _currentWorldPlayerId)
                    {
                        CompleteDeferredOpen(version);
                    }
                    else
                    {
                        RefreshCurrentPlayerAllianceState(profileDto, version);
                    }
                }
                else
                {
                    Debug.LogError($"[ProfileWindow] No data found for player {worldPlayerId}");
                    if (_playerNameLabel != null) _playerNameLabel.text = "Error loading data";
                    CompleteDeferredOpen(version);
                }
            }));
        }

        private void RefreshCurrentPlayerAllianceState(WorldPlayerProfileDTO targetProfile, int version)
        {
            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(
                _currentWorldPlayerId,
                NetworkManager.Instance.JwtToken,
                viewerProfile =>
                {
                    if (version != _requestVersion || !isActiveAndEnabled)
                    {
                        return;
                    }

                    var canInvite = viewerProfile != null &&
                        viewerProfile.WorldId == targetProfile.WorldId &&
                        viewerProfile.AllianceId != Guid.Empty &&
                        viewerProfile.AllianceRole == AllianceRoleDTO.Founder &&
                        targetProfile.AllianceId == Guid.Empty;

                    if (_inviteToAllianceButton != null)
                    {
                        _inviteToAllianceButton.style.display = canInvite ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    CompleteDeferredOpen(version);
                }));
        }

        private void UpdateUserProfileInterface(WorldPlayerProfileDTO data)
        {
            if (data == null)
            {
                return;
            }

            _displayedWorldPlayerId = data.WorldPlayerId;
            _displayedAllianceId = data.AllianceId;
            _currentDescriptionText = data.Description ?? string.Empty;

            var isSelfView = data.WorldPlayerId == _currentWorldPlayerId;
            if (_actionRow != null) _actionRow.style.display = isSelfView ? DisplayStyle.None : DisplayStyle.Flex;
            if (_messagePlayerButton != null) _messagePlayerButton.style.display = isSelfView ? DisplayStyle.None : DisplayStyle.Flex;
            if (_inviteToAllianceButton != null) _inviteToAllianceButton.style.display = DisplayStyle.None;
            if (_editDescriptionButton != null) _editDescriptionButton.style.display = isSelfView ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isSelfView)
            {
                _isEditingDescription = false;
            }

            ApplyDescriptionEditMode(isSelfView && _isEditingDescription);

            if (_playerNameLabel != null)
            {
                _playerNameLabel.text = string.IsNullOrWhiteSpace(data.UserName) ? "Unknown" : data.UserName;
                _playerNameLabel.SetEnabled(true);
            }

            if (_allianceNameLabel != null)
            {
                var hasAlliance = data.AllianceId != Guid.Empty && !string.IsNullOrWhiteSpace(data.AllianceName);
                _allianceNameLabel.text = hasAlliance ? data.AllianceName : "-";
                _allianceNameLabel.SetEnabled(hasAlliance);
                _allianceNameLabel.EnableInClassList("btn-entity-link", hasAlliance);
            }

            if (_rankValueLabel != null) _rankValueLabel.text = data.Ranking.ToString();
            if (_pointsValueLabel != null) _pointsValueLabel.text = data.TotalPoints.ToString("N0");
            if (_citiesValueLabel != null) _citiesValueLabel.text = data.CityCount.ToString();

            if (_descriptionViewLabel != null)
            {
                _descriptionViewLabel.text = string.IsNullOrWhiteSpace(data.Description)
                    ? "No description available."
                    : data.Description;
            }

            if (_descriptionInput != null)
            {
                _descriptionInput.SetValueWithoutNotify(_currentDescriptionText);
            }

            RenderCities(data.Cities);
        }

        private void ApplyDescriptionEditMode(bool isEditing)
        {
            _isEditingDescription = isEditing;

            var isSelfView = _displayedWorldPlayerId != Guid.Empty && _displayedWorldPlayerId == _currentWorldPlayerId;

            if (_descriptionViewLabel != null)
            {
                _descriptionViewLabel.style.display = isSelfView && !isEditing ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_descriptionEditorContainer != null)
            {
                _descriptionEditorContainer.style.display = isSelfView && isEditing ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_editDescriptionButton != null)
            {
                _editDescriptionButton.style.display = isSelfView && !isEditing ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_descriptionInput != null && isSelfView)
            {
                _descriptionInput.SetValueWithoutNotify(_currentDescriptionText);
            }

            if (_saveDescriptionButton != null)
            {
                _saveDescriptionButton.style.display = isSelfView && isEditing ? DisplayStyle.Flex : DisplayStyle.None;
                _saveDescriptionButton.SetEnabled(!isEditing || !_saveDescriptionInFlight);
            }

            if (_cancelDescriptionButton != null)
            {
                _cancelDescriptionButton.style.display = isSelfView && isEditing ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (isSelfView && isEditing && _descriptionInput != null)
            {
                _descriptionInput.Focus();
            }
        }

        private void RenderCities(List<CityDTO> cities)
        {
            if (_citiesList == null)
            {
                return;
            }

            _cities.Clear();
            if (cities != null)
            {
                _cities.AddRange(cities.OrderByDescending(city => city.Points).ThenBy(city => city.CityName));
            }

            _citiesList.Clear();

            if (_cities.Count == 0)
            {
                WindowAsyncStateHelper.ShowEmpty(_citiesList, "No cities available.");
                return;
            }

            foreach (var city in _cities)
            {
                var row = new VisualElement();
                row.AddToClassList("profile-city-row");

                var cityLink = new Label(city.CityName);
                cityLink.AddToClassList("profile-city-name");
                cityLink.AddToClassList("btn-entity-link");
                cityLink.RegisterCallback<ClickEvent>(_ =>
                    WindowNavigationHelper.OpenCityInspection(city.Id, city.X, city.Y));
                row.Add(cityLink);

                var coordinates = new Label($"{city.X}, {city.Y}");
                coordinates.AddToClassList("profile-city-coordinates");
                row.Add(coordinates);

                var points = new Label(city.Points.ToString("N0"));
                points.AddToClassList("profile-city-points");
                row.Add(points);

                _citiesList.Add(row);
            }
        }

        private void HandleSaveDescriptionClicked()
        {
            if (_saveDescriptionInFlight || NetworkManager.Instance == null || _descriptionInput == null || !_isEditingDescription)
            {
                return;
            }

            var description = _descriptionInput.value?.Trim() ?? string.Empty;
            if (description.Length > 500)
            {
                Debug.LogError("[ProfileWindow] Description is too long.");
                return;
            }

            _saveDescriptionInFlight = true;
            SetDescriptionSaveButtonEnabled(false, "SAVING");

            StartCoroutine(NetworkManager.Instance.WorldPlayer.UpdatePlayerDescription(
                _displayedWorldPlayerId,
                description,
                NetworkManager.Instance.JwtToken,
                profileDto =>
                {
                    _saveDescriptionInFlight = false;

                    if (!isActiveAndEnabled)
                    {
                        return;
                    }

                    if (profileDto == null)
                    {
                        Debug.LogError("[ProfileWindow] Failed to update profile description.");
                        ResetDescriptionSaveButton();
                        return;
                    }

                    UpdateUserProfileInterface(profileDto);
                    ApplyDescriptionEditMode(false);
                    ResetDescriptionSaveButton();
                }));
        }

        private void HandleEditDescriptionClicked()
        {
            if (_displayedWorldPlayerId != _currentWorldPlayerId)
            {
                return;
            }

            ApplyDescriptionEditMode(true);
        }

        private void HandleCancelDescriptionClicked()
        {
            if (_displayedWorldPlayerId != _currentWorldPlayerId)
            {
                return;
            }

            ApplyDescriptionEditMode(false);
        }

        private void SetDescriptionSaveButtonEnabled(bool enabled, string text)
        {
            if (_saveDescriptionButton != null)
            {
                _saveDescriptionButton.SetEnabled(enabled);
                _saveDescriptionButton.text = text;
            }
        }

        private void ResetDescriptionSaveButton()
        {
            SetDescriptionSaveButtonEnabled(true, "SAVE DESCRIPTION");
        }

        private void HandlePlayerNameClicked()
        {
            if (_displayedWorldPlayerId != Guid.Empty)
            {
                WindowNavigationHelper.OpenProfile(_displayedWorldPlayerId);
            }
        }

        private void HandleAllianceNameClicked()
        {
            if (_displayedAllianceId != Guid.Empty)
            {
                WindowNavigationHelper.OpenAlliance(_displayedAllianceId);
            }
        }

        private void HandleMessagePlayerClicked()
        {
            if (_displayedWorldPlayerId != Guid.Empty)
            {
                WindowNavigationHelper.OpenMessageToPlayer(_displayedWorldPlayerId);
            }
        }

        private void HandleInviteToAllianceClicked()
        {
            if (_inviteInFlight || NetworkManager.Instance == null ||
                _currentWorldPlayerId == Guid.Empty || _displayedWorldPlayerId == Guid.Empty ||
                _currentWorldPlayerId == _displayedWorldPlayerId)
            {
                return;
            }

            _inviteInFlight = true;
            SetInvitationStatus("Sending invitation...");
            SetInvitationButtonState(false, "SENDING...");
            var requestedPlayerId = _displayedWorldPlayerId;
            var version = _requestVersion;
            var dto = new InviteToAllianceDTO
            {
                WorldPlayerIdInviter = _currentWorldPlayerId,
                WorldPlayerIdInvited = requestedPlayerId
            };

            StartCoroutine(NetworkManager.Instance.Alliance.InviteToAlliance(
                dto,
                NetworkManager.Instance.JwtToken,
                success =>
                {
                    if (version != _requestVersion || !isActiveAndEnabled || requestedPlayerId != _displayedWorldPlayerId)
                    {
                        return;
                    }

                    _inviteInFlight = false;
                    if (success)
                    {
                        SetInvitationStatus("Alliance invitation sent.");
                        SetInvitationButtonState(false, "INVITATION SENT");
                    }
                    else
                    {
                        SetInvitationStatus("Could not send alliance invitation.");
                        SetInvitationButtonState(true, "INVITE TO ALLIANCE");
                    }
                }));
        }

        private void ResetInvitationAction()
        {
            _inviteInFlight = false;
            if (_inviteToAllianceButton != null)
            {
                _inviteToAllianceButton.style.display = DisplayStyle.None;
            }
            SetInvitationButtonState(true, "INVITE TO ALLIANCE");
            SetInvitationStatus(string.Empty);
        }

        private void SetInvitationButtonState(bool enabled, string text)
        {
            if (_inviteToAllianceButton == null)
            {
                return;
            }

            _inviteToAllianceButton.SetEnabled(enabled);
            _inviteToAllianceButton.text = text;
        }

        private void SetInvitationStatus(string message)
        {
            if (_actionStatusLabel == null)
            {
                return;
            }

            _actionStatusLabel.text = message ?? string.Empty;
            _actionStatusLabel.style.display = string.IsNullOrWhiteSpace(message)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
