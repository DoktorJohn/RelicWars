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
        public double CurrentResearchPoints;
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
        public bool IsLocked;
        public bool CanAfford;
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
