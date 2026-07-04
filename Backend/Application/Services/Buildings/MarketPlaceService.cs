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
    public class MarketPlaceService : IMarketPlaceService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly IModifierService _modifierService;
        private readonly IPlayerAccessService _playerAccessService;

        public MarketPlaceService(
            ICityRepository cityRepo,
            BuildingDataReader buildingDataReader,
            IModifierService modifierService,
            IPlayerAccessService playerAccessService)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
            _modifierService = modifierService;
            _playerAccessService = playerAccessService;
        }

        public async Task<List<MarketPlaceInfoDTO>> GetMarketPlaceInfoAsync(Guid cityId)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            var marketPlaceBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.MarketPlace);
            int currentBuildingLevel = marketPlaceBuilding?.Level ?? 0;
            const int maxLevel = 20;
            const int previewLevels = 5;
            int previewStartLevel = Math.Max(0, Math.Min(currentBuildingLevel, maxLevel - previewLevels + 1));

            var marketPlaceProjectionList = new List<MarketPlaceInfoDTO>();

            for (int i = 0; i < 5; i++)
            {
                int levelToCheck = previewStartLevel + i;
                if (levelToCheck > maxLevel) break;

                double baseMarketBonusValue = 0;

                if (levelToCheck > 0)
                {
                    var levelConfiguration = _buildingDataReader.GetConfig<MarketPlaceLevelData>(BuildingTypeEnum.MarketPlace, levelToCheck);

                    if (levelConfiguration == null) break;

                    baseMarketBonusValue = levelConfiguration.ModifiersInternal
                        .FirstOrDefault(modifier => modifier.Tag == ModifierTagEnum.Coins)?.Value ?? 0;
                }

                var marketModifierResult = _modifierService.CalculateCityValue(
                    cityEntity,
                    baseMarketBonusValue,
                    ModifierTagEnum.Market);

                marketPlaceProjectionList.Add(new MarketPlaceInfoDTO
                {
                    Level = levelToCheck,
                    ModifierIncrease = marketModifierResult.FinalValue,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return marketPlaceProjectionList;
        }
    }
}
