using Application.Interfaces.IServices;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("{cityId}/warehouse")]
        public async Task<IActionResult> GetWarehouseInfo(Guid cityId)
        {
            var data = await _buildingService.GetWarehouseProjectionAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/resource/{buildingType}")]
        public async Task<IActionResult> GetResourceBuildingInfo(Guid cityId, BuildingTypeEnum buildingType)
        {
            var data = await _buildingService.GetResourceBuildingInfoAsync(cityId, buildingType);
            return Ok(data);
        }

        [HttpGet("{cityId}/housing")]
        public async Task<IActionResult> GetHousingInfo(Guid cityId)
        {
            var data = await _buildingService.GetHousingInfoAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/wall")]
        public async Task<IActionResult> GetWallInfo(Guid cityId)
        {
            var data = await _buildingService.GetWallInfoAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/stable")]
        public async Task<IActionResult> GetStableInfo(Guid cityId)
        {
            var data = await _buildingService.GetStableInfoAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/workshop")]
        public async Task<IActionResult> GetWorkshopInfo(Guid cityId)
        {
            var data = await _buildingService.GetWorkshopInfoAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/academy")]
        public async Task<IActionResult> GetAcademyInfo(Guid cityId)
        {
            var data = await _buildingService.GetAcademyInfoAsync(cityId);
            return Ok(data);
        }
    }
}
