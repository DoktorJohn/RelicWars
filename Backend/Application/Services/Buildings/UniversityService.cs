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
    public class UniversityService : IUniversityService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;

        public UniversityService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<List<UniversityInfoDTO>> GetUniversityInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var university = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.University);
            int currentLevel = university?.Level ?? 0;

            var resultList = new List<UniversityInfoDTO>();

            // 2. Loop: Nuværende level + 5 næste
            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;

                int productionPerHour = 0;

                if (levelToCheck == 0)
                {
                    productionPerHour = 0;
                }
                else
                {
                    var config = _buildingDataReader.GetConfig<UniversityLevelData>(BuildingTypeEnum.University, levelToCheck);

                    if (config == null) break;

                    productionPerHour = config.ProductionPerHour;
                }

                resultList.Add(new UniversityInfoDTO
                {
                    Level = levelToCheck,
                    ProductionPerHour = productionPerHour,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }
    }
}
