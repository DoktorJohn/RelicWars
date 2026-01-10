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
        private readonly IGameWorldService _gameWorldService;

        public GameWorldController(IGameWorldService gameWorldService)
        {
            _gameWorldService = gameWorldService;
        }

        /// <summary>
        /// Henter en liste over alle aktive spilverdener, som spillere kan tilslutte sig.
        /// </summary>
        [HttpGet("available-worlds")]
        public async Task<ActionResult<List<GameWorldAvailableResponseDTO>>> RequestAvailableGameWorldList()
        {
            var activeGameWorlds = await _gameWorldService.ObtainAllActiveGameWorldsAsync();

            return Ok(activeGameWorlds);
        }

        /// <summary>
        /// Behandler en anmodning fra en spiller om at træde ind i en specifik spilverden.
        /// Hvis spilleren ikke har en karakter i verdenen, initialiseres en ny.
        /// </summary>
        [HttpPost("join")]
        public async Task<ActionResult<WorldPlayerJoinResponse>> ProcessPlayerWorldJoinRequest([FromBody] WorldPlayerJoinDTO worldPlayerJoinRequest)
        {
            // Vi kalder servicen med spillerens profil-ID og den valgte verdens ID
            var worldJoinResult = await _gameWorldService.AssignPlayerToGameWorldAsync(
                worldPlayerJoinRequest.PlayerProfileId,
                worldPlayerJoinRequest.WorldId
            );

            // Vi tjekker nu på 'ConnectionSuccessful' fra den nye PlayerWorldJoinResponse DTO
            if (!worldJoinResult.ConnectionSuccessful)
            {
                // Returnerer 400 Bad Request med fejlbeskrivelsen, hvis noget gik galt (f.eks. profil ikke fundet)
                return BadRequest(worldJoinResult);
            }

            // Returnerer 200 OK med data om spillerens landingpage-by
            return Ok(worldJoinResult);
        }
    }
}