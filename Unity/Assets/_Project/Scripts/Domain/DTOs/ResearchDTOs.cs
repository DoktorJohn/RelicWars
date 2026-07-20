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
    public enum ResearchEffectType
    {
        UnitRecruitment,
        Subjugation
    }

    [Serializable]
    public class ResearchEffectDTO
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ResearchEffectType Type;
        [JsonConverter(typeof(StringEnumConverter))]
        public Assets.Scripts.Domain.Enums.UnitTypeEnum? UnitType;
    }

    [Serializable]
    public class ResearchTreeDTO
    {
        public List<ResearchNodeDTO> Nodes;
        public ActiveResearchJobDTO ActiveJob;
        public double CurrentResearchPoints;
        public bool CanStartResearch;
        public List<string> UnmetRequirements;
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
        public double ResearchPointCost;
        public int ResearchTimeInSeconds;
        public bool IsCompleted;
        public bool IsResearching;
        public bool IsLocked;
        public bool CanAfford;
        public List<ResearchEffectDTO> Effects;
    }

    [Serializable]
    public class ActiveResearchJobDTO
    {
        public Guid JobId;
        public string ResearchId;
        public DateTime ExpectedCompletionTime;
        public double ProgressPercentage;
    }
}
