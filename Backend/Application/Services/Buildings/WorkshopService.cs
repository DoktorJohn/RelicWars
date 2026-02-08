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
    public class WorkshopService : IWorkshopService
    {
        private readonly ICityRepository _cityRepo;
        private readonly IJobRepository _jobRepo;
        private readonly RecruitmentTimeCalculationService _recruitmentCalculationService;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly UnitDataReader _unitDataReader;

        public WorkshopService(
            ICityRepository cityRepo,
            IJobRepository jobRepo,
            IResourceService resService,
            IResearchService researchService,
            BuildingDataReader buildingDataReader,
            UnitDataReader unitDataReader,
            ICityStatService statService,
            RecruitmentTimeCalculationService recruitmentCalculationService)
        {
            _cityRepo = cityRepo;
            _jobRepo = jobRepo;
            _buildingDataReader = buildingDataReader;
            _unitDataReader = unitDataReader;
            _recruitmentCalculationService = recruitmentCalculationService;
        }
        public async Task<WorkshopFullViewDTO> GetWorkshopOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null) throw new Exception("City not found");

            var workshopBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Workshop);
            int currentBuildingLevel = workshopBuilding?.Level ?? 0;

            var workshopResponse = new WorkshopFullViewDTO { BuildingLevel = currentBuildingLevel };

            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Siege) continue;

                double calculatedRecruitmentTimePerUnit = await _recruitmentCalculationService.CalculateFinalRecruitmentTimeAsync(userId, cityEntity, unitStaticData);
                int alreadyOwnedCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                bool isUnitTypeUnlocked = currentBuildingLevel > 0;

                workshopResponse.AvailableUnits.Add(new WorkshopUnitInfoDTO
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

            return workshopResponse;
        }
    }
}
