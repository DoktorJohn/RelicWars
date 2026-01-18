using Application.DTOs;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Services.Buildings;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Game.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Matcher din Unity URL
    [Authorize]
    public class EconomyBuildingController : ControllerBase
    {
        private readonly ILogger<EconomyBuildingController> _logger;
        private readonly IWarehouseService _wareHouseService;
        private readonly IResourceBuildingService _resourceBuildingService;
        private readonly IHousingService _housingService;

        public EconomyBuildingController(ILogger<EconomyBuildingController> logger, IWarehouseService warehouseService, IResourceBuildingService resourceBuildingService, IHousingService housingService)
        {
            _logger = logger;
            _wareHouseService = warehouseService;
            _resourceBuildingService = resourceBuildingService;
            _housingService = housingService;
        }

        [HttpGet("{cityId}/warehouse")]
        public async Task<IActionResult> GetWarehouseInfo(Guid cityId)
        {
            var data = await _wareHouseService.GetWarehouseProjectionAsync(cityId);
            return Ok(data);
        }

        [HttpGet("{cityId}/resource/{buildingType}")]
        public async Task<IActionResult> GetResourceBuildingInfo(Guid cityId, BuildingTypeEnum buildingType)
        {
            var data = await _resourceBuildingService.GetResourceBuildingInfoAsync(cityId, buildingType);
            return Ok(data);
        }

        [HttpGet("{cityId}/housing")]
        public async Task<IActionResult> GetHousingInfo(Guid cityId)
        {
            var data = await _housingService.GetHousingInfoAsync(cityId);
            return Ok(data);
        }

    }
}