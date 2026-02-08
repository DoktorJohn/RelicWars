using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers;
using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RecruitmentService : IRecruitmentService
    {
        private readonly ICityRepository _cityRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IResourceService _resourceService;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly IResearchService _researchService;
        private readonly ICityStatService _cityStatService;
        private readonly UnitDataReader _unitDataReader;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly RecruitmentTimeCalculationService _recruitmentTimeCalculationService;

        public RecruitmentService(
            ICityRepository cityRepository,
            IJobRepository jobRepository,
            IResourceService resourceService,
            IWorldPlayerService worldPlayerService,
            IResearchService researchService,
            UnitDataReader unitDataReader,
            BuildingDataReader buildingDataReader,
            ICityStatService cityStatService,
            RecruitmentTimeCalculationService recruitmentTimeCalculationService)
        {
            _cityRepository = cityRepository;
            _jobRepository = jobRepository;
            _resourceService = resourceService;
            _worldPlayerService = worldPlayerService;
            _researchService = researchService;
            _unitDataReader = unitDataReader;
            _buildingDataReader = buildingDataReader;
            _cityStatService = cityStatService;
            _recruitmentTimeCalculationService = recruitmentTimeCalculationService;
        }

        public async Task<RecruitmentResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity)
        {
            // 1. Hent entiteten for byen
            var cityEntity = await _cityRepository.GetByIdAsync(cityId);
            if (cityEntity == null)
            {
                return new RecruitmentResult(false, "Den forespurgte by blev ikke fundet.");
            }

            var unitStaticData = _unitDataReader.GetUnit(type);
            var currentDateTime = DateTime.UtcNow;

            // 2. Saml alle aktive jobs for at beregne nuværende befolkningskapacitet
            List<BaseJob> activeJobsInCity = new List<BaseJob>();
            var existingRecruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityId);
            var existingBuildingJobs = await _jobRepository.GetBuildingJobsAsync(cityId);

            activeJobsInCity.AddRange(existingRecruitmentJobs);
            activeJobsInCity.AddRange(existingBuildingJobs);

            // 3. Valider om der er tilstrækkelig ledig population
            int currentlyAvailablePopulation = _cityStatService.GetAvailablePopulation(cityEntity, activeJobsInCity);
            int totalPopulationRequiredForRequest = quantity * unitStaticData.PopulationCost;

            if (quantity <= 0)
            {
                return new RecruitmentResult(false, "Antallet af enheder skal være positivt.");
            }

            if (totalPopulationRequiredForRequest > currentlyAvailablePopulation)
            {
                return new RecruitmentResult(false, $"Utilstrækkelig boligkapacitet. Kræver {totalPopulationRequiredForRequest}, men der er kun {currentlyAvailablePopulation} ledig.");
            }

            // 4. Opdater ressourcetilstand (Globalt og lokalt) før fratrækning
            _worldPlayerService.UpdateGlobalResourceState(cityEntity.WorldPlayer, currentDateTime);
            var cityResourceSnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);

            // 5. Valider om byen har råd til rekrutteringen
            double totalWoodCost = unitStaticData.WoodCost * quantity;
            double totalStoneCost = unitStaticData.StoneCost * quantity;
            double totalMetalCost = unitStaticData.MetalCost * quantity;

            if (cityResourceSnapshot.Wood < totalWoodCost ||
                cityResourceSnapshot.Stone < totalStoneCost ||
                cityResourceSnapshot.Metal < totalMetalCost)
            {
                return new RecruitmentResult(false, "Byen mangler de nødvendige råmaterialer til denne rekruttering.");
            }

            // 6. Beregn endelig træningstid baseret på modifiers og træk ressourcerne
            double calculatedSecondsPerUnit = await _recruitmentTimeCalculationService.CalculateFinalRecruitmentTimeAsync(userId, cityEntity, unitStaticData);

            cityEntity.Wood = cityResourceSnapshot.Wood - totalWoodCost;
            cityEntity.Stone = cityResourceSnapshot.Stone - totalStoneCost;
            cityEntity.Metal = cityResourceSnapshot.Metal - totalMetalCost;
            cityEntity.LastResourceUpdate = currentDateTime;

            // 7. Persister ændringer til databasen
            await _cityRepository.UpdateAsync(cityEntity);

            // 8. Opret og gem selve rekrutterings-jobbet
            var recruitmentJob = new RecruitmentJob
            {
                WorldPlayerId = userId,
                CityId = cityId,
                UnitType = type,
                TotalQuantity = quantity,
                SecondsPerUnit = calculatedSecondsPerUnit,
                LastTickTime = currentDateTime,
                ExecutionTime = currentDateTime.AddSeconds(calculatedSecondsPerUnit),
                CompletedQuantity = 0,
                IsCompleted = false
            };

            await _jobRepository.AddAsync(recruitmentJob);

            // 9. Returner resultatet (Uden populationstallet da det nu håndteres via DetailedCityInfo synkronisering)
            return new RecruitmentResult(true, $"Træning af {quantity} er påbegyndt i {cityEntity.Name}.");
        }


        public async Task<List<RecruitmentQueueItemDTO>> GetRecruitmentQueueAsync(GetRecruitmentQueueItemsDTO dto)
        {

            var allActiveJobs = await _jobRepository.GetRecruitmentJobsAsync(dto.CityId);
            var queueItemDTO = new List<RecruitmentQueueItemDTO>();


            foreach (var recruitmentJob in allActiveJobs)
            {
                if (recruitmentJob.UnitType == UnitTypeEnum.None) continue;

                var unitInformation = _unitDataReader.GetUnit(recruitmentJob.UnitType);

                if (!dto.UnitCategories.Contains(unitInformation.Category)) continue;

                int remainingUnitsInJob = recruitmentJob.TotalQuantity - recruitmentJob.CompletedQuantity;
                double timeUntilNextUnitCalculatedInSeconds = Math.Max(0, (recruitmentJob.ExecutionTime - DateTime.UtcNow).TotalSeconds);
                double totalRemainingTimeInSeconds = timeUntilNextUnitCalculatedInSeconds + ((remainingUnitsInJob - 1) * recruitmentJob.SecondsPerUnit);

                queueItemDTO.Add(new RecruitmentQueueItemDTO
                {
                    QueueId = recruitmentJob.Id,
                    UnitType = recruitmentJob.UnitType,
                    Amount = remainingUnitsInJob,
                    TimeRemainingSeconds = totalRemainingTimeInSeconds,
                    TotalDurationSeconds = (int)(recruitmentJob.TotalQuantity * recruitmentJob.SecondsPerUnit)
                });
            }

            return queueItemDTO;

        }
    }
}