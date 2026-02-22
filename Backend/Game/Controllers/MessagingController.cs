using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagingController : ControllerBase
    {
        private readonly IMessagingService _messagingService;
        private readonly ILogger<MessagingController> _logger;

        public MessagingController(IMessagingService messagingService, ILogger<MessagingController> logger)
        {
            _messagingService = messagingService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var result = await _messagingService.SendMessageAsync(request.SenderId, request.ReceiverId, request.Content, request.Subject, request.ConversationId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{worldPlayerId}/conversations")]
        public async Task<IActionResult> GetConversations(Guid worldPlayerId)
        {
            try
            {
                var result = await _messagingService.GetConversationsAsync(worldPlayerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{worldPlayerId}/conversations/{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(Guid worldPlayerId, Guid conversationId)
        {
            try
            {
                var result = await _messagingService.GetMessagesAsync(conversationId, worldPlayerId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{worldPlayerId}/messages/{messageId}/read")]
        public async Task<IActionResult> MarkMessageAsRead(Guid worldPlayerId, Guid messageId)
        {
            try
            {
                await _messagingService.MarkMessageAsReadAsync(messageId, worldPlayerId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("search/{worldId}")]
        public async Task<IActionResult> SearchPlayers(Guid worldId, [FromQuery] string query)
        {
            try
            {
                var result = await _messagingService.SearchPlayersAsync(worldId, query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching players");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{worldPlayerId}/unread-status")]
        public async Task<IActionResult> HasUnreadMessages(Guid worldPlayerId)
        {
            try
            {
                bool hasUnread = await _messagingService.HasUnreadMessagesAsync(worldPlayerId);
                return Ok(new { hasUnread });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking unread messages");
                return BadRequest(ex.Message);
            }
        }
    }

    public class SendMessageRequest
    {
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public Guid? ConversationId { get; set; }
    }
}
