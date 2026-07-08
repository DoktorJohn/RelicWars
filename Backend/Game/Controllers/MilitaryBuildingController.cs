using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Services;
using Application.Services.Buildings;
using Domain.Enums;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MilitaryBuildingController : ControllerBase
    {
        private readonly IRecruitmentService _recruitmentService;
        private readonly IWorkshopService _workshopService;
        private readonly IBarracksService _barracksService;
        private readonly IStableService _stableService;
        private readonly ILogger<MilitaryBuildingController> _logger;

        public MilitaryBuildingController(IRecruitmentService recruitmentService, 
            ILogger<MilitaryBuildingController> logger, 
            IStableService stableService, 
            IWorkshopService workshopService, 
            IBarracksService barracksService)
        {
            _recruitmentService = recruitmentService;
            _logger = logger;
            _stableService = stableService;
            _workshopService = workshopService;
            _barracksService = barracksService;
        }

        [HttpPost("recruitmentQueue")]
        public async Task<IActionResult> GetRecruitmentQueue([FromBody] GetRecruitmentQueueItemsDTO dto)
        {
            try
            {
                var queue = await _recruitmentService.GetRecruitmentQueueAsync(dto);
                return Ok(queue);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{cityId}/stableRecruit")]
        public async Task<IActionResult> StableRecruit(Guid cityId, [FromBody] RecruitUnitRequestDTO request)
        {
            try
            {
                var userId = GetUserIdFromClaims();

                // Vi genbruger QueueRecruitmentAsync, da den allerede håndterer unit-kategorier (Cavalry/Infantry)
                var result = await _recruitmentService.QueueRecruitmentAsync(userId, cityId, request.UnitType, request.Amount);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(new ApiError("recruitment.operation_failed", result.Message));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved rekruttering af kavaleri i by {OriginCityId}", cityId);
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpGet("{cityId}/stableOverview")]
        public async Task<IActionResult> GetStableOverview(Guid cityId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var result = await _stableService.GetStableOverviewAsync(userId, cityId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved hentning af stald-oversigt for by {OriginCityId}", cityId);
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpGet("{cityId}/barracksOverview")]
        public async Task<IActionResult> GetBarracksOverview(Guid cityId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var result = await _barracksService.GetBarracksOverviewAsync(userId, cityId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get barracks overview for city {OriginCityId}", cityId);
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpGet("{cityId}/workshopOverview")]
        public async Task<IActionResult> GetWorkshopOverview(Guid cityId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var result = await _workshopService.GetWorkshopOverviewAsync(userId, cityId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{cityId}/barracksRecruit")]
        public async Task<IActionResult> BarracksRecruit(Guid cityId, [FromBody] RecruitUnitRequestDTO request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var result = await _recruitmentService.QueueRecruitmentAsync(userId, cityId, request.UnitType, request.Amount);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(new ApiError("recruitment.operation_failed", result.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recruit units in city {OriginCityId}", cityId);
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpPost("{cityId}/workshopRecruit")]
        public async Task<IActionResult> WorkshopRecruit(Guid cityId, [FromBody] RecruitUnitRequestDTO request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var result = await _recruitmentService.QueueRecruitmentAsync(userId, cityId, request.UnitType, request.Amount);

                if (result.Success) return Ok(result);
                return BadRequest(new ApiError("recruitment.operation_failed", result.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
