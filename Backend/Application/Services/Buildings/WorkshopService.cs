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

            // 1. Hent og filtrer Jobs til Køen (Kun Siege)
            var allActiveJobs = await _jobRepo.GetRecruitmentJobsAsync(cityId);
            var recruitmentJobsForWorkshop = allActiveJobs.OfType<RecruitmentJob>()
                .OrderBy(job => job.ExecutionTime)
                .ToList();

            foreach (var recruitmentJob in recruitmentJobsForWorkshop)
            {
                if (recruitmentJob.UnitType == UnitTypeEnum.None) continue;

                var unitInformation = _unitDataReader.GetUnit(recruitmentJob.UnitType);

                // FILTER: Vis kun belejringsvåben i værkstedets kø
                if (unitInformation.Category != UnitCategoryEnum.Siege) continue;

                int remainingUnitsInJob = recruitmentJob.TotalQuantity - recruitmentJob.CompletedQuantity;
                double timeUntilNextUnitCalculatedInSeconds = Math.Max(0, (recruitmentJob.ExecutionTime - DateTime.UtcNow).TotalSeconds);
                double totalRemainingTimeInSeconds = timeUntilNextUnitCalculatedInSeconds + ((remainingUnitsInJob - 1) * recruitmentJob.SecondsPerUnit);

                workshopResponse.RecruitmentQueue.Add(new RecruitmentQueueItemDTO
                {
                    QueueId = recruitmentJob.Id,
                    UnitType = recruitmentJob.UnitType,
                    Amount = remainingUnitsInJob,
                    TimeRemainingSeconds = totalRemainingTimeInSeconds,
                    TotalDurationSeconds = (int)(recruitmentJob.TotalQuantity * recruitmentJob.SecondsPerUnit)
                });
            }

            // 2. Hent tilgængelige enheder (Siege)
            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Siege) continue;

                double calculatedRecruitmentTimePerUnit = await _recruitmentCalculationService.CalculateFinalRecruitmentTimeAsync(userId, cityEntity, unitStaticData);
                int currentUnitInventoryCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;

                workshopResponse.AvailableUnits.Add(new WorkshopUnitInfoDTO
                {
                    UnitType = unitTypeCandidate,
                    UnitName = unitStaticData.Type.ToString(),
                    CurrentInventoryCount = currentUnitInventoryCount,
                    CostWood = unitStaticData.WoodCost,
                    CostStone = unitStaticData.StoneCost,
                    CostMetal = unitStaticData.MetalCost,
                    RecruitmentTimeInSeconds = (int)calculatedRecruitmentTimePerUnit,
                    IsUnlocked = currentBuildingLevel > 0
                });
            }

            return workshopResponse;
        }
    }
}
