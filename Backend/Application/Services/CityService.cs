using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _cityRepo;
        private readonly IResourceService _resService;
        private readonly ICityStatService _statService;

        public CityService(
            ICityRepository cityRepo,
            IResourceService resService,
            ICityStatService statService)
        {
            _cityRepo = cityRepo;
            _resService = resService;
            _statService = statService;
        }

        public async Task<CityControllerGetDetailedCityInformationDTO> GetDetailedCityInformationByCityIdentifierAsync(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepo.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);

            if (cityEntity == null)
            {
                return null;
            }

            // Verbøs mapping fra Domæne-model til den specifikke Controller-DTO
            return new CityControllerGetDetailedCityInformationDTO
            {
                CityId = cityEntity.Id,
                CityName = cityEntity.Name,
                CurrentWoodAmount = cityEntity.Wood,
                CurrentStoneAmount = cityEntity.Stone,
                CurrentMetalAmount = cityEntity.Metal,
                BuildingList = cityEntity.Buildings.Select(building => new CityControllerGetDetailedCityInformationBuildingDTO
                {
                    BuildingId = building.Id,
                    BuildingType = building.Type,
                    CurrentLevel = building.Level,
                    UpgradeStartedAt = building.TimeOfUpgradeStarted,
                    UpgradeFinishedAt = building.TimeOfUpgradeFinished,
                    IsCurrentlyUpgrading = building.IsUpgrading
                }).ToList()
            };
        }
        public async Task<CityDetailsDTO> GetCityOverviewAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) return null;

            // 1. Hent ressource-snapshot (ResourceService bruger nu internt CityStatService til Warehouse-cap)
            var resSnapshot = _resService.CalculateCurrent(city, DateTime.UtcNow);

            // 2. Hent stats fra CityStatService
            int maxPop = _statService.GetMaxPopulation(city);
            int usedPop = _statService.GetCurrentPopulationUsage(city);
            int availablePop = maxPop - usedPop;

            // 3. Map Bygninger til DTOs
            var buildingDtos = city.Buildings.Select(b => new BuildingDTO(
                b.Id,
                b.Type.ToString(),
                b.Level,
                b.TimeOfUpgradeFinished,
                b.IsUpgrading
            )).ToList();

            // 4. Map Enheder til DTOs
            var unitDtos = city.UnitStacks
                .Where(s => s.Quantity > 0)
                .Select(s => new UnitStackDTO(s.Type.ToString(), s.Quantity))
                .ToList();

            // 5. Returnér samlet DTO til Unity
            return new CityDetailsDTO(
                city.Id,
                city.Name,
                city.Points,
                Math.Floor(resSnapshot.Wood),
                Math.Floor(resSnapshot.Stone),
                Math.Floor(resSnapshot.Metal),
                city.X,
                city.Y,
                new PopulationDTO(maxPop, usedPop, availablePop),
                buildingDtos,
                unitDtos,
                new List<UnitDeploymentDTO>() // Kommer senere når vi implementerer bevægelser
            );
        }

        public async Task UpdateCityPointsAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) return;

            // Simpel pointberegning: 10 point pr. level pr. bygning
            city.Points = city.Buildings.Sum(b => b.Level * 10);

            await _cityRepo.UpdateAsync(city);
        }
    }
}