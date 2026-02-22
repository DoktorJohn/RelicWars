using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorldPlayerController : ControllerBase
    {
        private readonly ILogger<WorldPlayerController> _logger;
        private readonly IWorldPlayerService _worldPlayerService;

        public WorldPlayerController(ILogger<WorldPlayerController> logger, IWorldPlayerService worldPlayerService)
        {
            _logger = logger;
            _worldPlayerService = worldPlayerService;
        }

        [HttpGet("{worldPlayerId}/getWorldPlayerProfile")]
        public async Task<IActionResult> GetWorldPlayerProfile(Guid worldPlayerId)
        {
            try
            {
                var result = await _worldPlayerService.GetWorldPlayerProfileAsync(worldPlayerId);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved hentning af worldPlayerProfile");
                return BadRequest("Kunne ikke hente data for worldPlayerProfile.");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<PlayerSearchResultDTO>>> SearchPlayers([FromQuery] Guid worldId, [FromQuery] string query)
        {
            try
            {
                var result = await _worldPlayerService.SearchPlayersAsync(worldId, query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching players");
                return BadRequest("Failed to search players");
            }
        }

        [HttpGet("{worldPlayerId}/economy")]
        public async Task<ActionResult<WorldPlayerEconomyDTO>> GetWorldPlayerEconomy(Guid worldPlayerId)
        {
            _logger.LogInformation("[WorldPlayerController] Request received for economy of player {PlayerId}", worldPlayerId);
            try
            {
                var result = await _worldPlayerService.GetWorldPlayerEconomyAsync(worldPlayerId);
                _logger.LogInformation("[WorldPlayerController] Economy retrieved for {PlayerId}. Silver: {Silver}, Rate: {Rate}", worldPlayerId, result.CurrentSilverAmount, result.SilverProductionPerHour);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("[WorldPlayerController] Player {PlayerId} not found.", worldPlayerId);
                return NotFound();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error retrieving economy data for player {PlayerId}", worldPlayerId);
                return StatusCode(500, "Internal server error retrieving economy data.");
            }
        }

        [HttpPost("selectIdeology")]
        public async Task<IActionResult> SelectIdeology([FromBody] SelectIdeologyRequest request)
        {
            try
            {
                var result = await _worldPlayerService.SelectIdeology(request);
                if (!result.ConnectionSuccessful) return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved valg af ideologi");
                return StatusCode(500, "Intern serverfejl ved valg af ideologi.");
            }
        }


        [HttpPost("join")]
        public async Task<ActionResult<WorldPlayerJoinResponse>> ProcessPlayerWorldJoinRequest([FromBody] WorldPlayerDTO request)
        {
            var result = await _worldPlayerService.AssignPlayerToGameWorldAsync(request.PlayerProfileId, request.WorldId);

            if (!result.ConnectionSuccessful)
            {
                _logger.LogWarning("Join World failed for Player {PlayerId} on World {WorldId}. Reason: {Reason}",
                    request.PlayerProfileId, request.WorldId, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Player {PlayerId} successfully accessed World {WorldId}.", request.PlayerProfileId, request.WorldId);
            return Ok(result);
        }

        [HttpPost("{worldPlayerId}/cheat")]
        public async Task<IActionResult> ApplyCheat(Guid worldPlayerId, [FromQuery] Guid cityId)
        {
            try
            {
                var success = await _worldPlayerService.ApplyAlphaCheatAsync(worldPlayerId, cityId);
                if (!success) return BadRequest("Cheat failed.");
                return Ok(new { Message = "Cheat Applied: +1000 Resources, +10 RP/IP" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying cheat");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
