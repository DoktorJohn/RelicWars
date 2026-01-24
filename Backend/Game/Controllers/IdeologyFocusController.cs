using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Services;
using Azure.Core;
using Domain.Entities;
using Domain.Enums;
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

            if (result.Success)
            {
                return Ok(new { Message = result.Message });
            }

            return BadRequest(result.Message);
        }
    }
}
