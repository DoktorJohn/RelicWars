using Domain.Enums;

namespace Application.DTOs
{
    public sealed record DailyObjectivesDTO(
        DateTime DayStartUtc,
        DateTime ResetAtUtc,
        List<DailyObjectiveRowDTO> Rows);

    public sealed record DailyObjectiveRowDTO(
        int Slot,
        int DefinitionId,
        string Name,
        string CompletionInfo,
        DailyObjectiveTierEnum RewardTier,
        double Progress,
        double Target,
        DailyObjectiveStateEnum State);
}
