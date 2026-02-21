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
    public class HousingService : IHousingService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly IModifierService _modifierService;

        public HousingService(
            ICityRepository cityRepo,
            BuildingDataReader buildingDataReader,
            IModifierService modifierService)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
            _modifierService = modifierService;
        }

        public async Task<List<HousingInfoDTO>> GetHousingInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var housing = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Housing);
            int currentLevel = housing?.Level ?? 0;

            var resultList = new List<HousingInfoDTO>();

            for (int i = 0; i < 5; i++)
            {
                if (currentLevel + i > 19) break;

                int levelToCheck = currentLevel + i;

                double basePopulation = 0;

                if (levelToCheck == 0)
                {
                    basePopulation = 100;
                }
                else
                {
                    var config = _buildingDataReader.GetConfig<HousingLevelData>(BuildingTypeEnum.Housing, levelToCheck);

                    if (config == null) break;

                    basePopulation = config.Population;
                }

                var populationModifierResult = _modifierService.CalculateCityValue(city, basePopulation, ModifierTagEnum.Population);

                int finalCalculatedPopulation = (int)Math.Floor(populationModifierResult.FinalValue);

                resultList.Add(new HousingInfoDTO
                {
                    Level = levelToCheck,
                    Population = finalCalculatedPopulation,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }
    }
}
