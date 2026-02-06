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

        [HttpPost("deployUnitDeployment")]
        public async Task<IActionResult> DeployUnitDeployment([FromBody] DeployUnitRequestDTO dto)
        {
            try
            {
                var result = await _unitDeploymentService.DeployUnitDeploymentAsync(dto);
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

        [HttpPost("moveUnitDeployment")]
        public async Task<IActionResult> MoveUnitDeployment([FromBody] MoveUnitRequestDTO dto)
        {
            try
            {
                var result = await _unitDeploymentService.MoveUnitDeployment(dto);
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

        [HttpPost("haltUnitDeployment/{id}")]
        public async Task<IActionResult> HaltUnitDeployment(Guid id)
        {
            try
            {
                var result = await _unitDeploymentService.HaltUnitDeploymentAsync(id);
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

        [HttpPost("returnToOriginCity/{id}")]
        public async Task<IActionResult> ReturnToOriginCity(Guid id)
        {
            try
            {
                var result = await _unitDeploymentService.ReturnToOriginCityAsync(id);
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
