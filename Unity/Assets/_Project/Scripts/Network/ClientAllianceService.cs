using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Project.Network
{
    public class ClientAllianceService
    {
        private readonly string _baseUrl;

        public ClientAllianceService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/Alliance";
        }

        public IEnumerator GetAllianceInfo(Guid allianceId, string jwtToken, Action<AllianceDTO> callback)
        {
            string url = $"{_baseUrl}/getAllianceInfo/{allianceId}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "Alliance");
            }
        }

        public IEnumerator CreateAlliance(CreateAllianceDTO dto, string jwtToken, Action<AllianceDTO> callback)
        {
            string url = $"{_baseUrl}/create";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "Alliance");
            }
        }

        public IEnumerator DisbandAlliance(DisbandAllianceDTO dto, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/disband";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return BackendRequestHelper.SendCommand(
                    request,
                    callback,
                    _ => true,
                    _ => false,
                    "Alliance");
            }
        }

        public IEnumerator InviteToAlliance(InviteToAllianceDTO dto, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/inviteToAlliance";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return BackendRequestHelper.SendCommand(
                    request,
                    callback,
                    _ => true,
                    _ => false,
                    "Alliance");
            }
        }

        public IEnumerator KickPlayer(KickPlayerFromAllianceDTO dto, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/kickPlayer";

            using (var request = BackendRequestHelper.CreatePostRequest(url, dto, jwtToken))
            {
                yield return BackendRequestHelper.SendCommand(
                    request,
                    callback,
                    _ => true,
                    _ => false,
                    "Alliance");
            }
        }

        public IEnumerator GetInvitations(Guid worldPlayerId, string jwtToken, Action<List<AllianceInvitationDTO>> callback)
        {
            using (var request = BackendRequestHelper.CreateGetRequest($"{_baseUrl}/{worldPlayerId}/invitations", jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "Alliance", _ => new List<AllianceInvitationDTO>());
            }
        }

        public IEnumerator AcceptInvitation(RespondToAllianceInvitationDTO dto, string jwtToken, Action<AllianceDTO> callback)
        {
            using (var request = BackendRequestHelper.CreatePostRequest($"{_baseUrl}/invitations/accept", dto, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "Alliance");
            }
        }

        public IEnumerator DeclineInvitation(RespondToAllianceInvitationDTO dto, string jwtToken, Action<bool> callback)
        {
            yield return SendBooleanCommand("invitations/decline", dto, jwtToken, callback);
        }

        public IEnumerator LeaveAlliance(LeaveAllianceDTO dto, string jwtToken, Action<bool> callback)
        {
            yield return SendBooleanCommand("leave", dto, jwtToken, callback);
        }

        public IEnumerator SetMemberRole(SetAllianceMemberRoleDTO dto, string jwtToken, Action<AllianceDTO> callback)
        {
            yield return SendJsonCommand("members/role", dto, jwtToken, callback);
        }

        public IEnumerator UpdateDescription(UpdateAllianceDescriptionDTO dto, string jwtToken, Action<AllianceDTO> callback)
        {
            yield return SendJsonCommand("description", dto, jwtToken, callback);
        }

        public IEnumerator SearchAlliances(Guid worldId, string query, string jwtToken, Action<List<AllianceSearchResultDTO>> callback)
        {
            var url = $"{_baseUrl}/search?worldId={worldId}&query={UnityWebRequest.EscapeURL(query)}";

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "Alliance", _ => new List<AllianceSearchResultDTO>());
            }
        }

        public IEnumerator GetGeopolitics(Guid allianceId, string jwtToken, Action<AllianceGeopoliticsDTO> callback)
        {
            using (var request = BackendRequestHelper.CreateGetRequest($"{_baseUrl}/{allianceId}/geopolitics", jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "Alliance");
            }
        }

        public IEnumerator SendPactInvite(SendPactInviteDTO dto, string jwtToken, Action<AllianceRelationDTO> callback)
        {
            yield return SendJsonCommand("pact-invite", dto, jwtToken, callback);
        }

        public IEnumerator RespondToPactInvite(RespondToPactInviteDTO dto, string jwtToken, Action<AllianceRelationDTO> callback)
        {
            yield return SendJsonCommand("pact-invite/respond", dto, jwtToken, callback);
        }

        public IEnumerator DeclareWar(DeclareWarDTO dto, string jwtToken, Action<AllianceRelationDTO> callback)
        {
            yield return SendJsonCommand("declare-war", dto, jwtToken, callback);
        }

        private IEnumerator SendJsonCommand<TResponse>(string path, object dto, string jwtToken, Action<TResponse> callback)
            where TResponse : class
        {
            using (var request = BackendRequestHelper.CreatePostRequest($"{_baseUrl}/{path}", dto, jwtToken))
            {
                yield return BackendRequestHelper.SendJson(request, callback, "Alliance");
            }
        }

        private IEnumerator SendBooleanCommand(string path, object dto, string jwtToken, Action<bool> callback)
        {
            using (var request = BackendRequestHelper.CreatePostRequest($"{_baseUrl}/{path}", dto, jwtToken))
            {
                yield return BackendRequestHelper.SendCommand(
                    request,
                    callback,
                    _ => true,
                    _ => false,
                    "Alliance");
            }
        }
    }
}
