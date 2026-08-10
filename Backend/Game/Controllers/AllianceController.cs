using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AllianceController : ControllerBase
    {
        private readonly IAllianceService _allianceService;
        private readonly ILogger<AllianceController> _logger;

        public AllianceController(IAllianceService allianceService, ILogger<AllianceController> logger)
        {
            _allianceService = allianceService;
            _logger = logger;
        }

        [HttpGet("getAllianceInfo/{allianceId}")]
        public async Task<IActionResult> GetAllianceInfo(Guid allianceId)
        {
            try
            {
                var result = await _allianceService.GetAllianceInfo(allianceId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAlliance([FromBody] CreateAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.CreateAlliance(dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new ApiError("request.invalid", exception.Message));
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new ApiError("alliance.conflict", exception.Message));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpPost("disband")]
        public async Task<IActionResult> DisbandAlliance([FromBody] DisbandAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.DisbandAlliance(dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpPost("inviteToAlliance")]
        public async Task<IActionResult> InviteToAlliance([FromBody] InviteToAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.InviteToAlliance(dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpPost("kickPlayer")]
        public async Task<IActionResult> KickPlayer([FromBody] KickPlayerFromAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.KickPlayer(dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpGet("{worldPlayerId}/invitations")]
        public async Task<ActionResult<List<AllianceInvitationDTO>>> GetInvitations(Guid worldPlayerId) =>
            Ok(await _allianceService.GetInvitations(worldPlayerId));

        [HttpGet("{worldPlayerId}/invited-players")]
        public async Task<ActionResult<List<AllianceInvitedPlayerDTO>>> GetInvitedPlayers(Guid worldPlayerId) =>
            Ok(await _allianceService.GetInvitedPlayers(worldPlayerId));

        [HttpPost("invitations/cancel")]
        public async Task<ActionResult<bool>> CancelInvitation([FromBody] CancelAllianceInvitationDTO dto) =>
            Ok(await _allianceService.CancelInvitation(dto));

        [HttpPost("invitations/accept")]
        public async Task<ActionResult<AllianceDTO>> AcceptInvitation([FromBody] RespondToAllianceInvitationDTO dto) =>
            Ok(await _allianceService.AcceptInvitation(dto));

        [HttpPost("invitations/decline")]
        public async Task<ActionResult<bool>> DeclineInvitation([FromBody] RespondToAllianceInvitationDTO dto) =>
            Ok(await _allianceService.DeclineInvitation(dto));

        [HttpPost("leave")]
        public async Task<ActionResult<bool>> LeaveAlliance([FromBody] LeaveAllianceDTO dto) =>
            Ok(await _allianceService.LeaveAlliance(dto));

        [HttpPost("members/role")]
        public async Task<ActionResult<AllianceDTO>> SetMemberRole([FromBody] SetAllianceMemberRoleDTO dto) =>
            Ok(await _allianceService.SetMemberRole(dto));

        [HttpPost("description")]
        public async Task<ActionResult<AllianceDTO>> UpdateDescription([FromBody] UpdateAllianceDescriptionDTO dto) =>
            Ok(await _allianceService.UpdateDescription(dto));

        [HttpGet("search")]
        public async Task<ActionResult<List<AllianceSearchResultDTO>>> SearchAlliances([FromQuery] Guid worldId, [FromQuery] string query) =>
            Ok(await _allianceService.SearchAlliances(worldId, query));

        [HttpGet("{allianceId}/geopolitics")]
        public async Task<ActionResult<AllianceGeopoliticsDTO>> GetGeopolitics(Guid allianceId) =>
            Ok(await _allianceService.GetGeopolitics(allianceId));

        [HttpPost("pact-invite")]
        public async Task<ActionResult<AllianceRelationDTO>> SendPactInvite([FromBody] SendPactInviteDTO dto) =>
            Ok(await _allianceService.SendPactInvite(dto));

        [HttpPost("pact-invite/respond")]
        public async Task<ActionResult<AllianceRelationDTO>> RespondToPactInvite([FromBody] RespondToPactInviteDTO dto) =>
            Ok(await _allianceService.RespondToPactInvite(dto));

        [HttpPost("declare-war")]
        public async Task<ActionResult<AllianceRelationDTO>> DeclareWar([FromBody] DeclareWarDTO dto) =>
            Ok(await _allianceService.DeclareWar(dto));



    }
}
