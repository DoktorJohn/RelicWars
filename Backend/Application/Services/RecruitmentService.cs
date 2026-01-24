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

        public async Task<BuildingResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity)
        {
            if (quantity <= 0) return new BuildingResult(false, "Antal skal være større end 0.");

            var cityEntity = await _cityRepository.GetByIdAsync(cityId);
            if (cityEntity == null || cityEntity.WorldPlayer == null)
            {
                return new BuildingResult(false, "Byen eller ejeren blev ikke fundet.");
            }

            var unitStaticData = _unitDataReader.GetUnit(type);
            var currentDateTime = DateTime.UtcNow;

            List<BaseJob> activeJobsInCity = new();
            var recruitmentJobs = await _jobRepository.GetRecruitmentJobsAsync(cityId);
            var buildingJobs = await _jobRepository.GetBuildingJobsAsync(cityId);
            activeJobsInCity.AddRange(recruitmentJobs);
            activeJobsInCity.AddRange(buildingJobs);

            int availablePopulationCalculated = _cityStatService.GetAvailablePopulation(cityEntity, activeJobsInCity);
            int totalPopulationRequired = quantity * unitStaticData.PopulationCost;

            if (totalPopulationRequired > availablePopulationCalculated)
                return new BuildingResult(false, $"Ikke nok boliger. Kræver {totalPopulationRequired}, har {availablePopulationCalculated}.");

            _worldPlayerService.UpdateGlobalResourceState(cityEntity.WorldPlayer, currentDateTime);

            var citySnapshot = _resourceService.CalculateCityResources(cityEntity, currentDateTime);

            if (citySnapshot.Wood < (unitStaticData.WoodCost * quantity) ||
                citySnapshot.Stone < (unitStaticData.StoneCost * quantity) ||
                citySnapshot.Metal < (unitStaticData.MetalCost * quantity))
            {
                return new BuildingResult(false, "Ikke nok ressourcer i byens lager.");
            }

            double calculatedSecondsPerUnit = await _recruitmentTimeCalculationService.CalculateFinalRecruitmentTimeAsync(userId, cityEntity, unitStaticData);

            cityEntity.Wood = citySnapshot.Wood - (unitStaticData.WoodCost * quantity);
            cityEntity.Stone = citySnapshot.Stone - (unitStaticData.StoneCost * quantity);
            cityEntity.Metal = citySnapshot.Metal - (unitStaticData.MetalCost * quantity);

            cityEntity.LastResourceUpdate = currentDateTime;

            await _cityRepository.UpdateAsync(cityEntity);

            await _jobRepository.AddAsync(new RecruitmentJob
            {
                UserId = userId,
                CityId = cityId,
                UnitType = type,
                TotalQuantity = quantity,
                SecondsPerUnit = calculatedSecondsPerUnit,
                LastTickTime = currentDateTime,
                ExecutionTime = currentDateTime.AddSeconds(calculatedSecondsPerUnit),
                CompletedQuantity = 0
            });

            return new BuildingResult(true, $"Træning af {quantity}x {type} startet i {cityEntity.Name}.");
        }
    }
}