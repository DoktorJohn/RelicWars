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
    public class TownHallService : ITownHallService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;

        public TownHallService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<TownHallInfoDTO> GetTownHallInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

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
