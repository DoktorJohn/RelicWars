using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Application.Utility;
using Domain.Entities;
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
        private readonly IResourceService _resourceService;
        private readonly ICityRepository _cityRepo;
        private readonly IWorldPlayerService _worldPlayerService;
        private readonly IWorldPlayerRepository _worldPlayerRepo;
        private readonly IJobRepository _jobRepo;
        private readonly CityPointCalculator _cityPointCalculator;
        private readonly ILogger<JobService> _logger;

        public JobService(
            IResourceService resourceService,
            ICityRepository cityRepo,
            ILogger<JobService> logger,
            IWorldPlayerRepository userRepo,
            IJobRepository jobRepo, // Tilføjet til constructoren
            IWorldPlayerService worldPlayerService,
            CityPointCalculator cityPointCalculator)
        {
            _resourceService = resourceService;
            _cityRepo = cityRepo;
            _logger = logger;
            _worldPlayerRepo = userRepo;
            _jobRepo = jobRepo; // Mappet korrekt
            _worldPlayerService = worldPlayerService;
            _cityPointCalculator = cityPointCalculator;
        }

        public async Task ProcessAsync(BaseJob job)
        {
            _logger.LogInformation("[JobService] Starter processering af {JobType} med ID {JobId}", job.GetType().Name, job.Id);

            switch (job)
            {
                case BuildingJob buildingJob:
                    await ExecuteCityLinkedJobProcessing(buildingJob, buildingJob.CityId);
                    break;

                case RecruitmentJob recruitmentJob:
                    await ExecuteCityLinkedJobProcessing(recruitmentJob, recruitmentJob.CityId);
                    break;

                case ResearchJob researchJob:
                    await ExecuteGlobalResearchJobProcessing(researchJob);
                    break;

                default:
                    _logger.LogWarning("Uunderstøttet jobtype: {JobType}", job.GetType().Name);
                    break;
            }
        }

        /// <summary>
        /// Håndterer processeringen af jobs der kræver en by (Bygninger og Rekruttering).
        /// </summary>
        private async Task ExecuteCityLinkedJobProcessing(BaseJob job, Guid cityId)
        {
            var city = await _cityRepo.GetByIdAsync(cityId);
            if (city == null)
            {
                _logger.LogError("Job {JobId} fejlede. Byen {CityId} findes ikke.", job.Id, cityId);
                return;
            }

            SyncResourcesToJobCompletion(city, job.ExecutionTime);

            if (job is BuildingJob buildingJob)
            {
                HandleBuildingJob(city, buildingJob);
                job.IsCompleted = true; // Markeres som færdig her
            }
            else if (job is RecruitmentJob recruitmentJob)
            {
                HandleRecruitmentJob(city, recruitmentJob);
                // Status for RecruitmentJob sættes inde i HandleRecruitmentJob da det kan være delvist færdigt
            }

            // 1. Gem byens tilstand (inkl. de nye bygninger/units)
            await _cityRepo.UpdateAsync(city);

            // 2. VIGTIG FIX: Vi skal nu også gemme jobbets status i databasen
            await _jobRepo.UpdateAsync(job);

            _logger.LogInformation("[JobService] By-relateret job {JobId} er færdigbehandlet og gemt.", job.Id);
        }

        /// <summary>
        /// Håndterer processeringen af globale jobs knyttet til spilleren (Forskning).
        /// </summary>
        private async Task ExecuteGlobalResearchJobProcessing(ResearchJob activeResearchJob)
        {
            // Vi henter spilleren inklusiv deres nuværende research-liste
            var targetPlayer = await _worldPlayerRepo.GetByIdWithResearchAsync(activeResearchJob.WorldPlayerId);

            if (targetPlayer == null)
            {
                _logger.LogError("Kritisk fejl: ResearchJob {JobId} fejlede, da WorldPlayer {PlayerId} ikke blev fundet.", activeResearchJob.Id, activeResearchJob.WorldPlayerId);
                return;
            }

            CompleteResearchForPlayer(targetPlayer, activeResearchJob);

            // 1. Opdater spillerens CompletedResearches liste
            await _worldPlayerRepo.UpdateAsync(targetPlayer);

            // 2. FIX: Markér jobbet som færdigt i databasen så det ikke kører igen
            await _jobRepo.UpdateAsync(activeResearchJob);

            _logger.LogInformation("[JobService] Research {ResearchId} gemt for spiller {PlayerId}.", activeResearchJob.ResearchId, targetPlayer.Id);
        }

        private void SyncResourcesToJobCompletion(City city, DateTime executionTime)
        {
            var citySnapshot = _resourceService.CalculateCityResources(city, executionTime);

            city.Wood = citySnapshot.Wood;
            city.Stone = citySnapshot.Stone;
            city.Metal = citySnapshot.Metal;
            city.LastResourceUpdate = executionTime;

            if (city.WorldPlayer != null)
            {
                _worldPlayerService.UpdateGlobalResourceState(city.WorldPlayer, executionTime);
            }
        }

        private void CompleteResearchForPlayer(WorldPlayer player, ResearchJob finishedJob)
        {
            // FIX FOR 0000... GUID BUG:
            // Vi sikrer at WorldPlayerId bliver sat eksplicit på det nye objekt.
            var completedResearchEntry = new Research
            {
                WorldPlayerId = player.Id, // EF bruger nu denne til foreign key
                ResearchId = finishedJob.ResearchId,
                CompletedAt = DateTime.UtcNow
            };

            player.CompletedResearches.Add(completedResearchEntry);

            // Jobbet markeres som færdigt (skal gemmes via _jobRepo bagefter)
            finishedJob.IsCompleted = true;

            _logger.LogDebug("Research entry oprettet for {ResearchId} tilkoblet WorldPlayer {PlayerId}", finishedJob.ResearchId, player.Id);
        }

        private void HandleBuildingJob(City city, BuildingJob job)
        {
            var building = city.Buildings.FirstOrDefault(x => x.Type == job.BuildingType);

            if (building == null)
            {
                building = new Building { Type = job.BuildingType, Level = job.TargetLevel, CityId = city.Id };
                city.Buildings.Add(building);
            }
            else
            {
                building.Level = job.TargetLevel;
            }

            city.Points = _cityPointCalculator.CalculateTotalPointsForCity(city);
        }

        private void HandleRecruitmentJob(City city, RecruitmentJob job)
        {
            var now = DateTime.UtcNow;
            double secondsSinceLastTick = (now - job.LastTickTime).TotalSeconds + 0.01;

            int unitsToDeliver = (int)Math.Floor(secondsSinceLastTick / job.SecondsPerUnit);
            int remaining = job.TotalQuantity - job.CompletedQuantity;

            if (unitsToDeliver > remaining) unitsToDeliver = remaining;

            if (unitsToDeliver > 0)
            {
                var stack = city.UnitStacks.FirstOrDefault(x => x.Type == job.UnitType);
                if (stack == null)
                {
                    city.UnitStacks.Add(new UnitStack { Type = job.UnitType, Quantity = unitsToDeliver });
                }
                else
                {
                    stack.Quantity += unitsToDeliver;
                }

                job.CompletedQuantity += unitsToDeliver;
                job.LastTickTime = job.LastTickTime.AddSeconds(unitsToDeliver * job.SecondsPerUnit);
            }

            // Markér som færdig hvis vi er i mål
            if (job.CompletedQuantity >= job.TotalQuantity)
            {
                job.IsCompleted = true;
            }
            else
            {
                job.IsCompleted = false;
                job.ExecutionTime = job.LastTickTime.AddSeconds(job.SecondsPerUnit);
            }
        }
    }
}