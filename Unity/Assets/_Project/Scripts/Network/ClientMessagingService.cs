using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Network
{
    public class ClientMessagingService
    {
        private readonly string _baseUrl;

        public ClientMessagingService(string baseUrl)
        {
            _baseUrl = $"{baseUrl}/Messaging";
        }

        public IEnumerator GetConversations(Guid worldPlayerId, string jwtToken, Action<List<ConversationDTO>> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/conversations";
            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try {
                        var data = JsonConvert.DeserializeObject<List<ConversationDTO>>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception e) {
                        Debug.LogError($"[Messaging] Failed to deserialize conversations: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Messaging] GetConversations Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator GetMessages(Guid worldPlayerId, Guid conversationId, string jwtToken, Action<List<MessageDTO>> callback)
        {
            return GetMessages(worldPlayerId, conversationId, null, 50, jwtToken, callback);
        }

        public IEnumerator GetMessages(Guid worldPlayerId, Guid conversationId, DateTime? before, int take, string jwtToken, Action<List<MessageDTO>> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/conversations/{conversationId}/messages";
            var queryParts = new List<string>();
            if (before.HasValue)
            {
                queryParts.Add($"before={UnityWebRequest.EscapeURL(before.Value.ToUniversalTime().ToString("O"))}");
            }
            if (take > 0)
            {
                queryParts.Add($"take={take}");
            }
            if (queryParts.Count > 0)
            {
                url += "?" + string.Join("&", queryParts);
            }

            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try {
                        var data = JsonConvert.DeserializeObject<List<MessageDTO>>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    } catch (Exception e) {
                        Debug.LogError($"[Messaging] Failed to deserialize messages: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Messaging] GetMessages Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator StartConversation(Guid senderId, Guid receiverId, string subject, string content, string jwtToken, Action<ConversationDTO> callback)
        {
            return StartConversation(senderId, new[] { receiverId }, subject, content, jwtToken, callback);
        }

        public IEnumerator StartConversation(Guid senderId, IEnumerable<Guid> receiverIds, string subject, string content, string jwtToken, Action<ConversationDTO> callback)
        {
            return StartConversation(senderId, receiverIds, subject, content, null, jwtToken, callback);
        }

        public IEnumerator StartConversation(Guid senderId, IEnumerable<Guid> receiverIds, string subject, string content, Guid? battleReportId, string jwtToken, Action<ConversationDTO> callback)
        {
            string url = $"{_baseUrl}/{senderId}/conversations";
            var recipientList = receiverIds?.Where(id => id != Guid.Empty).ToList() ?? new List<Guid>();
            var payload = new
            {
                ReceiverWorldPlayerId = recipientList.FirstOrDefault(),
                ParticipantWorldPlayerIds = recipientList,
                Subject = subject,
                Content = content,
                BattleReportId = battleReportId
            };
            
            using (var request = BackendRequestHelper.CreatePostRequest(url, payload, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try {
                        var data = JsonConvert.DeserializeObject<ConversationDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    } catch (Exception e) {
                        Debug.LogError($"[Messaging] Failed to deserialize started conversation: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Messaging] StartConversation Failed: {request.error} - {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator ReplyToConversation(Guid senderId, Guid conversationId, string content, string jwtToken, Action<MessageDTO> callback)
        {
            return ReplyToConversation(senderId, conversationId, content, null, jwtToken, callback);
        }

        public IEnumerator ReplyToConversation(Guid senderId, Guid conversationId, string content, Guid? battleReportId, string jwtToken, Action<MessageDTO> callback)
        {
            string url = $"{_baseUrl}/{senderId}/conversations/{conversationId}/messages";
            var payload = new { Content = content, BattleReportId = battleReportId };

            using (var request = BackendRequestHelper.CreatePostRequest(url, payload, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try {
                        var data = JsonConvert.DeserializeObject<MessageDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    } catch (Exception e) {
                        Debug.LogError($"[Messaging] Failed to deserialize reply message: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Messaging] ReplyToConversation Failed: {request.error} - {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

        public IEnumerator MarkConversationAsRead(Guid worldPlayerId, Guid conversationId, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/conversations/{conversationId}/read";
            using (var request = BackendRequestHelper.CreatePutRequest(url, null, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Messaging] MarkConversationAsRead Failed: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator DeleteConversation(Guid worldPlayerId, Guid conversationId, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/conversations/{conversationId}";
            using (var request = UnityWebRequest.Delete(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
                yield return request.SendWebRequest();
                bool success = request.result == UnityWebRequest.Result.Success;
                if (!success) Debug.LogError($"[Messaging] DeleteConversation Failed: {request.error}");
                callback?.Invoke(success);
            }
        }

        public IEnumerator MarkAsRead(Guid worldPlayerId, Guid messageId, string jwtToken, Action<bool> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/messages/{messageId}/read";
            using (var request = BackendRequestHelper.CreatePutRequest(url, null, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Messaging] MarkAsRead Failed: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator HasUnreadMessages(Guid worldPlayerId, string jwtToken, Action<bool> callback)
        {
            return GetUnreadMessageCount(worldPlayerId, jwtToken, count => callback?.Invoke(count > 0));
        }

        public IEnumerator GetUnreadMessageCount(Guid worldPlayerId, string jwtToken, Action<int> callback)
        {
            string url = $"{_baseUrl}/{worldPlayerId}/unread-status";
            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<UnreadStatusDTO>(request.downloadHandler.text);
                        callback?.Invoke(Math.Max(0, data.UnreadCount));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Messaging] Failed to deserialize unread status: {e.Message}");
                        callback?.Invoke(0);
                    }
                }
                else
                {
                    // Silent fail is better for polling
                    callback?.Invoke(0);
                }
            }
        }

        private class UnreadStatusDTO
        {
            public bool HasUnread { get; set; }
            public int UnreadCount { get; set; }
        }

        public IEnumerator SearchPlayers(Guid worldId, string query, string jwtToken, Action<List<PlayerSearchResultDTO>> callback)
        {
            string url = $"{_baseUrl}/search/{worldId}?query={UnityWebRequest.EscapeURL(query)}";
            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<List<PlayerSearchResultDTO>>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Messaging] Failed to deserialize search results: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Messaging] SearchPlayers Failed: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}
