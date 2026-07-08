using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Services;
using Azure.Core;
using Domain.Entities;
using Domain.Enums;
using Game.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IdeologyFocusController : ControllerBase
    {
        private readonly ILogger<IdeologyFocusController> _logger;
        private readonly IIdeologyFocusService _ideologyService;

        public IdeologyFocusController(ILogger<IdeologyFocusController> logger, IIdeologyFocusService ideologyService)
        {
            _logger = logger;
            _ideologyService = ideologyService;
        }

        [HttpPost("enactIdeologyFocus/{ideologyFocus}")]
        public async Task<IActionResult> EnactIdeologyFocus(IdeologyFocusRequestDTO ideologyFocusDTO)
        {
            var result = await _ideologyService.EnactIdeologyFocus(ideologyFocusDTO);

            if (result == null)
            {
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new ApiError("ideology_focus.enact_failed", result.Message));
        }

        [HttpPost("getIdeologyOverview/{cityId}")]
        public async Task<IActionResult> GetIdeologyOverview(Guid cityId)
        {
            var result = await _ideologyService.GetIdeologyOverview(cityId);

            if (result != null)
            {
                return Ok(result);
            }

            return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
        }
    }
}
