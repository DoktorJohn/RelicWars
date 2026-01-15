using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Dette fjerner 404 fejlen
    [Authorize]
    public class StableController : ControllerBase
    {
        private readonly IRecruitmentService _recruitmentService;
        private readonly ILogger<StableController> _logger;

        public StableController(IRecruitmentService recruitmentService, ILogger<StableController> logger)
        {
            _recruitmentService = recruitmentService;
            _logger = logger;
        }

        [HttpGet("{cityId}/overview")]
        public async Task<IActionResult> GetStableOverview(Guid cityId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var result = await _recruitmentService.GetStableOverviewAsync(userId, cityId);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved hentning af stald-oversigt for by {CityId}", cityId);
                return BadRequest("Kunne ikke hente data for stalden.");
            }
        }

        [HttpPost("{cityId}/recruit")]
        public async Task<IActionResult> RecruitCavalry(Guid cityId, [FromBody] RecruitUnitRequestDTO request)
        {
            try
            {
                var userId = GetUserIdFromClaims();

                // Vi genbruger QueueRecruitmentAsync, da den allerede håndterer unit-kategorier (Cavalry/Infantry)
                var result = await _recruitmentService.QueueRecruitmentAsync(userId, cityId, request.UnitType, request.Amount);

                if (result.Success)
                {
                    return Ok(new { Message = result.Message });
                }

                return BadRequest(result.Message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved rekruttering af kavaleri i by {CityId}", cityId);
                return StatusCode(500, "Intern serverfejl under rekruttering.");
            }
        }

        private Guid GetUserIdFromClaims()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out Guid userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Ugyldigt bruger-ID i token.");
        }
    }
}