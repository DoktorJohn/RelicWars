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
    public class WallService : IWallService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;

        public WallService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<List<WallInfoDTO>> GetWallInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var wall = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Wall);
            int currentLevel = wall?.Level ?? 0;

            var resultList = new List<WallInfoDTO>();

            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;

                ModifierDTO modifier = new();

                if (levelToCheck == 0)
                {
                    modifier.ModifierTag = ModifierTagEnum.Wall;
                    modifier.ModifierType = ModifierTypeEnum.Increased;
                    modifier.Value = 0;
                }

                else
                {
                    var config = _buildingDataReader.GetConfig<WallLevelData>(BuildingTypeEnum.Wall, levelToCheck);

                    if (config == null) break;

                    modifier.ModifierTag = ModifierTagEnum.Wall;
                    modifier.ModifierType = ModifierTypeEnum.Increased;
                    modifier.Value = config.ModifiersInternal.FirstOrDefault(x => x.Tag == ModifierTagEnum.Wall)?.Value ?? 0;
                }

                resultList.Add(new WallInfoDTO
                {
                    Level = levelToCheck,
                    DefensiveModifier = modifier,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }
    }
}
