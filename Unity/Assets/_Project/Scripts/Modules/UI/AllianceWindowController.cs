using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public partial class AllianceWindowController : BaseWindow
    {
        protected override string WindowName => "Alliance";
        protected override string VisualContainerName => "Alliance-Window-MainContainer";
        protected override string HeaderName => "Alliance-Window-Header";

        private VisualElement _createView, _infoView, _memberList, _invitationList, _inviteSection, _searchResults;
        private VisualElement _overviewPanel, _membersPanel, _geopoliticsPanel, _descriptionEditor, _diplomacySection;
        private VisualElement _allianceSearchResults, _incomingPacts, _outgoingPacts, _activePacts, _activeWars;
        private TextField _nameInput, _tagInput, _playerSearchInput, _descriptionInput, _allianceSearchInput;
        private Button _createButton, _leaveButton, _searchButton, _editDescriptionButton, _saveDescriptionButton, _cancelDescriptionButton, _searchAllianceButton;
        private Button _overviewTab, _membersTab, _geopoliticsTab;
        private Label _name;
        private Label _error, _status, _loading, _tag, _description, _memberCount, _points, _currentRoleLabel;
        private Guid _worldPlayerId, _allianceId, _requestedAllianceId;
        private AllianceRoleDTO _currentRole = AllianceRoleDTO.None;
        private bool _isForeignView;
        private bool _canEditDescription;
        private int _requestVersion;

        public override void OnOpen(object dataPayload)
        {
            var version = BeginDeferredOpen();
            _requestVersion = version;
            BindElements();
            BindButtons();

            if (NetworkManager.Instance == null)
            {
                SetStatus("Network unavailable.");
                CompleteDeferredOpen(version);
                return;
            }

            _worldPlayerId = ResolveCurrentWorldPlayerId();
            if (_worldPlayerId == Guid.Empty)
            {
                SetStatus("No active world player.");
                CompleteDeferredOpen(version);
                return;
            }

            _requestedAllianceId = ResolveAllianceId(dataPayload);
            LoadInitialView(version);
        }

        private void OnDisable()
        {
            InvalidateDeferredOpen();
            StopAllCoroutines();
        }

        private void BindElements()
        {
            _createView = Root.Q("View-CreateAlliance"); _infoView = Root.Q("View-AllianceInfo");
            _overviewPanel = Root.Q("Panel-Overview"); _membersPanel = Root.Q("Panel-Members"); _geopoliticsPanel = Root.Q("Panel-Geopolitics");
            _memberList = Root.Q("Alliance-MemberList"); _invitationList = Root.Q("Invitation-List");
            _inviteSection = Root.Q("Invite-Section"); _searchResults = Root.Q("Player-SearchResults");
            _descriptionEditor = Root.Q("Description-Editor"); _diplomacySection = Root.Q("Diplomacy-Section");
            _allianceSearchResults = Root.Q("Alliance-SearchResults"); _incomingPacts = Root.Q("Incoming-Pacts");
            _outgoingPacts = Root.Q("Outgoing-Pacts"); _activePacts = Root.Q("Active-Pacts"); _activeWars = Root.Q("Active-Wars");
            _nameInput = Root.Q<TextField>("Input-AllianceName"); _tagInput = Root.Q<TextField>("Input-AllianceTag");
            _playerSearchInput = Root.Q<TextField>("Input-PlayerSearch"); _descriptionInput = Root.Q<TextField>("Input-AllianceDescription");
            _allianceSearchInput = Root.Q<TextField>("Input-AllianceSearch");
            _createButton = Root.Q<Button>("Btn-CreateAlliance"); _leaveButton = Root.Q<Button>("Btn-LeaveAlliance");
            _searchButton = Root.Q<Button>("Btn-SearchPlayer"); _editDescriptionButton = Root.Q<Button>("Btn-EditDescription");
            _saveDescriptionButton = Root.Q<Button>("Btn-SaveDescription"); _cancelDescriptionButton = Root.Q<Button>("Btn-CancelDescription");
            _searchAllianceButton = Root.Q<Button>("Btn-SearchAlliance");
            _overviewTab = Root.Q<Button>("Tab-Overview"); _membersTab = Root.Q<Button>("Tab-Members"); _geopoliticsTab = Root.Q<Button>("Tab-Geopolitics");
            _error = Root.Q<Label>("Lbl-ErrorStatus"); _status = Root.Q<Label>("Lbl-AllianceStatus");
            _loading = Root.Q<Label>("Lbl-LoadingStatus"); _name = Root.Q<Label>("Lbl-AllianceName");
            _tag = Root.Q<Label>("Lbl-AllianceTag"); _description = Root.Q<Label>("Lbl-AllianceDescription");
            _memberCount = Root.Q<Label>("Lbl-MemberCount"); _points = Root.Q<Label>("Lbl-TotalPoints");
            _currentRoleLabel = Root.Q<Label>("Lbl-CurrentRole");
        }

        private void BindButtons()
        {
            Bind(Root.Q<Button>("Header-Close-Button"), Close);
            if (_name != null)
            {
                _name.UnregisterCallback<ClickEvent>(HandleAllianceNameClickEvent);
                _name.RegisterCallback<ClickEvent>(HandleAllianceNameClickEvent);
            }
            Bind(_createButton, CreateAlliance);
            Bind(_leaveButton, LeaveAlliance);
            Bind(_searchButton, SearchPlayers);
            Bind(_editDescriptionButton, BeginEditingDescription);
            Bind(_saveDescriptionButton, SaveDescription);
            Bind(_cancelDescriptionButton, CancelEditingDescription);
            Bind(_searchAllianceButton, SearchAlliances);
            Bind(_overviewTab, ShowOverviewTab);
            Bind(_membersTab, ShowMembersTab);
            Bind(_geopoliticsTab, ShowGeopoliticsTab);
        }

        private static void Bind(Button button, Action action)
        {
            if (button == null || action == null) return;
            button.clicked -= action;
            button.clicked += action;
        }

        private void HandleAllianceNameClickEvent(ClickEvent _) => OnAllianceNameClicked();

        private static bool CanManage(AllianceRoleDTO role) =>
            role == AllianceRoleDTO.Founder || role == AllianceRoleDTO.Leader;

        private void ShowOverviewTab() => SelectTab(_overviewPanel, _overviewTab);
        private void ShowMembersTab() => SelectTab(_membersPanel, _membersTab);
        private void ShowGeopoliticsTab()
        {
            SelectTab(_geopoliticsPanel, _geopoliticsTab);
            LoadGeopolitics(_requestVersion);
        }

        private void LoadInitialView(int version)
        {
            if (_loading != null) _loading.style.display = DisplayStyle.Flex;
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _createButton, _leaveButton, _searchButton, _saveDescriptionButton, _searchAllianceButton }, false);

            StartCoroutine(NetworkManager.Instance.WorldPlayer.GetPlayerProfile(_worldPlayerId, Token, profile =>
            {
                if (version != _requestVersion || !isActiveAndEnabled)
                {
                    return;
                }

                if (_loading != null) _loading.style.display = DisplayStyle.None;
                WindowAsyncStateHelper.SetButtonsEnabled(new[] { _createButton, _leaveButton, _searchButton, _saveDescriptionButton, _searchAllianceButton }, true);

                if (profile == null)
                {
                    ShowCreateView(version);
                    SetError("Could not load alliance status.");
                    CompleteDeferredOpen(version);
                    return;
                }

                if (_requestedAllianceId != Guid.Empty && profile.AllianceId != _requestedAllianceId)
                {
                    ShowAlliance(_requestedAllianceId, true, version);
                }
                else if (profile.AllianceId != Guid.Empty)
                {
                    ShowAlliance(profile.AllianceId, false, version);
                }
                else
                {
                    ShowCreateView(version);
                }
            }));
        }

        private void ShowCreateView(int version)
        {
            if (version != _requestVersion) return;

            _isForeignView = false;
            _allianceId = Guid.Empty;
            _currentRole = AllianceRoleDTO.None;
            _createView.style.display = DisplayStyle.Flex;
            _infoView.style.display = DisplayStyle.None;
            if (_leaveButton != null) _leaveButton.style.display = DisplayStyle.None;
            LoadInvitations(version);
            CompleteDeferredOpen(version);
        }

        private void ShowAlliance(Guid allianceId, bool isForeignView, int version)
        {
            if (version != _requestVersion) return;

            _isForeignView = isForeignView;
            _createView.style.display = DisplayStyle.None;
            _infoView.style.display = DisplayStyle.Flex;
            SetStatus("Loading alliance...");
            WindowAsyncStateHelper.SetButtonsEnabled(new[] { _createButton }, false);

            StartCoroutine(NetworkManager.Instance.Alliance.GetAllianceInfo(allianceId, Token, alliance =>
            {
                if (version != _requestVersion || !isActiveAndEnabled)
                {
                    return;
                }

                WindowAsyncStateHelper.SetButtonsEnabled(new[] { _createButton }, true);
                if (alliance != null)
                {
                    RenderAlliance(alliance, version);
                }
                else
                {
                    SetStatus("Could not load alliance.");
                }
            }));
        }

        private void RenderAlliance(AllianceDTO alliance, int version)
        {
            if (version != _requestVersion) return;

            _allianceId = alliance.Id;
            _createView.style.display = DisplayStyle.None;
            _infoView.style.display = DisplayStyle.Flex;

            _name.text = alliance.Name;
            _tag.text = $"[{alliance.Tag}]";
            _description.text = alliance.Description ?? string.Empty;
            _descriptionInput.SetValueWithoutNotify(alliance.Description ?? string.Empty);
            _memberCount.text = $"{alliance.MemberCount} / {alliance.MaxPlayers}";
            _points.text = alliance.TotalPoints.ToString("N0");

            AllianceMemberDTO current = null;
            foreach (var member in alliance.Members ?? new List<AllianceMemberDTO>())
            {
                if (member.WorldPlayerId == _worldPlayerId)
                {
                    current = member;
                    break;
                }
            }

            _currentRole = _isForeignView ? AllianceRoleDTO.None : current?.Role ?? AllianceRoleDTO.None;
            _currentRoleLabel.text = _isForeignView ? "VISITOR" : _currentRole.ToString().ToUpperInvariant();

            _memberList.Clear();
            foreach (var member in alliance.Members ?? new List<AllianceMemberDTO>())
            {
                _memberList.Add(CreateMemberRow(member));
            }

            var canManage = !_isForeignView && CanManage(_currentRole);
            _canEditDescription = canManage;
            _inviteSection.style.display = canManage ? DisplayStyle.Flex : DisplayStyle.None;
            SetDescriptionEditMode(false);
            _diplomacySection.style.display = canManage ? DisplayStyle.Flex : DisplayStyle.None;
            _leaveButton.style.display = _isForeignView ? DisplayStyle.None : DisplayStyle.Flex;
            _leaveButton.text = _currentRole == AllianceRoleDTO.Founder ? "DISBAND ALLIANCE" : "LEAVE ALLIANCE";
            _leaveButton.SetEnabled(true);

            SelectTab(_overviewPanel, _overviewTab);
            SetStatus(string.Empty);
            CompleteDeferredOpen(version);
        }

        private VisualElement CreateMemberRow(AllianceMemberDTO member)
        {
            var row = CreateRow("member-row");
            row.Add(CreatePlayerLinkButton(member.UserName, member.WorldPlayerId, "member-name"));

            if (!_isForeignView && _currentRole == AllianceRoleDTO.Founder && member.Role != AllianceRoleDTO.Founder)
            {
                var roles = new List<string> { AllianceRoleDTO.Leader.ToString(), AllianceRoleDTO.Member.ToString() };
                var role = new DropdownField(roles, member.Role.ToString());
                role.AddToClassList("member-role-select");
                role.RegisterValueChangedCallback(evt => SetMemberRole(member.WorldPlayerId, (AllianceRoleDTO)Enum.Parse(typeof(AllianceRoleDTO), evt.newValue)));
                row.Add(role);
            }
            else
            {
                var role = new Label(member.Role.ToString());
                role.AddToClassList("member-role");
                row.Add(role);
            }

            var points = new Label(member.TotalPoints.ToString("N0"));
            points.AddToClassList("member-points");
            row.Add(points);

            if (CanKick(member))
            {
                row.Add(SmallButton("KICK", () => KickMember(member.WorldPlayerId)));
            }

            return row;
        }

        private bool CanKick(AllianceMemberDTO member) => !_isForeignView &&
            member.WorldPlayerId != _worldPlayerId &&
            member.Role != AllianceRoleDTO.Founder && (_currentRole == AllianceRoleDTO.Founder ||
            _currentRole == AllianceRoleDTO.Leader && member.Role == AllianceRoleDTO.Member);

        private void SetMemberRole(Guid targetId, AllianceRoleDTO role)
        {
            var dto = new SetAllianceMemberRoleDTO { WorldPlayerIdActor = _worldPlayerId, WorldPlayerIdTarget = targetId, Role = role };
            StartCoroutine(NetworkManager.Instance.Alliance.SetMemberRole(dto, Token, alliance =>
            { if (alliance != null) RenderAlliance(alliance, _requestVersion); else SetStatus("Could not change member role."); }));
        }

        private void KickMember(Guid targetId)
        {
            var dto = new KickPlayerFromAllianceDTO { WorldPlayerIdKicker = _worldPlayerId, WorldPlayerIdKicked = targetId };
            StartCoroutine(NetworkManager.Instance.Alliance.KickPlayer(dto, Token, success =>
            { if (success) ShowAlliance(_allianceId, false, _requestVersion); else SetStatus("Could not kick member."); }));
        }

        private void SaveDescription()
        {
            if (!_canEditDescription) return;
            var description = _descriptionInput.value.Trim();
            var dto = new UpdateAllianceDescriptionDTO { WorldPlayerId = _worldPlayerId, AllianceId = _allianceId, Description = description };
            StartCoroutine(NetworkManager.Instance.Alliance.UpdateDescription(dto, Token, alliance =>
            { if (alliance != null) RenderAlliance(alliance, _requestVersion); else SetStatus("Could not update description."); }));
        }

        private void BeginEditingDescription()
        {
            if (!_canEditDescription) return;
            _descriptionInput.SetValueWithoutNotify(_description.text ?? string.Empty);
            SetDescriptionEditMode(true);
            _descriptionInput.Focus();
        }

        private void CancelEditingDescription()
        {
            _descriptionInput.SetValueWithoutNotify(_description.text ?? string.Empty);
            SetDescriptionEditMode(false);
        }

        private void SetDescriptionEditMode(bool isEditing)
        {
            _description.style.display = isEditing ? DisplayStyle.None : DisplayStyle.Flex;
            _descriptionEditor.style.display = isEditing ? DisplayStyle.Flex : DisplayStyle.None;
            _editDescriptionButton.style.display = _canEditDescription && !isEditing ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SearchPlayers()
        {
            if (_playerSearchInput.value.Trim().Length < 2) { SetStatus("Enter at least 2 characters."); return; }
            var worldId = NetworkManager.Instance.ActiveWorldId;
            StartCoroutine(NetworkManager.Instance.WorldPlayer.SearchPlayers(worldId, _playerSearchInput.value.Trim(), Token, results =>
            {
                _searchResults.Clear();
                foreach (var player in results ?? new List<PlayerSearchResultDTO>())
                {
                    if (player.WorldPlayerId == _worldPlayerId) continue;
                    var row = CreateRow("search-result-row");
                    row.Add(CreatePlayerLinkButton(player.Username, player.WorldPlayerId, "member-name"));
                    row.Add(SmallButton("INVITE", () => InvitePlayer(player.WorldPlayerId)));
                    _searchResults.Add(row);
                }
            }));
        }

        private void InvitePlayer(Guid playerId)
        {
            var dto = new InviteToAllianceDTO { WorldPlayerIdInviter = _worldPlayerId, WorldPlayerIdInvited = playerId };
            StartCoroutine(NetworkManager.Instance.Alliance.InviteToAlliance(dto, Token, success => SetStatus(success ? "Invitation sent." : "Could not send invitation.")));
        }

        private void SearchAlliances()
        {
            var query = _allianceSearchInput.value.Trim();
            if (query.Length < 2) { SetStatus("Enter at least 2 characters."); return; }
            StartCoroutine(NetworkManager.Instance.Alliance.SearchAlliances(NetworkManager.Instance.ActiveWorldId, query, Token, results =>
            {
                _allianceSearchResults.Clear();
                if (results == null) { SetStatus("Could not search alliances."); return; }
                foreach (var alliance in results)
                {
                    if (alliance.Id == _allianceId) continue;
                    var row = CreateRow("search-result-row");
                    row.Add(CreateAllianceLinkButton($"[{alliance.Tag}] {alliance.Name}", alliance.Id, "member-name"));
                    row.Add(SmallButton("PACT", () => SendPact(alliance.Id)));
                    row.Add(SmallButton("WAR", () => DeclareWar(alliance.Id), "danger-action"));
                    _allianceSearchResults.Add(row);
                }
            }));
        }

        private void LeaveAlliance()
        {
            if (_isForeignView)
            {
                return;
            }

            if (_currentRole == AllianceRoleDTO.Founder)
            {
                StartCoroutine(NetworkManager.Instance.Alliance.DisbandAlliance(new DisbandAllianceDTO
                { WorldPlayerId = _worldPlayerId, AllianceId = _allianceId }, Token, success =>
                { if (success) ShowCreateView(_requestVersion); else SetStatus("Could not disband alliance."); }));
                return;
            }

            StartCoroutine(NetworkManager.Instance.Alliance.LeaveAlliance(new LeaveAllianceDTO { WorldPlayerId = _worldPlayerId }, Token, success =>
            { if (success) ShowCreateView(_requestVersion); else SetStatus("Could not leave alliance."); }));
        }

        private void SelectTab(VisualElement panel, Button button)
        {
            _overviewPanel.style.display = panel == _overviewPanel ? DisplayStyle.Flex : DisplayStyle.None;
            _membersPanel.style.display = panel == _membersPanel ? DisplayStyle.Flex : DisplayStyle.None;
            _geopoliticsPanel.style.display = panel == _geopoliticsPanel ? DisplayStyle.Flex : DisplayStyle.None;
            _overviewTab.EnableInClassList("alliance-tab-active", button == _overviewTab);
            _membersTab.EnableInClassList("alliance-tab-active", button == _membersTab);
            _geopoliticsTab.EnableInClassList("alliance-tab-active", button == _geopoliticsTab);
        }

        private void ClearRelationLists()
        {
            _incomingPacts.Clear(); _outgoingPacts.Clear(); _activePacts.Clear(); _activeWars.Clear();
        }

        private static VisualElement CreateRow(string className = "invitation-row")
        {
            var row = new VisualElement();
            row.AddToClassList(className);
            return row;
        }

        private static Button SmallButton(string text, Action action, string extraClass = null)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("small-action");
            if (!string.IsNullOrEmpty(extraClass)) button.AddToClassList(extraClass);
            return button;
        }

        private Button CreatePlayerLinkButton(string text, Guid worldPlayerId, string extraClass = null)
        {
            var button = WindowNavigationHelper.CreateLinkButton(text, null, extraClass);
            if (worldPlayerId == Guid.Empty)
            {
                button.SetEnabled(false);
                return button;
            }

            button.clicked += () => WindowNavigationHelper.OpenProfile(worldPlayerId);
            return button;
        }

        private Button CreateAllianceLinkButton(string text, Guid allianceId, string extraClass = null)
        {
            var button = WindowNavigationHelper.CreateLinkButton(text, null, extraClass);
            if (allianceId == Guid.Empty)
            {
                button.SetEnabled(false);
                return button;
            }

            button.clicked += () => WindowNavigationHelper.OpenAlliance(allianceId);
            return button;
        }

        private void OnAllianceNameClicked()
        {
            if (_allianceId != Guid.Empty)
            {
                WindowNavigationHelper.OpenAlliance(_allianceId);
            }
        }

        private Guid ResolveCurrentWorldPlayerId() => Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out var id) ? id : Guid.Empty;

        private Guid ResolveAllianceId(object payload)
        {
            if (payload is Guid allianceId)
            {
                return allianceId;
            }

            if (payload is string allianceIdText && Guid.TryParse(allianceIdText, out var parsedId))
            {
                return parsedId;
            }

            return Guid.Empty;
        }

        private string Token => NetworkManager.Instance.JwtToken;
        private void SetError(string value) { if (_error != null) _error.text = value; }
        private void SetStatus(string value) { if (_status != null) _status.text = value; }
    }
}
