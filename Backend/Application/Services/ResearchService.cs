using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ResearchService : IResearchService
    {
        private readonly IJobRepository _jobRepo;
        private readonly IWorldPlayerRepository _userRepo;
        private readonly ResearchDataReader _researchReader;

        public ResearchService(
            IJobRepository jobRepo,
            IWorldPlayerRepository userRepo,
            ResearchDataReader researchReader)
        {
            _jobRepo = jobRepo;
            _userRepo = userRepo;
            _researchReader = researchReader;
        }

        public async Task<ResearchTreeDTO> GetResearchTreeAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdWithResearchAsync(userId);
            if (user == null) throw new Exception("Bruger ikke fundet.");

            var activeJob = await _jobRepo.GetResearchJobAsync(userId);
            var allStaticNodes = _researchReader.GetAll();

            var nodeDtos = allStaticNodes.Select(staticNode =>
            {
                bool isCompleted = user.CompletedResearches.Any(r => r.ResearchId == staticNode.Id);

                // En node er låst hvis den har en forælder, som endnu ikke er udforsket
                bool parentIsCompleted = string.IsNullOrEmpty(staticNode.ParentId) ||
                                         user.CompletedResearches.Any(r => r.ResearchId == staticNode.ParentId);

                return new ResearchNodeDTO(
                    staticNode.Id,
                    staticNode.Name,
                    staticNode.Description,
                    staticNode.ResearchType, // Mapped direkte fra static data
                    staticNode.ParentId,
                    staticNode.ResearchPointCost,
                    staticNode.ResearchTimeInSeconds,
                    isCompleted,
                    !parentIsCompleted, // IsLocked
                    user.ResearchPoints >= staticNode.ResearchPointCost // CanAfford
                );
            }).ToList();

            ActiveResearchJobDTO? activeJobDto = null;
            if (activeJob != null)
            {
                activeJobDto = new ActiveResearchJobDTO(
                    activeJob.Id,
                    activeJob.ResearchId,
                    activeJob.ExecutionTime,
                    0 // Progress beregnes i Unity eller her hvis starttid haves
                );
            }

            return new ResearchTreeDTO(nodeDtos, activeJobDto, user.ResearchPoints);
        }

        public async Task<BuildingResult> QueueResearchAsync(Guid userId, string researchId)
        {
            var user = await _userRepo.GetByIdWithResearchAsync(userId);
            var node = _researchReader.GetNode(researchId);

            if (user == null) return new BuildingResult(false, "Bruger ikke fundet.");

            if (user.CompletedResearches.Any(r => r.ResearchId == researchId))
                return new BuildingResult(false, "Denne teknologi er allerede udforsket.");

            if (!string.IsNullOrEmpty(node.ParentId))
            {
                if (!user.CompletedResearches.Any(r => r.ResearchId == node.ParentId))
                    return new BuildingResult(false, "Du skal udforske den forrige teknologi i træet først.");
            }

            var globalResearchJob = await _jobRepo.GetResearchJobAsync(userId);
            if (globalResearchJob != null)
                return new BuildingResult(false, "Du er allerede i gang med at forske.");

            if (user.ResearchPoints < node.ResearchPointCost)
                return new BuildingResult(false, $"Mangler Research Points. Kræver: {node.ResearchPointCost}");

            user.ResearchPoints -= node.ResearchPointCost;

            var job = new ResearchJob
            {
                UserId = userId,
                ResearchId = researchId,
                ExecutionTime = DateTime.UtcNow.AddSeconds(node.ResearchTimeInSeconds),
                IsCompleted = false
            };

            await _userRepo.UpdateAsync(user);
            await _jobRepo.AddAsync(job);

            return new BuildingResult(true, $"Forskning af {node.Name} er påbegyndt.");
        }

        public async Task<BuildingResult> CancelResearchAsync(Guid userId, Guid jobId)
        {
            var job = await _jobRepo.GetByIdAsync(jobId) as ResearchJob;
            if (job == null || job.UserId != userId) return new BuildingResult(false, "Job ikke fundet.");

            var user = await _userRepo.GetByIdAsync(userId);
            var node = _researchReader.GetNode(job.ResearchId);

            user.ResearchPoints += node.ResearchPointCost;

            await _userRepo.UpdateAsync(user);
            await _jobRepo.DeleteAsync(jobId);

            return new BuildingResult(true, "Forskning annulleret og point refunderet.");
        }

        public async Task<List<Modifier>> GetUserResearchModifiersAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdWithResearchAsync(userId);
            if (user == null) return new List<Modifier>();

            return user.CompletedResearches
                .Select(ur => _researchReader.GetNode(ur.ResearchId))
                .SelectMany(node => node.ModifiersInternal)
                .ToList();
        }
    }
}