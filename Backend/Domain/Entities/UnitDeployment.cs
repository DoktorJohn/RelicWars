using Domain.Abstraction;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UnitDeployment : BaseEntity, IModifierProvider
    {
        public Guid OriginCityId { get; set; }  // Hvem ejer tropperne?
        public Guid TargetCityId { get; set; }  // Hvor er de på vej hen / hvor står de?

        public UnitTypeEnum UnitType { get; set; }
        public int Quantity { get; set; }
        public double LootWood { get; set; }
        public double LootStone { get; set; }
        public double LootMetal { get; set; }
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();

        public UnitDeploymentMovementStatusEnum UnitDeploymentMovementStatus { get; set; } // Enums: Moving, Stationed, Returning
        public UnitDeploymentTypeEnum UnitDeploymentType { get; set; }
        public DateTime ArrivalTime { get; set; }    // Hvornår når de frem?

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
