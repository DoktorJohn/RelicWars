using Project.Modules.UI;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Project.Modules.UI.Windows.Implementations
{
    public partial class AllianceWindowController
    {
        private void LoadGeopolitics(int version)
        {
            if (version != _requestVersion || _allianceId == Guid.Empty) return;
            WindowAsyncStateHelper.ShowLoading(_incomingPacts, "Loading diplomacy...");
            WindowAsyncStateHelper.ShowLoading(_outgoingPacts, "Loading diplomacy...");
            WindowAsyncStateHelper.ShowLoading(_activePacts, "Loading diplomacy...");
            WindowAsyncStateHelper.ShowLoading(_activeWars, "Loading diplomacy...");
            StartCoroutine(NetworkManager.Instance.Alliance.GetGeopolitics(_allianceId, Token, geopolitics =>
            {
                if (version != _requestVersion || !isActiveAndEnabled)
                {
                    return;
                }

                ClearRelationLists();
                if (geopolitics == null)
                {
                    SetStatus("Could not load geopolitics.");
                    WindowAsyncStateHelper.ShowError(_incomingPacts, "Could not load geopolitics.", () => LoadGeopolitics(version));
                    WindowAsyncStateHelper.ShowEmpty(_outgoingPacts, "No outgoing pacts.");
                    WindowAsyncStateHelper.ShowEmpty(_activePacts, "No active pacts.");
                    WindowAsyncStateHelper.ShowEmpty(_activeWars, "No active wars.");
                    return;
                }

                if ((geopolitics.IncomingPactInvites?.Count ?? 0) == 0) WindowAsyncStateHelper.ShowEmpty(_incomingPacts, "No incoming pacts.");
                if ((geopolitics.OutgoingPactInvites?.Count ?? 0) == 0) WindowAsyncStateHelper.ShowEmpty(_outgoingPacts, "No outgoing pacts.");
                if ((geopolitics.ActivePacts?.Count ?? 0) == 0) WindowAsyncStateHelper.ShowEmpty(_activePacts, "No active pacts.");
                if ((geopolitics.ActiveWars?.Count ?? 0) == 0) WindowAsyncStateHelper.ShowEmpty(_activeWars, "No active wars.");
                foreach (var relation in geopolitics.IncomingPactInvites ?? new List<AllianceRelationDTO>()) _incomingPacts.Add(CreateIncomingPactRow(relation));
                foreach (var relation in geopolitics.OutgoingPactInvites ?? new List<AllianceRelationDTO>()) _outgoingPacts.Add(CreateRelationRow(relation));
                foreach (var relation in geopolitics.ActivePacts ?? new List<AllianceRelationDTO>()) _activePacts.Add(CreateRelationRow(relation));
                foreach (var relation in geopolitics.ActiveWars ?? new List<AllianceRelationDTO>()) _activeWars.Add(CreateRelationRow(relation));
            }));
        }

        private VisualElement CreateIncomingPactRow(AllianceRelationDTO relation)
        {
            var row = CreateRelationRow(relation);
            if (!_isForeignView && CanManage(_currentRole))
            {
                row.Add(SmallButton("ACCEPT", () => RespondToPact(relation.Id, true)));
                row.Add(SmallButton("DECLINE", () => RespondToPact(relation.Id, false)));
            }
            return row;
        }

        private VisualElement CreateRelationRow(AllianceRelationDTO relation)
        {
            var row = CreateRow("relation-row");
            row.Add(CreateAllianceLinkButton($"[{relation.OtherAllianceTag}] {relation.OtherAllianceName}", relation.OtherAllianceId, "member-name"));
            return row;
        }

        private void SendPact(Guid targetId)
        {
            var dto = new SendPactInviteDTO { WorldPlayerId = _worldPlayerId, AllianceId = _allianceId, TargetAllianceId = targetId };
            StartCoroutine(NetworkManager.Instance.Alliance.SendPactInvite(dto, Token, result =>
            { SetStatus(result != null ? "Pact invitation sent." : "Could not send pact invitation."); if (result != null) LoadGeopolitics(_requestVersion); }));
        }

        private void RespondToPact(Guid relationId, bool accept)
        {
            var dto = new RespondToPactInviteDTO { WorldPlayerId = _worldPlayerId, RelationId = relationId, Accept = accept };
            StartCoroutine(NetworkManager.Instance.Alliance.RespondToPactInvite(dto, Token, result =>
            { SetStatus(result != null ? (accept ? "Pact accepted." : "Pact declined.") : "Could not respond to pact."); if (result != null) LoadGeopolitics(_requestVersion); }));
        }

        private void DeclareWar(Guid targetId)
        {
            var dto = new DeclareWarDTO { WorldPlayerId = _worldPlayerId, AllianceId = _allianceId, TargetAllianceId = targetId };
            StartCoroutine(NetworkManager.Instance.Alliance.DeclareWar(dto, Token, result =>
            { SetStatus(result != null ? "War declared." : "Could not declare war."); if (result != null) LoadGeopolitics(_requestVersion); }));
        }
    }
}
