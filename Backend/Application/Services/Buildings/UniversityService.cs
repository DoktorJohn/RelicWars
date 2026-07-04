using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
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
    public class UniversityService : IUniversityService
    {
        private readonly ICityRepository _cityRepository;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly IModifierService _modifierService;
        private readonly IPlayerAccessService _playerAccessService;

        public UniversityService(
            ICityRepository cityRepository,
            BuildingDataReader buildingDataReader,
            IModifierService modifierService,
            IPlayerAccessService playerAccessService)
        {
            _cityRepository = cityRepository;
            _buildingDataReader = buildingDataReader;
            _modifierService = modifierService;
            _playerAccessService = playerAccessService;
        }

        public async Task<List<UniversityInfoDTO>> GetUniversityInfoAsync(Guid cityId)
        {
            // 1. Hent byen for at få adgang til alle aktive modifier providers (Research, Ideology, etc.)
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            var universityBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.University);
            int currentBuildingLevel = universityBuilding?.Level ?? 0;

            var projectionList = new List<UniversityInfoDTO>();

            // Vi looper: Nuværende level + de næste 5 (Max level 20 / index 19)
            for (int i = 0; i < 5; i++)
            {
                int levelToCheck = currentBuildingLevel + i;

                if (levelToCheck > 19) break;

                double baseResearchProduction = 0;

                if (levelToCheck > 0)
                {
                    var levelConfiguration = _buildingDataReader.GetConfig<UniversityLevelData>(BuildingTypeEnum.University, levelToCheck);

                    if (levelConfiguration == null) break;

                    baseResearchProduction = levelConfiguration.ProductionPerHour;
                }

                // Anvend modifiers på basis-research-produktionen (f.eks. +20% Research Speed fra en ideologi)
                var modifierCalculationResult = _modifierService.CalculateCityValue(
                    cityEntity,
                    baseResearchProduction,
                    ModifierTagEnum.Research);

                // Konverterer resultatet til int, da vi ikke ønsker halve forskningspoint i UI'et
                int finalCalculatedProduction = (int)Math.Floor(modifierCalculationResult.FinalValue);

                projectionList.Add(new UniversityInfoDTO
                {
                    Level = levelToCheck,
                    ProductionPerHour = finalCalculatedProduction,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return projectionList;
        }
    }
}
