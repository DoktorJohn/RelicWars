using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagingController : ControllerBase
    {
        private readonly IMessagingService _messagingService;
        private readonly IWorldPlayerRepository _worldPlayerRepository;
        private readonly ILogger<MessagingController> _logger;

        public MessagingController(
            IMessagingService messagingService,
            IWorldPlayerRepository worldPlayerRepository,
            ILogger<MessagingController> logger)
        {
            _messagingService = messagingService;
            _worldPlayerRepository = worldPlayerRepository;
            _logger = logger;
        }

        [HttpPost("{worldPlayerId}/conversations")]
        public async Task<IActionResult> StartConversation(Guid worldPlayerId, [FromBody] StartConversationRequestDTO request)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                var participantIds = request.ParticipantWorldPlayerIds?.Count > 0
                    ? request.ParticipantWorldPlayerIds
                    : new List<Guid> { request.ReceiverWorldPlayerId };
                var result = await _messagingService.StartConversationAsync(
                    validatedWorldPlayerId,
                    participantIds,
                    request.Subject,
                    request.Content);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error starting conversation");
            }
        }

        [HttpPost("{worldPlayerId}/conversations/{conversationId}/messages")]
        public async Task<IActionResult> ReplyToConversation(Guid worldPlayerId, Guid conversationId, [FromBody] ReplyMessageRequestDTO request)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                var result = await _messagingService.ReplyToConversationAsync(validatedWorldPlayerId, conversationId, request.Content);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error sending message");
            }
        }

        [HttpGet("{worldPlayerId}/conversations")]
        public async Task<IActionResult> GetConversations(Guid worldPlayerId)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                var result = await _messagingService.GetConversationsAsync(validatedWorldPlayerId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error getting conversations");
            }
        }

        [HttpGet("{worldPlayerId}/conversations/{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(Guid worldPlayerId, Guid conversationId, [FromQuery] DateTime? before, [FromQuery] int take = 50)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                var result = await _messagingService.GetMessagesAsync(conversationId, validatedWorldPlayerId, before, take);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error getting messages");
            }
        }

        [HttpPut("{worldPlayerId}/conversations/{conversationId}/read")]
        public async Task<IActionResult> MarkConversationAsRead(Guid worldPlayerId, Guid conversationId)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                await _messagingService.MarkConversationAsReadAsync(conversationId, validatedWorldPlayerId);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error marking conversation as read");
            }
        }

        [HttpPut("{worldPlayerId}/messages/{messageId}/read")]
        public async Task<IActionResult> MarkMessageAsRead(Guid worldPlayerId, Guid messageId)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                await _messagingService.MarkMessageAsReadAsync(messageId, validatedWorldPlayerId);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error marking message as read");
            }
        }

        [HttpDelete("{worldPlayerId}/conversations/{conversationId}")]
        public async Task<IActionResult> DeleteConversation(Guid worldPlayerId, Guid conversationId)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                await _messagingService.DeleteConversationAsync(conversationId, validatedWorldPlayerId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error deleting conversation for player");
            }
        }

        [HttpGet("search/{worldId}")]
        public async Task<IActionResult> SearchPlayers(Guid worldId, [FromQuery] string query)
        {
            try
            {
                await ValidateWorldMembershipAsync(worldId);
                var result = await _messagingService.SearchPlayersAsync(worldId, query);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error searching players");
            }
        }

        [HttpGet("{worldPlayerId}/unread-status")]
        public async Task<IActionResult> HasUnreadMessages(Guid worldPlayerId)
        {
            try
            {
                var validatedWorldPlayerId = await ValidateWorldPlayerOwnershipAsync(worldPlayerId);
                int unreadCount = await _messagingService.CountUnreadMessagesAsync(validatedWorldPlayerId);
                return Ok(new { hasUnread = unreadCount > 0, unreadCount });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return HandleMessagingException(ex, "Error checking unread messages");
            }
        }

        private IActionResult HandleMessagingException(Exception exception, string logMessage)
        {
            _logger.LogError(exception, logMessage);

            return exception switch
            {
                ArgumentException => BadRequest(new ApiError("request.invalid", "Anmodningen er ugyldig.")),
                KeyNotFoundException => NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet.")),
                InvalidOperationException => Conflict(new ApiError("resource.conflict", "Handlingen er i konflikt med den aktuelle tilstand.")),
                _ => StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."))
            };
        }

        private async Task<Guid> ValidateWorldPlayerOwnershipAsync(Guid worldPlayerId)
        {
            var profileId = GetAuthenticatedProfileId();
            var worldPlayer = await _worldPlayerRepository.GetByIdAsync(worldPlayerId);

            if (worldPlayer == null || worldPlayer.PlayerProfileId != profileId)
            {
                throw new UnauthorizedAccessException("World player does not belong to the authenticated profile.");
            }

            return worldPlayer.Id;
        }

        private async Task ValidateWorldMembershipAsync(Guid worldId)
        {
            var profileId = GetAuthenticatedProfileId();
            var worldPlayer = await _worldPlayerRepository.GetByProfileAndWorldAsync(profileId, worldId);

            if (worldPlayer == null)
            {
                throw new UnauthorizedAccessException("Authenticated profile is not a member of this world.");
            }
        }

        private Guid GetAuthenticatedProfileId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idClaim, out Guid profileId))
            {
                throw new UnauthorizedAccessException("Invalid authenticated profile id.");
            }

            return profileId;
        }
    }
}
