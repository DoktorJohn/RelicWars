using Application.DTOs;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.StaticData.Readers;
using Domain.Workers;

namespace Application.Services;

public class ResearchService : IResearchService
{
    private const string UniversityRequirementMessage = "Build a University in one of your cities to begin research.";

    private readonly IJobRepository _jobRepository;
    private readonly IPlayerAccessService _playerAccessService;
    private readonly ResearchDataReader _researchReader;
    private readonly ResearchPrerequisiteEvaluator _prerequisiteEvaluator;
    private readonly IResearchRateCalculator _rateCalculator;
    private readonly ResearchProgressService _progressService;
    private readonly TimeProvider _timeProvider;

    public ResearchService(
        IJobRepository jobRepository,
        IPlayerAccessService playerAccessService,
        ResearchDataReader researchReader,
        ResearchPrerequisiteEvaluator prerequisiteEvaluator,
        IResearchRateCalculator rateCalculator,
        ResearchProgressService progressService,
        TimeProvider timeProvider)
    {
        _jobRepository = jobRepository;
        _playerAccessService = playerAccessService;
        _researchReader = researchReader;
        _prerequisiteEvaluator = prerequisiteEvaluator;
        _rateCalculator = rateCalculator;
        _progressService = progressService;
        _timeProvider = timeProvider;
    }

    public async Task<ResearchTreeDTO> GetResearchTreeAsync(Guid worldPlayerId)
    {
        var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
        DateTime now = UtcNow();
        var activeJob = await _jobRepository.GetResearchJobAsync(worldPlayerId);
        var completedIds = player.CompletedResearches.Select(research => research.ResearchId).ToHashSet();
        var rate = _rateCalculator.Calculate(player, now);
        bool hasResearchCapacity = rate.SpeedMultiplier > 0d;
        bool hasActiveJob = activeJob != null;

        var nodes = _researchReader.GetAll().Select(node =>
        {
            bool isCompleted = completedIds.Contains(node.Id);
            bool isResearching = activeJob?.ResearchId == node.Id;
            bool prerequisitesSatisfied = _prerequisiteEvaluator.AreSatisfied(node, completedIds);
            bool canStart = node.IsResearchable &&
                            !isCompleted &&
                            !isResearching &&
                            prerequisitesSatisfied &&
                            hasResearchCapacity &&
                            !hasActiveJob;
            return new ResearchNodeDTO(
                node.Id,
                node.Name,
                node.Description,
                node.ResearchType,
                node.ParentId,
                node.PrerequisiteIds,
                node.PrerequisiteRule,
                node.Tier,
                node.NodeKind,
                node.IsResearchable,
                node.ResearchTimeInSeconds,
                isCompleted,
                isResearching,
                !prerequisitesSatisfied,
                canStart);
        }).ToList();

        ActiveResearchJobDTO? activeJobDto = null;
        if (activeJob != null)
        {
            var progress = _progressService.Project(activeJob, player, now);
            activeJobDto = new ActiveResearchJobDTO(
                activeJob.Id,
                activeJob.ResearchId,
                progress.ExpectedCompletionTime,
                progress.ProgressPercentage);
        }

        var unmetRequirements = GetUnmetResearchRequirements(rate);
        return new ResearchTreeDTO(
            nodes,
            activeJobDto,
            ToDto(rate),
            now,
            unmetRequirements.Count == 0,
            unmetRequirements);
    }

    public async Task<BuildingResult> QueueResearchAsync(Guid worldPlayerId, string researchId)
    {
        var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
        var research = _researchReader.GetNode(researchId);

        if (!research.IsResearchable)
        {
            return new BuildingResult(false, "This research is currently display-only.");
        }

        if (player.CompletedResearches.Any(completed => completed.ResearchId == researchId))
        {
            return new BuildingResult(false, "Denne teknologi er allerede færdiggjort.");
        }

        var completedIds = player.CompletedResearches
            .Select(completed => completed.ResearchId)
            .ToHashSet();
        if (!_prerequisiteEvaluator.AreSatisfied(research, completedIds))
        {
            return new BuildingResult(false, $"Forudsætningerne for {research.Name} er ikke opfyldt.");
        }

        DateTime now = UtcNow();
        var rate = _rateCalculator.Calculate(player, now);
        var unmetRequirements = GetUnmetResearchRequirements(rate);
        if (unmetRequirements.Count > 0)
        {
            return new BuildingResult(false, unmetRequirements[0]);
        }

        if (await _jobRepository.GetResearchJobAsync(worldPlayerId) != null)
        {
            return new BuildingResult(false, "Laboratoriet er optaget. Du kan kun forske i én teknologi ad gangen.");
        }

        var job = new ResearchJob
        {
            WorldPlayerId = worldPlayerId,
            ResearchId = researchId
        };
        _progressService.Initialize(job, player, research.ResearchTimeInSeconds, now);
        await _jobRepository.AddAsync(job);

        return new BuildingResult(true, $"Forskningen af {research.Name} er nu sat i gang.");
    }

    public async Task<BuildingResult> CancelResearchAsync(Guid worldPlayerId, Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId) as ResearchJob;
        if (job == null || job.WorldPlayerId != worldPlayerId)
        {
            return new BuildingResult(false, "Job ikke fundet.");
        }

        await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
        await _jobRepository.DeleteAsync(jobId);
        return new BuildingResult(true, "Forskning annulleret.");
    }

    public async Task<List<Modifier>> GetUserResearchModifiersAsync(Guid worldPlayerId)
    {
        var player = await _playerAccessService.RequireOwnedWorldPlayerAsync(worldPlayerId);
        return player.CompletedResearches
            .Select(completed => _researchReader.GetNode(completed.ResearchId))
            .SelectMany(node => node.ModifiersInternal)
            .ToList();
    }

    private static List<string> GetUnmetResearchRequirements(ResearchRateSnapshot rate) =>
        rate.SpeedMultiplier > 0d ? [] : [UniversityRequirementMessage];

    private static ResearchRateDTO ToDto(ResearchRateSnapshot rate) => new(
        rate.BaseResearchPower,
        rate.EffectiveResearchPower,
        rate.SpeedMultiplier);

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
