using Domain.StaticData.Data;

namespace Application.Services;

public sealed class ResearchPrerequisiteEvaluator
{
    public bool AreSatisfied(ResearchData research, IReadOnlySet<string> completedResearchIds)
    {
        ArgumentNullException.ThrowIfNull(research);
        ArgumentNullException.ThrowIfNull(completedResearchIds);

        IReadOnlyList<string> prerequisiteIds = research.PrerequisiteIds ?? [];
        return research.PrerequisiteRule switch
        {
            ResearchPrerequisiteRule.Start => prerequisiteIds.Count == 0,
            ResearchPrerequisiteRule.RequiresAll =>
                prerequisiteIds.Count > 0 && prerequisiteIds.All(completedResearchIds.Contains),
            ResearchPrerequisiteRule.RequiresAny =>
                prerequisiteIds.Count > 0 && prerequisiteIds.Any(completedResearchIds.Contains),
            _ => false
        };
    }
}
