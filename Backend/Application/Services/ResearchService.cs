using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Enums;
using Domain.StaticData.Data;
using Domain.StaticData.Readers;
using Domain.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ResearchService : IResearchService
    {
        private readonly ICityRepository _cityRepo;
        private readonly IJobRepository _jobRepo;
        private readonly IWorldPlayerRepository _userRepo; // Vi skal bruge en repo til at tjekke UserResearch
        private readonly IResourceService _resService;
        private readonly ResearchDataReader _researchReader;

        public ResearchService(
            ICityRepository cityRepo,
            IJobRepository jobRepo,
            IWorldPlayerRepository userRepo,
            IResourceService resService,
            ResearchDataReader researchReader)
        {
            _cityRepo = cityRepo;
            _jobRepo = jobRepo;
            _userRepo = userRepo;
            _resService = resService;
            _researchReader = researchReader;
        }

        public async Task<BuildingResult> QueueResearchAsync(Guid userId, Guid cityId, string researchId)
        {
            // 1. Hent data
            var user = await _userRepo.GetByIdWithResearchAsync(userId);
            var city = await _cityRepo.GetByIdAsync(cityId);
            var node = _researchReader.GetNode(researchId);

            if (user == null || city == null) return new BuildingResult(false, "Bruger eller by ikke fundet.");

            // 2. VALIDERING: Er den allerede færdig?
            if (user.CompletedResearches.Any(r => r.ResearchId == researchId))
                return new BuildingResult(false, "Denne teknologi er allerede udforsket på din konto.");

            // 3. VALIDERING: Chaining (Prerequisites i træet)
            if (!string.IsNullOrEmpty(node.ParentId))
            {
                if (!user.CompletedResearches.Any(r => r.ResearchId == node.ParentId))
                    return new BuildingResult(false, "Du skal udforske den forrige node i træet først.");
            }

            // 4. VALIDERING: Er der allerede en aktiv research i gang på denne konto?
            // Vi tillader kun én global research af gangen for at undgå exploits.
            var globalResearchJob = await _jobRepo.GetActiveResearchJobForUserAsync(userId);
            if (globalResearchJob != null)
                return new BuildingResult(false, "Du er allerede i gang med at forske et andet sted.");

            // 5. VALIDERING: Academy Level i den valgte by
            var academy = city.Buildings.FirstOrDefault(b => b.Type == BuildingTypeEnum.Academy);
            if (academy == null || academy.Level < node.RequiredAcademyLevel)
                return new BuildingResult(false, $"Byens Academy skal være level {node.RequiredAcademyLevel}.");

            // 6. VALIDERING: Ressourcer
            var now = DateTime.UtcNow;
            var snapshot = _resService.CalculateCurrent(city, now);

            if (snapshot.Wood < node.WoodCost || snapshot.Stone < node.StoneCost || snapshot.Metal < node.MetalCost)
                return new BuildingResult(false, "Ikke nok ressourcer i denne by.");

            // 7. EKSEKVERING: Træk ressourcer og opret job
            city.Wood = snapshot.Wood - node.WoodCost;
            city.Stone = snapshot.Stone - node.StoneCost;
            city.Metal = snapshot.Metal - node.MetalCost;
            city.LastResourceUpdate = now;

            var job = new ResearchJob
            {
                UserId = userId,
                CityId = cityId,
                ResearchId = researchId,
                ExecutionTime = now.AddSeconds(node.ResearchTimeInSeconds),
                IsCompleted = false
            };

            await _cityRepo.UpdateAsync(city);
            await _jobRepo.AddAsync(job);

            return new BuildingResult(true, $"Forskning af {node.Name} er startet!");
        }

        public async Task<List<ModifierData>> GetUserResearchModifiersAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdWithResearchAsync(userId);
            if (user == null) return new List<ModifierData>();

            // Samler alle modifiers fra alle de noder, brugeren har låst op
            return user.CompletedResearches
                .Select(ur => _researchReader.GetNode(ur.ResearchId))
                .SelectMany(node => node.Modifiers)
                .ToList();
        }

        public async Task<BuildingResult> CancelResearchAsync(Guid userId, Guid jobId)
        {
            var job = await _jobRepo.GetByIdAsync(jobId) as ResearchJob;
            if (job == null || job.UserId != userId) return new BuildingResult(false, "Job ikke fundet.");

            // Her kunne man give 50% ressourcer tilbage til byen, hvis man ville
            await _jobRepo.DeleteAsync(jobId);
            return new BuildingResult(true, "Forskning annulleret.");
        }
    }
}
