using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;
using Domain.Workers;

namespace Application.Services
{
    public class CityStatService : ICityStatService
    {
        private readonly BuildingDataReader _buildingData;
        private readonly UnitDataReader _unitData;

        public CityStatService(BuildingDataReader buildingData, UnitDataReader unitData)
        {
            _buildingData = buildingData;
            _unitData = unitData;
        }

        public double GetWarehouseCapacity(City city)
        {
            var warehouse = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Warehouse);
            if (warehouse == null || warehouse.Level == 0) return 500.0; // Basis start-kapacitet

            var config = _buildingData.GetConfig<WarehouseLevelData>(BuildingTypeEnum.Warehouse, warehouse.Level);
            return config?.Capacity ?? 500.0;
        }

        public int GetMaxPopulation(City city)
        {
            var housing = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Housing);
            if (housing == null || housing.Level == 0) return 100; // Basis start-population

            var config = _buildingData.GetConfig<HousingLevelData>(BuildingTypeEnum.Housing, housing.Level);
            return config?.Population ?? 100;
        }

        public int GetCurrentPopulationUsage(City city)
        {
            int totalUsage = 0;

            // 1. Population brugt af bygninger
            foreach (var b in city.Buildings)
            {
                var config = _buildingData.GetConfig<BuildingLevelData>(b.Type, b.Level);
                if (config != null)
                {
                    totalUsage += config.PopulationCost;
                }
            }

            // 2. Population brugt af enheder (Garrison)
            foreach (var s in city.UnitStacks)
            {
                var unitDef = _unitData.GetUnit(s.Type);
                if (unitDef != null)
                {
                    totalUsage += (s.Quantity * unitDef.PopulationCost);
                }
            }

            return totalUsage;
        }

        public int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs)
        {
            int maxPop = GetMaxPopulation(city);
            int currentUsed = GetCurrentPopulationUsage(city);
            int reservedInQueue = 0;

            foreach (var job in activeJobs)
            {
                if (job is RecruitmentJob rJob)
                {
                    var unitDef = _unitData.GetUnit(rJob.UnitType);
                    reservedInQueue += (rJob.TotalQuantity - rJob.CompletedQuantity) * unitDef.PopulationCost;
                }
                else if (job is BuildingJob bJob)
                {
                    // Vi skal finde ud af, hvor meget EKSTRA pop dette level koster i forhold til det forrige
                    var nextLevelConfig = _buildingData.GetConfig<BuildingLevelData>(bJob.BuildingType, bJob.TargetLevel);

                    // Hvis det er level 1, trækker vi 0 fra. Ellers trækker vi prisen for level-1 fra.
                    int previousLevelPop = 0;
                    if (bJob.TargetLevel > 1)
                    {
                        var prevLevelConfig = _buildingData.GetConfig<BuildingLevelData>(bJob.BuildingType, bJob.TargetLevel - 1);
                        previousLevelPop = prevLevelConfig?.PopulationCost ?? 0;
                    }

                    reservedInQueue += (nextLevelConfig.PopulationCost - previousLevelPop);
                }
            }

            return maxPop - (currentUsed + reservedInQueue);
        }
    }
}