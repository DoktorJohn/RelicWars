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
        private readonly ICityRepository _cityRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IResourceService _resService;
        private readonly IResearchService _researchService;
        private readonly ICityStatService _statService;
        private readonly UnitDataReader _unitData;
        private readonly BuildingDataReader _buildingData;

        public RecruitmentService(ICityRepository cityRepo, IJobRepository jobRepo, IResourceService resService,
            IResearchService researchService, UnitDataReader unitData, BuildingDataReader buildingData, ICityStatService statService)
        {
            _cityRepo = cityRepo; _jobRepo = jobRepo; _resService = resService;
            _researchService = researchService; _unitData = unitData; _buildingData = buildingData;
            _statService = statService;
        }

        public async Task<BuildingResult> QueueRecruitmentAsync(Guid userId, Guid cityId, UnitTypeEnum type, int quantity)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            var unitStats = _unitData.GetUnit(type);
            var activeJobs = await _jobRepo.GetJobsByCityAsync(cityId);

            // --- POPULATION CHECK ---
            int availablePop = _statService.GetAvailablePopulation(city, activeJobs);
            int totalPopNeeded = quantity * unitStats.PopulationCost;

            if (totalPopNeeded > availablePop)
                return new BuildingResult(false, $"Ikke nok boliger. Kræver {totalPopNeeded} pladser, men kun {availablePop} er ledige.");

            // --- RESOURCES & PREREQUISITES ---
            var snapshot = _resService.CalculateCurrent(city, DateTime.UtcNow);
            if (snapshot.Wood < (unitStats.WoodCost * quantity) || snapshot.Stone < (unitStats.StoneCost * quantity) || snapshot.Metal < (unitStats.MetalCost * quantity))
                return new BuildingResult(false, "Ikke nok ressourcer.");

            // (Prerequisites tjek udeladt for korthed, men bør forblive her)

            // --- EXECUTION ---
            double secondsPerUnit = await CalculateFinalTime(userId, city, unitStats);

            city.Wood -= (unitStats.WoodCost * quantity);
            city.Stone -= (unitStats.StoneCost * quantity);
            city.Metal -= (unitStats.MetalCost * quantity);
            city.LastResourceUpdate = DateTime.UtcNow;

            await _cityRepo.UpdateAsync(city);
            await _jobRepo.AddAsync(new RecruitmentJob
            {
                UserId = userId,
                CityId = cityId,
                UnitType = type,
                TotalQuantity = quantity,
                SecondsPerUnit = secondsPerUnit,
                LastTickTime = DateTime.UtcNow,
                ExecutionTime = DateTime.UtcNow.AddSeconds(secondsPerUnit)
            });

            return new BuildingResult(true, $"Træning af {quantity}x {type} startet.");
        }

        private async Task<double> CalculateFinalTime(Guid userId, City city, UnitData unit)
        {
            var tags = new List<ModifierTagEnum>(unit.ModifiersThatAffects);
            if (!tags.Contains(ModifierTagEnum.Recruitment))
            {
                tags.Add(ModifierTagEnum.Recruitment);
            }

            var allModifiers = new List<Modifier>();

            BuildingTypeEnum buildingType = unit.Category switch
            {
                UnitCategoryEnum.Infantry => BuildingTypeEnum.Barracks,
                UnitCategoryEnum.Cavalry => BuildingTypeEnum.Stable,
                UnitCategoryEnum.Siege => BuildingTypeEnum.Workshop,
                _ => BuildingTypeEnum.Barracks
            };

            var building = city.Buildings.FirstOrDefault(b => b.Type == buildingType);
            if (building != null && building.Level > 0)
            {
                var config = _buildingData.GetConfig<BuildingLevelData>(buildingType, building.Level);
                if (config != null)
                {
                    allModifiers.AddRange(config.ModifiersInternal);
                }
            }

            var researchModifiers = await _researchService.GetUserResearchModifiersAsync(userId);
            allModifiers.AddRange(researchModifiers);

            double speedMultiplier = StatCalculator.ApplyModifiers(1.0, tags, allModifiers);
            double finalTime = unit.RecruitmentTimeInSeconds / Math.Max(0.1, speedMultiplier);

            return Math.Max(finalTime, 1.0);
        }
    }
}