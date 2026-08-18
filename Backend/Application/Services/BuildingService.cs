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
using Application.Utility;
using Application.Interfaces;

namespace Application.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IModifierService _modifierService;
        private readonly IPlayerAccessService _playerAccessService;
        private readonly ICityRepository _cityRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IResourceService _resService;
        private readonly ICityStatService _statService;
        private readonly BuildingDataReader _buildingDataReader;
        private readonly ConstructionTimeCalculator _constructionTimeCalculator;
        private readonly ITransactionManager _transactionManager;

        public BuildingService(
            IModifierService modifierService,
            ICityRepository cityRepo,
            IJobRepository jobRepo,
            IResourceService resService,
            BuildingDataReader buildingDataReader,
            ICityStatService statService,
            ConstructionTimeCalculator constructionTimeCalculator,
            IPlayerAccessService playerAccessService,
            ITransactionManager transactionManager)
        {
            _modifierService = modifierService;
            _cityRepo = cityRepo;
            _jobRepo = jobRepo;
            _resService = resService;
            _buildingDataReader = buildingDataReader;
            _statService = statService;
            _constructionTimeCalculator = constructionTimeCalculator;
            _playerAccessService = playerAccessService;
            _transactionManager = transactionManager;
        }

        public async Task<List<BuildingDTO>> GetBuildingQueueAsync(Guid cityId)
        {
            await _playerAccessService.RequireOwnedCityAsync(cityId);
            var activeJobsInCity = await _jobRepo.GetBuildingJobsAsync(cityId);

            var orderedJobs = activeJobsInCity
                .OfType<BuildingJob>()
                .OrderBy(job => job.ExecutionTime)
                .ThenBy(job => job.Id)
                .ToList();

            return MapBuildingQueue(orderedJobs);
        }

        public async Task<BuildingResult> QueueUpgradeAsync(Guid cityId, BuildingTypeEnum type)
        {
            return await _transactionManager.ExecuteAsync(async () =>
            {
                await _cityRepo.AcquireBuildingQueueLockAsync(cityId);
                var city = await _playerAccessService.RequireOwnedCityAsync(cityId);
                return await QueueUpgradeForCityAsync(city, type, transactionOwned: true);
            });
        }

        public async Task<List<BuildingDTO>> CancelQueuedUpgradeAsync(Guid cityId, Guid jobId)
        {
            return await _transactionManager.ExecuteAsync(async () =>
            {
                await _cityRepo.AcquireBuildingQueueLockAsync(cityId);
                await _playerAccessService.RequireOwnedCityAsync(cityId);

                var jobs = (await _jobRepo.GetBuildingJobsAsync(cityId))
                    .OrderBy(job => job.ExecutionTime)
                    .ThenBy(job => job.Id)
                    .ToList();
                var requested = jobs.FirstOrDefault(job => job.Id == jobId)
                    ?? throw new KeyNotFoundException("Building queue job was not found.");

                if (requested.ExecutionTime <= DateTime.UtcNow)
                    throw new InvalidOperationException("A due building job cannot be cancelled.");
                if (jobs[^1].Id != requested.Id)
                    throw new InvalidOperationException("Only the last building queue job can be cancelled.");

                await _jobRepo.DeleteAsync(requested.Id);
                jobs.Remove(requested);
                return MapBuildingQueue(jobs);
            });
        }

        public async Task<BuildingResult> QueueNPCUpgradeAsync(Guid cityId, BuildingTypeEnum type)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null || !city.IsNPC || city.WorldPlayerId != null)
                return new BuildingResult(false, "NPC-byen blev ikke fundet.");

            return await QueueUpgradeForCityAsync(city, type);
        }

        public Task<BuildingResult> QueueNPCUpgradeAsync(City city, BuildingTypeEnum type)
        {
            if (!city.IsNPC || city.WorldPlayerId != null)
                return Task.FromResult(new BuildingResult(false, "NPC-byen blev ikke fundet."));

            return QueueUpgradeForCityAsync(city, type, Array.Empty<BuildingJob>());
        }

        private async Task<BuildingResult> QueueUpgradeForCityAsync(
            City city,
            BuildingTypeEnum type,
            IReadOnlyCollection<BuildingJob>? knownBuildingJobs = null,
            bool transactionOwned = false)
        {
            Guid cityId = city.Id;

            var buildingJobs = knownBuildingJobs?.ToList()
                ?? await _jobRepo.GetBuildingJobsAsync(cityId);

            int maximumQueueSize = city.IsNPC ? 1 : 7;
            if (buildingJobs.Count >= maximumQueueSize)
                return new BuildingResult(false, "Byggekøen er fuld.");

            var currentBuilding = city.Buildings.FirstOrDefault(b => b.Type == type);
            int queuedLevels = buildingJobs.Count(j => j.BuildingType == type);
            int nextLevel = currentBuilding is null
                ? queuedLevels + 1
                : currentBuilding.Level + queuedLevels + 1;

            int maximumLevel = _buildingDataReader.GetMaximumLevel(type);
            if (nextLevel > maximumLevel) return new BuildingResult(false, "Maksimum niveau nået.");

            var config = _buildingDataReader.GetConfig<BuildingLevelData>(type, nextLevel);


            // Brug den nye arkitektur: Send byen, baseværdien, og det tag der skal beregnes på.
            var buildingWoodCost = _modifierService.CalculateCityValue(city, config.WoodCost, ModifierTagEnum.ConstructionCost);
            var buildingStoneCost = _modifierService.CalculateCityValue(city, config.StoneCost, ModifierTagEnum.ConstructionCost);
            var buildingMetalCost = _modifierService.CalculateCityValue(city, config.MetalCost, ModifierTagEnum.ConstructionCost);

            // Bonus: Hvis du også vil anvende modifiers på byggetiden (fx en 'Construction' tag):
            TimeSpan finalBuildTime = TimeSpan.FromSeconds(
                _constructionTimeCalculator.CalculateSeconds(city, config.BuildTime.TotalSeconds));


            // --- PREREQUISITES ---
            foreach (var req in config.Prerequisites)
            {
                var prerequisiteBuilding = city.Buildings.FirstOrDefault(b => b.Type == req.Type);
                int queuedPrerequisiteLevels = buildingJobs.Count(j => j.BuildingType == req.Type);
                int effectivePrerequisiteLevel = prerequisiteBuilding is null
                    ? queuedPrerequisiteLevels
                    : prerequisiteBuilding.Level + queuedPrerequisiteLevels;

                if (effectivePrerequisiteLevel < req.RequiredLevel)
                    return new BuildingResult(false, $"Mangler krav: {req.Type} lvl {req.RequiredLevel}.");
            }

            // --- RESOURCE CALCULATION ---
            DateTime currentDateTime = DateTime.UtcNow;
            var snapshot = _resService.CalculateCityResources(city, currentDateTime);

            if (snapshot.Wood < buildingWoodCost.FinalValue ||
                snapshot.Stone < buildingStoneCost.FinalValue ||
                snapshot.Metal < buildingMetalCost.FinalValue)
            {
                return new BuildingResult(false, "Ikke nok ressourcer.");
            }

            // --- EXECUTION ---
            DateTime startTime = buildingJobs.Any() ? buildingJobs.Last().ExecutionTime : currentDateTime;

            // Opdater byens ressourcer baseret på den modificerede pris
            city.Wood = snapshot.Wood - buildingWoodCost.FinalValue;
            city.Stone = snapshot.Stone - buildingStoneCost.FinalValue;
            city.Metal = snapshot.Metal - buildingMetalCost.FinalValue;

            city.LastResourceUpdate = currentDateTime;

            var buildingJob = new BuildingJob
            {
                WorldPlayerId = city.WorldPlayerId ?? Guid.Empty,
                CityId = cityId,
                BuildingType = type,
                TargetLevel = nextLevel,
                ExecutionTime = startTime.Add(finalBuildTime)
            };

            async Task PersistAsync()
            {
                await _cityRepo.UpdateAsync(city);
                await _jobRepo.AddAsync(buildingJob);
            }

            if (transactionOwned)
                await PersistAsync();
            else
                await _transactionManager.ExecuteAsync(PersistAsync);

            return new BuildingResult(true, $"{type} lvl {nextLevel} i kø.");
        }

        private static List<BuildingDTO> MapBuildingQueue(IEnumerable<BuildingJob> jobs)
        {
            List<BuildingJob> orderedJobs = jobs
                .OrderBy(job => job.ExecutionTime)
                .ThenBy(job => job.Id)
                .ToList();

            var result = new List<BuildingDTO>(orderedJobs.Count);
            foreach (BuildingJob job in orderedJobs)
            {
                result.Add(new BuildingDTO(
                    job.Id,
                    job.BuildingType.ToString(),
                    job.TargetLevel,
                    job.DateCreated,
                    job.ExecutionTime,
                    true));
            }

            return result;
        }

        public async Task<BuildingResult> RepairAsync(Guid cityId, BuildingTypeEnum type)
        {
            var city = await _playerAccessService.RequireOwnedCityAsync(cityId);
            var building = city?.Buildings.FirstOrDefault(x => x.Type == type);
            if (city == null || building == null) return new BuildingResult(false, "Building not found.");
            if (building.Damage <= 0) return new BuildingResult(false, "Building is not damaged.");

            double baseCost = building.Damage * Math.Max(1, building.Level) * 10;
            double cost = _modifierService.CalculateCityValue(city, baseCost, ModifierTagEnum.RepairCost).FinalValue;
            if (city.Stone < cost) return new BuildingResult(false, "Not enough stone.");
            city.Stone -= cost;
            building.Damage = 0;
            await _cityRepo.UpdateAsync(city);
            return new BuildingResult(true, $"{type} repaired for {cost:0} stone.");
        }
    }
}
