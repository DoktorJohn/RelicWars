using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Enums;
using Domain.StaticData.Readers;
using Domain.User;
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

            // Hent alle jobs ÉN gang uden for loopet for at undgå N+1
            var activeJob = await _jobRepo.GetResearchJobAsync(userId);
            var allCurrentJobs = await _jobRepo.GetResearchJobsByIdAsync(userId);

            var allStaticNodes = _researchReader.GetAll();
            var completedIds = user.CompletedResearches.Select(r => r.ResearchId).ToHashSet();
            var researchingIds = allCurrentJobs
                .Where(r => r.ExecutionTime > DateTime.UtcNow)
                .Select(r => r.ResearchId)
                .ToHashSet();

            var nodeDtos = allStaticNodes.Select(staticNode =>
            {
                bool isCompleted = completedIds.Contains(staticNode.Id);
                bool isResearching = researchingIds.Contains(staticNode.Id);

                bool parentIsCompleted = string.IsNullOrEmpty(staticNode.ParentId) ||
                                         completedIds.Contains(staticNode.ParentId);

                return new ResearchNodeDTO(
                    staticNode.Id,
                    staticNode.Name,
                    staticNode.Description,
                    staticNode.ResearchType,
                    staticNode.ParentId,
                    staticNode.ResearchPointCost,
                    staticNode.ResearchTimeInSeconds,
                    isCompleted,
                    isResearching,
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
                    0
                );
            }

            return new ResearchTreeDTO(nodeDtos, activeJobDto, user.ResearchPoints);
        }

        public async Task<BuildingResult> QueueResearchAsync(Guid worldPlayerId, string researchId)
        {
            var worldPlayer = await _userRepo.GetByIdWithResearchAsync(worldPlayerId);
            var researchNode = _researchReader.GetNode(researchId);

            if (worldPlayer == null)
            {
                return new BuildingResult(false, "Spilleren blev ikke fundet i systemet.");
            }

            // Tjek om teknologien allerede er udforsket
            if (worldPlayer.CompletedResearches.Any(research => research.ResearchId == researchId))
            {
                return new BuildingResult(false, "Denne teknologi er allerede færdiggjort.");
            }

            // Tjek forudsætninger (Parent teknologi)
            if (!string.IsNullOrEmpty(researchNode.ParentId))
            {
                bool hasRequiredParent = worldPlayer.CompletedResearches.Any(research => research.ResearchId == researchNode.ParentId);
                if (!hasRequiredParent)
                {
                    return new BuildingResult(false, $"Du skal udforske {researchNode.ParentId} før du kan påbegynde {researchNode.Name}.");
                }
            }

            // Tjek om der allerede kører et forsknings-job
            var existingResearchJob = await _jobRepo.GetResearchJobAsync(worldPlayerId);
            if (existingResearchJob != null)
            {
                return new BuildingResult(false, "Laboratoriet er optaget. Du kan kun forske i én teknologi ad gangen.");
            }

            // Tjek økonomi
            if (worldPlayer.ResearchPoints < researchNode.ResearchPointCost)
            {
                return new BuildingResult(false, $"Utilstrækkelige forskningspoint. Mangler: {researchNode.ResearchPointCost - worldPlayer.ResearchPoints}");
            }

            // Foretag betaling
            worldPlayer.ResearchPoints -= researchNode.ResearchPointCost;

            // Opret selve jobbet
            var newResearchJob = new ResearchJob
            {
                WorldPlayerId = worldPlayerId,
                ResearchId = researchId,
                ExecutionTime = DateTime.UtcNow.AddSeconds(researchNode.ResearchTimeInSeconds),
                IsCompleted = false
            };

            // Gem begge dele. Ideelt set bør dette ligge i en Unit of Work / Transaction.
            await _userRepo.UpdateAsync(worldPlayer);
            await _jobRepo.AddAsync(newResearchJob);

            return new BuildingResult(true, $"Forskningen af {researchNode.Name} er nu sat i gang.");
        }

        public async Task<BuildingResult> CancelResearchAsync(Guid userId, Guid jobId)
        {
            var job = await _jobRepo.GetByIdAsync(jobId) as ResearchJob;
            if (job == null || job.WorldPlayerId != userId) return new BuildingResult(false, "Job ikke fundet.");

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