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
    public class HousingService : IHousingService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;

        public HousingService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<List<HousingInfoDTO>> GetHousingInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var housing = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Housing);
            int currentLevel = housing?.Level ?? 0;

            var resultList = new List<HousingInfoDTO>();

            // 2. Loop: Nuværende level + 5 næste
            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;

                int population = 0;

                if (levelToCheck == 0)
                {
                    population = 100;
                }
                else
                {
                    var config = _buildingDataReader.GetConfig<HousingLevelData>(BuildingTypeEnum.Housing, levelToCheck);

                    if (config == null) break;

                    population = config.Population;
                }

                resultList.Add(new HousingInfoDTO
                {
                    Level = levelToCheck,
                    Population = population,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }
    }
}
