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
    public class AcademyService : IAcademyService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;

        public AcademyService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<List<AcademyInfoDTO>> GetAcademyInfoAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var building = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Academy);
            int currentLevel = building?.Level ?? 0;

            var result = new List<AcademyInfoDTO>();

            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i; 
                if (levelToCheck == 0) levelToCheck = 1;

                var config = _buildingDataReader.GetConfig<AcademyLevelData>(BuildingTypeEnum.Academy, levelToCheck);
                if (config == null && levelToCheck > 1) break;

                result.Add(new AcademyInfoDTO
                {
                    Level = levelToCheck,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return result;
        }
    }
}
