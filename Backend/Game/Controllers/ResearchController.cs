using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ResearchController : ControllerBase
    {
        private readonly IResearchService _researchService;

        public ResearchController(IResearchService researchService)
        {
            _researchService = researchService;
        }

        [HttpGet("tree/{worldPlayerId}")]
        public async Task<ActionResult<ResearchTreeDTO>> GetResearchTree(Guid worldPlayerId)
        {
            var result = await _researchService.GetResearchTreeAsync(worldPlayerId);
            return Ok(result);
        }

        [HttpPost("start/{worldPlayerId}/{researchId}")]
        public async Task<IActionResult> StartResearch(Guid worldPlayerId, string researchId)
        {
            var result = await _researchService.QueueResearchAsync(worldPlayerId, researchId);

            if (result.Success) return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpPost("cancel/{worldPlayerId}/{jobId}")]
        public async Task<IActionResult> CancelResearch(Guid worldPlayerId, Guid jobId)
        {
            var result = await _researchService.CancelResearchAsync(worldPlayerId, jobId);

            if (result.Success) return Ok(result);
            return BadRequest(result.Message);
        }
    }
}