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
        private readonly IResourceService _resourceService;

        public CityController(ICityService cityService, IResourceService resourceService)
        {
            _cityService = cityService;
            _resourceService = resourceService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCity(Guid id)
        {
            var city = await _cityService.GetCityOverviewAsync(id);
            if (city == null) return NotFound("Byen blev ikke fundet.");

            return Ok(city);
        }

        [HttpGet("GetDetailedCityInformation/{cityIdentifier}")]
        public async Task<ActionResult<CityControllerGetDetailedCityInformationDTO>> GetDetailedCityInformation(Guid cityIdentifier)
        {
            // Eager loading af bygninger via Repository
            var detailedCityInformationResult = await _cityService.GetDetailedCityInformationByCityIdentifierAsync(cityIdentifier);

            if (detailedCityInformationResult == null)
            {
                return NotFound(new { Message = $"Byen med ID {cityIdentifier} blev ikke fundet." });
            }

            return Ok(detailedCityInformationResult);
        }
    }
}