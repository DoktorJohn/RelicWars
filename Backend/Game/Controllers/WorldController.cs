using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorldController : ControllerBase
    {
        private readonly IWorldService _worldService;
        private readonly ILogger<WorldController> _logger;

        public WorldController(IWorldService worldService, ILogger<WorldController> logger)
        {
            _worldService = worldService; 
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
    }
}