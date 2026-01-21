using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
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
        private readonly IWorldPlayerRepository _userRepo;
        private readonly ILogger<JobService> _logger;

        public JobService(
            IResourceService resourceService,
            ICityRepository cityRepo,
            ILogger<JobService> logger,
            IWorldPlayerRepository userRepo)
        {
            _resourceService = resourceService;
            _cityRepo = cityRepo;
            _logger = logger;
            _userRepo = userRepo;
        }

        public async Task ProcessAsync(BaseJob job)
        {
            // Vi bruger Pattern Matching til at bestemme om jobbet er bundet til en by eller er globalt
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
                _logger.LogError("Job kunne ikke processeres. Byen {CityId} findes ikke.", cityId);
                return;
            }

            // FÆLLES LOGIK FOR BYER: Opdater ressourcer frem til jobbets eksekveringstid
            SyncResourcesToJobCompletion(city, job.ExecutionTime);

            if (job is BuildingJob buildingJob)
            {
                HandleBuildingJob(city, buildingJob);
                job.IsCompleted = true;
            }
            else if (job is RecruitmentJob recruitmentJob)
            {
                HandleRecruitmentJob(city, recruitmentJob);
            }

            // Gem ændringer for både by og WorldPlayer (pga. Silver-opdatering i Sync)
            await _cityRepo.UpdateAsync(city);
        }

        /// <summary>
        /// Håndterer processeringen af globale jobs knyttet til spilleren (Forskning).
        /// </summary>
        private async Task ExecuteGlobalResearchJobProcessing(ResearchJob researchJob)
        {
            var user = await _userRepo.GetByIdWithResearchAsync(researchJob.UserId);
            if (user == null)
            {
                _logger.LogError("Research kunne ikke færdiggøres. Bruger {UserId} ikke fundet.", researchJob.UserId);
                return;
            }

            // Vi delegerer selve research-logikken til hjælperen
            CompleteResearchForPlayer(user, researchJob);

            // Gemmer den globale spiller-tilstand
            await _userRepo.UpdateAsync(user);
        }

        private void SyncResourcesToJobCompletion(City city, DateTime executionTime)
        {
            var snapshot = _resourceService.CalculateCurrent(city, executionTime);

            // Opdater lokale lagerbeholdninger
            city.Wood = snapshot.Wood;
            city.Stone = snapshot.Stone;
            city.Metal = snapshot.Metal;

            // GLOBAL VALUTA: Opdater spillerens sølv og research points genereret af denne by
            if (city.WorldPlayer != null)
            {
                city.WorldPlayer.Silver += snapshot.SilverGeneratedByThisCity;
                city.WorldPlayer.ResearchPoints += snapshot.ResearchGeneratedByThisCity;

                _logger.LogInformation("Job Sync: Tilføjede {Silver:F2} sølv og {RP:F2} research points til {PlayerId} fra by {CityId}",
                    snapshot.SilverGeneratedByThisCity, snapshot.ResearchGeneratedByThisCity, city.WorldPlayerId, city.Id);
            }

            city.LastResourceUpdate = executionTime;
        }

        private void CompleteResearchForPlayer(WorldPlayer user, ResearchJob job)
        {
            user.CompletedResearches.Add(new Research
            {
                ResearchId = job.ResearchId,
                CompletedAt = DateTime.UtcNow
            });

            job.IsCompleted = true;
            _logger.LogInformation("Bruger {UserId} færdiggjort research: {ResearchId}", user.Id, job.ResearchId);
        }

        private void HandleBuildingJob(City city, BuildingJob job)
        {
            var building = city.Buildings.FirstOrDefault(x => x.Type == job.BuildingType);
            if (building == null)
            {
                city.Buildings.Add(new Building { Type = job.BuildingType, Level = job.TargetLevel });
            }
            else
            {
                building.Level = job.TargetLevel;
            }
            city.Points += 10;
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
                    city.UnitStacks.Add(new UnitStack
                    {
                        Type = job.UnitType,
                        Quantity = unitsToDeliver,
                        CityId = city.Id
                    });
                }
                else
                {
                    stack.Quantity += unitsToDeliver;
                }

                job.CompletedQuantity += unitsToDeliver;
                job.LastTickTime = job.LastTickTime.AddSeconds(unitsToDeliver * job.SecondsPerUnit);
            }

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