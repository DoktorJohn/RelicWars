using Application.DTOs;
using Application.Interfaces.IRepositories;
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
        private readonly IJobRepository _jobRepo;
        private readonly RecruitmentTimeCalculationService _recruitmentTimeCalculationService;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly UnitDataReader _unitDataReader;

        public StableService(ICityRepository cityRepo, BuildingDataReader buildingDataReader, IJobRepository jobRepo, UnitDataReader unitDataReader, RecruitmentTimeCalculationService recruitmentTimeCalculationService)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
            _jobRepo = jobRepo;
            _unitDataReader = unitDataReader;
            _recruitmentTimeCalculationService = recruitmentTimeCalculationService;
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

            // 2. Hent tilgængelige enheder til rekruttering (Kun Cavalry)
            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Cavalry) continue;

                double calculatedRecruitmentTimePerUnit = await _recruitmentTimeCalculationService.CalculateFinalRecruitmentTimeAsync(userId, cityEntity, unitStaticData);
                int alreadyOwnedCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                bool isUnitTypeUnlocked = currentBuildingLevel > 0;

                stableResponse.AvailableUnits.Add(new StableUnitInfoDTO
                {
                    UnitType = unitTypeCandidate,
                    UnitName = unitStaticData.Type.ToString(),
                    AlreadyOwnedCount = alreadyOwnedCount,
                    CostWood = unitStaticData.WoodCost,
                    CostStone = unitStaticData.StoneCost,
                    CostMetal = unitStaticData.MetalCost,
                    Power = unitStaticData.Power,
                    Armor = unitStaticData.Armor,
                    Discipline = unitStaticData.Discipline,
                    Mobility = unitStaticData.Mobility,
                    Reach = unitStaticData.Reach,
                    LootCapacity = unitStaticData.LootCapacity,
                    PopulationCost = unitStaticData.PopulationCost,
                    RecruitmentTimeInSeconds = (int)calculatedRecruitmentTimePerUnit,
                    IsUnlocked = isUnitTypeUnlocked
                });
            }

            return stableResponse;
        }

    }
}
