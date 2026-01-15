using Application.DTOs;
using Application.Interfaces.IServices;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Matcher din Unity URL
    [Authorize]
    public class BarracksController : ControllerBase
    {
        private readonly IRecruitmentService _recruitmentService;
        private readonly ILogger<BarracksController> _logger;

        public BarracksController(IRecruitmentService recruitmentService, ILogger<BarracksController> logger)
        {
            _recruitmentService = recruitmentService;
            _logger = logger;
        }

        [HttpGet("{cityId}/overview")]
        public async Task<IActionResult> GetBarracksOverview(Guid cityId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _recruitmentService.GetBarracksOverviewAsync(userId, cityId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get barracks overview for city {CityId}", cityId);
                return BadRequest("Could not fetch barracks data.");
            }
        }

        [HttpPost("{cityId}/recruit")]
        public async Task<IActionResult> RecruitUnits(Guid cityId, [FromBody] RecruitUnitRequestDTO request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _recruitmentService.QueueRecruitmentAsync(userId, cityId, request.UnitType, request.Amount);

                if (result.Success)
                {
                    // RETTELSE: Brug .Message (som defineret i din record)
                    return Ok(new { Message = result.Message });
                }

                // RETTELSE: Brug .Message
                return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recruit units in city {CityId}", cityId);
                return StatusCode(500, "Internal server error.");
            }
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out Guid userId)) return userId;
            throw new UnauthorizedAccessException("Invalid Token");
        }
    }
}