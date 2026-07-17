using Application.DTOs;
using Application.Services;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CombatSimulatorController : ControllerBase
    {
        private readonly CombatSimulatorService _combatSimulatorService;
        private readonly ILogger<CombatSimulatorController> _logger;

        public CombatSimulatorController(CombatSimulatorService combatSimulatorService, ILogger<CombatSimulatorController> logger)
        {
            _combatSimulatorService = combatSimulatorService;
            _logger = logger;
        }

        [HttpPost("simulate")]
        public async Task<ActionResult<CombatSimulationResultDTO>> Simulate([FromBody] CombatSimulationRequestDTO request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var result = await _combatSimulatorService.SimulateBattleAsync(userId, request);
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
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Combat simulator request rejected.");
                return BadRequest(new ApiError("combat_simulator.invalid_state", exception.Message));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected combat simulator error.");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
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
