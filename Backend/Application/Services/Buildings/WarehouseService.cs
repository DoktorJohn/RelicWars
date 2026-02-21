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
    public class WarehouseService : IWarehouseService
    {
        private readonly ICityRepository _cityRepo;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly IModifierService _modifierService;

        public WarehouseService(
            ICityRepository cityRepo,
            BuildingDataReader buildingDataReader,
            IModifierService modifierService)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
            _modifierService = modifierService;
        }

        public async Task<List<WarehouseProjectionDTO>> GetWarehouseProjectionAsync(Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null) throw new Exception("City not found");

            var warehouse = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Warehouse);
            int currentLevel = warehouse?.Level ?? 0;

            var resultList = new List<WarehouseProjectionDTO>();

            for (int i = 0; i < 5; i++)
            {
                if (currentLevel + i > 19)
                {
                    break;
                }

                int levelToCheck = currentLevel + i;
                double baseCapacity = 0;

                if (levelToCheck == 0)
                {
                    baseCapacity = 500;
                }
                else
                {
                    var config = _buildingDataReader.GetConfig<WarehouseLevelData>(BuildingTypeEnum.Warehouse, levelToCheck);

                    if (config == null) break;

                    baseCapacity = config.Capacity;
                }

                var capacityModifierResult = _modifierService.CalculateCityValue(city, baseCapacity, ModifierTagEnum.WarehouseCapacity);

                int finalModifiedCapacity = (int)Math.Floor(capacityModifierResult.FinalValue);

                resultList.Add(new WarehouseProjectionDTO
                {
                    Level = levelToCheck,
                    Capacity = finalModifiedCapacity,
                    IsCurrentLevel = (levelToCheck == currentLevel)
                });
            }

            return resultList;
        }
    }
}
