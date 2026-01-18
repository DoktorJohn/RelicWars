using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Workers;
using Domain.Workers.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class JobService : IJobService
    {
        private readonly IResourceService _resourceService;
        private readonly ICityRepository _cityRepo;
        private readonly IWorldPlayerRepository _userRepo;
        private readonly ILogger<JobService> _logger;

        public JobService(IResourceService resourceService, ICityRepository cityRepo, ILogger<JobService> logger, IWorldPlayerRepository userRepo)
        {
            _resourceService = resourceService;
            _cityRepo = cityRepo;
            _logger = logger;
            _userRepo = userRepo;
        }

        public async Task ProcessAsync(BaseJob job)
        {
            var city = await _cityRepo.GetByIdAsync(job.CityId);
            if (city == null) return;

            // FÆLLES LOGIK: Opdater ressourcer frem til jobbets eksekveringstid
            SyncResourcesToJobCompletion(city, job.ExecutionTime);

            // DELEGERING
            switch (job)
            {
                case BuildingJob bJob:
                    HandleBuildingJob(city, bJob);
                    // Bygninger færdiggøres med det samme
                    job.IsCompleted = true;
                    break;

                case RecruitmentJob rJob:
                    // RecruitmentJob styrer selv sin IsCompleted status inde i metoden!
                    HandleRecruitmentJob(city, rJob);
                    break;

                case ResearchJob resJob:
                    await HandleResearchJob(resJob);
                    break;
            }

            await _cityRepo.UpdateAsync(city);

        }

        private void SyncResourcesToJobCompletion(City city, DateTime executionTime)
        {
            var snapshot = _resourceService.CalculateCurrent(city, executionTime);
            city.Wood = snapshot.Wood;
            city.Stone = snapshot.Stone;
            city.Metal = snapshot.Metal;
            city.LastResourceUpdate = executionTime;
        }

        private async Task HandleResearchJob(ResearchJob job)
        {
            // 1. Find brugeren
            var user = await _userRepo.GetByIdWithResearchAsync(job.UserId);

            // 2. Registrer den færdige research
            user.CompletedResearches.Add(new Research
            {
                ResearchId = job.ResearchId,
                CompletedAt = DateTime.UtcNow
            });

            // 3. Gem brugeren
            await _userRepo.UpdateAsync(user);

            job.IsCompleted = true; // Markér jobbet færdigt til sletning
            _logger.LogInformation($"Bruger {user.PlayerProfile.UserName} har færdiggjort research: {job.ResearchId}");
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
            city.Points += 10; // Eller en mere kompleks beregning
        }

        private void HandleRecruitmentJob(City city, RecruitmentJob job)
        {
            var now = DateTime.UtcNow;
            // Tilføj lille epsilon for at undgå afrundingsfejl
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

                _logger.LogInformation($"Leverede {unitsToDeliver}x {job.UnitType}. ({job.CompletedQuantity}/{job.TotalQuantity})");
            }

            // VIGTIG STATUS-LOGIK
            if (job.CompletedQuantity >= job.TotalQuantity)
            {
                job.IsCompleted = true;
            }
            else
            {
                job.IsCompleted = false;
                // Planlæg næste "tick" i databasen
                job.ExecutionTime = job.LastTickTime.AddSeconds(job.SecondsPerUnit);
            }
        }

        //private void HandleResearchJob(City city, ResearchJob job)
        //{
        //    // Implementer University research logik her...
        //}
    }
}
