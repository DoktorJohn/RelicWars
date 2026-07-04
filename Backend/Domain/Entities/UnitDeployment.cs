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
    public class UnitDeployment : BaseEntity, IModifierProvider
    {
        public string Name { get; set; } = string.Empty;
        public double LootWood { get; set; }
        public double LootStone { get; set; }
        public double LootMetal { get; set; }
        public int Mobility { get; set; }
        public UnitDeploymentTypeEnum Type { get; set; }
        public UnitDeploymentMovementStatusEnum UnitDeploymentMovementStatus { get; set; }
        public UnitDeploymentPhaseEnum Phase { get; set; }
        public DateTime ArrivalTime { get; set; }  
        public DateTime DepartureTime { get; set; }
        public int LegStartX { get; set; }
        public int LegStartY { get; set; }
        public int LegEndX { get; set; }
        public int LegEndY { get; set; }
        public DateTime? StationedAt { get; set; }

        //Navprop
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();
        public List<UnitStack> UnitStacks { get; set; } = new();

        //FK
        public City? TargetCity { get; set; }
        public Guid? TargetCityId { get; set; }
        public required City OriginCity { get; set; }
        public Guid OriginCityId { get; set; }
        public WorldPlayer? OwnerWorldPlayer { get; set; }
        public Guid WorldPlayerId { get; set; }
        public World? World { get; set; }
        public Guid WorldId { get; set; }

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
