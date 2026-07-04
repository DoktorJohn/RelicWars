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
    public class TownHallService : ITownHallService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly IPlayerAccessService _playerAccessService;

        public TownHallService(ICityRepository cityRepo, BuildingDataReader buildingDataReader, IPlayerAccessService playerAccessService)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
            _playerAccessService = playerAccessService;
        }

        public async Task<TownHallInfoDTO> GetTownHallInfoAsync(Guid cityId)
        {
            var city = await _playerAccessService.RequireOwnedCityForTownHallAsync(cityId);

            var townHall = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.TownHall);
            int currentLevel = townHall?.Level ?? 0;

            var result = new TownHallInfoDTO();
            ModifierDTO modifier = new();

            var config = _buildingDataReader.GetConfig<TownHallLevelData>(BuildingTypeEnum.TownHall, currentLevel);

            if (config == null) return null;

            modifier.ModifierTag = ModifierTagEnum.Construction;
            modifier.ModifierType = ModifierTypeEnum.Increased;
            modifier.Value = config.ModifiersInternal.FirstOrDefault(x => x.Tag == ModifierTagEnum.Construction)?.Value ?? 0;

            result.Level = currentLevel;
            result.BuildingSpeedModifier = modifier;

            return result;
        }
    }
}
