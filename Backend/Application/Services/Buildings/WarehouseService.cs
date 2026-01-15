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
    public class WarehouseService : IWarehouseService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;

        public WarehouseService(ICityRepository cityRepo, BuildingDataReader buildingDataReader)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
        }

        public async Task<List<WarehouseProjectionDTO>> GetWarehouseProjectionAsync(Guid cityId)
        {
            // 1. Hent byen for at finde nuværende warehouse level
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var warehouse = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Warehouse);
            int currentLevel = warehouse?.Level ?? 0;

            var resultList = new List<WarehouseProjectionDTO>();

            // 2. Loop: Nuværende level + 5 næste
            for (int i = 0; i <= 5; i++)
            {
                int levelToCheck = currentLevel + i;


                int capacity = 0;

                if (levelToCheck == 0)
                {
                    capacity = 500;
                }
                else
                {
                    var config = _buildingDataReader.GetConfig<WarehouseLevelData>(BuildingTypeEnum.Warehouse, levelToCheck);

                    if (config == null) break;

                    capacity = config.Capacity;
                }

                resultList.Add(new WarehouseProjectionDTO
                {
                    Level = levelToCheck,
                    Capacity = capacity,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }
    }
}
