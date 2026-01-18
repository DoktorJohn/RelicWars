using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Microsoft.AspNetCore.Mvc;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketPlaceBuildingController : ControllerBase
    {
        private readonly IMarketPlaceService _marketPlaceService;

        public MarketPlaceBuildingController(IMarketPlaceService marketPlaceService)
        {
            _marketPlaceService = marketPlaceService;
        }

        [HttpGet("{cityId}/marketPlace")]
        public async Task<IActionResult> GetMarketPlaceInfo(Guid cityId)
        {
            var data = await _marketPlaceService.GetMarketPlaceInfoAsync(cityId);
            return Ok(data);
        }
    }
}
