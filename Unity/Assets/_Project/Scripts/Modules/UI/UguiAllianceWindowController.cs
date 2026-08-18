using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using Sunvale.AncientRomeUI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Modules.UI
{
    public sealed class UguiAllianceWindowController : MonoBehaviour
    {
        [Header("Authored content roots")]
        [SerializeField] private GameObject overviewContent;
        [SerializeField] private GameObject membersContent;
        [SerializeField] private GameObject politicsContent;

        [Header("Overview")]
        [SerializeField] private TMP_Text allianceNameText;
        [SerializeField] private TMP_Text allianceTagText;
        [SerializeField] private TMP_Text membersAmountText;
        [SerializeField] private TMP_Text pointsAmountText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button editDescriptionButton;

        [Header("Members")]
        [SerializeField] private RectTransform memberRowsRoot;
        [SerializeField] private GameObject playerDataRowTemplate;
        [SerializeField] private GameObject invitePlayerRoot;
        [SerializeField] private TMP_InputField invitePlayerInput;
        [SerializeField] private GameObject playerSuggestionsDropdown;
        [SerializeField] private RectTransform playerSuggestionRowsRoot;
        [SerializeField] private GameObject playerInviteSearchRowTemplate;

        [Header("Politics")]
        [SerializeField] private RectTransform alliesRowsRoot;
        [SerializeField] private GameObject friendlyAllianceRowTemplate;
        [SerializeField] private RectTransform hostileRowsRoot;
        [SerializeField] private GameObject hostileAllianceRowTemplate;
        [SerializeField] private GameObject politicalActionsRoot;
        [SerializeField] private TMP_InputField allianceSearchInput;
        [SerializeField] private GameObject allianceSuggestionsDropdown;
        [SerializeField] private RectTransform allianceSuggestionRowsRoot;
        [SerializeField] private GameObject allianceSearchRowTemplate;

        private TMP_InputField _descriptionInput;
        private AllianceDTO _alliance;
        private AllianceGeopoliticsDTO _geopolitics;
        private Guid _worldPlayerId;
        private AllianceRoleDTO _currentRole;
        private Coroutine _playerSearchRoutine;
        private Coroutine _allianceSearchRoutine;
        private int _requestVersion;
        private bool _editingDescription;
        private bool _descriptionRequestInFlight;
        private string _lastSavedDescription = string.Empty;
        private string _pendingDescription = string.Empty;
        private readonly List<GameObject> _memberRows = new();
        private readonly List<GameObject> _playerSearchRows = new();
        private readonly List<GameObject> _friendlyRows = new();
        private readonly List<GameObject> _hostileRows = new();
        private readonly List<GameObject> _allianceSearchRows = new();

        private void Awake()
        {
            InitializeAuthoredUi();
        }

        private void InitializeAuthoredUi()
        {
            BindAuthoredReferences();
            EnsureDescriptionInput();
            SetTemplateState(playerDataRowTemplate, false);
            SetTemplateState(playerInviteSearchRowTemplate, false);
            SetTemplateState(friendlyAllianceRowTemplate, false);
            SetTemplateState(hostileAllianceRowTemplate, false);
            SetTemplateState(allianceSearchRowTemplate, false);
            SetTemplateState(Find(politicsContent != null ? politicsContent.transform : transform, "PoliticsRequestDataRow")?.gameObject, false);
            SetDropdown(playerSuggestionsDropdown, false);
            SetDropdown(allianceSuggestionsDropdown, false);
            ClearAuthoredDummyValues();
        }

        private void OnEnable()
        {
            // OnEnable deliberately repeats the idempotent binding. Unity can recompile scripts while
            // the window already exists in Play Mode, in which case Awake is not invoked again.
            InitializeAuthoredUi();
            if (editDescriptionButton != null) editDescriptionButton.onClick.AddListener(BeginDescriptionEdit);
            if (_descriptionInput != null) _descriptionInput.onValueChanged.AddListener(OnDescriptionChanged);
            if (invitePlayerInput != null) invitePlayerInput.onValueChanged.AddListener(OnPlayerSearchChanged);
            if (allianceSearchInput != null) allianceSearchInput.onValueChanged.AddListener(OnAllianceSearchChanged);
            LoadAlliance();
        }

        private void ClearAuthoredDummyValues()
        {
            SetText(allianceNameText, string.Empty);
            SetText(allianceTagText, string.Empty);
            SetText(membersAmountText, string.Empty);
            SetText(pointsAmountText, string.Empty);
            SetText(roleText, string.Empty);
            SetDescription(string.Empty);
        }

        private void OnDisable()
        {
            if (editDescriptionButton != null) editDescriptionButton.onClick.RemoveListener(BeginDescriptionEdit);
            if (_descriptionInput != null) _descriptionInput.onValueChanged.RemoveListener(OnDescriptionChanged);
            if (invitePlayerInput != null) invitePlayerInput.onValueChanged.RemoveListener(OnPlayerSearchChanged);
            if (allianceSearchInput != null) allianceSearchInput.onValueChanged.RemoveListener(OnAllianceSearchChanged);
            _requestVersion++;
            StopAllCoroutines();
            _playerSearchRoutine = null;
            _allianceSearchRoutine = null;
        }

        private void LoadAlliance()
        {
            int version = ++_requestVersion;
            NetworkManager network = NetworkManager.Instance;
            if (network == null || !Guid.TryParse(network.WorldPlayerId, out _worldPlayerId)) return;

            StartCoroutine(network.WorldPlayer.GetPlayerProfile(_worldPlayerId, network.JwtToken, profile =>
            {
                if (!CanApply(version) || profile == null || profile.AllianceId == Guid.Empty) return;
                StartCoroutine(network.Alliance.GetAllianceInfo(profile.AllianceId, network.JwtToken, alliance =>
                {
                    if (!CanApply(version) || alliance == null) return;
                    RenderAlliance(alliance);
                    LoadGeopolitics(version);
                }));
            }));
        }

        private void RenderAlliance(AllianceDTO alliance)
        {
            _alliance = alliance;
            _currentRole = alliance.Members?.FirstOrDefault(member => member.WorldPlayerId == _worldPlayerId)?.Role
                ?? AllianceRoleDTO.None;

            SetText(allianceNameText, alliance.Name);
            SetText(allianceTagText, $"[{alliance.Tag}]");
            SetText(membersAmountText, alliance.MemberCount.ToString("N0"));
            SetText(pointsAmountText, alliance.TotalPoints.ToString("N0"));
            SetText(roleText, _currentRole.ToString());
            SetDescription(alliance.Description ?? string.Empty);
            RenderMembers(alliance.Members ?? new List<AllianceMemberDTO>());

            bool canManage = CanManage(_currentRole);
            if (editDescriptionButton != null) editDescriptionButton.gameObject.SetActive(canManage);
            if (invitePlayerRoot != null) invitePlayerRoot.SetActive(_currentRole == AllianceRoleDTO.Founder);
            if (politicalActionsRoot != null) politicalActionsRoot.SetActive(canManage);
        }

        private void BeginDescriptionEdit()
        {
            if (!CanManage(_currentRole) || _descriptionInput == null) return;
            _editingDescription = true;
            _descriptionInput.readOnly = false;
            _descriptionInput.SetTextWithoutNotify(_lastSavedDescription);
            StartCoroutine(FocusDescriptionAfterButtonClick());
        }

        private IEnumerator FocusDescriptionAfterButtonClick()
        {
            yield return new WaitForEndOfFrame();
            if (!_editingDescription || _descriptionInput == null) yield break;
            _descriptionInput.Select();
            _descriptionInput.ActivateInputField();
        }

        private void OnDescriptionChanged(string value)
        {
            if (!_editingDescription || _alliance == null) return;
            _pendingDescription = NormalizeDescription(value);
            if (_pendingDescription != value) _descriptionInput.SetTextWithoutNotify(_pendingDescription);
            TrySavePendingDescription();
        }

        private void TrySavePendingDescription()
        {
            if (_descriptionRequestInFlight || DescriptionEqualsSavedState(_pendingDescription) || _alliance == null) return;
            NetworkManager network = NetworkManager.Instance;
            if (network == null) return;

            string sentDescription = _pendingDescription;
            int version = _requestVersion;
            _descriptionRequestInFlight = true;
            var dto = new UpdateAllianceDescriptionDTO
            {
                WorldPlayerId = _worldPlayerId,
                AllianceId = _alliance.Id,
                Description = sentDescription
            };
            StartCoroutine(network.Alliance.UpdateDescription(dto, network.JwtToken, updated =>
            {
                if (!CanApply(version)) return;
                _descriptionRequestInFlight = false;
                if (updated != null)
                {
                    _lastSavedDescription = updated.Description ?? string.Empty;
                    _alliance.Description = _lastSavedDescription;
                }
                else
                {
                    _pendingDescription = _lastSavedDescription;
                    _descriptionInput.SetTextWithoutNotify(_lastSavedDescription);
                }
                TrySavePendingDescription();
            }));
        }

        private void SetDescription(string description)
        {
            _lastSavedDescription = NormalizeDescription(description);
            _pendingDescription = _lastSavedDescription;
            if (_descriptionInput != null)
            {
                _descriptionInput.SetTextWithoutNotify(_lastSavedDescription);
                _descriptionInput.readOnly = !_editingDescription;
            }
            else SetText(descriptionText, _lastSavedDescription);
        }

        private void RenderMembers(IEnumerable<AllianceMemberDTO> members)
        {
            List<AllianceMemberDTO> memberList = members?.ToList() ?? new List<AllianceMemberDTO>();
            ClearRuntimeRows(_memberRows);
            if (memberRowsRoot == null || playerDataRowTemplate == null) return;
            foreach (AllianceMemberDTO member in memberList)
            {
                GameObject row = CloneTemplate(playerDataRowTemplate, memberRowsRoot, _memberRows);
                SetText(FindTextIn(row.transform, "PlayerText"), member.UserName);
                SetText(FindTextIn(row.transform, "RoleText"), member.Role.ToString());
                SetText(FindTextIn(row.transform, "PointsText"), member.TotalPoints.ToString("N0"));
                SetText(FindTextIn(row.transform, "CitiesText"), member.CityCount.ToString("N0"));

                CarvedPressButton kickButton = Find<CarvedPressButton>(row.transform, "KickBtn");
                bool canKick = CanKick(member);
                if (kickButton == null) continue;
                kickButton.gameObject.SetActive(canKick);
                if (!canKick) continue;
                Guid targetId = member.WorldPlayerId;
                kickButton.OnButtonActivatedClicked += _ => KickMember(targetId, kickButton);
            }
        }

        private void KickMember(Guid targetId, CarvedPressButton button)
        {
            if (!button.enabled) return;
            button.enabled = false;
            var dto = new KickPlayerFromAllianceDTO { WorldPlayerIdKicker = _worldPlayerId, WorldPlayerIdKicked = targetId };
            StartCoroutine(NetworkManager.Instance.Alliance.KickPlayer(dto, NetworkManager.Instance.JwtToken, success =>
            {
                if (!this || !isActiveAndEnabled) return;
                if (success) LoadAlliance();
                else if (button != null) button.enabled = true;
            }));
        }

        private void OnPlayerSearchChanged(string query)
        {
            if (_playerSearchRoutine != null) StopCoroutine(_playerSearchRoutine);
            ClearRuntimeRows(_playerSearchRows);
            SetDropdown(playerSuggestionsDropdown, false);
            query = query?.Trim() ?? string.Empty;
            if (query.Length >= 2) _playerSearchRoutine = StartCoroutine(SearchPlayersAfterDelay(query, _requestVersion));
        }

        private IEnumerator SearchPlayersAfterDelay(string query, int version)
        {
            yield return new WaitForSeconds(0.3f);
            if (!CanApply(version) || query != invitePlayerInput.text.Trim()) yield break;
            NetworkManager network = NetworkManager.Instance;
            StartCoroutine(network.WorldPlayer.SearchPlayers(network.ActiveWorldId, query, network.JwtToken, results =>
            {
                if (!CanApply(version) || query != invitePlayerInput.text.Trim()) return;
                ClearRuntimeRows(_playerSearchRows);
                foreach (PlayerSearchResultDTO player in (results ?? new List<PlayerSearchResultDTO>()).Where(CanInviteSearchResult))
                    CreatePlayerInviteRow(player);
                SetDropdown(playerSuggestionsDropdown, _playerSearchRows.Count > 0);
            }));
        }

        private void CreatePlayerInviteRow(PlayerSearchResultDTO player)
        {
            GameObject row = CloneTemplate(playerInviteSearchRowTemplate, playerSuggestionRowsRoot, _playerSearchRows);
            SetText(Find<TMP_Text>(row.transform, "PlayerNameLabel"), player.Username);
            CarvedPressButton inviteButton = Find<CarvedPressButton>(row.transform, "InviteBtn");
            if (inviteButton == null) return;
            Guid playerId = player.WorldPlayerId;
            inviteButton.OnButtonActivatedClicked += _ => InvitePlayer(playerId, inviteButton);
        }

        private void InvitePlayer(Guid playerId, CarvedPressButton button)
        {
            if (!button.enabled) return;
            button.enabled = false;
            var dto = new InviteToAllianceDTO { WorldPlayerIdInviter = _worldPlayerId, WorldPlayerIdInvited = playerId };
            StartCoroutine(NetworkManager.Instance.Alliance.InviteToAlliance(dto, NetworkManager.Instance.JwtToken, success =>
            {
                if (!this || !isActiveAndEnabled) return;
                if (!success) { if (button != null) button.enabled = true; return; }
                invitePlayerInput.SetTextWithoutNotify(string.Empty);
                ClearRuntimeRows(_playerSearchRows);
                SetDropdown(playerSuggestionsDropdown, false);
            }));
        }

        private void LoadGeopolitics(int version)
        {
            if (_alliance == null) return;
            StartCoroutine(NetworkManager.Instance.Alliance.GetGeopolitics(_alliance.Id, NetworkManager.Instance.JwtToken, data =>
            {
                if (!CanApply(version) || data == null) return;
                _geopolitics = data;
                RenderRelations(alliesRowsRoot, friendlyAllianceRowTemplate, data.ActivePacts, _friendlyRows, false);
                RenderRelations(hostileRowsRoot, hostileAllianceRowTemplate, data.ActiveWars, _hostileRows, true);
            }));
        }

        private void RenderRelations(RectTransform root, GameObject template, IEnumerable<AllianceRelationDTO> relations,
            List<GameObject> runtimeRows, bool hostile)
        {
            ClearRuntimeRows(runtimeRows);
            if (root == null || template == null) return;
            foreach (AllianceRelationDTO relation in relations ?? Enumerable.Empty<AllianceRelationDTO>())
            {
                GameObject row = CloneTemplate(template, root, runtimeRows);
                SetText(FindTextIn(row.transform, "AllianceNameText"), $"[{relation.OtherAllianceTag}] {relation.OtherAllianceName}");
                SetText(FindTextIn(row.transform, "AgreementText"), hostile ? "War" : "Alliance");
                SetText(FindTextIn(row.transform, "PointsText"), relation.OtherAllianceTotalPoints.ToString("N0"));
                if (hostile) SetText(FindTextIn(row.transform, "WarLengthText"), FormatDuration(DateTime.UtcNow - relation.CreatedAt));
                SetUnavailableAction(Find<CarvedPressButton>(row.transform, hostile ? "RequestBtn" : "CancelBtn"));
            }
        }

        private void OnAllianceSearchChanged(string query)
        {
            if (_allianceSearchRoutine != null) StopCoroutine(_allianceSearchRoutine);
            ClearRuntimeRows(_allianceSearchRows);
            SetDropdown(allianceSuggestionsDropdown, false);
            query = query?.Trim() ?? string.Empty;
            if (query.Length >= 2 && CanManage(_currentRole))
                _allianceSearchRoutine = StartCoroutine(SearchAlliancesAfterDelay(query, _requestVersion));
        }

        private IEnumerator SearchAlliancesAfterDelay(string query, int version)
        {
            yield return new WaitForSeconds(0.3f);
            if (!CanApply(version) || query != allianceSearchInput.text.Trim()) yield break;
            NetworkManager network = NetworkManager.Instance;
            StartCoroutine(network.Alliance.SearchAlliances(network.ActiveWorldId, query, network.JwtToken, results =>
            {
                if (!CanApply(version) || query != allianceSearchInput.text.Trim()) return;
                ClearRuntimeRows(_allianceSearchRows);
                foreach (AllianceSearchResultDTO result in (results ?? new List<AllianceSearchResultDTO>()).Where(item => item.Id != _alliance.Id))
                    CreateAllianceSearchRow(result);
                SetDropdown(allianceSuggestionsDropdown, _allianceSearchRows.Count > 0);
            }));
        }

        private void CreateAllianceSearchRow(AllianceSearchResultDTO result)
        {
            GameObject row = CloneTemplate(allianceSearchRowTemplate, allianceSuggestionRowsRoot, _allianceSearchRows);
            SetText(Find<TMP_Text>(row.transform, "AllianceNameLabel"), $"[{result.Tag}] {result.Name}");
            CarvedPressButton allyButton = Find<CarvedPressButton>(row.transform, "AllyBtn");
            CarvedPressButton warButton = Find<CarvedPressButton>(row.transform, "WarBtn");
            bool activePact = HasRelation(_geopolitics?.ActivePacts, result.Id);
            bool pendingPact = HasRelation(_geopolitics?.IncomingPactInvites, result.Id) || HasRelation(_geopolitics?.OutgoingPactInvites, result.Id);
            bool activeWar = HasRelation(_geopolitics?.ActiveWars, result.Id);
            if (allyButton != null)
            {
                allyButton.gameObject.SetActive(!activePact && !pendingPact && !activeWar);
                Guid targetId = result.Id;
                allyButton.OnButtonActivatedClicked += _ => SendPact(targetId, allyButton, warButton);
            }
            if (warButton != null)
            {
                warButton.gameObject.SetActive(!activeWar);
                Guid targetId = result.Id;
                warButton.OnButtonActivatedClicked += _ => DeclareWar(targetId, allyButton, warButton);
            }
        }

        private void SendPact(Guid targetId, CarvedPressButton allyButton, CarvedPressButton warButton)
        {
            SetActionButtonsEnabled(allyButton, warButton, false);
            var dto = new SendPactInviteDTO { WorldPlayerId = _worldPlayerId, AllianceId = _alliance.Id, TargetAllianceId = targetId };
            StartCoroutine(NetworkManager.Instance.Alliance.SendPactInvite(dto, NetworkManager.Instance.JwtToken, result =>
                CompleteDiplomacyAction(result != null, allyButton, warButton)));
        }

        private void DeclareWar(Guid targetId, CarvedPressButton allyButton, CarvedPressButton warButton)
        {
            SetActionButtonsEnabled(allyButton, warButton, false);
            var dto = new DeclareWarDTO { WorldPlayerId = _worldPlayerId, AllianceId = _alliance.Id, TargetAllianceId = targetId };
            StartCoroutine(NetworkManager.Instance.Alliance.DeclareWar(dto, NetworkManager.Instance.JwtToken, result =>
                CompleteDiplomacyAction(result != null, allyButton, warButton)));
        }

        private void CompleteDiplomacyAction(bool success, CarvedPressButton allyButton, CarvedPressButton warButton)
        {
            if (!this || !isActiveAndEnabled) return;
            if (!success) { SetActionButtonsEnabled(allyButton, warButton, true); return; }
            allianceSearchInput.SetTextWithoutNotify(string.Empty);
            ClearRuntimeRows(_allianceSearchRows);
            SetDropdown(allianceSuggestionsDropdown, false);
            LoadGeopolitics(_requestVersion);
        }

        private void BindAuthoredReferences()
        {
            Transform overview = overviewContent != null ? overviewContent.transform : FindDirectContent("Overview");
            Transform allianceName = Find(overview, "AllianceName");
            if (!allianceNameText) allianceNameText = Find<TMP_Text>(allianceName, "AllianceNameText");
            if (!allianceTagText) allianceTagText = Find<TMP_Text>(allianceName, "AllianceTagText");
            if (!membersAmountText) membersAmountText = Find<TMP_Text>(Find(overview, "Members"), "MembersAmount");
            if (!pointsAmountText) pointsAmountText = Find<TMP_Text>(Find(overview, "Points"), "MembersAmount");
            if (!roleText) roleText = Find<TMP_Text>(overview, "RoleText");
            Transform bulletinBoard = Find(overview, "BulletinBoard");
            if (!descriptionText) descriptionText = Find<TMP_Text>(Find(bulletinBoard, "Description"), "Description Text");
            Transform editTransform = Find(bulletinBoard, "EditBtn");
            if (editDescriptionButton == null && editTransform != null)
            {
                editDescriptionButton = editTransform.GetComponent<Button>() ?? editTransform.gameObject.AddComponent<Button>();
                editDescriptionButton.targetGraphic = editTransform.GetComponent<Graphic>();
            }

            Transform members = membersContent != null ? membersContent.transform : FindDirectContent("Members");
            Transform membersPanel = Find(members, "Members panel");
            if (!playerDataRowTemplate)
            {
                Transform authoredMemberRow = Find(membersPanel, "PlayerDataRow");
                if (authoredMemberRow == null && membersPanel != null)
                {
                    foreach (RectTransform candidate in membersPanel.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (Find(candidate, "PlayerText") == null || Find(candidate, "PointsText") == null ||
                            Find(candidate, "CitiesText") == null || Find(candidate, "RoleText") == null) continue;
                        authoredMemberRow = candidate;
                    }
                }
                playerDataRowTemplate = authoredMemberRow ? authoredMemberRow.gameObject : null;
            }
            if (!memberRowsRoot) memberRowsRoot = playerDataRowTemplate ? playerDataRowTemplate.transform.parent as RectTransform : ResolveContent(membersPanel);
            if (!invitePlayerRoot) invitePlayerRoot = Find(members, "InvitePlayer")?.gameObject;
            if (!invitePlayerInput) invitePlayerInput = Find<TMP_InputField>(invitePlayerRoot ? invitePlayerRoot.transform : null, "InvitePlayerInputField");
            if (!playerSuggestionsDropdown) playerSuggestionsDropdown = Find(invitePlayerRoot ? invitePlayerRoot.transform : null, "PlayerSuggestionsDropdown")?.gameObject;
            if (!playerInviteSearchRowTemplate) playerInviteSearchRowTemplate = Find(playerSuggestionsDropdown ? playerSuggestionsDropdown.transform : null, "PlayerInviteSearchRow")?.gameObject;
            if (!playerSuggestionRowsRoot) playerSuggestionRowsRoot = playerInviteSearchRowTemplate ? playerInviteSearchRowTemplate.transform.parent as RectTransform : playerSuggestionsDropdown ? playerSuggestionsDropdown.transform as RectTransform : null;

            Transform politics = politicsContent != null ? politicsContent.transform : FindDirectContent("Politics");
            Transform alliesPanel = FindTrimmed(politics, "Allies Panel");
            if (!friendlyAllianceRowTemplate) friendlyAllianceRowTemplate = Find(alliesPanel, "FriendlyAllianceDataRow")?.gameObject;
            if (!alliesRowsRoot) alliesRowsRoot = friendlyAllianceRowTemplate ? friendlyAllianceRowTemplate.transform.parent as RectTransform : ResolveContent(alliesPanel);
            Transform hostilePanel = Find(politics, "Hostile Panel");
            if (!hostileAllianceRowTemplate) hostileAllianceRowTemplate = Find(hostilePanel, "HostileAllianceDataRow")?.gameObject;
            if (!hostileRowsRoot) hostileRowsRoot = hostileAllianceRowTemplate ? hostileAllianceRowTemplate.transform.parent as RectTransform : ResolveContent(hostilePanel);
            if (!politicalActionsRoot) politicalActionsRoot = Find(politics, "PoliticalActionsOutgoing")?.gameObject;
            if (!allianceSearchInput) allianceSearchInput = politicalActionsRoot ? politicalActionsRoot.GetComponentInChildren<TMP_InputField>(true) : null;
            if (!allianceSuggestionsDropdown) allianceSuggestionsDropdown = Find(politics, "AllianceSuggestionsDropdown")?.gameObject;
            if (!allianceSearchRowTemplate) allianceSearchRowTemplate = Find(allianceSuggestionsDropdown ? allianceSuggestionsDropdown.transform : null, "AllianceSearchRow")?.gameObject;
            if (!allianceSuggestionRowsRoot) allianceSuggestionRowsRoot = allianceSearchRowTemplate && allianceSearchRowTemplate.scene.IsValid()
                ? allianceSearchRowTemplate.transform.parent as RectTransform
                : allianceSuggestionsDropdown ? allianceSuggestionsDropdown.transform as RectTransform : null;
        }

        private Transform FindDirectContent(string contentName)
        {
            Transform mainContent = Find(transform, "MainContent");
            if (mainContent == null) return null;
            foreach (Transform child in mainContent)
                if (child.name.Equals(contentName, StringComparison.Ordinal)) return child;
            return null;
        }

        private void EnsureDescriptionInput()
        {
            if (descriptionText == null) return;
            RectTransform root = descriptionText.transform.parent as RectTransform;
            _descriptionInput = root.GetComponent<TMP_InputField>() ?? root.gameObject.AddComponent<TMP_InputField>();
            _descriptionInput.textViewport = root;
            _descriptionInput.textComponent = descriptionText;
            _descriptionInput.targetGraphic = descriptionText;
            _descriptionInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            _descriptionInput.characterLimit = 500;
            _descriptionInput.interactable = true;
            _descriptionInput.readOnly = true;
            _descriptionInput.richText = false;
        }

        private bool CanInviteSearchResult(PlayerSearchResultDTO player) => player != null && player.WorldPlayerId != Guid.Empty &&
            player.WorldPlayerId != _worldPlayerId && (_alliance?.Members?.All(member => member.WorldPlayerId != player.WorldPlayerId) ?? true);
        private bool CanKick(AllianceMemberDTO member) => member.WorldPlayerId != _worldPlayerId && member.Role != AllianceRoleDTO.Founder &&
            (_currentRole == AllianceRoleDTO.Founder || _currentRole == AllianceRoleDTO.Leader && member.Role == AllianceRoleDTO.Member);
        private static bool CanManage(AllianceRoleDTO role) => role == AllianceRoleDTO.Founder || role == AllianceRoleDTO.Leader;
        private static bool HasRelation(IEnumerable<AllianceRelationDTO> relations, Guid allianceId) => relations?.Any(item => item.OtherAllianceId == allianceId) == true;
        private bool CanApply(int version) => this && isActiveAndEnabled && version == _requestVersion;
        private bool DescriptionEqualsSavedState(string value) => string.Equals((value ?? string.Empty).Trim(), _lastSavedDescription, StringComparison.Ordinal);
        private static string NormalizeDescription(string value) { value ??= string.Empty; return value.Length <= 500 ? value : value.Substring(0, 500); }
        private static string FormatDuration(TimeSpan value) => value.TotalDays >= 1 ? $"{(int)value.TotalDays}d {value.Hours}h" : $"{Math.Max(0, value.Hours)}h";
        private static void SetText(TMP_Text text, string value) { if (text != null) text.text = value ?? string.Empty; }
        private static void SetTemplateState(GameObject template, bool active)
        {
            // External prefab assets are valid clone sources but are not scene objects and must not be mutated.
            if (template != null && template.scene.IsValid()) template.SetActive(active);
        }
        private static void SetDropdown(GameObject dropdown, bool active) { if (dropdown != null) dropdown.SetActive(active); }
        private static void SetUnavailableAction(CarvedPressButton button) { if (button != null) button.gameObject.SetActive(false); }
        private static void SetActionButtonsEnabled(CarvedPressButton first, CarvedPressButton second, bool enabled) { if (first != null) first.enabled = enabled; if (second != null) second.enabled = enabled; }

        private static GameObject CloneTemplate(GameObject template, RectTransform root, List<GameObject> runtimeRows)
        {
            GameObject row = Instantiate(template, root, false);
            row.name = template.name;
            row.SetActive(true);
            runtimeRows.Add(row);
            return row;
        }

        private static TMP_Text FindTextIn(Transform root, string containerName)
        {
            Transform container = Find(root, containerName);
            if (container == null) return null;

            TMP_Text text = container.GetComponent<TMP_Text>();
            return text != null ? text : container.GetComponentInChildren<TMP_Text>(true);
        }

        private static void ClearRuntimeRows(List<GameObject> rows)
        {
            foreach (GameObject row in rows) if (row != null) Destroy(row);
            rows.Clear();
        }

        private static RectTransform ResolveContent(Transform root)
        {
            if (root == null) return null;
            ScrollRect scroll = root.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null && scroll.content != null) return scroll.content;
            return Find(root, "Content") as RectTransform;
        }

        private static T Find<T>(Transform root, string name) where T : Component { Transform item = Find(root, name); return item != null ? item.GetComponent<T>() : null; }
        private static Transform FindTrimmed(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
            {
                if (child.name.Trim().Equals(name, StringComparison.Ordinal)) return child;
                Transform nested = FindTrimmed(child, name);
                if (nested != null) return nested;
            }
            return null;
        }
        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
            {
                if (child.name.Equals(name, StringComparison.Ordinal)) return child;
                Transform nested = Find(child, name);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
