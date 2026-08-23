using Assets._Project.Scripts.Domain.Enums;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Scripts.Domain.DTOs
{
    [Serializable]
    public class ResearchTreeDTO
    {
        public List<ResearchNodeDTO> Nodes;
        public ActiveResearchJobDTO ActiveJob;
        public ResearchRateDTO ResearchRate;
        public DateTime ServerTimeUtc;
        public bool CanStartResearch;
        public List<string> UnmetRequirements;
    }

    [Serializable]
    public class ResearchRateDTO
    {
        public double BaseResearchPower;
        public double EffectiveResearchPower;
        public double SpeedMultiplier;
    }

    public enum ResearchPrerequisiteRule
    {
        Start,
        RequiresAll,
        RequiresAny
    }

    public enum ResearchNodeKind
    {
        Origin,
        Notable,
        Keystone
    }

    [Serializable]
    public class ResearchNodeDTO
    {
        public string Id;
        public string Name;
        public string Description;
        [JsonConverter(typeof(StringEnumConverter))]
        public ResearchTypeEnum ResearchType;
        public string ParentId;
        public List<string> PrerequisiteIds;
        [JsonConverter(typeof(StringEnumConverter))]
        public ResearchPrerequisiteRule PrerequisiteRule;
        public int Tier;
        [JsonConverter(typeof(StringEnumConverter))]
        public ResearchNodeKind NodeKind;
        public bool IsResearchable;
        public int ResearchTimeInSeconds;
        public bool IsCompleted;
        public bool IsResearching;
        public bool IsLocked;
        public bool CanStart;
    }

    [Serializable]
    public class ActiveResearchJobDTO
    {
        public Guid JobId;
        public string ResearchId;
        public DateTime? ExpectedCompletionTime;
        public double ProgressPercentage;
    }
}
