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
    public class UnitStack : BaseEntity
    {
        public UnitTypeEnum Type { get; set; }
        public int Quantity { get; set; }
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();

        //FK
        public Guid? CityId { get; set; }
        public City? City { get; set; }
        public Guid? UnitDeploymentId { get; set; }
        public UnitDeployment? UnitDeployment { get; set; }
        public Guid? WorldPlayerId { get; set; }
        public WorldPlayer? WorldPlayer { get; set; }

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
