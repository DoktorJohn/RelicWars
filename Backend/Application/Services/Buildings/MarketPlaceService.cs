using Application.DTOs;
using Application.Interfaces.IRepositories;
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

        public MarketPlaceService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<List<MarketPlaceInfoDTO>> GetMarketPlaceInfoAsync(Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null)
            {
                throw new Exception($"City with ID {cityId} not found");
            }

            var marketPlaceBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.MarketPlace);
            int currentBuildingLevel = marketPlaceBuilding?.Level ?? 0;

            var marketPlaceProjectionList = new List<MarketPlaceInfoDTO>();

            // Vi looper: Nuværende level + de næste 5
            for (int i = 0; i < 5; i++)
            {
                int levelToCheck = currentBuildingLevel + i;

                // Stop hvis vi går ud over max level (20)
                if (levelToCheck > 19) break;

                double silverModifierValue = 0;

                if (levelToCheck > 0)
                {
                    var levelConfiguration = _buildingDataReader.GetConfig<MarketPlaceLevelData>(BuildingTypeEnum.MarketPlace, levelToCheck);

                    if (levelConfiguration == null) break;

                    // Find værdien for Silver-tagget direkte i listens modifiers
                    silverModifierValue = levelConfiguration.ModifiersInternal
                        .FirstOrDefault(m => m.Tag == ModifierTagEnum.Silver)?.Value ?? 0;
                }

                marketPlaceProjectionList.Add(new MarketPlaceInfoDTO
                {
                    Level = levelToCheck,
                    ModifierIncrease = silverModifierValue,
                    IsCurrentLevel = (levelToCheck == currentBuildingLevel)
                });
            }

            return marketPlaceProjectionList;
        }
    }
}
