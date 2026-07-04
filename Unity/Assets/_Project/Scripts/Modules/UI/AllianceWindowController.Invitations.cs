using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public partial class AllianceWindowController
    {
        private void LoadInvitations(int version)
        {
            if (version != _requestVersion) return;
            WindowAsyncStateHelper.ShowLoading(_invitationList, "Loading invitations...");
            StartCoroutine(NetworkManager.Instance.Alliance.GetInvitations(_worldPlayerId, Token, invitations =>
            {
                if (version != _requestVersion || !isActiveAndEnabled)
                {
                    return;
                }

                if (invitations == null)
                {
                    WindowAsyncStateHelper.ShowError(_invitationList, "Could not load invitations.", () => LoadInvitations(version));
                    return;
                }

                if (invitations.Count == 0)
                {
                    WindowAsyncStateHelper.ShowEmpty(_invitationList, "No alliance invitations.");
                    return;
                }

                _invitationList.Clear();
                foreach (var invitation in invitations) _invitationList.Add(CreateInvitationRow(invitation));
            }));
        }

        private VisualElement CreateInvitationRow(AllianceInvitationDTO invitation)
        {
            var row = CreateRow("invitation-row");
            row.Add(CreateAllianceLinkButton($"[{invitation.AllianceTag}] {invitation.AllianceName}", invitation.AllianceId, "member-name"));
            row.Add(new Label("invited by"));
            row.Add(CreatePlayerLinkButton(invitation.InvitedByUserName, invitation.InvitedByWorldPlayerId, "invitation-name"));
            row.Add(SmallButton("ACCEPT", () => Respond(invitation.Id, true)));
            row.Add(SmallButton("DECLINE", () => Respond(invitation.Id, false)));
            return row;
        }

        private void Respond(Guid invitationId, bool accept)
        {
            var dto = new RespondToAllianceInvitationDTO { WorldPlayerId = _worldPlayerId, InvitationId = invitationId };
            if (accept) StartCoroutine(NetworkManager.Instance.Alliance.AcceptInvitation(dto, Token, alliance =>
            { if (alliance != null) RenderAlliance(alliance, _requestVersion); else SetError("Could not accept invitation."); }));
            else StartCoroutine(NetworkManager.Instance.Alliance.DeclineInvitation(dto, Token, success =>
            { if (success) LoadInvitations(_requestVersion); else SetError("Could not decline invitation."); }));
        }

        private void CreateAlliance()
        {
            var name = _nameInput.value.Trim(); var tag = _tagInput.value.Trim();
            if (name.Length < 3 || tag.Length < 3) { SetError("Name and tag must contain at least 3 characters."); return; }
            _createButton.SetEnabled(false); SetError("Creating...");
            StartCoroutine(NetworkManager.Instance.Alliance.CreateAlliance(new CreateAllianceDTO
            { WorldPlayerIdFounder = _worldPlayerId, Name = name, Tag = tag }, Token, alliance =>
            {
                _createButton.SetEnabled(true);
                if (alliance != null) RenderAlliance(alliance, _requestVersion); else SetError("Could not create alliance.");
            }));
        }
    }
}
