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
            // Vi omdøber variablen til 'result', da den indeholder både Success og Message
            var result = await _buildingService.QueueUpgradeAsync(cityId, type);

            // Tjek 'result.Success' i stedet for blot 'result'
            if (result.Success)
            {
                return Ok(result.Message);
            }

            return BadRequest(result.Message);
        }
    }
}
