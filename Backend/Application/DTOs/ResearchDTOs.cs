using Domain.Enums;
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
        double CurrentResearchPoints
    );

    public record ResearchNodeDTO(
        string Id,
        string Name,
        string Description,
        ResearchTypeEnum ResearchType,
        string? ParentId,
        double ResearchPointCost,
        int ResearchTimeInSeconds,
        bool IsCompleted,
        bool IsLocked,
        bool CanAfford
    );

    public record ActiveResearchJobDTO(
        Guid JobId,
        string ResearchId,
        DateTime ExpectedCompletionTime,
        double ProgressPercentage
    );
}
