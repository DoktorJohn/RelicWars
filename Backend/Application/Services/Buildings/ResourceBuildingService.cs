using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
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
        private readonly IModifierService _modifierService;

        public ResourceBuildingService(
            ICityRepository cityRepo,
            BuildingDataReader buildingDataReader,
            IModifierService modifierService)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
            _modifierService = modifierService;
        }

        public async Task<List<ResourceBuildingInfoDTO>> GetResourceBuildingInfoAsync(Guid cityId, BuildingTypeEnum resourceBuildingType)
        {
            var targetCity = await _cityRepo.GetByIdAsync(cityId);
            if (targetCity == null) throw new Exception($"City with ID {cityId} not found");

            var existingBuilding = targetCity.Buildings.FirstOrDefault(b => b.Type == resourceBuildingType);
            int currentBuildingLevel = existingBuilding?.Level ?? 0;

            // Definer strategien for at hente basisproduktionen
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

            // Definer hvilke tags der skal udregnes for denne bygningstype
            ModifierTagEnum[] targetModifierTags = resourceBuildingType switch
            {
                BuildingTypeEnum.TimberCamp => new[] { ModifierTagEnum.Wood, ModifierTagEnum.ResourceProduction },
                BuildingTypeEnum.StoneQuarry => new[] { ModifierTagEnum.Stone, ModifierTagEnum.ResourceProduction },
                BuildingTypeEnum.MetalMine => new[] { ModifierTagEnum.Metal, ModifierTagEnum.ResourceProduction },
                _ => Array.Empty<ModifierTagEnum>()
            };

            var buildingProjectionList = new List<ResourceBuildingInfoDTO>();
            int levelsToProject = 5;

            for (int i = 0; i < levelsToProject; i++)
            {
                if (currentBuildingLevel + i > 19) break;

                int levelToCheck = currentBuildingLevel + i;
                double baseProduction = 0;

                if (levelToCheck > 0)
                {
                    baseProduction = getProductionStrategy(levelToCheck);
                }

                // Anvend modifiers på basisproduktionen
                var productionModifierResult = _modifierService.CalculateCityValue(targetCity, baseProduction, targetModifierTags);

                // Konverter den endelige værdi tilbage til int for visning
                int finalCalculatedProduction = (int)Math.Floor(productionModifierResult.FinalValue);

                buildingProjectionList.Add(new ResourceBuildingInfoDTO
                {
                    Level = levelToCheck,
                    ProductionPrHour = finalCalculatedProduction,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return buildingProjectionList;
        }
    }
}
