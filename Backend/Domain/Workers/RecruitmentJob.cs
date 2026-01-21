using Domain.Enums;
using Domain.Workers.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Workers
{
    public class RecruitmentJob : BaseJob
    {
        public UnitTypeEnum UnitType { get; set; }
        public Guid CityId { get; set; }
        public int TotalQuantity { get; set; }      // Hvor mange blev bestilt?
        public int CompletedQuantity { get; set; }  // Hvor mange er leveret?
        public double SecondsPerUnit { get; set; }  // Hvor lang tid tager én enhed?
        public DateTime LastTickTime { get; set; }  // Hvornår leverede vi sidst en enhed?
    }
}
