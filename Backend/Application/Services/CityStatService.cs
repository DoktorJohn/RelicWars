using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers.Abstraction;
using Domain.Workers;
using Application.Utility;
using Domain.Abstraction;

namespace Application.Services
{
    public class CityStatService : ICityStatService
    {
        private readonly BuildingDataReader _buildingData;
        private readonly UnitDataReader _unitData;
        private readonly IModifierService _modifierService;

        public CityStatService(BuildingDataReader buildingData, UnitDataReader unitData, IModifierService modifierService)
        {
            _buildingData = buildingData;
            _unitData = unitData;
            _modifierService = modifierService;
        }

        public double GetWarehouseCapacity(City city)
        {
            var warehouse = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Warehouse);
            double baseCapacity = 500.0;
            if (warehouse != null && warehouse.Level > 0)
                baseCapacity = _buildingData.GetConfig<WarehouseLevelData>(BuildingTypeEnum.Warehouse, warehouse.Level)?.Capacity ?? baseCapacity;
            return _modifierService.CalculateCityValue(city, baseCapacity, ModifierTagEnum.WarehouseCapacity).FinalValue;
        }

        public int GetMaxPopulation(City cityEntity)
        {
            double basePopulationCapacity = 0;

            // 1. Find alle bygninger af typen Housing og summer deres befolkningstilvækst
            var housingBuildings = cityEntity.Buildings.Where(building => building.Type == BuildingTypeEnum.Housing);

            foreach (var building in housingBuildings)
            {
                var housingConfig = _buildingData.GetConfig<HousingLevelData>(BuildingTypeEnum.Housing, building.Level);
                if (housingConfig != null)
                {
                    basePopulationCapacity += housingConfig.Population;
                }
            }

            // 2. Saml alle providers for at se om der er bonusser til befolkningstallet
            var modifierProviders = new List<IModifierProvider> { cityEntity };
            if (cityEntity.WorldPlayer != null)
            {
                modifierProviders.Add(cityEntity.WorldPlayer);
                if (cityEntity.WorldPlayer.Alliance != null)
                {
                    modifierProviders.Add(cityEntity.WorldPlayer.Alliance);
                }
            }

            // 3. Beregn den endelige kapacitet (Vi genbruger ResourceProduction tagget eller definerer et specifikt hvis ønsket)
            var calculationResult = _modifierService.CalculateEntityValueWithModifiers(
                basePopulationCapacity,
                new[] { ModifierTagEnum.ResourceProduction },
                modifierProviders);

            return (int)calculationResult.FinalValue;
        }

        public int GetCurrentPopulationUsage(City city, IEnumerable<BaseJob> activeJobs)
        {
            int stationedUnitUsage = city.UnitStacks
                .Select(s => new { Stack = s, Def = _unitData.GetUnit(s.Type) })
                .Where(x => x.Def != null)
                .Sum(x => x.Stack.Quantity * x.Def!.PopulationCost);
            int deployedUnitUsage = city.OriginUnitDeployments
                .SelectMany(deployment => deployment.UnitStacks)
                .Select(s => new { Stack = s, Def = _unitData.GetUnit(s.Type) })
                .Where(x => x.Def != null)
                .Sum(x => x.Stack.Quantity * x.Def!.PopulationCost);
            int reservedInQueue = 0;

            foreach (var job in activeJobs)
            {
                if (job is RecruitmentJob rJob)
                {
                    var unitDef = _unitData.GetUnit(rJob.UnitType);
                    reservedInQueue += (rJob.TotalQuantity - rJob.CompletedQuantity) * unitDef.PopulationCost;
                }
            }

            return stationedUnitUsage + deployedUnitUsage + reservedInQueue;
        }

        public int GetAvailablePopulation(City city, IEnumerable<BaseJob> activeJobs)
        {
            int maxPop = GetMaxPopulation(city);
            int currentUsed = GetCurrentPopulationUsage(city, activeJobs);

            return maxPop - currentUsed;
        }
    }
}
