using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices.IBuildings;
using Application.Utility;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Buildings
{
    public class ResourceBuildingService : IResourceBuildingService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;

        public ResourceBuildingService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<List<ResourceBuildingInfoDTO>> GetResourceBuildingInfoAsync(Guid cityId, BuildingTypeEnum resourceBuildingType)
        {
            var targetCity = await _cityRepo.GetByIdAsync(cityId);
            if (targetCity == null) throw new Exception($"City with ID {cityId} not found");

            var existingBuilding = targetCity.Buildings.FirstOrDefault(b => b.Type == resourceBuildingType);
            int currentBuildingLevel = existingBuilding?.Level ?? 0;

            Func<int, int> getProductionStrategy = resourceBuildingType switch
            {
                BuildingTypeEnum.TimberCamp => (level) =>
                    _buildingDataReader.GetConfig<TimberCampLevelData>(BuildingTypeEnum.TimberCamp, level)?.ProductionPerHour ?? 0,

                BuildingTypeEnum.StoneQuarry => (level) =>
                    _buildingDataReader.GetConfig<StoneQuarryLevelData>(BuildingTypeEnum.StoneQuarry, level)?.ProductionPerHour ?? 0,

                BuildingTypeEnum.MetalMine => (level) =>
                    _buildingDataReader.GetConfig<MetalMineLevelData>(BuildingTypeEnum.MetalMine, level)?.ProductionPerHour ?? 0,

                _ => (level) => 0
            };

            var buildingProjectionList = new List<ResourceBuildingInfoDTO>();

            int levelsToProject = 5;

            for (int i = 0; i <= levelsToProject; i++)
            {
                int levelToCheck = currentBuildingLevel + i;
                int calculatedProduction = 0;

                if (levelToCheck > 0)
                {
                    calculatedProduction = getProductionStrategy(levelToCheck);
                }

                buildingProjectionList.Add(new ResourceBuildingInfoDTO
                {
                    Level = levelToCheck,
                    ProductionPrHour = calculatedProduction,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return buildingProjectionList;
        }
    }
}
