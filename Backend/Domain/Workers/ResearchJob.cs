using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Workers
{
    public class ResearchJob : BaseJob
    {
        public string ResearchId { get; set; } = string.Empty;
        public double TotalWorkSeconds { get; set; }
        public double RemainingWorkSeconds { get; set; }
        public DateTime LastProgressAt { get; set; }
        public double AppliedSpeedMultiplier { get; set; }
    }
}
