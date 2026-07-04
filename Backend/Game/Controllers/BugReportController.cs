using Application.Interfaces.IServices;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BugReportController : ControllerBase
    {
        private readonly IBugReportService _bugReportService;

        public BugReportController(IBugReportService bugReportService)
        {
            _bugReportService = bugReportService;
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitBugReportRequest request)
        {
            var result = await _bugReportService.SubmitAsync(request.Description);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
