using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Utility;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Buildings
{
    public class StableService : IStableService
    {
        private readonly ICityRepository _cityRepo;
        private readonly UnitDataReader _unitDataReader;
        private readonly IModifierService _modifierService;

        public StableService(
            ICityRepository cityRepo,
            UnitDataReader unitDataReader,
            IModifierService modifierService)
        {
            _cityRepo = cityRepo;
            _unitDataReader = unitDataReader;
            _modifierService = modifierService;
        }

        public async Task<StableFullViewDTO> GetStableOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null) throw new Exception("City not found");

            var stableBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Stable);
            int currentBuildingLevel = stableBuilding?.Level ?? 0;

            var stableResponse = new StableFullViewDTO
            {
                BuildingLevel = currentBuildingLevel
            };

            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Cavalry) continue;

                var modifiedCosts = MilitaryUnitModifierHelper.GetModifiedCosts(_modifierService, cityEntity, unitStaticData);
                var modifiedStats = MilitaryUnitModifierHelper.GetModifiedStats(_modifierService, cityEntity, unitStaticData);
                int calculatedRecruitmentTime = MilitaryUnitModifierHelper.GetModifiedRecruitmentTime(_modifierService, cityEntity, unitStaticData);

                int alreadyOwnedCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                bool isUnitTypeUnlocked = currentBuildingLevel > 0;

                stableResponse.AvailableUnits.Add(new StableUnitInfoDTO
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
                    IsUnlocked = isUnitTypeUnlocked
                });
            }

            return stableResponse;
        }
    }
}
