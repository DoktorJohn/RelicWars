using System;
using System.Collections.Generic;

namespace Project.Scripts.Domain.DTOs
{
    public enum DailyObjectiveTier
    {
        Fixed,
        Uncommon,
        Rare,
        Unique
    }

    public enum DailyObjectiveState
    {
        InProgress,
        Complete,
        ComingSoon
    }

    [Serializable]
    public class DailyObjectivesDTO
    {
        public DateTime DayStartUtc;
        public DateTime ResetAtUtc;
        public List<DailyObjectiveRowDTO> Rows = new();
    }

    [Serializable]
    public class DailyObjectiveRowDTO
    {
        public int Slot;
        public int DefinitionId;
        public string Name;
        public string CompletionInfo;
        public DailyObjectiveTier RewardTier;
        public double Progress;
        public double Target;
        public DailyObjectiveState State;
    }
}
