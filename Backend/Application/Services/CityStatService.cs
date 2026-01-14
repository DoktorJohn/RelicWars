using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;
using Domain.Workers;
using Application.Utility;

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
            if (warehouse == null || warehouse.Level == 0) return 500.0;

            var config = _buildingData.GetConfig<WarehouseLevelData>(BuildingTypeEnum.Warehouse, warehouse.Level);
            return config?.Capacity ?? 500.0;
        }

        public int GetMaxPopulation(City city)
        {
            var housingBuilding = city.Buildings.FirstOrDefault(building => building.Type == BuildingTypeEnum.Housing);

            if (housingBuilding == null || housingBuilding.Level == 0)
            {
                return 100;
            }

            var housingLevelConfig = _buildingData.GetConfig<HousingLevelData>(BuildingTypeEnum.Housing, housingBuilding.Level);
            var activePlayerModifiers = city.WorldPlayer?.ModifiersAppliedToWorldPlayer;

            double basePopulationValue = (double)(housingLevelConfig?.Population ?? 0);

            double modifiedPopulationValue = StatCalculator.ApplyModifiers(
                basePopulationValue,
                housingLevelConfig?.ModifiersThatAffects,
                activePlayerModifiers
            );

            return (int)modifiedPopulationValue;
        }

        public int GetCurrentPopulationUsage(City city)
        {
            int buildingUsage = city.Buildings
                .Select(b => _buildingData.GetConfig<BuildingLevelData>(b.Type, b.Level))
                .Where(c => c != null)
                .Sum(c => c!.PopulationCost);

            int unitUsage = city.UnitStacks
                .Select(s => new { Stack = s, Def = _unitData.GetUnit(s.Type) })
                .Where(x => x.Def != null)
                .Sum(x => x.Stack.Quantity * x.Def!.PopulationCost);

            return buildingUsage + unitUsage;
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