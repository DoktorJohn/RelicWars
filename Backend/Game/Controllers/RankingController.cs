using Application.Interfaces.IServices;
using Domain.Entities;
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
                return BadRequest("Kunne ikke hente data for rankings.");
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
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved hentning af rankings for spiller {worldPlayerId}", worldPlayerId);
                return BadRequest("Kunne ikke hente data for rankings.");
            }
        }

    }
}
