using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Services.Buildings;
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
    public class MiscBuildingController : ControllerBase
    {
        private readonly ILogger<MiscBuildingController> _logger;
        private readonly IWallService _wallService;
        private readonly IAcademyService _academyService;

        public MiscBuildingController(ILogger<MiscBuildingController> logger, IWallService wallService, IAcademyService academyService)
        {
            _logger = logger;
            _wallService = wallService;
            _academyService = academyService;
        }

        [HttpGet("{cityId}/wall")]
        public async Task<IActionResult> GetWallInfo(Guid cityId)
        {
            var data = await _wallService.GetWallInfoAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/academy")]
        public async Task<IActionResult> GetAcademyInfo(Guid cityId)
        {
            var data = await _academyService.GetAcademyInfoAsync(cityId);
            return Ok(data);
        }
    }
}