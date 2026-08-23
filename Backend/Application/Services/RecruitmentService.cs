using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Interfaces;
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
        private readonly IPlayerAccessService _playerAccessService;
        private readonly ICityStatService _cityStatService;
        private readonly UnitDataReader _unitDataReader;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly RecruitmentTimeCalculationService _recruitmentTimeCalculationService;
        private readonly ITransactionManager _transactionManager;
        private readonly UnitAvailabilityEvaluator _unitAvailabilityEvaluator;

        public RecruitmentService(
            ICityRepository cityRepository,
            IJobRepository jobRepository,
            IResourceService resourceService,
            IWorldPlayerService worldPlayerService,
            IPlayerAccessService playerAccessService,
            UnitDataReader unitDataReader,
            BuildingDataReader buildingDataReader,
            ICityStatService cityStatService,
            RecruitmentTimeCalculationService recruitmentTimeCalculationService,
            ITransactionManager transactionManager,
            UnitAvailabilityEvaluator unitAvailabilityEvaluator)
        {
            _cityRepository = cityRepository;
            _jobRepository = jobRepository;
            _resourceService = resourceService;
            _worldPlayerService = worldPlayerService;
            _playerAccessService = playerAccessService;
            _unitDataReader = unitDataReader;
            _buildingDataReader = buildingDataReader;
            _cityStatService = cityStatService;
            _recruitmentTimeCalculationService = recruitmentTimeCalculationService;
            _transactionManager = transactionManager;
            _unitAvailabilityEvaluator = unitAvailabilityEvaluator;
        }

        public async Task<RecruitmentResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity)
        {
            // 1. Hent entiteten for byen
            var cityEntity = await _playerAccessService.RequireOwnedCityAsync(cityId);

            var unitStaticData = _unitDataReader.GetUnit(type);
            var currentDateTime = DateTime.UtcNow;

            if (quantity <= 0)
            {
                return new RecruitmentResult(false, "Antallet af enheder skal være positivt.");
            }

            var availability = _unitAvailabilityEvaluator.Evaluate(cityEntity, unitStaticData);
            if (!availability.IsUnlocked)
            {
                return new RecruitmentResult(false, $"Unit locked: {string.Join(" ", availability.UnmetRequirements)}");
            }

            // 2. Saml alle aktive jobs for at beregne nuværende befolkningskapacitet
            List<BaseJob> activeJobsInCity = new List<BaseJob>();
            var existingRecruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityId);
            var existingBuildingJobs = await _jobRepository.GetBuildingJobsAsync(cityId);

            activeJobsInCity.AddRange(existingRecruitmentJobs);
            activeJobsInCity.AddRange(existingBuildingJobs);

            // 3. Valider om der er tilstrækkelig ledig population
            int currentlyAvailablePopulation = _cityStatService.GetAvailablePopulation(cityEntity, activeJobsInCity);
            int totalPopulationRequiredForRequest = quantity * unitStaticData.PopulationCost;

            if (totalPopulationRequiredForRequest > currentlyAvailablePopulation)
            {
                return new RecruitmentResult(false, $"Utilstrækkelig boligkapacitet. Kræver {totalPopulationRequiredForRequest}, men der er kun {currentlyAvailablePopulation} ledig.");
            }

            // 4. Opdater ressourcetilstand (Globalt og lokalt) før fratrækning
            await _worldPlayerService.SyncGlobalResourcesAsync(cityEntity.WorldPlayer, currentDateTime);
            var cityResourceSnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);

            // 5. Valider om byen har råd til rekrutteringen
            var modifiedUnitCosts = _recruitmentTimeCalculationService.CalculateFinalResourceCosts(cityEntity, unitStaticData);
            double totalWoodCost = modifiedUnitCosts.wood * quantity;
            double totalStoneCost = modifiedUnitCosts.stone * quantity;
            double totalMetalCost = modifiedUnitCosts.metal * quantity;

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

            await _transactionManager.ExecuteAsync(async () =>
            {
                await _cityRepository.UpdateAsync(cityEntity);
                await _jobRepository.AddAsync(recruitmentJob);
            });

            // 9. Returner resultatet (Uden populationstallet da det nu håndteres via DetailedCityInfo synkronisering)
            return new RecruitmentResult(true, $"Træning af {quantity} er påbegyndt i {cityEntity.Name}.");
        }


        public async Task<List<RecruitmentQueueItemDTO>> GetRecruitmentQueueAsync(GetRecruitmentQueueItemsDTO dto)
        {
            await _playerAccessService.RequireOwnedCityAsync(dto.CityId);
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
                    TotalDurationSeconds = (int)(recruitmentJob.TotalQuantity * recruitmentJob.SecondsPerUnit),
                    SecondsPerUnit = recruitmentJob.SecondsPerUnit
                });
            }

            return queueItemDTO;

        }

        public async Task<RecruitmentResult> CancelRecruitmentAsync(Guid cityId, Guid queueId)
        {
            await _playerAccessService.RequireOwnedCityAsync(cityId);

            return await _transactionManager.ExecuteAsync(async () =>
            {
                var allJobs = (await _jobRepository.GetRecruitmentJobsAsync(cityId))
                    .Where(job => !job.IsCompleted)
                    .ToList();
                var requestedJob = allJobs.FirstOrDefault(job => job.Id == queueId);
                if (requestedJob == null)
                {
                    return new RecruitmentResult(false, "Recruitment queue item was not found.");
                }

                UnitCategoryEnum requestedCategory = _unitDataReader.GetUnit(requestedJob.UnitType).Category;
                var jobs = allJobs
                    .Where(job => IsSameRecruitmentBuilding(
                        requestedCategory,
                        _unitDataReader.GetUnit(job.UnitType).Category))
                    .OrderBy(job => job.ExecutionTime)
                    .ThenBy(job => job.Id)
                    .ToList();

                if (jobs[^1].Id != queueId)
                {
                    return new RecruitmentResult(false, "Only the last recruitment queue item can be cancelled.");
                }

                await _jobRepository.DeleteAsync(queueId);
                return new RecruitmentResult(true, "Recruitment cancelled.");
            });
        }

        private static bool IsSameRecruitmentBuilding(UnitCategoryEnum first, UnitCategoryEnum second)
        {
            bool firstIsBarracks = first is UnitCategoryEnum.Infantry or UnitCategoryEnum.Ranged;
            bool secondIsBarracks = second is UnitCategoryEnum.Infantry or UnitCategoryEnum.Ranged;
            if (firstIsBarracks) return secondIsBarracks;

            bool firstIsWorkshop = first is UnitCategoryEnum.Siege or UnitCategoryEnum.Support;
            bool secondIsWorkshop = second is UnitCategoryEnum.Siege or UnitCategoryEnum.Support;
            return firstIsWorkshop ? secondIsWorkshop : first == second;
        }
    }
}
