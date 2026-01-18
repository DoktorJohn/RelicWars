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
        private readonly IUniversityService _universityService;
        private readonly ITownHallService _townHallService;

        public MiscBuildingController(ILogger<MiscBuildingController> logger, IWallService wallService, IUniversityService universityService, ITownHallService townHallService)
        {
            _logger = logger;
            _wallService = wallService;
            _universityService = universityService;
            _townHallService = townHallService;
        }

        [HttpGet("{cityId}/wall")]
        public async Task<IActionResult> GetWallInfo(Guid cityId)
        {
            var data = await _wallService.GetWallInfoAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/university")]
        public async Task<IActionResult> GetUniversityInfo(Guid cityId)
        {
            var data = await _universityService.GetUniversityInfoAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/townHall")]
        public async Task<IActionResult> GetTownHallInfo(Guid cityId)
        {
            var data = await _townHallService.GetTownHallInfoAsync(cityId);
            return Ok(data);
        }
    }
}