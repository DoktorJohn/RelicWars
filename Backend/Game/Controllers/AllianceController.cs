using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AllianceController : ControllerBase
    {
        private readonly IAllianceService _allianceService;
        private readonly ILogger<AllianceController> _logger;

        public AllianceController(IAllianceService allianceService, ILogger<AllianceController> logger)
        {
            _allianceService = allianceService;
            _logger = logger;
        }

        [HttpGet("getAllianceInfo/{allianceId}")]
        public async Task<IActionResult> GetAllianceInfo(Guid allianceId)
        {
            try
            {
                var result = await _allianceService.GetAllianceInfo(allianceId);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return BadRequest("Kunne ikke hente data for alliance.");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAlliance([FromBody] CreateAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.CreateAlliance(dto);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return BadRequest("Kunne ikke hente data for alliance.");
            }
        }

        [HttpPost("disband")]
        public async Task<IActionResult> DisbandAlliance([FromBody] DisbandAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.DisbandAlliance(dto);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return BadRequest("Kunne ikke hente data for alliance.");
            }
        }

        [HttpPost("inviteToAlliance")]
        public async Task<IActionResult> InviteToAlliance([FromBody] InviteToAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.InviteToAlliance(dto);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return BadRequest("Kunne ikke hente data for alliance.");
            }
        }

        [HttpPost("kickPlayer")]
        public async Task<IActionResult> KickPlayer([FromBody] KickPlayerFromAllianceDTO dto)
        {
            try
            {
                var result = await _allianceService.KickPlayer(dto);
                return Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fejl ved oprettelse af alliance");
                return BadRequest("Kunne ikke hente data for alliance.");
            }
        }



    }
}
