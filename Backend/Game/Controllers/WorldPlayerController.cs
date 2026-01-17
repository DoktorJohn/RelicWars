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
    }
}
