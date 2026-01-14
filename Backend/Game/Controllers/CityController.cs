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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCityOverview(Guid id)
        {
            var city = await _cityService.GetCityOverviewAsync(id);
            if (city == null)
            {
                _logger.LogWarning("City Overview not found for ID: {CityId}", id);
                return NotFound("City not found.");
            }

            return Ok(city);
        }

        [HttpGet("GetDetailedCityInformation/{cityIdentifier}")]
        public async Task<ActionResult<CityControllerGetDetailedCityInformationDTO>> GetDetailedCityInformation(Guid cityIdentifier)
        {
            var detailedInfo = await _cityService.GetDetailedCityInformationByCityIdentifierAsync(cityIdentifier);

            if (detailedInfo == null)
            {
                _logger.LogWarning("Detailed info request failed. City ID {CityId} not found.", cityIdentifier);
                return NotFound(new { Message = $"City with ID {cityIdentifier} was not found." });
            }

            return Ok(detailedInfo);
        }

        [HttpGet("{cityIdentifier}/senate/available-buildings")]
        public async Task<ActionResult<List<AvailableBuildingDTO>>> GetSenateBuildingData(Guid cityIdentifier)
        {
            var buildings = await _cityService.GetAvailableBuildingsForSenateAsync(cityIdentifier);
            return Ok(buildings);
        }
    }
}