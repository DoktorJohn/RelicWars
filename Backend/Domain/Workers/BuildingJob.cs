using Domain.Enums;
using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Workers
{
    public class BuildingJob : BaseJob
    {
        public BuildingTypeEnum BuildingType { get; set; }
        public int TargetLevel { get; set; }
    }
}
