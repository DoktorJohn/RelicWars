using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces.IServices.IBuildings;
using Application.Utility;
using Domain.Enums;
using Domain.StaticData.Readers;

namespace Application.Services.Buildings
{
    public class HarborService : IHarborService
    {
        private readonly ICityRepository _cityRepo;
        private readonly UnitDataReader _unitDataReader;
        private readonly IModifierService _modifierService;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly UnitAvailabilityEvaluator _unitAvailabilityEvaluator;

        public HarborService(
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

        public async Task<HarborFullViewDTO> GetHarborOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            var harborBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Harbor);
            int currentBuildingLevel = harborBuilding?.Level ?? 0;

            var harborResponse = new HarborFullViewDTO
            {
                BuildingLevel = currentBuildingLevel
            };

            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Naval) continue;

                var modifiedCosts = MilitaryUnitModifierHelper.GetModifiedCosts(_modifierService, cityEntity, unitStaticData);
                var modifiedStats = MilitaryUnitModifierHelper.GetModifiedStats(_modifierService, cityEntity, unitStaticData);
                int calculatedRecruitmentTime = MilitaryUnitModifierHelper.GetModifiedRecruitmentTime(_modifierService, cityEntity, unitStaticData);

                int alreadyOwnedCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                var availability = _unitAvailabilityEvaluator.Evaluate(cityEntity, unitStaticData);

                harborResponse.AvailableUnits.Add(new HarborUnitInfoDTO
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
                    UnitCapacity = unitStaticData.UnitCapacity,
                    RecruitmentTimeInSeconds = calculatedRecruitmentTime,
                    IsUnlocked = availability.IsUnlocked,
                    UnmetRequirements = availability.UnmetRequirements
                });
            }

            return harborResponse;
        }
    }
}
