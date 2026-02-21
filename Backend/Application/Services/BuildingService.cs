using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Abstraction;
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
    public class BuildingService : IBuildingService
    {
        private readonly IModifierService _modifierService;
        private readonly ICityRepository _cityRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IResourceService _resService;
        private readonly ICityStatService _statService;
        private readonly BuildingDataReader _buildingDataReader;

        public BuildingService(
            IModifierService modifierService,
            ICityRepository cityRepo,
            IJobRepository jobRepo,
            IResourceService resService,
            BuildingDataReader buildingDataReader,
            ICityStatService statService)
        {
            _modifierService = modifierService;
            _cityRepo = cityRepo;
            _jobRepo = jobRepo;
            _resService = resService;
            _buildingDataReader = buildingDataReader;
            _statService = statService;
        }

        public async Task<List<BuildingDTO>> GetBuildingQueueAsync(Guid cityId)
        {
            var activeJobsInCity = await _jobRepo.GetBuildingJobsAsync(cityId);

            return activeJobsInCity
                .OfType<BuildingJob>()
                .OrderBy(job => job.ExecutionTime)
                .Select(job => new BuildingDTO(
                    job.Id,
                    job.BuildingType.ToString(),
                    job.TargetLevel,
                    job.ExecutionTime,
                    true
                ))
                .ToList();
        }

        public async Task<BuildingResult> QueueUpgradeAsync(Guid cityId, BuildingTypeEnum type)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null || !city.WorldPlayerId.HasValue)
                return new BuildingResult(false, "Byen eller ejeren blev ikke fundet.");

            var activeJobs = await _jobRepo.GetBuildingJobsAsync(cityId);
            var buildingJobs = activeJobs.OfType<BuildingJob>().ToList();

            if (buildingJobs.Count >= 5)
                return new BuildingResult(false, "Byggekøen er fuld.");

            var currentBuilding = city.Buildings.FirstOrDefault(b => b.Type == type);
            int currentLevel = currentBuilding?.Level ?? 0;
            int nextLevel = currentLevel + buildingJobs.Count(j => j.BuildingType == type) + 1;

            if (nextLevel > 30) return new BuildingResult(false, "Maksimum niveau nået.");

            var config = _buildingDataReader.GetConfig<BuildingLevelData>(type, nextLevel);


            // Brug den nye arkitektur: Send byen, baseværdien, og det tag der skal beregnes på.
            var buildingWoodCost = _modifierService.CalculateCityValue(city, config.WoodCost, ModifierTagEnum.ConstructionCost);
            var buildingStoneCost = _modifierService.CalculateCityValue(city, config.StoneCost, ModifierTagEnum.ConstructionCost);
            var buildingMetalCost = _modifierService.CalculateCityValue(city, config.MetalCost, ModifierTagEnum.ConstructionCost);

            // Bonus: Hvis du også vil anvende modifiers på byggetiden (fx en 'Construction' tag):
            var buildingTimeResult = _modifierService.CalculateCityValue(city, config.BuildTime.TotalSeconds, ModifierTagEnum.Construction);
            TimeSpan finalBuildTime = TimeSpan.FromSeconds(buildingTimeResult.FinalValue);


            // --- PREREQUISITES ---
            foreach (var req in config.Prerequisites)
            {
                var baseLevel = city.Buildings.FirstOrDefault(b => b.Type == req.Type)?.Level ?? 0;
                if ((baseLevel + buildingJobs.Count(j => j.BuildingType == req.Type)) < req.RequiredLevel)
                    return new BuildingResult(false, $"Mangler krav: {req.Type} lvl {req.RequiredLevel}.");
            }

            // --- RESOURCE CALCULATION ---
            var snapshot = _resService.CalculateCityResources(city, DateTime.UtcNow);

            if (snapshot.Wood < buildingWoodCost.FinalValue ||
                snapshot.Stone < buildingStoneCost.FinalValue ||
                snapshot.Metal < buildingMetalCost.FinalValue)
            {
                return new BuildingResult(false, "Ikke nok ressourcer.");
            }

            // --- EXECUTION ---
            DateTime startTime = buildingJobs.Any() ? buildingJobs.Last().ExecutionTime : DateTime.UtcNow;

            // Opdater byens ressourcer baseret på den modificerede pris
            city.Wood = snapshot.Wood - buildingWoodCost.FinalValue;
            city.Stone = snapshot.Stone - buildingStoneCost.FinalValue;
            city.Metal = snapshot.Metal - buildingMetalCost.FinalValue;

            city.LastResourceUpdate = DateTime.UtcNow;

            await _cityRepo.UpdateAsync(city);

            await _jobRepo.AddAsync(new BuildingJob
            {
                WorldPlayerId = city.WorldPlayerId.Value,
                CityId = cityId,
                BuildingType = type,
                TargetLevel = nextLevel,
                ExecutionTime = startTime.Add(finalBuildTime)
            });

            return new BuildingResult(true, $"{type} lvl {nextLevel} i kø.");
        }
    }
}