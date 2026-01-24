using Domain.Abstraction;
using Domain.Enums;
using Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class IdeologyFocus : BaseEntity
    {
        public DateTime? TimeOfIdeologyStarted { get; set; }
        public DateTime? TimeOfIdeologyFinished { get; set; }
        public bool IsActive => TimeOfIdeologyStarted <= DateTime.UtcNow &&
                        (!TimeOfIdeologyFinished.HasValue || TimeOfIdeologyFinished > DateTime.UtcNow);

        //FKs
        public Guid CityId { get; set; }
        public City? City { get; set; }
    }
}
