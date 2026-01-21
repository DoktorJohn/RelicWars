using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
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
        private readonly ICityRepository _cityRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IResourceService _resService;
        private readonly ICityStatService _statService;
        private readonly BuildingDataReader _buildingDataReader;

        public BuildingService(
            ICityRepository cityRepo,
            IJobRepository jobRepo,
            IResourceService resService,
            BuildingDataReader buildingDataReader,
            ICityStatService statService)
        {
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

            // --- POPULATION CHECK ---
            int availablePop = _statService.GetAvailablePopulation(city, activeJobs);
            int currentPopCost = currentLevel > 0
                ? _buildingDataReader.GetConfig<BuildingLevelData>(type, currentLevel).PopulationCost
                : 0;
            int additionalNeeded = config.PopulationCost - currentPopCost;

            if (additionalNeeded > availablePop)
                return new BuildingResult(false, $"Mangler population: {additionalNeeded - availablePop} flere frie borgere påkrævet.");

            // --- PREREQUISITES ---
            foreach (var req in config.Prerequisites)
            {
                var baseLevel = city.Buildings.FirstOrDefault(b => b.Type == req.Type)?.Level ?? 0;
                if ((baseLevel + buildingJobs.Count(j => j.BuildingType == req.Type)) < req.RequiredLevel)
                    return new BuildingResult(false, $"Mangler krav: {req.Type} lvl {req.RequiredLevel}.");
            }

            // --- RESOURCE CALCULATION ---
            var snapshot = _resService.CalculateCurrent(city, DateTime.UtcNow);

            if (snapshot.Wood < config.WoodCost || snapshot.Stone < config.StoneCost || snapshot.Metal < config.MetalCost)
                return new BuildingResult(false, "Ikke nok ressourcer.");

            // --- EXECUTION ---
            DateTime startTime = buildingJobs.Any() ? buildingJobs.Last().ExecutionTime : DateTime.UtcNow;

            // Opdater byens ressourcer baseret på snapshot
            city.Wood = snapshot.Wood - config.WoodCost;
            city.Stone = snapshot.Stone - config.StoneCost;
            city.Metal = snapshot.Metal - config.MetalCost;
            // city.Silver = snapshot.Silver - config.SilverCost; // Hvis bygninger begynder at koste Silver

            city.LastResourceUpdate = DateTime.UtcNow;

            await _cityRepo.UpdateAsync(city);
            await _jobRepo.AddAsync(new BuildingJob
            {
                UserId = city.WorldPlayerId.Value,
                CityId = cityId,
                BuildingType = type,
                TargetLevel = nextLevel,
                ExecutionTime = startTime.Add(config.BuildTime)
            });

            return new BuildingResult(true, $"{type} lvl {nextLevel} i kø.");
        }
    }
}