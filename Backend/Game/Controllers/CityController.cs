using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.DTOs;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CityController : ControllerBase
    {
        private readonly ICityService _cityService;
        private readonly ILogger<CityController> _logger;

        public CityController(ICityService cityService, ILogger<CityController> logger)
        {
            _cityService = cityService;
            _logger = logger;
        }

        [HttpGet("GetDetailedCityInformation/{cityIdentifier}")]
        public async Task<ActionResult<CityControllerGetDetailedCityInformationDTO>> GetDetailedCityInformation(Guid cityIdentifier)
        {
            var detailedInfo = await _cityService.GetDetailedCityInformationByCityIdentifierAsync(cityIdentifier);

            if (detailedInfo == null)
            {
                _logger.LogWarning("Detailed info request failed. City ID {OriginCityId} not found.", cityIdentifier);
                return NotFound(new { Message = $"City with ID {cityIdentifier} was not found." });
            }

            return Ok(detailedInfo);
        }

        [HttpGet("CityOverviewHUD/{cityIdentifier}")]
        public async Task<ActionResult<CityControllerGetDetailedCityInformationDTO>> GetCityOverviewHUD(Guid cityIdentifier)
        {
            var detailedInfo = await _cityService.GetCityOverviewHUD(cityIdentifier);

            if (detailedInfo == null)
            {
                _logger.LogWarning("Detailed info request failed. City ID {OriginCityId} not found.", cityIdentifier);
                return NotFound(new { Message = $"City with ID {cityIdentifier} was not found." });
            }

            return Ok(detailedInfo);
        }

        [HttpGet("{cityIdentifier}/townHall/available-buildings")]
        public async Task<ActionResult<List<AvailableBuildingDTO>>> GetTownHallBuildingData(Guid cityIdentifier)
        {
            var buildings = await _cityService.GetAvailableBuildingsForTownHallAsync(cityIdentifier);
            return Ok(buildings);
        }
    }
}