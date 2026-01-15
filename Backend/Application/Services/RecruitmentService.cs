using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers;
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
        private readonly IResearchService _researchService;
        private readonly ICityStatService _cityStatService;
        private readonly UnitDataReader _unitDataReader;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly RecruitmentTimeCalculationService _recruitmentTimeCalculationService;

        public RecruitmentService(
            ICityRepository cityRepository,
            IJobRepository jobRepository,
            IResourceService resourceService,
            IResearchService researchService,
            UnitDataReader unitDataReader,
            BuildingDataReader buildingDataReader,
            ICityStatService cityStatService,
            RecruitmentTimeCalculationService recruitmentTimeCalculationService)
        {
            _cityRepository = cityRepository;
            _jobRepository = jobRepository;
            _resourceService = resourceService;
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
            var unitStaticData = _unitDataReader.GetUnit(type);

            var activeJobsInCity = await _jobRepository.GetJobsByCityAsync(cityId);

            // --- POPULATION CHECK ---
            int availablePopulationCalculated = _cityStatService.GetAvailablePopulation(cityEntity, activeJobsInCity);
            int totalPopulationRequired = quantity * unitStaticData.PopulationCost;

            if (totalPopulationRequired > availablePopulationCalculated)
                return new BuildingResult(false, $"Ikke nok boliger. Kræver {totalPopulationRequired}, har {availablePopulationCalculated}.");

            // --- RESOURCES ---
            var resourceSnapshot = _resourceService.CalculateCurrent(cityEntity, DateTime.UtcNow);

            if (resourceSnapshot.Wood < (unitStaticData.WoodCost * quantity) ||
                resourceSnapshot.Stone < (unitStaticData.StoneCost * quantity) ||
                resourceSnapshot.Metal < (unitStaticData.MetalCost * quantity))
                return new BuildingResult(false, "Ikke nok ressourcer.");

            // --- EXECUTION TIME ---
            double calculatedSecondsPerUnit = await _recruitmentTimeCalculationService.CalculateFinalRecruitmentTimeAsync(userId, cityEntity, unitStaticData);

            cityEntity.Wood = resourceSnapshot.Wood - (unitStaticData.WoodCost * quantity);
            cityEntity.Stone = resourceSnapshot.Stone - (unitStaticData.StoneCost * quantity);
            cityEntity.Metal = resourceSnapshot.Metal - (unitStaticData.MetalCost * quantity);
            cityEntity.LastResourceUpdate = DateTime.UtcNow;

            await _cityRepository.UpdateAsync(cityEntity);

            await _jobRepository.AddAsync(new RecruitmentJob
            {
                UserId = userId,
                CityId = cityId,
                UnitType = type,
                TotalQuantity = quantity,
                SecondsPerUnit = calculatedSecondsPerUnit,
                LastTickTime = DateTime.UtcNow,
                ExecutionTime = DateTime.UtcNow.AddSeconds(calculatedSecondsPerUnit),
                CompletedQuantity = 0
            });

            return new BuildingResult(true, $"Træning af {quantity}x {type} startet.");
        }
    }
}