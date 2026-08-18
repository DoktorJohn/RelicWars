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

    public enum DailyObjectiveRewardType
    {
        Coins,
        Wood,
        Stone,
        Metal
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
        public List<DailyObjectiveRewardDTO> Rewards = new();
        public double Progress;
        public double Target;
        public bool IsCompleted;
        public bool IsCollected;
        public DailyObjectiveState State;
    }


    [Serializable]
    public class DailyObjectiveRewardDTO
    {
        public DailyObjectiveRewardType Type;
        public int Amount;
    }
}
