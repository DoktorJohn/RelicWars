using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameWorldController : ControllerBase
    {
        private readonly IWorldService _worldService;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly ILogger<GameWorldController> _logger;

        public GameWorldController(IWorldService worldService, IWorldPlayerService worldPlayerService, ILogger<GameWorldController> logger)
        {
            _worldService = worldService;
            _worldPlayerService = worldPlayerService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a list of all active game worlds available for players to join.
        /// </summary>
        [HttpGet("available-worlds")]
        public async Task<ActionResult<List<WorldAvailableResponseDTO>>> RequestAvailableGameWorldList()
        {
            var activeGameWorlds = await _worldService.ObtainAllActiveGameWorldsAsync();
            return Ok(activeGameWorlds);
        }

        /// <summary>
        /// Processes a player's request to join a specific game world, initializing character and city if necessary.
        /// </summary>
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