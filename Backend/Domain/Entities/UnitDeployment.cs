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

        //FK
        public required City OriginCity { get; set; }
        public Guid OriginCityId { get; set; }
        public City? TargetCity { get; set; }
        public Guid? TargetCityId { get; set; }

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
