using Domain.Enums;
using Domain.StaticData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record ResearchTreeDTO(
        List<ResearchNodeDTO> Nodes,
        ActiveResearchJobDTO? ActiveJob,
        ResearchRateDTO ResearchRate,
        DateTime ServerTimeUtc,
        bool CanStartResearch,
        List<string> UnmetRequirements
    );

    public record ResearchRateDTO(
        double BaseResearchPower,
        double EffectiveResearchPower,
        double SpeedMultiplier
    );

    public record ResearchNodeDTO(
        string Id,
        string Name,
        string Description,
        ResearchTypeEnum ResearchType,
        string? ParentId,
        List<string> PrerequisiteIds,
        ResearchPrerequisiteRule PrerequisiteRule,
        int Tier,
        ResearchNodeKind NodeKind,
        bool IsResearchable,
        int ResearchTimeInSeconds,
        bool IsCompleted,
        bool IsResearching,
        bool IsLocked,
        bool CanStart
    );

    public record ActiveResearchJobDTO(
        Guid JobId,
        string ResearchId,
        DateTime? ExpectedCompletionTime,
        double ProgressPercentage
    );
}
