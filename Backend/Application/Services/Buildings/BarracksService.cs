using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Utility;
using Domain.Enums;
using Domain.StaticData.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Buildings
{
    public class BarracksService : IBarracksService
    {
        private readonly ICityRepository _cityRepo;
        private readonly UnitDataReader _unitDataReader;
        private readonly IModifierService _modifierService;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly UnitAvailabilityEvaluator _unitAvailabilityEvaluator;

        public BarracksService(
            ICityRepository cityRepo,
            UnitDataReader unitDataReader,
            IModifierService modifierService,
            IPlayerAccessService playerAccessService,
            UnitAvailabilityEvaluator unitAvailabilityEvaluator)
        {
            _cityRepo = cityRepo;
            _unitDataReader = unitDataReader;
            _modifierService = modifierService;
            _playerAccessService = playerAccessService;
            _unitAvailabilityEvaluator = unitAvailabilityEvaluator;
        }

        public async Task<BarracksFullViewDTO> GetBarracksOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            var barracksBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Barracks);
            int currentBuildingLevel = barracksBuilding?.Level ?? 0;

            var barracksResponse = new BarracksFullViewDTO
            {
                BuildingLevel = currentBuildingLevel
            };

            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null ||
                    (unitStaticData.Category != UnitCategoryEnum.Infantry && unitStaticData.Category != UnitCategoryEnum.Ranged)) continue;

                // --- BRUG NY HJÆLPER MED MODIFIERS ---
                var modifiedCosts = MilitaryUnitModifierHelper.GetModifiedCosts(_modifierService, cityEntity, unitStaticData);
                var modifiedStats = MilitaryUnitModifierHelper.GetModifiedStats(_modifierService, cityEntity, unitStaticData);
                int calculatedRecruitmentTime = MilitaryUnitModifierHelper.GetModifiedRecruitmentTime(_modifierService, cityEntity, unitStaticData);

                int alreadyOwnedCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                var availability = _unitAvailabilityEvaluator.Evaluate(cityEntity, unitStaticData);

                barracksResponse.AvailableUnits.Add(new BarracksUnitInfoDTO
                {
                    UnitType = unitTypeCandidate,
                    UnitName = unitStaticData.Type.ToString(),
                    AlreadyOwnedCount = alreadyOwnedCount,

                    CostWood = modifiedCosts.wood,
                    CostStone = modifiedCosts.stone,
                    CostMetal = modifiedCosts.metal,

                    Power = modifiedStats.power,
                    Armor = modifiedStats.armor,
                    Discipline = modifiedStats.discipline,
                    Mobility = modifiedStats.mobility,
                    Reach = modifiedStats.reach,
                    LootCapacity = modifiedStats.loot,

                    PopulationCost = unitStaticData.PopulationCost,
                    RecruitmentTimeInSeconds = calculatedRecruitmentTime,
                    IsUnlocked = availability.IsUnlocked,
                    UnmetRequirements = availability.UnmetRequirements
                });
            }

            return barracksResponse;
        }
    }
}
