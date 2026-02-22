using Newtonsoft.Json;
using Project.Network.Helper;
using Project.Scripts.Domain.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
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
            string url = $"{_baseUrl}/{worldPlayerId}/conversations/{conversationId}/messages";
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

        public IEnumerator SendMessage(Guid senderId, Guid receiverId, string content, string jwtToken, Action<MessageDTO> callback, string subject = null, Guid? conversationId = null)
        {
            string url = $"{_baseUrl}/send";
            var payload = new { SenderId = senderId, ReceiverId = receiverId, Content = content, Subject = subject, ConversationId = conversationId };
            
            using (var request = BackendRequestHelper.CreatePostRequest(url, payload, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try {
                        var data = JsonConvert.DeserializeObject<MessageDTO>(request.downloadHandler.text);
                        callback?.Invoke(data);
                    } catch (Exception e) {
                        Debug.LogError($"[Messaging] Failed to deserialize sent message: {e.Message}");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Messaging] SendMessage Failed: {request.error} - {request.downloadHandler.text}");
                    callback?.Invoke(null);
                }
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
            string url = $"{_baseUrl}/{worldPlayerId}/unread-status";
            using (var request = BackendRequestHelper.CreateGetRequest(url, jwtToken))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<UnreadStatusDTO>(request.downloadHandler.text);
                        callback?.Invoke(data.HasUnread);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Messaging] Failed to deserialize unread status: {e.Message}");
                        callback?.Invoke(false);
                    }
                }
                else
                {
                    // Silent fail is better for polling
                    callback?.Invoke(false);
                }
            }
        }

        private class UnreadStatusDTO { public bool HasUnread { get; set; } }

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
