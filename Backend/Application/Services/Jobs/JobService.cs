using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
using Domain.Enums;
using Domain.User;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Jobs
{
    public class JobService : IJobService
    {
        private readonly IBattleReportRepository _battleReportRepository;
        private readonly IResourceService _resourceService;
        private readonly ICityRepository _cityRepo;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly IWorldPlayerRepository _worldPlayerRepo;
        private readonly CityPointCalculator _cityPointCalculator;
        private readonly ILogger<JobService> _logger;

        public JobService(
            IBattleReportRepository battleReportRepository,
            IResourceService resourceService,
            ICityRepository cityRepo,
            ILogger<JobService> logger,
            IWorldPlayerRepository userRepo,
            IWorldPlayerService worldPlayerService,
            CityPointCalculator cityPointCalculator)
        {
            _battleReportRepository = battleReportRepository;
            _resourceService = resourceService;
            _cityRepo = cityRepo;
            _logger = logger;
            _worldPlayerRepo = userRepo;
            _worldPlayerService = worldPlayerService;
            _cityPointCalculator = cityPointCalculator;
        }

        public async Task ProcessAsync(BaseJob job)
        {
            _logger.LogInformation("[JobService] Processing {JobType} {JobId}", job.GetType().Name, job.Id);

            switch (job)
            {
                case BuildingJob buildingJob:
                    await ProcessCityJob(buildingJob, (c, j) => HandleBuildingJob(c, (BuildingJob)j));
                    break;
                case RecruitmentJob recruitmentJob:
                    await ProcessCityJob(recruitmentJob, (c, j) => HandleRecruitmentJob(c, (RecruitmentJob)j));
                    break;
                case ResearchJob researchJob:
                    await ProcessResearchJob(researchJob);
                    break;
            }
        }

        private async Task ProcessCityJob(BaseJob job, Action<City, BaseJob> handler)
        {
            var city = await _cityRepo.GetByIdAsync((Guid)job.GetType().GetProperty("CityId")!.GetValue(job)!);
            if (city == null) return;

            // 1. Synkroniser ressourcer til jobbets afslutningstidspunkt
            SyncResourcesToJobCompletion(city, job.ExecutionTime);

            // 2. Kør den specifikke logik (Bygning eller Rekruttering)
            handler(city, job);

            // 3. Gem byen. Workeren gemmer selve job-tilstanden efterfølgende.
            await _cityRepo.UpdateAsync(city);
            await CreateCompletionReportAsync(city, job);
        }

        private async Task ProcessResearchJob(ResearchJob job)
        {
            var player = await _worldPlayerRepo.GetByIdWithResearchAsync(job.WorldPlayerId);
            if (player == null) return;

            if (!player.CompletedResearches.Any(research => research.ResearchId == job.ResearchId))
            {
                player.CompletedResearches.Add(new Research
                {
                    WorldPlayerId = player.Id,
                    ResearchId = job.ResearchId,
                    CompletedAt = DateTime.UtcNow
                });
            }

            job.IsCompleted = true;
            await _worldPlayerRepo.UpdateAsync(player);
        }

        private void HandleBuildingJob(City city, BuildingJob job)
        {
            var building = city.Buildings.FirstOrDefault(x => x.Type == job.BuildingType);
            if (building == null)
                city.Buildings.Add(new Building { Type = job.BuildingType, Level = job.TargetLevel, CityId = city.Id });
            else
                building.Level = job.TargetLevel;

            city.Points = _cityPointCalculator.CalculateTotalPointsForCity(city);
            job.IsCompleted = true;
        }

        private void HandleRecruitmentJob(City city, RecruitmentJob job)
        {
            var now = DateTime.UtcNow;
            // Vi beregner hvor mange hele enheder der er færdige siden sidst
            double elapsedSeconds = (now - job.LastTickTime).TotalSeconds;
            int unitsToDeliver = (int)Math.Floor(elapsedSeconds / job.SecondsPerUnit);
            int remaining = job.TotalQuantity - job.CompletedQuantity;

            if (unitsToDeliver > remaining) unitsToDeliver = remaining;

            if (unitsToDeliver > 0)
            {
                var stack = city.UnitStacks.FirstOrDefault(x => x.Type == job.UnitType);
                if (stack == null)
                    city.UnitStacks.Add(new UnitStack { Type = job.UnitType, Quantity = unitsToDeliver });
                else
                    stack.Quantity += unitsToDeliver;

                job.CompletedQuantity += unitsToDeliver;
                job.LastTickTime = job.LastTickTime.AddSeconds(unitsToDeliver * job.SecondsPerUnit);
            }

            // VIGTIGT: Tjek completion og opdater næste ExecutionTime korrekt
            if (job.CompletedQuantity >= job.TotalQuantity)
            {
                job.IsCompleted = true;
            }
            else
            {
                job.IsCompleted = false;
                // Næste kørsel er præcis når den NÆSTE enhed er færdig
                job.ExecutionTime = job.LastTickTime.AddSeconds(job.SecondsPerUnit);
            }
        }

        private async Task CreateCompletionReportAsync(City city, BaseJob job)
        {
            if (!job.IsCompleted)
            {
                return;
            }

            switch (job)
            {
                case BuildingJob buildingJob:
                    await _battleReportRepository.AddAsync(new BattleReport
                    {
                        Id = Guid.NewGuid(),
                        WorldPlayerId = buildingJob.WorldPlayerId,
                        ReportType = ReportTypeEnum.BuildingCompleted,
                        Title = $"Construction completed: {buildingJob.BuildingType}",
                        Body = $"{buildingJob.BuildingType} level {buildingJob.TargetLevel} was completed in {city.Name}.",
                        OccurredAt = DateTime.UtcNow,
                        AttackerLossesJson = "[]",
                        DefenderLossesJson = "[]",
                        RevivedUnitsJson = "[]",
                        AppliedModifiersJson = "[]"
                    });
                    break;
                case RecruitmentJob recruitmentJob:
                    await _battleReportRepository.AddAsync(new BattleReport
                    {
                        Id = Guid.NewGuid(),
                        WorldPlayerId = recruitmentJob.WorldPlayerId,
                        ReportType = ReportTypeEnum.RecruitmentCompleted,
                        Title = $"Training completed: {recruitmentJob.UnitType}",
                        Body = $"{recruitmentJob.TotalQuantity} {recruitmentJob.UnitType} finished training in {city.Name}.",
                        OccurredAt = DateTime.UtcNow,
                        AttackerLossesJson = "[]",
                        DefenderLossesJson = "[]",
                        RevivedUnitsJson = "[]",
                        AppliedModifiersJson = "[]"
                    });
                    break;
            }
        }

        private void SyncResourcesToJobCompletion(City city, DateTime executionTime)
        {
            var snapshot = _resourceService.CalculateCityResources(city, executionTime);
            city.Wood = snapshot.Wood;
            city.Stone = snapshot.Stone;
            city.Metal = snapshot.Metal;
            city.LastResourceUpdate = executionTime;

            if (city.WorldPlayer != null)
                _worldPlayerService.SyncGlobalResources(city.WorldPlayer, executionTime);
        }
    }
}
