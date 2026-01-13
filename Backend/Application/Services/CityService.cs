using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;
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
        private readonly BuildingDataReader _buildingData;

        public CityService(
            ICityRepository cityRepo,
            IResourceService resService,
            ICityStatService statService,
            BuildingDataReader buildingData)
        {
            _cityRepo = cityRepo;
            _resService = resService;
            _statService = statService;
            _buildingData = buildingData;
        }

        public async Task<CityControllerGetDetailedCityInformationDTO> GetDetailedCityInformationByCityIdentifierAsync(Guid cityIdentifier)
        {
            // 1. Hent entiteten med bygninger og modifiers
            var cityEntity = await _cityRepo.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
            if (cityEntity == null) return null;

            // 2. Beregn live ressource-tals (ResourceService)
            var currentResourceSnapshot = _resService.CalculateCurrent(cityEntity, DateTime.UtcNow);

            // 3. Beregn populations-statistikker (CityStatService)
            int maxPopulation = _statService.GetMaxPopulation(cityEntity);
            int currentUsage = _statService.GetCurrentPopulationUsage(cityEntity);

            // 4. Map til DTO
            return new CityControllerGetDetailedCityInformationDTO
            {
                CityId = cityEntity.Id,
                CityName = cityEntity.Name,

                CurrentWoodAmount = currentResourceSnapshot.Wood,
                CurrentStoneAmount = currentResourceSnapshot.Stone,
                CurrentMetalAmount = currentResourceSnapshot.Metal,
                CurrentSilverAmount = cityEntity.WorldPlayer?.Silver ?? 0,

                MaxWoodCapacity = _statService.GetWarehouseCapacity(cityEntity),
                MaxStoneCapacity = _statService.GetWarehouseCapacity(cityEntity),
                MaxMetalCapacity = _statService.GetWarehouseCapacity(cityEntity),

                WoodProductionPerHour = currentResourceSnapshot.WoodProductionPerHour,
                StoneProductionPerHour = currentResourceSnapshot.StoneProductionPerHour,
                MetalProductionPerHour = currentResourceSnapshot.MetalProductionPerHour,

                // TILDELING AF POPULATION
                CurrentPopulationUsage = currentUsage,
                MaxPopulationCapacity = maxPopulation,

                BuildingList = cityEntity.Buildings.Select(b => new CityControllerGetDetailedCityInformationBuildingDTO
                {
                    BuildingId = b.Id,
                    BuildingType = b.Type,
                    CurrentLevel = b.Level,
                    IsCurrentlyUpgrading = b.IsUpgrading
                }).ToList()
            };
        }

        public async Task<CityDetailsDTO> GetCityOverviewAsync(Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null) return null;

            // 1. Hent live ressource-snapshot
            var currentResourceSnapshot = _resService.CalculateCurrent(cityEntity, DateTime.UtcNow);

            // 2. Hent populations-statistikker via StatService
            int maximumPopulationCapacity = _statService.GetMaxPopulation(cityEntity);
            int currentPopulationUsage = _statService.GetCurrentPopulationUsage(cityEntity);
            int totalAvailablePopulation = maximumPopulationCapacity - currentPopulationUsage;

            // 3. Mapping af bygnings-data
            var buildingDataTransferObjects = cityEntity.Buildings.Select(building => new BuildingDTO(
                building.Id,
                building.Type.ToString(),
                building.Level,
                building.TimeOfUpgradeFinished,
                building.IsUpgrading
            )).ToList();

            // 4. Mapping af militære enheder
            var unitStackDataTransferObjects = cityEntity.UnitStacks
                .Where(stack => stack.Quantity > 0)
                .Select(stack => new UnitStackDTO(stack.Type.ToString(), stack.Quantity))
                .ToList();

            return new CityDetailsDTO(
                cityEntity.Id,
                cityEntity.Name,
                cityEntity.Points,
                Math.Floor(currentResourceSnapshot.Wood),
                Math.Floor(currentResourceSnapshot.Stone),
                Math.Floor(currentResourceSnapshot.Metal),
                cityEntity.X,
                cityEntity.Y,
                new PopulationDTO(maximumPopulationCapacity, currentPopulationUsage, totalAvailablePopulation),
                buildingDataTransferObjects,
                unitStackDataTransferObjects,
                new List<UnitDeploymentDTO>() // Placeholder til fremtidig militær logik
            );
        }

        public async Task<List<AvailableBuildingDTO>> GetAvailableBuildingsForSenateAsync(Guid cityIdentifier)
        {
            var cityEntity = await _cityRepo.GetCityWithBuildingsByCityIdentifierAsync(cityIdentifier);
            if (cityEntity == null) return new List<AvailableBuildingDTO>();

            var responseList = new List<AvailableBuildingDTO>();
            var allBuildingTypes = Enum.GetValues(typeof(BuildingTypeEnum)).Cast<BuildingTypeEnum>();

            foreach (var buildingType in allBuildingTypes)
            {
                var existingBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == buildingType);
                int currentLevel = existingBuilding?.Level ?? 0;
                int nextLevel = currentLevel + 1;

                // Hent statisk data for det næste niveau
                var nextLevelConfig = _buildingData.GetConfig<BuildingLevelData>(buildingType, nextLevel);
                if (nextLevelConfig == null) continue; // Bygningen har måske ikke flere levels

                // Tjek ressourcer og population (Brug din CityStatService)
                double warehouseCapacity = _statService.GetWarehouseCapacity(cityEntity);

                int availablePop = _statService.GetAvailablePopulation(cityEntity, new List<BaseJob>());

                responseList.Add(new AvailableBuildingDTO
                {
                    BuildingType = buildingType,
                    BuildingName = buildingType.ToString(),
                    CurrentLevel = currentLevel,
                    WoodCost = nextLevelConfig.WoodCost,
                    StoneCost = nextLevelConfig.StoneCost,
                    MetalCost = nextLevelConfig.MetalCost,
                    PopulationCost = nextLevelConfig.PopulationCost,
                    ConstructionTimeInSeconds = nextLevelConfig.BuildTime.Seconds,
                    IsCurrentlyUpgrading = existingBuilding?.IsUpgrading ?? false,
                    CanAfford = cityEntity.Wood >= nextLevelConfig.WoodCost &&
                                cityEntity.Stone >= nextLevelConfig.StoneCost &&
                                cityEntity.Metal >= nextLevelConfig.MetalCost,
                    HasPopulationRoom = availablePop >= nextLevelConfig.PopulationCost
                });
            }

            return responseList;
        }

        /// <summary>
        /// Opdaterer byens samlede pointtal baseret på bygningsmassen.
        /// </summary>
        public async Task UpdateCityPointsAsync(Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null) return;

            // Forretningslogik: 10 point pr. level pr. bygning
            cityEntity.Points = cityEntity.Buildings.Sum(building => building.Level * 10);

            await _cityRepo.UpdateAsync(cityEntity);
        }
    }
}