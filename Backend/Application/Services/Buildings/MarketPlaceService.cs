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

        public async Task<MarketPlaceInfoDTO> GetMarketPlaceInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var marketPlace = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.MarketPlace);
            int currentLevel = marketPlace?.Level ?? 0;

            var result = new MarketPlaceInfoDTO();
            ModifierDTO modifier = new();

            var config = _buildingDataReader.GetConfig<MarketPlaceLevelData>(BuildingTypeEnum.MarketPlace, currentLevel);

            if (config == null) return null;

            modifier.ModifierTag = ModifierTagEnum.Silver;
            modifier.ModifierType = ModifierTypeEnum.Flat;
            modifier.Value = config.ModifiersInternal.FirstOrDefault(x => x.Tag == ModifierTagEnum.Silver)?.Value ?? 0;

            result.Level = currentLevel;
            result.Modifier = modifier;

            return result;
        }
    }
}
