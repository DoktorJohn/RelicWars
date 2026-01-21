using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingController : ControllerBase
    {
        private readonly IBuildingService _buildingService;

        public BuildingController(IBuildingService buildingService)
        {
            _buildingService = buildingService;
        }

        [HttpPost("{cityId}/upgrade/{type}")]
        public async Task<IActionResult> Upgrade(Guid cityId, BuildingTypeEnum type)
        {
            var result = await _buildingService.QueueUpgradeAsync(cityId, type);

            if (result.Success)
            {
                return Ok(result.Message);
            }

            return BadRequest(result.Message);
        }

        [HttpGet("{cityId}/buildingQueue")]
        public async Task<ActionResult<List<BuildingDTO>>> GetBuildingQueue(Guid cityId)
        {
            var queue = await _buildingService.GetBuildingQueueAsync(cityId);
            return Ok(queue);
        }


    }
}
