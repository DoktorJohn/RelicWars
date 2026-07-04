using Application.DTOs;
using Application.Interfaces.IServices;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnitDeploymentController : ControllerBase
    {
        private readonly IUnitDeploymentService _unitDeploymentService;
        private readonly ILogger<UnitDeploymentController> _logger;

        public UnitDeploymentController(IUnitDeploymentService unitDeploymentService, ILogger<UnitDeploymentController> logger)
        {
            _unitDeploymentService = unitDeploymentService;
            _logger = logger;
        }

        [HttpPost("attacks")]
        public async Task<IActionResult> AttackCityDeployment([FromBody] AttackCityDeploymentRequestDTO dto)
        {
            try
            {
                var result = await _unitDeploymentService.AttackCityDeploymentAsync(dto);
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
            catch (InvalidOperationException)
            {
                return BadRequest(new ApiError(
                    "deployment.invalid_state",
                    "Angrebet kunne ikke oprettes i den aktuelle tilstand."));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af angreb");
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpPost("supports")]
        public async Task<IActionResult> SupportCityDeployment([FromBody] SupportCityDeploymentRequestDTO dto)
        {
            try { return Ok(await _unitDeploymentService.SupportCityDeploymentAsync(dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException) { return BadRequest(new ApiError("deployment.invalid_state", "Support could not be created in the current state.")); }
        }

        [HttpPost("travel-estimate")]
        public async Task<IActionResult> EstimateTravel([FromBody] DeploymentTravelEstimateRequestDTO dto)
        {
            try { return Ok(await _unitDeploymentService.EstimateTravelAsync(dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException) { return BadRequest(new ApiError("deployment.invalid_selection", "Travel time could not be estimated for this selection.")); }
        }

        [HttpPost("{deploymentId:guid}/recall")]
        public async Task<IActionResult> Recall(Guid deploymentId)
        {
            try { return Ok(await _unitDeploymentService.RecallAsync(deploymentId)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException) { return BadRequest(new ApiError("deployment.invalid_state", "The deployment cannot be recalled.")); }
        }

        [HttpGet("worldPlayers/{worldPlayerId:guid}/deployments")]
        public async Task<IActionResult> GetActiveDeployments(Guid worldPlayerId)
        {
            try
            {
                var result = await _unitDeploymentService.GetDeploymentsAsync(worldPlayerId);
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
                _logger.LogError(exception, "Fejl ved hentning af aktive deployment-ordrer for {WorldPlayerId}", worldPlayerId);
                return StatusCode(500, new ApiError("server.error", "En intern serverfejl opstod."));
            }
        }

        [HttpGet("worldPlayers/{worldPlayerId:guid}/incoming-attacks")]
        public async Task<IActionResult> GetIncomingAttacks(Guid worldPlayerId)
        {
            try { return Ok(await _unitDeploymentService.GetIncomingAttacksAsync(worldPlayerId)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}
