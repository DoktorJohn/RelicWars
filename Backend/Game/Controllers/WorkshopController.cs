using Application.DTOs;
using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkshopController : ControllerBase
    {
        private readonly IRecruitmentService _recruitmentService;

        public WorkshopController(IRecruitmentService recruitmentService)
        {
            _recruitmentService = recruitmentService;
        }

        [HttpGet("{cityId}/overview")]
        public async Task<IActionResult> GetWorkshopOverview(Guid cityId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var result = await _recruitmentService.GetWorkshopOverviewAsync(userId, cityId);
            return Ok(result);
        }

        [HttpPost("{cityId}/recruit")]
        public async Task<IActionResult> RecruitSiege(Guid cityId, [FromBody] RecruitUnitRequestDTO request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var result = await _recruitmentService.QueueRecruitmentAsync(userId, cityId, request.UnitType, request.Amount);

            if (result.Success) return Ok(new { Message = result.Message });
            return BadRequest(result.Message);
        }
    }
}