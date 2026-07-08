using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Game.Contracts;
using Microsoft.EntityFrameworkCore;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CityController : ControllerBase
    {
        private readonly ICityService _cityService;
        private readonly IExoticResourceService _exoticResourceService;
        private readonly ILogger<CityController> _logger;

        public CityController(ICityService cityService, IExoticResourceService exoticResourceService, ILogger<CityController> logger)
        {
            _cityService = cityService;
            _exoticResourceService = exoticResourceService;
            _logger = logger;
        }

        [HttpGet("GetDetailedCityInformation/{cityIdentifier}")]
        public async Task<ActionResult<CityControllerGetDetailedCityInformationDTO>> GetDetailedCityInformation(Guid cityIdentifier)
        {
            var detailedInfo = await _cityService.GetDetailedCityInformationByCityIdentifierAsync(cityIdentifier);

            if (detailedInfo == null)
            {
                _logger.LogWarning("Detailed info request failed. City ID {OriginCityId} not found.", cityIdentifier);
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }

            return Ok(detailedInfo);
        }

        [HttpGet("{cityIdentifier}/resources")]
        public async Task<ActionResult<CityResourcesDTO>> GetCityResources(Guid cityIdentifier)
        {
            var resources = await _cityService.GetCityResourcesAsync(cityIdentifier);

            if (resources == null)
            {
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }

            return Ok(resources);
        }

        [HttpGet("CityOverviewHUD/{cityIdentifier}")]
        public async Task<ActionResult<CityOverviewHUD>> GetCityOverviewHUD(Guid cityIdentifier)
        {
            var detailedInfo = await _cityService.GetCityOverviewHUD(cityIdentifier);

            if (detailedInfo == null)
            {
                _logger.LogWarning("Detailed info request failed. City ID {OriginCityId} not found.", cityIdentifier);
                return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
            }

            return Ok(detailedInfo);
        }

        [HttpGet("{cityIdentifier}/townHall/available-buildings")]
        public async Task<ActionResult<List<AvailableBuildingDTO>>> GetTownHallBuildingData(Guid cityIdentifier)
        {
            var buildings = await _cityService.GetAvailableBuildingsForTownHallAsync(cityIdentifier);
            return Ok(buildings);
        }

        [HttpPost("ChangeCityName/{cityIdentifier}/{newCityName}")]
        public async Task<IActionResult> ChangeCityName(Guid cityIdentifier, string newCityName)
        {
            var result = await _cityService.ChangeCityName(cityIdentifier, newCityName);

            if (result.Success)
            {
                return Ok(result);
            }

            return NotFound(new ApiError("resource.not_found", "Ressourcen blev ikke fundet."));
        }

        [HttpGet("{cityIdentifier}/my-cities")]
        public async Task<ActionResult<List<CityDTO>>> GetPlayerCities(Guid cityIdentifier)
        {
            var cities = await _cityService.GetPlayerCitiesByCityId(cityIdentifier);
            return Ok(cities);
        }

        [HttpPost("{cityIdentifier}/exotic-resources/invest")]
        public async Task<ActionResult<ExoticResourceInvestmentResponseDTO>> InvestInExoticResource(
            Guid cityIdentifier,
            [FromBody] ExoticResourceInvestmentRequestDTO request)
        {
            var result = await _exoticResourceService.InvestAsync(cityIdentifier, request);
            return Ok(result);
        }
    }
}
