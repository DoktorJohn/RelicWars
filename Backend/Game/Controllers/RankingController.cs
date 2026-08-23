using Application.Interfaces.IServices;
using Domain.Entities;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RankingController : ControllerBase
    {
        private readonly ILogger<RankingController> _logger;
        private readonly IRankingService _rankingService;

        public RankingController(ILogger<RankingController> logger, IRankingService rankingService)
        {
            _logger = logger;
            _rankingService = rankingService;
        }

        [HttpGet("ranking")]
        public async Task<IActionResult> GetRankings()
        {
            try
            {
                var result = await _rankingService.GetRankings();
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved hentning af rankings");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpGet("{worldPlayerId}/getRankingById")]
        public async Task<IActionResult> GetRankingById(Guid worldPlayerId)
        {
            try
            {
                var result = await _rankingService.GetRankingById(worldPlayerId);
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
                _logger.LogError(exception, "Fejl ved hentning af rankings for spiller {worldPlayerId}", worldPlayerId);
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

    }
}
