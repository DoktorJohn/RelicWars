using Application.DTOs;
using Application.Interfaces.IRepositories;
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
        private readonly IJobRepository _jobRepo;
        private readonly RecruitmentTimeCalculationService _recruitmentTimeCalculationService;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly UnitDataReader _unitDataReader;

        public BarracksService(ICityRepository cityRepo, BuildingDataReader buildingDataReader, IJobRepository jobRepo, UnitDataReader unitDataReader, RecruitmentTimeCalculationService recruitmentTimeCalculationService)
        {
            _cityRepo = cityRepo;
            _buildingDataReader = buildingDataReader;
            _jobRepo = jobRepo;
            _unitDataReader = unitDataReader;
            _recruitmentTimeCalculationService = recruitmentTimeCalculationService;
        }

        public async Task<BarracksFullViewDTO> GetBarracksOverviewAsync(Guid userId, Guid cityId)
        {
            var cityEntity = await _cityRepo.GetByIdAsync(cityId);
            if (cityEntity == null) throw new Exception("City not found");

            var barracksBuilding = cityEntity.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Barracks);
            int currentBuildingLevel = barracksBuilding?.Level ?? 0;

            var barracksResponse = new BarracksFullViewDTO
            {
                BuildingLevel = currentBuildingLevel
            };

            // 1. Hent og filtrer Jobs til Køen (Kun Infantry)
            var allActiveJobs = await _jobRepo.GetRecruitmentJobsAsync(cityId);

            foreach (var recruitmentJob in allActiveJobs)
            {
                if (recruitmentJob.UnitType == UnitTypeEnum.None) continue;

                var unitInformation = _unitDataReader.GetUnit(recruitmentJob.UnitType);

                // FILTER: Vis kun infanteri i kasernens kø
                if (unitInformation.Category != UnitCategoryEnum.Infantry) continue;

                int remainingUnitsInJob = recruitmentJob.TotalQuantity - recruitmentJob.CompletedQuantity;
                double timeUntilNextUnitCalculatedInSeconds = Math.Max(0, (recruitmentJob.ExecutionTime - DateTime.UtcNow).TotalSeconds);
                double totalRemainingTimeInSeconds = timeUntilNextUnitCalculatedInSeconds + ((remainingUnitsInJob - 1) * recruitmentJob.SecondsPerUnit);

                barracksResponse.RecruitmentQueue.Add(new RecruitmentQueueItemDTO
                {
                    QueueId = recruitmentJob.Id,
                    UnitType = recruitmentJob.UnitType,
                    Amount = remainingUnitsInJob,
                    TimeRemainingSeconds = totalRemainingTimeInSeconds,
                    TotalDurationSeconds = (int)(recruitmentJob.TotalQuantity * recruitmentJob.SecondsPerUnit)
                });
            }

            // 2. Hent tilgængelige enheder (Infantry)
            foreach (UnitTypeEnum unitTypeCandidate in Enum.GetValues(typeof(UnitTypeEnum)))
            {
                if (unitTypeCandidate == UnitTypeEnum.None) continue;

                var unitStaticData = _unitDataReader.GetUnit(unitTypeCandidate);
                if (unitStaticData == null || unitStaticData.Category != UnitCategoryEnum.Infantry) continue;

                double calculatedRecruitmentTimePerUnit = await _recruitmentTimeCalculationService.CalculateFinalRecruitmentTimeAsync(userId, cityEntity, unitStaticData);
                int currentUnitInventoryCount = cityEntity.UnitStacks.FirstOrDefault(stack => stack.Type == unitTypeCandidate)?.Quantity ?? 0;
                bool isUnitTypeUnlocked = currentBuildingLevel > 0;

                barracksResponse.AvailableUnits.Add(new BarracksUnitInfoDTO
                {
                    UnitType = unitTypeCandidate,
                    UnitName = unitStaticData.Type.ToString(),
                    CurrentInventoryCount = currentUnitInventoryCount,
                    CostWood = unitStaticData.WoodCost,
                    CostStone = unitStaticData.StoneCost,
                    CostMetal = unitStaticData.MetalCost,
                    RecruitmentTimeInSeconds = (int)calculatedRecruitmentTimePerUnit,
                    IsUnlocked = isUnitTypeUnlocked
                });
            }

            return barracksResponse;
        }
    }
}
