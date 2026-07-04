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
    public class WallService : IWallService
    {
        private readonly ICityRepository _cityRepository;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly IModifierService _modifierService;
        private readonly IPlayerAccessService _playerAccessService;

        public WallService(
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

        public async Task<List<WallInfoDTO>> GetWallInfoAsync(Guid cityId)
        {
            // 1. Hent byen for at få adgang til aktive modifier providers
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            var wallBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Wall);
            int currentBuildingLevel = wallBuilding?.Level ?? 0;

            var wallProjectionList = new List<WallInfoDTO>();

            // Vi looper: Nuværende level + de næste 5
            for (int i = 0; i < 5; i++)
            {
                int levelToCheck = currentBuildingLevel + i;

                // Stop hvis vi går ud over max level (20)
                if (levelToCheck > 19) break;

                double baseWallModifierValue = 0;

                if (levelToCheck > 0)
                {
                    var levelConfiguration = _buildingDataReader.GetConfig<WallLevelData>(BuildingTypeEnum.Wall, levelToCheck);

                    if (levelConfiguration == null) break;

                    // Find basisværdien for Wall-tagget i bygningens konfiguration
                    baseWallModifierValue = levelConfiguration.ModifiersInternal
                        .FirstOrDefault(modifier => modifier.Tag == ModifierTagEnum.Wall)?.Value ?? 0;
                }

                // Anvend modifiers på murens egen bonus (f.eks. hvis man har "+10% Wall Effectiveness")
                var wallModifierCalculationResult = _modifierService.CalculateCityValue(
                    cityEntity,
                    baseWallModifierValue,
                    ModifierTagEnum.Wall);

                // Vi pakker resultatet ind i en ModifierDTO til frontenden
                ModifierDTO finalDefensiveModifier = new ModifierDTO
                {
                    ModifierTag = ModifierTagEnum.Wall,
                    ModifierType = ModifierTypeEnum.Increased,
                    Value = wallModifierCalculationResult.FinalValue
                };

                wallProjectionList.Add(new WallInfoDTO
                {
                    Level = levelToCheck,
                    DefensiveModifier = finalDefensiveModifier,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return wallProjectionList;
        }
    }
}
