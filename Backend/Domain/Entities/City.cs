using Domain.Abstraction;
using Domain.Enums;
using Domain.User;
using Domain.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class City : BaseEntity, IModifierProvider
    {
        public string Name { get; set; } = string.Empty;
        public int Points { get; set; } = 100;

        //Ressourcer
        public double Wood { get; set; }
        public double Stone { get; set; }
        public double Metal { get; set; }
        public int Population { get; set; }

        public DateTime LastResourceUpdate { get; set; } = DateTime.UtcNow;

        public bool IsNPC { get; set; }

        //Tile placering
        public int X { get; set; }
        public int Y { get; set; }

        //Navprops
        public List<Building> Buildings { get; set; } = new();
        public List<BuildingJob> BuildingQueue { get; set; } = new();
        public List<UnitStack> UnitStacks { get; set; } = new();
        public List<UnitDeployment> OriginUnitDeployments { get; set; } = new();
        public List<UnitDeployment> TargetUnitDeployments { get; set; } = new();
        public List<IdeologyFocus> ActiveFocuses { get; set; } = new();
        public List<Modifier> ModifiersInternal { get; set; } = new();
        public List<ModifierTagEnum> ModifiersThatAffectsThis { get; set; } = new();

        //Foreign key
        public Guid? WorldPlayerId { get; set; }
        public WorldPlayer? WorldPlayer { get; set; }
        public Guid? WorldId { get; set; }
        public World? World { get; set; }

        public IEnumerable<Modifier> GetModifiers()
        {
            return ModifiersInternal;
        }
    }
}
