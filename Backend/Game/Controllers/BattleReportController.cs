using Application.Interfaces.IServices;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BattleReportController : ControllerBase
    {
        private readonly IBattleReportService _battleReportService;
        private readonly ILogger<BattleReportController> _logger;

        public BattleReportController(IBattleReportService battleReportService, ILogger<BattleReportController> logger)
        {
            _battleReportService = battleReportService;
            _logger = logger;
        }

        [HttpGet("{worldPlayerId}/reports")]
        public async Task<IActionResult> GetBattleReports(Guid worldPlayerId)
        {
            try
            {
                var result = await _battleReportService.GetBattleReportsAsync(worldPlayerId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception exception)
            {
                return HandleException(exception, "Fejl ved hentning af reports");
            }
        }

        [HttpGet("{worldPlayerId}/unread-status")]
        public async Task<IActionResult> GetUnreadStatus(Guid worldPlayerId)
        {
            try
            {
                var result = await _battleReportService.GetUnreadStatusAsync(worldPlayerId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception exception)
            {
                return HandleException(exception, "Fejl ved hentning af report unread status");
            }
        }

        [HttpPut("{worldPlayerId}/reports/{battleReportId}/read")]
        public async Task<IActionResult> MarkAsRead(Guid worldPlayerId, Guid battleReportId)
        {
            try
            {
                await _battleReportService.MarkBattleReportAsReadAsync(worldPlayerId, battleReportId);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest(new ApiError("request.invalid", "Anmodningen er ugyldig."));
            }
            catch (InvalidOperationException)
            {
                return Conflict(new ApiError("resource.conflict", "Handlingen er i konflikt med den aktuelle tilstand."));
            }
            catch (Exception exception)
            {
                return HandleException(exception, "Fejl ved markering af report som læst");
            }
        }

        [HttpDelete("{worldPlayerId}/reports/{battleReportId}")]
        public async Task<IActionResult> DeleteReport(Guid worldPlayerId, Guid battleReportId)
        {
            try
            {
                await _battleReportService.DeleteBattleReportAsync(worldPlayerId, battleReportId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest(new ApiError("request.invalid", "Anmodningen er ugyldig."));
            }
            catch (InvalidOperationException)
            {
                return Conflict(new ApiError("resource.conflict", "Handlingen er i konflikt med den aktuelle tilstand."));
            }
            catch (Exception exception)
            {
                return HandleException(exception, "Fejl ved sletning af report");
            }
        }

        private IActionResult HandleException(Exception exception, string logMessage)
        {
            _logger.LogError(exception, logMessage);

            return exception switch
            {
                ArgumentException => BadRequest(new ApiError("request.invalid", "Anmodningen er ugyldig.")),
                KeyNotFoundException => NotFound(),
                InvalidOperationException => Conflict(new ApiError("resource.conflict", "Handlingen er i konflikt med den aktuelle tilstand.")),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ApiError("server.error", "En intern serverfejl opstod."))
            };
        }
    }
}
