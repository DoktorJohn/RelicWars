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
    public class UnitDeployment : BaseEntity, IModifierProvider, IMapEntity
    {
        public string Name { get; set; } = string.Empty;
        public int CurrentX { get; set; }
        public int CurrentY { get; set; }
        public int NextX { get; set; }
        public int NextY { get; set; }
        public int FinalX { get; set; }
        public int FinalY { get; set; }

        public double LootWood { get; set; }
        public double LootStone { get; set; }
        public double LootMetal { get; set; }
        public int Mobility { get; set; }
        public UnitDeploymentMovementStatusEnum UnitDeploymentMovementStatus { get; set; }
        public DateTime ArrivalTime { get; set; }  
        public DateTime DepartureTime { get; set; }
        public DateTime LastStepTime { get; set; }
        public DateTime NextStepTime { get; set; }
        public string? RemainingPathJson { get; set; }

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

        Guid IMapEntity.Id => Id;
        int IMapEntity.X => CurrentX;
        int IMapEntity.Y => CurrentY;
        Guid IMapEntity.WorldId => WorldId;
        MapObjectTypeEnum IMapEntity.MapObjectType => MapObjectTypeEnum.UnitDeployment;

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
