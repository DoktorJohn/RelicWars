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
        List<DailyObjectiveRewardDTO> Rewards,
        double Progress,
        double Target,
        bool IsCompleted,
        bool IsCollected,
        DailyObjectiveStateEnum State);

    public sealed record DailyObjectiveRewardDTO(
        DailyObjectiveRewardTypeEnum Type,
        int Amount);
}
