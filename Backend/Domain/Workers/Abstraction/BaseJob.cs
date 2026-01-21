using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Workers.Abstraction
{
    public abstract class BaseJob : BaseEntity
    {
        public Guid UserId { get; set; }
        public DateTime ExecutionTime { get; set; }
        public bool IsCompleted { get; set; }
    }
}
