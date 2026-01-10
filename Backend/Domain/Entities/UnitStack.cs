using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UnitStack : BaseEntity
    {
        public UnitTypeEnum Type { get; set; }
        public int Quantity { get; set; }
        public Guid CityId { get; set; }
    }
}
