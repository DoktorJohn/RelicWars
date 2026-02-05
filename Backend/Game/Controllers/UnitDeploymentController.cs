using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnitDeploymentController : ControllerBase
    {
        private readonly IUnitDeploymentService _unitDeploymentService;
        private readonly ILogger<UnitDeploymentController> _logger;

        public UnitDeploymentController(IUnitDeploymentService unitDeploymentService, ILogger<UnitDeploymentController> logger)
        {
            _unitDeploymentService = unitDeploymentService;
            _logger = logger;
        }

        [HttpPost("deployUnits")]
        public async Task<IActionResult> DeployUnits([FromBody] DeployUnitRequestDTO dto)
        {
            try
            {
                var result = await _unitDeploymentService.DeployUnitsAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved udsendelse af enheder");
                return StatusCode(500, "En intern serverfejl opstod.");
            }
        }

        [HttpPost("moveUnits")]
        public async Task<IActionResult> MoveUnits([FromBody] MoveUnitRequestDTO dto)
        {
            try
            {
                var result = await _unitDeploymentService.MoveUnits(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved udsendelse af enheder");
                return StatusCode(500, "En intern serverfejl opstod.");
            }
        }

        [HttpPost("abortUnits/{id}")]
        public async Task<IActionResult> AbortUnits(Guid id)
        {
            try
            {
                var result = await _unitDeploymentService.AbortMovementAsync(id);
                return Ok(result);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Fejl ved afbrydelse af bevægelse for enhed: {id}");
                return StatusCode(500, "En intern serverfejl opstod.");
            }
        }
    }
}
